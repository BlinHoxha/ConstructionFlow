using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

// Domain-specific queries and one unit of work. No EF, HTTP or provider types cross this boundary.
public interface IConstructionStore
{
    Task<User?> FindActiveUser(string subject, CancellationToken ct);
    Task<bool> IsTenantActive(Guid tenantId, CancellationToken ct);
    Task<TenantMembership?> FindActiveMembership(Guid userId, CancellationToken ct);
    Task<Project?> FindProject(Guid id, CancellationToken ct);
    Task<ProjectRole?> FindProjectRole(Guid projectId, Guid userId, CancellationToken ct);
    Task<bool> ProjectCodeExists(string code, CancellationToken ct);
    Task<bool> ProjectMemberExists(Guid projectId, Guid userId, CancellationToken ct);
    Task<Rfq?> FindRfq(Guid id, CancellationToken ct);
    Task<SupplierQuote[]> FindQuotes(Guid rfqId, CancellationToken ct);
    Task<bool> SupplierIsActive(Guid id, CancellationToken ct);
    Task<DailyReport?> FindReport(Guid id, CancellationToken ct);
    Task<int> CountProjectDocuments(Guid projectId, Guid[] documentIds, CancellationToken ct);
    void Add<T>(T entity) where T : TenantEntity;
    Task Save(CancellationToken ct);
}
