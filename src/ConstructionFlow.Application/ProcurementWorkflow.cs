using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed class ProcurementWorkflow(IConstructionStore store, AccessGuard guard, IRequestContext context, TimeProvider clock)
{
    public async Task<QuoteComparison> Compare(Guid rfqId, CancellationToken ct)
    {
        await guard.Workspace(ct);
        var rfq = await store.FindRfq(rfqId, ct) ?? throw new KeyNotFoundException("RFQ was not found.");
        await guard.Project(rfq.ProjectId, ProjectOperation.Procurement, ct);
        var quotes = await store.FindQuotes(rfqId, ct);
        if (quotes.Length > 100) throw new WorkflowConflict("Comparison exceeds the initial limit of 100 offers.");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.GetUtcNow(), "Europe/Belgrade").DateTime);
        return QuoteComparer.Compare(rfq, quotes, today);
    }
    public async Task<SupplierResponse> CreateSupplier(CreateSupplier request, CancellationToken ct)
    {
        if (!AccessPolicy.CanManageSuppliers((await guard.Workspace(ct)).Role)) throw new AccessDenied();
        var supplier = new Supplier(context.TenantId, request.Name, request.RegistrationNumber);
        store.Add(supplier); await store.Save(ct);
        return new(supplier.Id, supplier.Name, supplier.RegistrationNumber);
    }
    public async Task<RfqResponse> CreateRfq(CreateRfq request, CancellationToken ct)
    {
        var project = await guard.Project(request.ProjectId, ProjectOperation.Procurement, ct);
        var rfq = new Rfq(context.TenantId, project.Id, request.Description, request.UnitCode, request.Quantity, project.CurrencyCode);
        store.Add(rfq); await store.Save(ct);
        return new(rfq.Id, rfq.ProjectId, rfq.Description, rfq.UnitCode, rfq.Quantity, rfq.CurrencyCode);
    }
    public async Task<QuoteCreatedResponse> CreateQuote(Guid rfqId, CreateQuote request, CancellationToken ct)
    {
        await guard.Workspace(ct);
        var rfq = await store.FindRfq(rfqId, ct) ?? throw new KeyNotFoundException("RFQ was not found.");
        await guard.Project(rfq.ProjectId, ProjectOperation.Procurement, ct);
        if (!await store.SupplierIsActive(request.SupplierId, ct)) throw new KeyNotFoundException("Supplier was not found.");
        var quote = new SupplierQuote(context.TenantId, rfq.ProjectId, rfq.Id, request.SupplierId, request.Reference,
            request.UnitCode, rfq.CurrencyCode, request.Quantity, request.UnitPrice, request.VatPercent,
            request.SpecificationConfirmed, request.ValidUntil);
        store.Add(quote); await store.Save(ct);
        return new(quote.Id, PriceRules.Totals(quote.Quantity, quote.UnitPrice, quote.VatPercent));
    }
}

public sealed record SupplierResponse(Guid Id, string Name, string? RegistrationNumber);
public sealed record RfqResponse(Guid Id, Guid ProjectId, string Description, string UnitCode, decimal Quantity, string CurrencyCode);
public sealed record QuoteCreatedResponse(Guid Id, PriceTotals Totals);
