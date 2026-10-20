using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AIFramework.Core.Abstractions;
using AIFramework.Core.Models;
using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Infrastructure;

// Durable, bounded lexical baseline. pgvector/hybrid search remains a later measured adapter.
public sealed class ProjectKnowledgeStore(ConstructionDbContext db, IRequestContext context)
    : IKnowledgeIndex, IKnowledgeRetriever
{
    public async Task IndexAsync(KnowledgeDocument document, IReadOnlyCollection<string> chunks, CancellationToken cancellationToken)
    {
        if (document.TenantId != context.TenantId || document.ScopeId is not Guid projectId || context.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authorized project scope is required.");
        if (document.Profile != ConstructionProfiles.Name) throw new ArgumentException("Unknown profile.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", chunks))));
        if (await db.Documents.AnyAsync(x => x.ProjectId == projectId && x.Sha256 == hash, cancellationToken))
            throw new WorkflowConflict("This reviewed text has already been indexed in the project.");
        db.Documents.Add(new ProjectDocument(document.Id, context.TenantId, projectId, document.Title,
            document.Language, hash, context.UserId));
        db.Chunks.AddRange(chunks.Select((content, index) =>
            new DocumentChunk(context.TenantId, projectId, document.Id, index, content)));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<KnowledgeChunk>> SearchAsync(KnowledgeSearchQuery query, CancellationToken cancellationToken)
    {
        if (query.TenantId != context.TenantId || query.ScopeId is not Guid projectId || context.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authorized project scope is required.");
        if (query.Profile != ConstructionProfiles.Name) return [];
        var terms = Regex.Matches(query.Question.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}")
            .Select(x => x.Value).Distinct().Take(12).ToArray();
        if (terms.Length == 0 || query.DocumentIds is { Count: 0 }) return [];
        var source = db.Chunks.AsNoTracking().Where(x => x.ProjectId == projectId);
        if (query.DocumentIds is not null) source = source.Where(x => query.DocumentIds.Contains(x.DocumentId));
        // OR search built as an expression so filtering occurs in PostgreSQL, before the candidate limit.
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(DocumentChunk), "chunk");
        System.Linq.Expressions.Expression predicate = System.Linq.Expressions.Expression.Constant(false);
        var content = System.Linq.Expressions.Expression.Call(
            System.Linq.Expressions.Expression.Property(parameter, nameof(DocumentChunk.Content)), nameof(string.ToLower), Type.EmptyTypes);
        foreach (var term in terms)
            predicate = System.Linq.Expressions.Expression.OrElse(predicate,
                System.Linq.Expressions.Expression.Call(content, nameof(string.Contains), Type.EmptyTypes,
                    System.Linq.Expressions.Expression.Constant(term)));
        source = source.Where(System.Linq.Expressions.Expression.Lambda<Func<DocumentChunk, bool>>(predicate, parameter));
        var candidates = await source.OrderBy(x => x.DocumentId).ThenBy(x => x.ChunkIndex).Take(200)
            .Join(db.Documents.AsNoTracking().Where(x => x.ProjectId == projectId), c => c.DocumentId, d => d.Id,
                (c, d) => new { Chunk = c, Document = d }).ToArrayAsync(cancellationToken);
        return candidates.Select(x => new KnowledgeChunk(x.Chunk.Id.ToString(), x.Document.Id, x.Chunk.ChunkIndex,
                x.Document.Title, x.Chunk.Content, ConstructionProfiles.Name, context.TenantId, projectId,
                x.Document.Language, null, terms.Count(t => x.Chunk.Content.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(x => x.Score).ThenBy(x => x.DocumentId).ThenBy(x => x.ChunkNumber)
            .Take(query.MaxResults).ToArray();
    }
}
