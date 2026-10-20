using AIFramework.Core.Abstractions;
using AIFramework.Core.Models;

namespace ConstructionFlow.Application;

public sealed class ConstructionProfiles : IDocumentProfileCatalog
{
    public const string Name = "construction-project";
    private static readonly DocumentProfile Profile = new(Name,
        "Reviewed construction project evidence; financial, contractual and engineering decisions require human review.",
        ["ingest", "question-answering"], ["sq", "en", "sr"], true);
    public IReadOnlyCollection<DocumentProfile> GetAll() => [Profile];
    public DocumentProfile GetRequired(string name) => name == Name ? Profile
        : throw new ArgumentException("Unknown construction document profile.");
}
