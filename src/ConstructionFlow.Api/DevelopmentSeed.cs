using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Api;

public static class DevelopmentSeed
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid EngineerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task Run(IServiceProvider services, IConfiguration configuration)
    {
        var ownerSubject = configuration["DevelopmentSeed:OwnerSubject"];
        var engineerSubject = configuration["DevelopmentSeed:EngineerSubject"];
        if (string.IsNullOrWhiteSpace(ownerSubject) || string.IsNullOrWhiteSpace(engineerSubject) || ownerSubject == engineerSubject)
            throw new InvalidOperationException("Set distinct DevelopmentSeed__OwnerSubject and DevelopmentSeed__EngineerSubject.");
        using var scope = services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var http = new DefaultHttpContext();
        http.User = new(new System.Security.Claims.ClaimsIdentity([
            new("tenant_id", TenantId.ToString()), new("sub", ownerSubject)], "development-seed"));
        accessor.HttpContext = http;
        scope.ServiceProvider.GetRequiredService<RequestContext>().UserId = OwnerId;
        var db = scope.ServiceProvider.GetRequiredService<ConstructionDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Tenants.AnyAsync(x => x.Id == TenantId))
        {
            if (!await db.Users.AnyAsync(x => x.Id == OwnerId && x.Subject == ownerSubject && x.IsActive) ||
                !await db.Users.AnyAsync(x => x.Id == EngineerId && x.Subject == engineerSubject && x.IsActive))
                throw new InvalidOperationException("Existing synthetic identities do not match the configured subjects. Use the original subjects or a fresh development database.");
            return;
        }
        db.Tenants.Add(new Tenant(TenantId, "Synthetic Construction Pilot"));
        db.Users.AddRange(new User(OwnerId, ownerSubject, "Synthetic Owner"), new User(EngineerId, engineerSubject, "Synthetic Engineer"));
        db.Memberships.AddRange(new TenantMembership(TenantId, OwnerId, TenantRole.Owner),
            new TenantMembership(TenantId, EngineerId, TenantRole.Engineer));
        var project = new Project(TenantId, "DEMO-001", "Synthetic Prishtina Site", "Prishtina", new DateOnly(2026, 10, 1), null, "EUR");
        db.Projects.Add(project);
        db.ProjectMembers.AddRange(new ProjectMember(TenantId, project.Id, OwnerId, ProjectRole.Manager),
            new ProjectMember(TenantId, project.Id, EngineerId, ProjectRole.Engineer));
        db.Suppliers.Add(new Supplier(TenantId, "Synthetic Materials Supplier", "DEMO-NOT-A-REAL-REGISTRATION"));
        await db.SaveChangesAsync();
        Console.WriteLine($"Synthetic tenant {TenantId}; project {project.Id}; owner {OwnerId}; engineer {EngineerId}.");
    }
}
