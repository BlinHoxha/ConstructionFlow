using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Api;

public static class ReportEndpoints
{
    public static void MapReports(this RouteGroupBuilder api)
    {
        api.MapGet("/site-reports/{id:guid}", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Workspace(ct);
            var report = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Report was not found.");
            await guard.Project(report.ProjectId, ProjectOperation.Read, ct);
            return Results.Ok(ReportResponse.From(report));
        });
        api.MapPost("/site-reports/drafts", async (CreateSiteReport request, SiteReportWorkflow workflow, CancellationToken ct) =>
        {
            var report = await workflow.Create(request, ct);
            return Results.Created($"/api/v1/site-reports/{report.Id}", report);
        });
        api.MapGet("/projects/{id:guid}/site-reports", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Project(id, ProjectOperation.Read, ct);
            var reports = await db.Reports.AsNoTracking().Where(x => x.ProjectId == id).OrderByDescending(x => x.ReportDate).ThenBy(x => x.Id).Take(100).ToArrayAsync(ct);
            return Results.Ok(reports.Select(ReportResponse.From));
        });
        api.MapPost("/site-reports/{id:guid}/submit", async (Guid id, VersionCommand request, SiteReportWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.Submit(id, request, ct)));
        api.MapPut("/site-reports/{id:guid}/draft", async (Guid id, EditSiteReport request, SiteReportWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.Edit(id, request, ct)));
        api.MapPost("/site-reports/{id:guid}/review", async (Guid id, ReviewSiteReport request, SiteReportWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.Review(id, request, ct)));
    }
}
