using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Plugins.Samples;

/// <summary>Tags leads that already look high-value based on score or qualified stage.</summary>
public sealed class HighValueLeadTaggerPlugin : ILeadEnrichmentPlugin
{
    public string Id => "samples.high-value-lead-tagger";

    public string Name => "High-value lead tagger";

    public string Version => "1.0.0";

    public string Description => "Adds a high-value tag when score ≥ 70 or the stage is Qualified.";

    public IReadOnlyList<string> Capabilities { get; } = [PluginCapabilities.LeadEnrichment];

    public Task<LeadEnrichmentResult> EnrichAsync(
        LeadEnrichmentContext context,
        CancellationToken cancellationToken = default)
    {
        var isQualified = context.StageName.Contains("Qualified", StringComparison.OrdinalIgnoreCase);
        if (context.Score < 70 && !isQualified)
        {
            return Task.FromResult(new LeadEnrichmentResult());
        }

        return Task.FromResult(new LeadEnrichmentResult(
            TagsToAdd: ["high-value"],
            NoteToAppend: $"[plugin:{Id}] Marked high-value (score={context.Score}, stage={context.StageName})."));
    }
}
