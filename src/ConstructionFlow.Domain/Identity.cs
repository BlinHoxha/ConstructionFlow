using Framework.Domain.Abstractions;

namespace ConstructionFlow.Domain;

public enum TenantRole { Owner, Admin, Accountant, Engineer, Procurement, Viewer }
public enum ProjectRole { Manager, Engineer, Procurement, Accountant, Viewer, Subcontractor }

public sealed class Tenant : BaseEntity<Guid>
{
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public string Locale { get; private set; } = "sq-XK";
    public string DefaultCurrency { get; private set; } = "EUR";
    private Tenant() { }
    public Tenant(Guid id, string name) { Id = id; Name = Rules.Text(name, 200, "Tenant name"); }
}

public sealed class User : BaseEntity<Guid>
{
    public string Subject { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    private User() { }
    public User(Guid id, string subject, string displayName)
    {
        Id = id;
        Subject = Rules.Text(subject, 300, "OIDC subject");
        DisplayName = Rules.Text(displayName, 160, "Display name");
    }
}

public sealed class TenantMembership : TenantEntity
{
    public Guid UserId { get; private set; }
    public TenantRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    private TenantMembership() { }
    public TenantMembership(Guid tenantId, Guid userId, TenantRole role) : base(tenantId)
    { UserId = userId; Role = role; }
}

public sealed class ProjectMember : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    private ProjectMember() { }
    public ProjectMember(Guid tenantId, Guid projectId, Guid userId, ProjectRole role) : base(tenantId)
    { ProjectId = projectId; UserId = userId; Role = role; }
}
