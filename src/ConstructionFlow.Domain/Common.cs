using Framework.Domain.Abstractions;

namespace ConstructionFlow.Domain;

public abstract class TenantEntity : BaseEntity<Guid>
{
    public Guid TenantId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public long RowVersion { get; private set; } = 1;
    protected TenantEntity() { }
    protected TenantEntity(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.");
        Id = Guid.NewGuid();
        TenantId = tenantId;
    }
}

public static class Rules
{
    public static string Text(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"{field} is required and must be at most {max} characters.");
        return value.Trim();
    }

    public static string Currency(string value)
    {
        value = Text(value, 3, "Currency").ToUpperInvariant();
        // Initial pilot supports EUR only; no implicit FX conversion or mixed-currency comparisons.
        if (value != "EUR") throw new ArgumentException("Only EUR is supported in the initial milestone.");
        return value;
    }

    public static void Version(long current, long expected)
    {
        if (current != expected) throw new WorkflowConflict("The record changed. Reload before trying again.");
    }
}

public sealed class WorkflowConflict(string message) : Exception(message);
