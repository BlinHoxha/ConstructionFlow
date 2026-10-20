using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ConstructionFlow.Infrastructure;

public sealed class ConstructionDbContext(DbContextOptions<ConstructionDbContext> options, IRequestContext request)
    : FrameworkDbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<SupplierQuote> Quotes => Set<SupplierQuote>();
    public DbSet<DailyReport> Reports => Set<DailyReport>();
    public DbSet<ProjectDocument> Documents => Set<ProjectDocument>();
    public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Tenant>().ToTable("tenants");
        model.Entity<Tenant>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<User>().ToTable("users");
        model.Entity<User>().HasIndex(x => x.Subject).IsUnique();
        model.Entity<User>().Property(x => x.Subject).HasMaxLength(300);
        model.Entity<User>().Property(x => x.DisplayName).HasMaxLength(160);
        Scoped<TenantMembership>(model, "tenant_memberships");
        Scoped<ProjectMember>(model, "project_members");
        Scoped<Project>(model, "projects");
        Scoped<Supplier>(model, "suppliers");
        Scoped<Rfq>(model, "rfqs");
        Scoped<SupplierQuote>(model, "supplier_quotes");
        Scoped<DailyReport>(model, "daily_reports");
        Scoped<ProjectDocument>(model, "documents");
        Scoped<DocumentChunk>(model, "document_chunks");
        Scoped<AuditEvent>(model, "audit_events");

        model.Entity<TenantMembership>().HasAlternateKey(x => new { x.TenantId, x.UserId });
        model.Entity<TenantMembership>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        model.Entity<ProjectMember>().HasIndex(x => new { x.TenantId, x.ProjectId, x.UserId }).IsUnique();
        model.Entity<ProjectMember>().HasOne<TenantMembership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId }).HasPrincipalKey(x => new { x.TenantId, x.UserId });
        model.Entity<Project>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        model.Entity<Project>().HasIndex(x => new { x.TenantId, x.Status });
        model.Entity<Project>().Property(x => x.Code).HasMaxLength(40);
        model.Entity<Project>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<Project>().Property(x => x.LocationText).HasMaxLength(300);
        model.Entity<Supplier>().Property(x => x.Name).HasMaxLength(200);
        model.Entity<Supplier>().Property(x => x.RegistrationNumber).HasMaxLength(60);
        ProjectForeignKey<ProjectMember>(model);
        ProjectForeignKey<Rfq>(model);
        ProjectForeignKey<SupplierQuote>(model);
        ProjectForeignKey<DailyReport>(model);
        ProjectForeignKey<ProjectDocument>(model);
        ProjectForeignKey<DocumentChunk>(model);
        model.Entity<Rfq>().HasAlternateKey(x => new { x.TenantId, x.ProjectId, x.Id });
        model.Entity<Rfq>().Property(x => x.Quantity).HasPrecision(18, 4);
        model.Entity<SupplierQuote>().HasOne<Rfq>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProjectId, x.RfqId })
            .HasPrincipalKey(x => new { x.TenantId, x.ProjectId, x.Id });
        model.Entity<SupplierQuote>().HasOne<Supplier>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id });
        model.Entity<SupplierQuote>().HasIndex(x => new { x.TenantId, x.RfqId, x.SupplierId, x.Reference }).IsUnique();
        model.Entity<SupplierQuote>().Property(x => x.Quantity).HasPrecision(18, 4);
        model.Entity<SupplierQuote>().Property(x => x.UnitPrice).HasPrecision(18, 4);
        model.Entity<SupplierQuote>().Property(x => x.VatPercent).HasPrecision(5, 2);
        model.Entity<SupplierQuote>().Property(x => x.Reference).HasMaxLength(100);
        model.Entity<DailyReport>().HasIndex(x => new { x.TenantId, x.ProjectId, x.ReportDate });
        model.Entity<DailyReport>().HasOne<TenantMembership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AuthorUserId }).HasPrincipalKey(x => new { x.TenantId, x.UserId });
        model.Entity<DailyReport>().HasOne<TenantMembership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReviewedBy }).HasPrincipalKey(x => new { x.TenantId, x.UserId });
        model.Entity<ProjectDocument>().HasAlternateKey(x => new { x.TenantId, x.ProjectId, x.Id });
        model.Entity<ProjectDocument>().HasOne<TenantMembership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ReviewedBy }).HasPrincipalKey(x => new { x.TenantId, x.UserId });
        model.Entity<ProjectDocument>().Property(x => x.Title).HasMaxLength(255);
        model.Entity<ProjectDocument>().Property(x => x.Sha256).HasMaxLength(64);
        model.Entity<ProjectDocument>().HasIndex(x => new { x.TenantId, x.ProjectId, x.Sha256 }).IsUnique();
        model.Entity<DocumentChunk>().HasOne<ProjectDocument>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ProjectId, x.DocumentId })
            .HasPrincipalKey(x => new { x.TenantId, x.ProjectId, x.Id });
        model.Entity<DocumentChunk>().HasIndex(x => new { x.TenantId, x.DocumentId, x.ChunkIndex }).IsUnique();
        model.Entity<AuditEvent>().HasOne<TenantMembership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ActorUserId }).HasPrincipalKey(x => new { x.TenantId, x.UserId });
        model.Entity<AuditEvent>().HasIndex(x => new { x.TenantId, x.CreatedAt });
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(Snake(property.Name));
                if (property.ClrType.IsEnum) property.SetProviderClrType(typeof(string));
            }
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private void Scoped<T>(ModelBuilder model, string table) where T : TenantEntity
    {
        var entity = model.Entity<T>();
        entity.HasBaseType((Type?)null);
        entity.ToTable(table);
        entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.TenantId, x.Id });
        entity.Property(x => x.RowVersion).IsConcurrencyToken();
        entity.HasQueryFilter(x => x.TenantId == request.TenantId);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId);
    }

    private static void ProjectForeignKey<T>(ModelBuilder model) where T : TenantEntity =>
        model.Entity<T>().HasOne<Project>().WithMany().HasForeignKey("TenantId", "ProjectId")
            .HasPrincipalKey(x => new { x.TenantId, x.Id });

    private static string Snake(string value) => string.Concat(value.Select((c, i) =>
        i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { Prepare(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { Prepare(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

    private void Prepare()
    {
        ChangeTracker.DetectChanges();
        var changed = ChangeTracker.Entries<TenantEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach (var entry in changed)
        {
            if (request.UserId == Guid.Empty || request.TenantId == Guid.Empty || entry.Entity.TenantId != request.TenantId)
                throw new UnauthorizedAccessException("Tenant-scoped writes require an authenticated workspace context.");
            if (entry.State != EntityState.Added && entry.Property(x => x.TenantId).OriginalValue != request.TenantId)
                throw new UnauthorizedAccessException("Tenant cannot be changed.");
            if (entry.State == EntityState.Deleted) throw new WorkflowConflict("Historical records cannot be deleted through this context.");
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is AuditEvent or ProjectDocument or DocumentChunk or SupplierQuote)
                    throw new WorkflowConflict("Evidence, offers and audit history are immutable.");
                entry.Property(x => x.RowVersion).CurrentValue = checked(entry.Property(x => x.RowVersion).OriginalValue + 1);
            }
            if (entry.Entity is not (AuditEvent or DocumentChunk))
                AuditEvents.Add(new AuditEvent(request.TenantId, request.UserId, entry.Entity.GetType().Name,
                    entry.Entity.Id, entry.State == EntityState.Added ? "created" : "changed"));
        }
    }
}
