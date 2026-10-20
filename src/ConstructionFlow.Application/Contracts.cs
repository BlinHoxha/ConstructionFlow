using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed record CreateProject(string Code, string Name, string Location, DateOnly PlannedStart, DateOnly? PlannedEnd, string CurrencyCode = "EUR");
public sealed record ChangeProjectStatus(ProjectStatus Status, long ExpectedVersion);
public sealed record AddProjectMember(Guid UserId, ProjectRole Role);
public sealed record CreateSupplier(string Name, string? RegistrationNumber);
public sealed record CreateRfq(Guid ProjectId, string Description, string UnitCode, decimal Quantity);
public sealed record CreateQuote(Guid SupplierId, string Reference, string UnitCode, decimal Quantity,
    decimal UnitPrice, decimal VatPercent, bool SpecificationConfirmed, DateOnly ValidUntil);
public sealed record CreateSiteReport(Guid ProjectId, DateOnly ReportDate, string Notes);
public sealed record VersionCommand(long ExpectedVersion);
public sealed record ReviewSiteReport(bool Approve, long ExpectedVersion);
public sealed record EditSiteReport(string Notes, long ExpectedVersion);
public sealed record IndexDocument(string Title, string Content, string Language, bool HumanReviewed);
public sealed record AskProject(string Question, Guid[]? DocumentIds, int MaxResults = 5);
public sealed record ProjectResponse(Guid Id, string Code, string Name, string Location, DateOnly PlannedStart,
    DateOnly? PlannedEnd, string CurrencyCode, ProjectStatus Status, long RowVersion)
{
    public static ProjectResponse From(Project p) => new(p.Id, p.Code, p.Name, p.LocationText, p.PlannedStart,
        p.PlannedEnd, p.CurrencyCode, p.Status, p.RowVersion);
}
public sealed record ReportResponse(Guid Id, Guid ProjectId, Guid AuthorUserId, DateOnly ReportDate,
    string Notes, ReportStatus Status, Guid? ReviewedBy, DateTimeOffset? ReviewedAt, long RowVersion)
{
    public static ReportResponse From(DailyReport r) => new(r.Id, r.ProjectId, r.AuthorUserId, r.ReportDate,
        r.Notes, r.Status, r.ReviewedBy, r.ReviewedAt, r.RowVersion);
}
