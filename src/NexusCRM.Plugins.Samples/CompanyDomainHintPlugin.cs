using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Plugins.Samples;

/// <summary>Infers a company hint from the email domain when company name is missing.</summary>
public sealed class CompanyDomainHintPlugin : ILeadEnrichmentPlugin
{
    public string Id => "samples.company-domain-hint";

    public string Name => "Company domain hint";

    public string Version => "1.0.0";

    public string Description => "Suggests a company tag from the lead email domain when company is empty.";

    public IReadOnlyList<string> Capabilities { get; } = [PluginCapabilities.LeadEnrichment];

    public Task<LeadEnrichmentResult> EnrichAsync(
        LeadEnrichmentContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(context.CompanyName) || string.IsNullOrWhiteSpace(context.Email))
        {
            return Task.FromResult(new LeadEnrichmentResult());
        }

        var at = context.Email.IndexOf('@');
        if (at < 0 || at >= context.Email.Length - 1)
        {
            return Task.FromResult(new LeadEnrichmentResult());
        }

        var domain = context.Email[(at + 1)..].Trim().ToLowerInvariant();
        if (domain is "gmail.com" or "outlook.com" or "hotmail.com" or "yahoo.com" or "example.com")
        {
            return Task.FromResult(new LeadEnrichmentResult());
        }

        var companyHint = domain.Split('.')[0];
        return Task.FromResult(new LeadEnrichmentResult(
            TagsToAdd: [$"domain:{companyHint}"],
            NoteToAppend: $"[plugin:{Id}] Inferred domain hint '{companyHint}' from email."));
    }
}
