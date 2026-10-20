using AIFramework.Application.Services;
using AIFramework.Contracts;
using ConstructionFlow.Domain;

namespace ConstructionFlow.Application;

public sealed class KnowledgeWorkflow(IConstructionStore store, AccessGuard guard, IRequestContext context,
    DocumentIngestionService ingestion, KnowledgeAnswerService answers)
{
    public async Task<KnowledgeDocumentResponse> Index(Guid projectId, IndexDocument request, CancellationToken ct)
    {
        await guard.Project(projectId, ProjectOperation.IngestDocument, ct);
        if (!request.HumanReviewed) throw new ArgumentException("A human must review text before it becomes project evidence.");
        if (request.Language is not ("sq" or "en" or "sr")) throw new ArgumentException("Supported languages: sq, en, sr.");
        if (request.Content is null || request.Content.Length > 100000) throw new ArgumentException("Reviewed text is limited to 100,000 characters.");
        return await ingestion.IngestAsync(new CreateKnowledgeDocumentRequest
        {
            TenantId = context.TenantId, ScopeId = projectId, Profile = ConstructionProfiles.Name,
            Title = Rules.Text(request.Title, 255, "Title"), Content = request.Content, Language = request.Language
        }, ct);
    }
    public async Task<GroundedAnswerResponse> Ask(Guid projectId, AskProject request, CancellationToken ct)
    {
        await guard.Project(projectId, ProjectOperation.Ask, ct);
        if (request.DocumentIds is { Length: > 100 }) throw new ArgumentException("At most 100 document IDs are permitted.");
        if (request.DocumentIds is not null &&
            await store.CountProjectDocuments(projectId, request.DocumentIds, ct) != request.DocumentIds.Distinct().Count())
            throw new KeyNotFoundException("Document was not found.");
        var answer = await answers.AnswerAsync(new AskKnowledgeRequest
        {
            TenantId = context.TenantId, ScopeId = projectId, Profile = ConstructionProfiles.Name,
            Question = request.Question, DocumentIds = request.DocumentIds, MaxResults = request.MaxResults
        }, ct);
        store.Add(new AuditEvent(context.TenantId, context.UserId, nameof(Project), projectId, "question_answered"));
        await store.Save(ct);
        return answer;
    }
}
