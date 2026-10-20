using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Api;

public static class ProjectEndpoints
{
    public static void MapProjects(this RouteGroupBuilder api)
    {
        api.MapGet("/projects", async (int? page, int? pageSize, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            var access = await guard.Workspace(ct);
            int number = page ?? 1, size = pageSize ?? 25;
            if (number is < 1 or > 100000 || size is < 1 or > 100) throw new ArgumentException("Invalid pagination.");
            var query = db.Projects.AsNoTracking();
            if (!AccessPolicy.CanCreateProject(access.Role))
                query = query.Where(p => db.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == access.UserId && m.Role != ProjectRole.Subcontractor));
            int total = await query.CountAsync(ct);
            var projects = await query.OrderBy(p => p.Code).ThenBy(p => p.Id).Skip((number - 1) * size).Take(size).ToArrayAsync(ct);
            return Results.Ok(new { items = projects.Select(ProjectResponse.From), page = number, pageSize = size, total });
        });
        api.MapPost("/projects", async (CreateProject request, ProjectWorkflow workflow, CancellationToken ct) =>
        {
            var project = await workflow.Create(request, ct);
            return Results.Created($"/api/v1/projects/{project.Id}/summary", project);
        });
        api.MapGet("/projects/{id:guid}/summary", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            var project = await guard.Project(id, ProjectOperation.Read, ct);
            return Results.Ok(new { project = ProjectResponse.From(project),
                approvedReports = await db.Reports.CountAsync(x => x.ProjectId == id && x.Status == ReportStatus.Approved, ct),
                documents = await db.Documents.CountAsync(x => x.ProjectId == id, ct),
                rfqs = await db.Rfqs.CountAsync(x => x.ProjectId == id, ct) });
        });
        api.MapPost("/projects/{id:guid}/status", async (Guid id, ChangeProjectStatus request, ProjectWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.ChangeStatus(id, request, ct)));
        api.MapPost("/projects/{id:guid}/members", async (Guid id, AddProjectMember request, ProjectWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.AddMember(id, request, ct)));
        api.MapGet("/projects/{id:guid}/audit", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Project(id, ProjectOperation.Manage, ct);
            // Initial endpoint covers the project aggregate itself; tenant-wide audit/export is later.
            return Results.Ok(await db.AuditEvents.AsNoTracking().Where(x => x.EntityType == nameof(Project) && x.EntityId == id)
                .OrderByDescending(x => x.CreatedAt).Take(100)
                .Select(x => new { x.Id, x.ActorUserId, x.EntityType, x.EntityId, x.Action, x.CreatedAt }).ToArrayAsync(ct));
        });
    }
}
