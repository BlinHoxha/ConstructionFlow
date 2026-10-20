using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Infrastructure;

public sealed class ConstructionStore(ConstructionDbContext db) : IConstructionStore
{
    public Task<User?> FindActiveUser(string subject, CancellationToken ct) =>
        db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Subject == subject && x.IsActive, ct);
    public Task<bool> IsTenantActive(Guid tenantId, CancellationToken ct) => db.Tenants.AnyAsync(x => x.Id == tenantId && x.IsActive, ct);
    public Task<TenantMembership?> FindActiveMembership(Guid userId, CancellationToken ct) =>
        db.Memberships.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);
    public Task<Project?> FindProject(Guid id, CancellationToken ct) => db.Projects.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<ProjectRole?> FindProjectRole(Guid projectId, Guid userId, CancellationToken ct) =>
        db.ProjectMembers.AsNoTracking().Where(x => x.ProjectId == projectId && x.UserId == userId)
            .Select(x => (ProjectRole?)x.Role).SingleOrDefaultAsync(ct);
    public Task<bool> ProjectCodeExists(string code, CancellationToken ct) => db.Projects.AnyAsync(x => x.Code == code, ct);
    public Task<bool> ProjectMemberExists(Guid projectId, Guid userId, CancellationToken ct) =>
        db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId, ct);
    public Task<Rfq?> FindRfq(Guid id, CancellationToken ct) => db.Rfqs.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<SupplierQuote[]> FindQuotes(Guid rfqId, CancellationToken ct) =>
        db.Quotes.AsNoTracking().Where(x => x.RfqId == rfqId).Take(101).ToArrayAsync(ct);
    public Task<bool> SupplierIsActive(Guid id, CancellationToken ct) => db.Suppliers.AnyAsync(x => x.Id == id && x.IsActive, ct);
    public Task<DailyReport?> FindReport(Guid id, CancellationToken ct) => db.Reports.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<int> CountProjectDocuments(Guid projectId, Guid[] ids, CancellationToken ct) =>
        db.Documents.CountAsync(x => x.ProjectId == projectId && ids.Contains(x.Id), ct);
    public void Add<T>(T entity) where T : TenantEntity => db.Set<T>().Add(entity);
    public async Task Save(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
