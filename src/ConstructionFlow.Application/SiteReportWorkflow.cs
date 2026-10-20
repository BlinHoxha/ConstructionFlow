using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed class SiteReportWorkflow(IConstructionStore store, AccessGuard guard, IRequestContext context)
{
    public async Task<ReportResponse> Create(CreateSiteReport request, CancellationToken ct)
    {
        await guard.Project(request.ProjectId, ProjectOperation.Report, ct);
        var report = new DailyReport(context.TenantId, request.ProjectId, context.UserId, request.ReportDate, request.Notes);
        store.Add(report); await store.Save(ct); return ReportResponse.From(report);
    }
    public async Task<ReportResponse> Submit(Guid id, VersionCommand request, CancellationToken ct)
    {
        var report = await Get(id, ProjectOperation.Report, ct);
        if (report.AuthorUserId != context.UserId) throw new AccessDenied();
        report.Submit(request.ExpectedVersion); await store.Save(ct); return ReportResponse.From(report);
    }
    public async Task<ReportResponse> Edit(Guid id, EditSiteReport request, CancellationToken ct)
    {
        var report = await Get(id, ProjectOperation.Report, ct);
        if (report.AuthorUserId != context.UserId) throw new AccessDenied();
        report.Edit(request.Notes, request.ExpectedVersion); await store.Save(ct); return ReportResponse.From(report);
    }
    public async Task<ReportResponse> Review(Guid id, ReviewSiteReport request, CancellationToken ct)
    {
        var report = await Get(id, ProjectOperation.ReviewReport, ct);
        report.Review(context.UserId, request.Approve, request.ExpectedVersion);
        await store.Save(ct); return ReportResponse.From(report);
    }
    private async Task<DailyReport> Get(Guid id, ProjectOperation operation, CancellationToken ct)
    {
        await guard.Workspace(ct);
        var report = await store.FindReport(id, ct) ?? throw new KeyNotFoundException("Report was not found.");
        await guard.Project(report.ProjectId, operation, ct);
        return report;
    }
}
