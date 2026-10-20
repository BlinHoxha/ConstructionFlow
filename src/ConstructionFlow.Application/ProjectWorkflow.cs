using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed class ProjectWorkflow(IConstructionStore store, AccessGuard guard, IRequestContext context)
{
    public async Task<ProjectResponse> Create(CreateProject request, CancellationToken ct)
    {
        var access = await guard.Workspace(ct);
        if (!AccessPolicy.CanCreateProject(access.Role)) throw new AccessDenied();
        var project = new Project(context.TenantId, request.Code, request.Name, request.Location,
            request.PlannedStart, request.PlannedEnd, request.CurrencyCode);
        if (await store.ProjectCodeExists(project.Code, ct)) throw new WorkflowConflict("Project code already exists.");
        store.Add(project);
        store.Add(new ProjectMember(context.TenantId, project.Id, access.UserId, ProjectRole.Manager));
        await store.Save(ct);
        return ProjectResponse.From(project);
    }

    public async Task<ProjectResponse> ChangeStatus(Guid id, ChangeProjectStatus request, CancellationToken ct)
    {
        var project = await guard.Project(id, ProjectOperation.Manage, ct);
        project.ChangeStatus(request.Status, request.ExpectedVersion);
        await store.Save(ct);
        return ProjectResponse.From(project);
    }

    public async Task<ProjectMemberResponse> AddMember(Guid id, AddProjectMember request, CancellationToken ct)
    {
        await guard.Project(id, ProjectOperation.Manage, ct);
        if (!Enum.IsDefined(request.Role)) throw new ArgumentException("Invalid project role.");
        if (await store.FindActiveMembership(request.UserId, ct) is null)
            throw new ArgumentException("User must have an active membership in this tenant.");
        if (await store.ProjectMemberExists(id, request.UserId, ct)) throw new WorkflowConflict("User is already a project member.");
        var member = new ProjectMember(context.TenantId, id, request.UserId, request.Role);
        store.Add(member); await store.Save(ct);
        return new(member.Id, member.UserId, member.Role);
    }
}

public sealed record ProjectMemberResponse(Guid Id, Guid UserId, ProjectRole Role);
