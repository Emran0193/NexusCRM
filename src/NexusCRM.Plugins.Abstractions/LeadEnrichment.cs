namespace NexusCRM.Plugins.Abstractions;

public interface ILeadEnrichmentPlugin : INexusPlugin
{
    Task<LeadEnrichmentResult> EnrichAsync(LeadEnrichmentContext context, CancellationToken cancellationToken = default);
}

public sealed record LeadEnrichmentContext(
    Guid TenantId,
    Guid LeadId,
    string Title,
    string? Email,
    string? CompanyName,
    string? Source,
    int Score,
    string Status,
    string StageName,
    IReadOnlyCollection<string> ExistingTags);

public sealed record LeadEnrichmentResult(
    int? SuggestedScore = null,
    IReadOnlyList<string>? TagsToAdd = null,
    string? NoteToAppend = null);
