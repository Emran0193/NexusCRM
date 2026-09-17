using FluentAssertions;
using NexusCRM.Application.Plugins;

namespace NexusCRM.Application.Tests.Plugins;

public sealed class TogglePluginCommandValidatorTests
{
    private readonly TogglePluginCommandValidator _validator = new();

    [Fact]
    public void Accepts_valid_plugin_id()
    {
        var result = _validator.Validate(new TogglePluginCommand("samples.high-value-lead-tagger", true));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_empty_plugin_id()
    {
        var result = _validator.Validate(new TogglePluginCommand(" ", false));
        result.IsValid.Should().BeFalse();
    }
}
