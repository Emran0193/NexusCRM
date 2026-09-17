using FluentAssertions;
using NexusCRM.Plugins.Abstractions;
using NexusCRM.Plugins.Samples;

namespace NexusCRM.Application.Tests.Plugins;

public sealed class SamplePluginTests
{
    [Fact]
    public async Task HighValueLeadTagger_tags_qualified_or_high_score()
    {
        var plugin = new HighValueLeadTaggerPlugin();
        var context = new LeadEnrichmentContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Acme",
            "a@acme.io",
            "Acme",
            "web",
            80,
            "Open",
            "New",
            []);

        var result = await plugin.EnrichAsync(context);
        result.TagsToAdd.Should().Contain("high-value");
    }

    [Fact]
    public async Task CompanyDomainHint_skips_public_email_domains()
    {
        var plugin = new CompanyDomainHintPlugin();
        var context = new LeadEnrichmentContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Solo",
            "me@gmail.com",
            null,
            null,
            10,
            "Open",
            "New",
            []);

        var result = await plugin.EnrichAsync(context);
        result.TagsToAdd.Should().BeNullOrEmpty();
    }
}
