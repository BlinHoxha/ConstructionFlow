using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed record ComparedQuote(Guid QuoteId, Guid SupplierId, string Reference, bool Comparable,
    string[] Reasons, PriceTotals Totals);
public sealed record QuoteComparison(ComparedQuote[] Quotes, Guid? LowestComparableQuoteId);

public static class QuoteComparer
{
    public static QuoteComparison Compare(Rfq rfq, IEnumerable<SupplierQuote> quotes, DateOnly today)
    {
        var compared = quotes.Select(quote =>
        {
            if (quote.TenantId != rfq.TenantId || quote.ProjectId != rfq.ProjectId || quote.RfqId != rfq.Id)
                throw new ArgumentException("Quote does not belong to this RFQ.");
            var reasons = new List<string>();
            if (quote.CurrencyCode != rfq.CurrencyCode) reasons.Add("Currency differs; confirmed FX conversion is required.");
            if (quote.UnitCode != rfq.UnitCode) reasons.Add("Units differ; explicit normalization is required.");
            if (quote.Quantity != rfq.Quantity) reasons.Add("Quoted quantity differs from requested quantity.");
            if (!quote.SpecificationConfirmed) reasons.Add("Specification compliance has not been confirmed.");
            if (quote.ValidUntil < today) reasons.Add("Offer has expired.");
            return new ComparedQuote(quote.Id, quote.SupplierId, quote.Reference, reasons.Count == 0,
                reasons.ToArray(), PriceRules.Totals(quote.Quantity, quote.UnitPrice, quote.VatPercent));
        }).ToArray();
        return new(compared, compared.Where(x => x.Comparable).OrderBy(x => x.Totals.Gross)
            .ThenBy(x => x.QuoteId).Select(x => (Guid?)x.QuoteId).FirstOrDefault());
    }
}
