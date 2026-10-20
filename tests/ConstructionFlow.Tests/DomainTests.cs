using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using Xunit;

namespace ConstructionFlow.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Financial_rounding_is_decimal_and_explicit()
    {
        Assert.Equal(new PriceTotals(1.01m, 0.18m, 1.19m), PriceRules.Totals(1, 1.005m, 18));
        Assert.Throws<ArgumentException>(() => PriceRules.Totals(1, -1, 18));
        Assert.Throws<ArgumentException>(() => PriceRules.Totals(1, 1, 101));
        Assert.Throws<ArgumentException>(() => PriceRules.Totals(0.00001m, 1, 18));
        Assert.Throws<ArgumentException>(() => PriceRules.Totals(1_000_000_000m, 1_000_000_000m, 18));
    }

    [Fact]
    public void Cheapest_noncompliant_quote_is_excluded()
    {
        var tenant = Guid.NewGuid(); var project = Guid.NewGuid(); var today = new DateOnly(2026, 10, 15);
        var rfq = new Rfq(tenant, project, "Concrete C30", "m3", 10, "EUR");
        SupplierQuote Quote(string unit, decimal price, bool confirmed, DateOnly validity) =>
            new(tenant, project, rfq.Id, Guid.NewGuid(), Guid.NewGuid().ToString(), unit, "EUR", 10, price, 18, confirmed, validity);
        var compliant = Quote("m3", 100, true, today);
        var result = QuoteComparer.Compare(rfq,
            [Quote("m2", 1, true, today), Quote("m3", 1, false, today), Quote("m3", 1, true, today.AddDays(-1)), compliant], today);
        Assert.Equal(compliant.Id, result.LowestComparableQuoteId);
        Assert.Equal(3, result.Quotes.Count(x => !x.Comparable));
        Assert.Equal(1180, result.Quotes.Single(x => x.Comparable).Totals.Gross);
    }

    [Fact]
    public void Report_requires_submission_and_an_independent_reviewer()
    {
        var author = Guid.NewGuid();
        var report = new DailyReport(Guid.NewGuid(), Guid.NewGuid(), author, new DateOnly(2026, 10, 15), "Foundation inspected.");
        Assert.Throws<WorkflowConflict>(() => report.Review(Guid.NewGuid(), true, 1));
        Assert.Throws<WorkflowConflict>(() => report.Submit(2));
        report.Submit(1);
        Assert.Throws<WorkflowConflict>(() => report.Review(author, true, 1));
        report.Review(Guid.NewGuid(), true, 1);
        Assert.Equal(ReportStatus.Approved, report.Status);
        Assert.NotNull(report.ReviewedAt);
    }

    [Theory]
    [InlineData(ProjectRole.Engineer, ProjectOperation.Manage, false)]
    [InlineData(ProjectRole.Engineer, ProjectOperation.Report, true)]
    [InlineData(ProjectRole.Viewer, ProjectOperation.Procurement, false)]
    [InlineData(ProjectRole.Procurement, ProjectOperation.Procurement, true)]
    [InlineData(ProjectRole.Subcontractor, ProjectOperation.Ask, false)]
    public void Project_role_controls_operation(ProjectRole role, ProjectOperation operation, bool allowed) =>
        Assert.Equal(allowed, AccessPolicy.CanAccess(TenantRole.Engineer, role, operation));

    [Fact]
    public void Returned_report_can_be_corrected_and_resubmitted_but_approved_report_is_frozen()
    {
        var author = Guid.NewGuid(); var reviewer = Guid.NewGuid();
        var report = new DailyReport(Guid.NewGuid(), Guid.NewGuid(), author, new DateOnly(2026, 10, 15), "Initial notes.");
        report.Submit(1); report.Review(reviewer, false, 1);
        report.Edit("Corrected notes.", 1);
        Assert.Equal(ReportStatus.Draft, report.Status); Assert.Null(report.ReviewedAt);
        report.Submit(1); report.Review(reviewer, true, 1);
        Assert.Throws<WorkflowConflict>(() => report.Edit("Overwrite approved report", 1));
    }

    [Fact]
    public void Tenant_role_does_not_grant_implicit_project_access()
    {
        Assert.False(AccessPolicy.CanAccess(TenantRole.Engineer, null, ProjectOperation.Read));
        Assert.True(AccessPolicy.CanAccess(TenantRole.Owner, null, ProjectOperation.Manage));
    }

    [Fact]
    public void Project_dates_currency_and_transitions_are_validated()
    {
        var tenant = Guid.NewGuid(); var start = new DateOnly(2026, 10, 15);
        Assert.Throws<ArgumentException>(() => new Project(tenant, "A", "Site", "Prishtina", start, start.AddDays(-1), "EUR"));
        Assert.Throws<ArgumentException>(() => new Project(tenant, "A", "Site", "Prishtina", start, null, "USD"));
        var project = new Project(tenant, "a", "Site", "Prishtina", start, null, "EUR");
        Assert.Equal("A", project.Code);
        Assert.Throws<WorkflowConflict>(() => project.ChangeStatus(ProjectStatus.Archived, 1));
        project.ChangeStatus(ProjectStatus.Active, 1);
    }
}
