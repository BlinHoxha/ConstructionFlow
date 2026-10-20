using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AIFramework.Application;
using AIFramework.Azure;
using AIFramework.Core.Abstractions;
using AIFramework.Local;
using ConstructionFlow.Api;
using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Framework.Persistence.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddJsonConsole();
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 512 * 1024);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<IRequestContext>(p => p.GetRequiredService<RequestContext>());
builder.Services.AddScoped<IAuthenticatedRequestContext>(p => p.GetRequiredService<RequestContext>());
builder.Services.AddScoped<IConstructionStore, ConstructionStore>();
builder.Services.AddScoped<AccessGuard>();
builder.Services.AddScoped<ProjectWorkflow>();
builder.Services.AddScoped<ProcurementWorkflow>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SiteReportWorkflow>();
builder.Services.AddScoped<KnowledgeWorkflow>();
var connection = builder.Configuration.GetConnectionString("Construction")
    ?? throw new InvalidOperationException("Set ConnectionStrings__Construction using secrets or environment configuration.");
builder.Services.AddFrameworkPostgreSql<ConstructionDbContext>(connection);
var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
var developmentKey = builder.Configuration["Authentication:DevelopmentSigningKey"];
if (!builder.Environment.IsDevelopment() && (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience)))
    throw new InvalidOperationException("Production requires OIDC authority and audience.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    if (!string.IsNullOrWhiteSpace(authority))
    {
        o.Authority = authority; o.Audience = audience;
    }
    else if (builder.Environment.IsDevelopment() && developmentKey is { Length: >= 32 })
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "constructionflow-development",
            ValidateAudience = true, ValidAudience = "constructionflow-api",
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(developmentKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    }
    // No anonymous fallback: unconfigured Development hosts reject business requests.
});
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = c => c.ProblemDetails.Extensions["traceId"] = c.HttpContext.TraceIdentifier);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("ai", http => RateLimitPartition.GetFixedWindowLimiter(
        (http.User.FindFirst("tenant_id")?.Value ?? "") + ":" + (http.User.FindFirst("sub")?.Value ?? ""),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddAiApplication();
builder.Services.AddSingleton<IDocumentProfileCatalog, ConstructionProfiles>();
switch (builder.Configuration["AI:Provider"] ?? "Local")
{
    case "Local" when builder.Environment.IsDevelopment(): builder.Services.AddLocalAi(); break;
    case "Azure":
        builder.Services.AddAzureModels(new AzureModelOptions(
            new Uri(builder.Configuration["AI:Azure:OpenAiEndpoint"] ?? ""),
            builder.Configuration["AI:Azure:ChatDeployment"] ?? "",
            builder.Configuration["AI:Azure:EmbeddingDeployment"] ?? ""));
        break;
    default: throw new InvalidOperationException("Select Azure for production or Local for Development.");
}
// Replace ephemeral/local index with application-owned, durable, tenant/project-scoped evidence storage.
builder.Services.AddScoped<ProjectKnowledgeStore>();
builder.Services.AddScoped<IKnowledgeIndex>(p => p.GetRequiredService<ProjectKnowledgeStore>());
builder.Services.AddScoped<IKnowledgeRetriever>(p => p.GetRequiredService<ProjectKnowledgeStore>());
var app = builder.Build();
if (args.Contains("--seed-development"))
{
    if (!app.Environment.IsDevelopment()) throw new InvalidOperationException("Synthetic seeding is Development-only.");
    await DevelopmentSeed.Run(app.Services, app.Configuration);
    return;
}
app.UseExceptionHandler(handler => handler.Run(async http =>
{
    var error = http.Features.Get<IExceptionHandlerFeature>()?.Error;
    int status = error switch
    {
        AccessDenied or UnauthorizedAccessException => 403,
        KeyNotFoundException => 404,
        ArgumentException or BadHttpRequestException => 400,
        WorkflowConflict or DbUpdateConcurrencyException => 409,
        DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => 409,
        _ => 500
    };
    var title = status switch
    {
        403 => "Workspace access denied.", 404 => "Resource was not found.",
        400 => error?.Message ?? "Invalid request.",
        409 => error is WorkflowConflict ? error.Message : "Record conflicts with existing data. Reload and try again.",
        _ => "The operation failed."
    };
    await Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?>
    { ["traceId"] = http.TraceIdentifier }).ExecuteAsync(http);
}));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();
app.MapGet("/health/ready", async (ConstructionDbContext db, CancellationToken ct) =>
{
    try { return await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch { return Results.StatusCode(503); }
}).AllowAnonymous();
var api = app.MapGroup("/api/v1").RequireAuthorization();
api.MapProjects(); api.MapProcurement(); api.MapReports(); api.MapKnowledge();
app.Run();

namespace ConstructionFlow.Api { public partial class Program; }
