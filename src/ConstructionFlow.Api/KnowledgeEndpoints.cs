using ConstructionFlow.Application;
using ConstructionFlow.Domain;
using ConstructionFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ConstructionFlow.Api;

public static class KnowledgeEndpoints
{
    public static void MapKnowledge(this RouteGroupBuilder api)
    {
        api.MapPost("/projects/{id:guid}/documents/reviewed-text", async (Guid id, IndexDocument request, KnowledgeWorkflow workflow, CancellationToken ct) =>
        {
            var result = await workflow.Index(id, request, ct);
            return Results.Created($"/api/v1/projects/{id}/documents/{result.DocumentId}/chunks", result);
        }).RequireRateLimiting("ai");
        api.MapGet("/projects/{id:guid}/documents", async (Guid id, AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Project(id, ProjectOperation.Read, ct);
            return Results.Ok(await db.Documents.AsNoTracking().Where(x => x.ProjectId == id).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(100)
                .Select(x => new { x.Id, x.Title, x.Language, x.VersionNo, x.ReviewedBy, x.CreatedAt }).ToArrayAsync(ct));
        });
        api.MapGet("/projects/{id:guid}/documents/{documentId:guid}/chunks", async (Guid id, Guid documentId,
            AccessGuard guard, ConstructionDbContext db, CancellationToken ct) =>
        {
            await guard.Project(id, ProjectOperation.Read, ct);
            if (!await db.Documents.AnyAsync(x => x.Id == documentId && x.ProjectId == id, ct)) throw new KeyNotFoundException("Document was not found.");
            return Results.Ok(await db.Chunks.AsNoTracking().Where(x => x.DocumentId == documentId && x.ProjectId == id)
                .OrderBy(x => x.ChunkIndex).Select(x => new { x.ChunkIndex, x.Content }).ToArrayAsync(ct));
        });
        api.MapPost("/projects/{id:guid}/ask", async (Guid id, AskProject request, KnowledgeWorkflow workflow, CancellationToken ct) =>
        {
            var answer = await workflow.Ask(id, request, ct);
            return Results.Ok(new { answer.Answer, answer.Grounded, answer.Provider,
                citations = answer.Citations.Select(c => new { c.DocumentId, c.Title, c.Excerpt, c.ChunkNumber,
                    sourcePath = $"/api/v1/projects/{id}/documents/{c.DocumentId}/chunks" }),
                requiresHumanReview = true });
        }).RequireRateLimiting("ai");
    }
}
