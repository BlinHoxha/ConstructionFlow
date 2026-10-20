using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Api;

public static class ProcurementEndpoints
{
    public static void MapProcurement(this RouteGroupBuilder api)
    {
        api.MapGet("/suppliers/{id:guid}", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            if (!AccessPolicy.CanManageSuppliers((await guard.Workspace(ct)).Role)) throw new AccessDenied();
            var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException("Supplier was not found.");
            return Results.Ok(new { supplier.Id, supplier.Name, supplier.RegistrationNumber });
        });
        api.MapGet("/rfqs/{id:guid}", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Workspace(ct);
            var rfq = await db.Rfqs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("RFQ was not found.");
            await guard.Project(rfq.ProjectId, ProjectOperation.Read, ct);
            return Results.Ok(new { rfq.Id, rfq.ProjectId, rfq.Description, rfq.UnitCode, rfq.Quantity, rfq.CurrencyCode });
        });
        api.MapGet("/rfqs/{id:guid}/quotes/{quoteId:guid}", async (Guid id, Guid quoteId, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Workspace(ct);
            var quote = await db.Quotes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == quoteId && x.RfqId == id, ct)
                ?? throw new KeyNotFoundException("Offer was not found.");
            await guard.Project(quote.ProjectId, ProjectOperation.Procurement, ct);
            return Results.Ok(new { quote.Id, quote.SupplierId, quote.Reference, quote.UnitCode, quote.Quantity,
                quote.UnitPrice, quote.VatPercent, quote.SpecificationConfirmed, quote.ValidUntil, quote.CurrencyCode,
                totals = PriceRules.Totals(quote.Quantity, quote.UnitPrice, quote.VatPercent) });
        });
        api.MapPost("/suppliers", async (CreateSupplier request, ProcurementWorkflow workflow, CancellationToken ct) =>
        {
            var supplier = await workflow.CreateSupplier(request, ct);
            return Results.Created($"/api/v1/suppliers/{supplier.Id}", supplier);
        });
        api.MapGet("/suppliers", async (AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            if (!AccessPolicy.CanManageSuppliers((await guard.Workspace(ct)).Role)) throw new AccessDenied();
            return Results.Ok(await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Take(100)
                .Select(x => new { x.Id, x.Name, x.RegistrationNumber }).ToArrayAsync(ct));
        });
        api.MapPost("/rfqs", async (CreateRfq request, ProcurementWorkflow workflow, CancellationToken ct) =>
        {
            var rfq = await workflow.CreateRfq(request, ct);
            return Results.Created($"/api/v1/rfqs/{rfq.Id}", rfq);
        });
        api.MapGet("/projects/{id:guid}/rfqs", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Project(id, ProjectOperation.Read, ct);
            return Results.Ok(await db.Rfqs.AsNoTracking().Where(x => x.ProjectId == id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(100)
                .Select(x => new { x.Id, x.ProjectId, x.Description, x.UnitCode, x.Quantity, x.CurrencyCode }).ToArrayAsync(ct));
        });
        api.MapPost("/rfqs/{id:guid}/quotes", async (Guid id, CreateQuote request, ProcurementWorkflow workflow, CancellationToken ct) =>
        {
            var quote = await workflow.CreateQuote(id, request, ct);
            return Results.Created($"/api/v1/rfqs/{id}/quotes/{quote.Id}", quote);
        });
        api.MapPost("/rfqs/{id:guid}/compare", async (Guid id, ProcurementWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.Compare(id, ct)));
    }
}
