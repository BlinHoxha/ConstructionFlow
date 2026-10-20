namespace ConstructionFlow.Domain;

public enum ProjectStatus { Draft, Active, OnHold, Completed, Archived }

public sealed class Project : TenantEntity
{
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string LocationText { get; private set; } = "";
    public DateOnly PlannedStart { get; private set; }
    public DateOnly? PlannedEnd { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public ProjectStatus Status { get; private set; }
    private Project() { }
    public Project(Guid tenantId, string code, string name, string location, DateOnly start, DateOnly? end, string currency) : base(tenantId)
    {
        Code = Rules.Text(code, 40, "Project code").ToUpperInvariant();
        Name = Rules.Text(name, 200, "Project name");
        LocationText = Rules.Text(location, 300, "Location");
        if (start == default || end < start) throw new ArgumentException("Valid project dates are required.");
        PlannedStart = start; PlannedEnd = end; CurrencyCode = Rules.Currency(currency);
    }

    public void ChangeStatus(ProjectStatus next, long expectedVersion)
    {
        Rules.Version(RowVersion, expectedVersion);
        var allowed = (Status, next) switch
        {
            (ProjectStatus.Draft, ProjectStatus.Active) => true,
            (ProjectStatus.Active, ProjectStatus.OnHold or ProjectStatus.Completed) => true,
            (ProjectStatus.OnHold, ProjectStatus.Active or ProjectStatus.Completed) => true,
            (ProjectStatus.Completed, ProjectStatus.Archived) => true,
            _ => false
        };
        if (!allowed) throw new WorkflowConflict("Invalid project status transition.");
        Status = next;
    }
}

public enum ReportStatus { Draft, Submitted, Approved, Returned }

public sealed class DailyReport : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public DateOnly ReportDate { get; private set; }
    public string Notes { get; private set; } = "";
    public ReportStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    private DailyReport() { }
    public DailyReport(Guid tenantId, Guid projectId, Guid authorId, DateOnly date, string notes) : base(tenantId)
    {
        if (date == default) throw new ArgumentException("Report date is required.");
        ProjectId = projectId; AuthorUserId = authorId; ReportDate = date;
        Notes = Rules.Text(notes, 20000, "Site notes");
    }
    public void Submit(long expected)
    {
        Rules.Version(RowVersion, expected);
        if (Status != ReportStatus.Draft) throw new WorkflowConflict("Only a draft can be submitted.");
        Status = ReportStatus.Submitted;
    }
    public void Edit(string notes, long expected)
    {
        Rules.Version(RowVersion, expected);
        if (Status is not (ReportStatus.Draft or ReportStatus.Returned))
            throw new WorkflowConflict("Only a draft or returned report can be edited.");
        Notes = Rules.Text(notes, 20000, "Site notes");
        Status = ReportStatus.Draft;
        ReviewedBy = null; ReviewedAt = null;
    }
    public void Review(Guid reviewer, bool approve, long expected)
    {
        Rules.Version(RowVersion, expected);
        if (Status != ReportStatus.Submitted) throw new WorkflowConflict("Only a submitted report can be reviewed.");
        if (reviewer == AuthorUserId) throw new WorkflowConflict("A report requires a different reviewer.");
        Status = approve ? ReportStatus.Approved : ReportStatus.Returned;
        ReviewedBy = reviewer; ReviewedAt = DateTimeOffset.UtcNow;
    }
}
