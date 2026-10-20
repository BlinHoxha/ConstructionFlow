namespace ConstructionFlow.Domain;

public sealed class Supplier : TenantEntity
{
    public string Name { get; private set; } = "";
    public string? RegistrationNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    private Supplier() { }
    public Supplier(Guid tenantId, string name, string? registrationNumber) : base(tenantId)
    {
        Name = Rules.Text(name, 200, "Supplier name");
        RegistrationNumber = registrationNumber is null ? null : Rules.Text(registrationNumber, 60, "Registration number");
    }
}

public sealed class Rfq : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = "";
    public string UnitCode { get; private set; } = "";
    public decimal Quantity { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    private Rfq() { }
    // Initial RFQ has one specification/line. Multi-line RFQs are a later schema migration.
    public Rfq(Guid tenantId, Guid projectId, string description, string unit, decimal quantity, string currency) : base(tenantId)
    {
        ProjectId = projectId; Description = Rules.Text(description, 2000, "Specification");
        UnitCode = PriceRules.Unit(unit); Quantity = PriceRules.Quantity(quantity); CurrencyCode = Rules.Currency(currency);
    }
}

public sealed class SupplierQuote : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RfqId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string Reference { get; private set; } = "";
    public string UnitCode { get; private set; } = "";
    public string CurrencyCode { get; private set; } = "EUR";
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal VatPercent { get; private set; }
    public bool SpecificationConfirmed { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    private SupplierQuote() { }
    public SupplierQuote(Guid tenantId, Guid projectId, Guid rfqId, Guid supplierId, string reference,
        string unit, string currency, decimal quantity, decimal unitPrice, decimal vatPercent,
        bool specificationConfirmed, DateOnly validUntil) : base(tenantId)
    {
        ProjectId = projectId; RfqId = rfqId; SupplierId = supplierId;
        Reference = Rules.Text(reference, 100, "Offer reference"); UnitCode = PriceRules.Unit(unit);
        CurrencyCode = Rules.Currency(currency); Quantity = PriceRules.Quantity(quantity);
        PriceRules.Totals(quantity, unitPrice, vatPercent);
        UnitPrice = unitPrice; VatPercent = vatPercent; SpecificationConfirmed = specificationConfirmed;
        if (validUntil == default) throw new ArgumentException("Offer validity is required.");
        ValidUntil = validUntil;
    }
}

public sealed record PriceTotals(decimal Net, decimal Vat, decimal Gross);

public static class PriceRules
{
    public static readonly string[] Units = ["m3", "m2", "m", "kg", "t", "item", "hour"];
    public static string Unit(string unit)
    {
        unit = Rules.Text(unit, 20, "Unit").ToLowerInvariant();
        if (!Units.Contains(unit)) throw new ArgumentException("Unsupported unit. Normalize units explicitly before comparing.");
        return unit;
    }
    public static decimal Quantity(decimal quantity)
    {
        if (quantity <= 0 || quantity > 1_000_000_000m || decimal.Round(quantity, 4) != quantity)
            throw new ArgumentException("Quantity must be positive with at most four decimal places.");
        return quantity;
    }
    public static PriceTotals Totals(decimal quantity, decimal unitPrice, decimal vatPercent)
    {
        Quantity(quantity);
        if (unitPrice < 0 || unitPrice > 1_000_000_000m || decimal.Round(unitPrice, 4) != unitPrice)
            throw new ArgumentException("Price must be nonnegative with at most four decimal places.");
        if (vatPercent < 0 || vatPercent > 100 || decimal.Round(vatPercent, 2) != vatPercent)
            throw new ArgumentException("VAT must be between 0 and 100 with at most two decimal places.");
        decimal net = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        decimal vat = decimal.Round(net * vatPercent / 100, 2, MidpointRounding.AwayFromZero);
        if (net + vat > 9_999_999_999_999_999.99m) throw new ArgumentException("Total exceeds monetary storage precision.");
        return new(net, vat, net + vat);
    }
}
