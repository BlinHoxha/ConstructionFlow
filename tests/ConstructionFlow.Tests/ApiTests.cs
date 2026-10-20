using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ConstructionFlow.Api;
using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ConstructionFlow.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "test-only-signing-key-with-at-least-32-characters";
    public Guid TenantId { get; } = Guid.NewGuid();
    public Guid OtherTenantId { get; } = Guid.NewGuid();
    public Guid OwnerId { get; } = Guid.NewGuid();
    public Guid EngineerId { get; } = Guid.NewGuid();
    public Guid ViewerId { get; } = Guid.NewGuid();
    public Guid ProjectId { get; private set; }
    public Guid HiddenProjectId { get; private set; }
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public ApiFactory() { connection.Open(); }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Construction", "Host=localhost;Database=unused;Username=unused");
        builder.UseSetting("Authentication:DevelopmentSigningKey", Key);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ConstructionDbContext>();
            services.RemoveAll<DbContextOptions<ConstructionDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ConstructionDbContext>>();
            services.AddDbContext<ConstructionDbContext>(o => o.UseSqlite(connection));
        });
    }
    public async Task Seed()
    {
        _ = Services;
        var context = new TestContext { TenantId = TenantId, UserId = OwnerId };
        await using var db = new ConstructionDbContext(new DbContextOptionsBuilder<ConstructionDbContext>().UseSqlite(connection).Options, context);
        await db.Database.EnsureCreatedAsync();
        db.Tenants.Add(new Tenant(TenantId, "Synthetic API Tenant"));
        db.Users.AddRange(new User(OwnerId, "owner", "Owner"), new User(EngineerId, "engineer", "Engineer"), new User(ViewerId, "viewer", "Viewer"));
        db.Memberships.AddRange(new TenantMembership(TenantId, OwnerId, TenantRole.Owner),
            new TenantMembership(TenantId, EngineerId, TenantRole.Engineer), new TenantMembership(TenantId, ViewerId, TenantRole.Viewer));
        var project = new Project(TenantId, "TEST", "Synthetic Site", "Prishtina", new DateOnly(2026, 10, 1), null, "EUR");
        var hidden = new Project(TenantId, "PRIVATE", "Unassigned Site", "Peja", new DateOnly(2026, 10, 1), null, "EUR");
        ProjectId = project.Id; HiddenProjectId = hidden.Id;
        db.Projects.AddRange(project, hidden);
        db.ProjectMembers.AddRange(new ProjectMember(TenantId, ProjectId, EngineerId, ProjectRole.Engineer),
            new ProjectMember(TenantId, ProjectId, ViewerId, ProjectRole.Viewer));
        await db.SaveChangesAsync();
        await using var other = new ConstructionDbContext(new DbContextOptionsBuilder<ConstructionDbContext>().UseSqlite(connection).Options,
            new TestContext { TenantId = OtherTenantId, UserId = OwnerId });
        other.Tenants.Add(new Tenant(OtherTenantId, "Second Synthetic Tenant"));
        other.Memberships.Add(new TenantMembership(OtherTenantId, OwnerId, TenantRole.Owner));
        await other.SaveChangesAsync();
    }
    public HttpClient Client(string subject, Guid? tenant = null, string? key = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = new JwtSecurityToken("constructionflow-development", "constructionflow-api",
            [new Claim("sub", subject), new Claim("tenant_id", (tenant ?? TenantId).ToString())],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? Key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public sealed class ApiTests
{
    [Fact]
    public async Task Anonymous_and_forged_tokens_cannot_access_projects()
    {
        using var factory = new ApiFactory(); await factory.Seed();
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/v1/projects")).StatusCode);
        using var forged = factory.Client("owner", key: "forged-key-that-is-definitely-long-enough");
        Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("/api/v1/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Tenant_claim_is_not_a_membership_and_project_counts_are_scoped()
    {
        using var factory = new ApiFactory(); await factory.Seed();
        using var wrongTenant = factory.Client("owner", Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await wrongTenant.GetAsync("/api/v1/projects")).StatusCode);
        using var engineer = factory.Client("engineer");
        var list = await engineer.GetFromJsonAsync<JsonElement>("/api/v1/projects");
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await engineer.GetAsync($"/api/v1/projects/{factory.HiddenProjectId}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await engineer.PostAsJsonAsync($"/api/v1/projects/{factory.HiddenProjectId}/ask",
            new AskProject("What does the contract say?", null))).StatusCode);
    }

    [Fact]
    public async Task Owner_in_two_tenants_cannot_read_project_or_evidence_from_the_other_tenant()
    {
        using var factory = new ApiFactory(); await factory.Seed();
        using var otherTenant = factory.Client("owner", factory.OtherTenantId);
        var list = await otherTenant.GetFromJsonAsync<JsonElement>("/api/v1/projects");
        Assert.Equal(0, list.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await otherTenant.GetAsync($"/api/v1/projects/{factory.ProjectId}/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherTenant.PostAsJsonAsync($"/api/v1/projects/{factory.ProjectId}/ask", new AskProject("concrete delivery", null))).StatusCode);
    }

    [Fact]
    public async Task Project_creation_normalizes_code_and_rejects_duplicates_and_stale_versions()
    {
        using var factory = new ApiFactory(); await factory.Seed(); using var owner = factory.Client("owner");
        var request = new CreateProject("new-01", "New Site", "Prizren", new DateOnly(2026, 10, 15), null);
        var created = await owner.PostAsJsonAsync("/api/v1/projects", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var project = await created.Content.ReadFromJsonAsync<JsonElement>(); var id = project.GetProperty("id").GetGuid();
        Assert.Equal("NEW-01", project.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync("/api/v1/projects", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync($"/api/v1/projects/{id}/status", new { status = "Active", expectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/projects/{id}/status", new { status = "Completed", expectedVersion = 1 })).StatusCode);
        using var viewer = factory.Client("viewer");
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/v1/projects", request)).StatusCode);
    }

    [Fact]
    public async Task Report_submission_and_independent_review_preserve_version()
    {
        using var factory = new ApiFactory(); await factory.Seed();
        using var engineer = factory.Client("engineer"); using var owner = factory.Client("owner");
        var response = await engineer.PostAsJsonAsync("/api/v1/site-reports/drafts",
            new CreateSiteReport(factory.ProjectId, new DateOnly(2026, 10, 15), "Concrete delivered; quantities manually checked."));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = report.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await engineer.PostAsJsonAsync($"/api/v1/site-reports/{id}/submit", new VersionCommand(1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await engineer.PostAsJsonAsync($"/api/v1/site-reports/{id}/review", new ReviewSiteReport(true, 2))).StatusCode);
        var approved = await owner.PostAsJsonAsync($"/api/v1/site-reports/{id}/review", new ReviewSiteReport(true, 2));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var result = await approved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", result.GetProperty("status").GetString()); Assert.Equal(3, result.GetProperty("rowVersion").GetInt64());
    }

    [Fact]
    public async Task Reviewed_evidence_is_durable_scoped_and_cited_and_missing_evidence_abstains()
    {
        using var factory = new ApiFactory(); await factory.Seed(); using var owner = factory.Client("owner");
        var path = $"/api/v1/projects/{factory.ProjectId}";
        var content = "The concrete delivery inspection must be completed by the site engineer before acceptance.";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(path + "/documents/reviewed-text",
            new IndexDocument("Inspection procedure", content, "en", false))).StatusCode);
        var indexed = await owner.PostAsJsonAsync(path + "/documents/reviewed-text", new IndexDocument("Inspection procedure", content, "en", true));
        Assert.Equal(HttpStatusCode.Created, indexed.StatusCode);
        var doc = await indexed.Content.ReadFromJsonAsync<JsonElement>(); var docId = doc.GetProperty("documentId").GetGuid();
        using var engineer = factory.Client("engineer");
        var answer = await engineer.PostAsJsonAsync(path + "/ask", new AskProject("Who completes concrete delivery inspection?", [docId]));
        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        var body = await answer.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("grounded").GetBoolean());
        Assert.Equal(docId, body.GetProperty("citations")[0].GetProperty("documentId").GetGuid());
        Assert.True(body.GetProperty("requiresHumanReview").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await engineer.GetAsync(path + $"/documents/{docId}/chunks")).StatusCode);
        var noEvidence = await engineer.PostAsJsonAsync(path + "/ask", new AskProject("xyzunknownterm", null));
        Assert.False((await noEvidence.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("grounded").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await engineer.PostAsJsonAsync(path + "/ask", new AskProject("inspection", [Guid.NewGuid()]))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await engineer.GetAsync($"/api/v1/projects/{factory.HiddenProjectId}/documents/{docId}/chunks")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync(path + "/documents/reviewed-text", new IndexDocument("Duplicate", content, "en", true))).StatusCode);
    }

    [Fact]
    public async Task Procurement_calculates_reviewed_offers_without_auto_awarding()
    {
        using var factory = new ApiFactory(); await factory.Seed(); using var owner = factory.Client("owner");
        var supplierResponse = await owner.PostAsJsonAsync("/api/v1/suppliers", new CreateSupplier("Synthetic Supplier", null));
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<JsonElement>(); var supplierId = supplier.GetProperty("id").GetGuid();
        var rfqResponse = await owner.PostAsJsonAsync("/api/v1/rfqs", new CreateRfq(factory.ProjectId, "C30 concrete", "m3", 10));
        var rfq = await rfqResponse.Content.ReadFromJsonAsync<JsonElement>(); var rfqId = rfq.GetProperty("id").GetGuid();
        var quote = new CreateQuote(supplierId, "OFFER-1", "m3", 10, 100, 18, true, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync($"/api/v1/rfqs/{rfqId}/quotes", quote)).StatusCode);
        var comparison = await owner.PostAsJsonAsync($"/api/v1/rfqs/{rfqId}/compare", new { });
        Assert.Equal(HttpStatusCode.OK, comparison.StatusCode);
        var body = await comparison.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1180, body.GetProperty("quotes")[0].GetProperty("totals").GetProperty("gross").GetDecimal());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("lowestComparableQuoteId").ValueKind);
    }
}
