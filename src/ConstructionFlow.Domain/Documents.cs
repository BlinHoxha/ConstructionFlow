namespace ConstructionFlow.Domain;

// Reviewed plain-text evidence only. Binary upload, scanning and OCR are separate future adapters.
public sealed class ProjectDocument : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = "";
    public string Language { get; private set; } = "sq";
    public string Sha256 { get; private set; } = "";
    public Guid ReviewedBy { get; private set; }
    public int VersionNo { get; private set; } = 1;
    private ProjectDocument() { }
    public ProjectDocument(Guid id, Guid tenantId, Guid projectId, string title, string language, string hash, Guid reviewer) : base(tenantId)
    {
        Id = id; ProjectId = projectId; Title = Rules.Text(title, 255, "Title");
        Language = Rules.Text(language, 10, "Language"); Sha256 = hash; ReviewedBy = reviewer;
    }
}

public sealed class DocumentChunk : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = "";
    private DocumentChunk() { }
    public DocumentChunk(Guid tenantId, Guid projectId, Guid documentId, int index, string content) : base(tenantId)
    { ProjectId = projectId; DocumentId = documentId; ChunkIndex = index; Content = content; }
}

public sealed class AuditEvent : TenantEntity
{
    public Guid ActorUserId { get; private set; }
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = "";
    private AuditEvent() { }
    public AuditEvent(Guid tenantId, Guid actorId, string entityType, Guid entityId, string action) : base(tenantId)
    { ActorUserId = actorId; EntityType = entityType; EntityId = entityId; Action = action; }
}
