using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public interface IRequestContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string Subject { get; }
}

public interface IAuthenticatedRequestContext : IRequestContext
{
    void SetUserId(Guid userId);
}

public sealed record WorkspaceAccess(Guid UserId, TenantRole Role);
public sealed class AccessDenied : Exception;

public sealed class AccessGuard(IConstructionStore store, IAuthenticatedRequestContext context)
{
    private WorkspaceAccess? access;
    public async Task<WorkspaceAccess> Workspace(CancellationToken ct)
    {
        if (access is not null) return access;
        if (context.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(context.Subject)) throw new AccessDenied();
        var user = await store.FindActiveUser(context.Subject, ct);
        if (user is null || !await store.IsTenantActive(context.TenantId, ct)) throw new AccessDenied();
        var membership = await store.FindActiveMembership(user.Id, ct);
        if (membership is null) throw new AccessDenied();
        context.SetUserId(user.Id);
        return access = new(user.Id, membership.Role);
    }

    public async Task<Project> Project(Guid id, ProjectOperation operation, CancellationToken ct)
    {
        var workspace = await Workspace(ct);
        var project = await store.FindProject(id, ct) ?? throw new KeyNotFoundException("Project was not found.");
        var role = await store.FindProjectRole(id, workspace.UserId, ct);
        if (!AccessPolicy.CanAccess(workspace.Role, role, operation)) throw new KeyNotFoundException("Project was not found.");
        return project;
    }
}

public enum ProjectOperation { Read, Manage, Procurement, Report, ReviewReport, IngestDocument, Ask }

public static class AccessPolicy
{
    public static bool CanCreateProject(TenantRole role) => role is TenantRole.Owner or TenantRole.Admin;
    public static bool CanManageSuppliers(TenantRole role) => role is TenantRole.Owner or TenantRole.Admin or TenantRole.Procurement;
    public static bool CanAccess(TenantRole tenantRole, ProjectRole? projectRole, ProjectOperation operation)
    {
        if (tenantRole is TenantRole.Owner or TenantRole.Admin) return true;
        return (projectRole, operation) switch
        {
            (ProjectRole.Manager, _) => true,
            (ProjectRole.Engineer, ProjectOperation.Read or ProjectOperation.Report or ProjectOperation.Ask) => true,
            (ProjectRole.Procurement, ProjectOperation.Read or ProjectOperation.Procurement or ProjectOperation.Ask) => true,
            (ProjectRole.Accountant or ProjectRole.Viewer, ProjectOperation.Read or ProjectOperation.Ask) => true,
            // Restricted subcontractor assignments/document ACLs require their own future workflow.
            _ => false
        };
    }
}
