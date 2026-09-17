using FluentAssertions;
using NexusCRM.Application.Search;

namespace NexusCRM.Application.Tests.Search;

public sealed class GlobalSearchQueryValidatorTests
{
    private readonly GlobalSearchQueryValidator _validator = new();

    [Fact]
    public async Task Valid_take_passes()
    {
        var result = await _validator.ValidateAsync(new GlobalSearchQuery("Acme", 8));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Take_above_max_fails()
    {
        var result = await _validator.ValidateAsync(new GlobalSearchQuery("Acme", 100));
        result.IsValid.Should().BeFalse();
    }
}
