using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace ConstructionFlow.Tests;

public sealed class TestContext : IRequestContext
{
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Subject => "test-owner";
}

public sealed class PersistenceTests
{
    private static async Task Seed(ConstructionDbContext db, TestContext context)
    {
        db.Tenants.Add(new Tenant(context.TenantId, "Synthetic Tenant"));
        if (!await db.Users.AnyAsync(x => x.Id == context.UserId))
            db.Users.Add(new User(context.UserId, context.UserId.ToString(), "Synthetic User"));
        db.Memberships.Add(new TenantMembership(context.TenantId, context.UserId, TenantRole.Owner));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Filters_and_composite_foreign_keys_prevent_cross_tenant_links()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ConstructionDbContext>().UseSqlite(connection).Options;
        var a = new TestContext(); var b = new TestContext();
        await using var first = new ConstructionDbContext(options, a);
        await first.Database.EnsureCreatedAsync(); await Seed(first, a);
        var project = new Project(a.TenantId, "A", "Site A", "Prishtina", new DateOnly(2026, 10, 1), null, "EUR");
        first.Projects.Add(project); await first.SaveChangesAsync();
        await using var second = new ConstructionDbContext(options, b); await Seed(second, b);
        Assert.Empty(await second.Projects.ToArrayAsync());
        second.Reports.Add(new DailyReport(b.TenantId, project.Id, b.UserId, new DateOnly(2026, 10, 15), "Invalid cross-tenant link"));
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Audit_is_atomic_and_immutable_and_concurrency_rejects_stale_writes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ConstructionDbContext>().UseSqlite(connection).Options;
        var context = new TestContext();
        await using var first = new ConstructionDbContext(options, context);
        await first.Database.EnsureCreatedAsync(); await Seed(first, context);
        var project = new Project(context.TenantId, "A", "Site", "Prishtina", new DateOnly(2026, 10, 1), null, "EUR");
        first.Projects.Add(project); await first.SaveChangesAsync();
        Assert.Single(await first.AuditEvents.Where(x => x.EntityId == project.Id).ToArrayAsync());
        await using var second = new ConstructionDbContext(options, context);
        var stale = await second.Projects.SingleAsync();
        project.ChangeStatus(ProjectStatus.Active, 1); await first.SaveChangesAsync();
        Assert.Equal(2, project.RowVersion);
        stale.ChangeStatus(ProjectStatus.Active, 1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        // Failed save must not append its audit row.
        Assert.Equal(2, await first.AuditEvents.CountAsync(x => x.EntityId == project.Id));
        first.AuditEvents.Remove(await first.AuditEvents.FirstAsync());
        await Assert.ThrowsAsync<WorkflowConflict>(() => first.SaveChangesAsync());
    }

    [Fact]
    public async Task Save_guard_rejects_foreign_tenant_entities()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ConstructionDbContext>().UseSqlite(connection).Options;
        var context = new TestContext(); await using var db = new ConstructionDbContext(options, context);
        await db.Database.EnsureCreatedAsync(); await Seed(db, context);
        db.Suppliers.Add(new Supplier(Guid.NewGuid(), "Other tenant supplier", null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void PostgreSql_migration_is_current_and_contains_tenant_constraints()
    {
        using var db = new ConstructionDbContext(new DbContextOptionsBuilder<ConstructionDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options, new TestContext());
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE", script);
        Assert.Contains("FOREIGN KEY (tenant_id, project_id)", script);
    }
}
