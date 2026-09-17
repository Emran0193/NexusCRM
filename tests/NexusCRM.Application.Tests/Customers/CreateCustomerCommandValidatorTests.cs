using FluentAssertions;
using NexusCRM.Application.Customers.Commands;

namespace NexusCRM.Application.Tests.Customers;

public sealed class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        var result = await _validator.ValidateAsync(
            new CreateCustomerCommand("Individual", "Ada Lovelace", "ada@example.com", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_type_fails()
    {
        var result = await _validator.ValidateAsync(
            new CreateCustomerCommand("Alien", "X", null, null));

        result.IsValid.Should().BeFalse();
    }
}
