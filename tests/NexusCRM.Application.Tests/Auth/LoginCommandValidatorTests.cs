using FluentAssertions;
using NexusCRM.Application.Auth;

namespace NexusCRM.Application.Tests.Auth;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Valid_login_passes()
    {
        var result = await _validator.ValidateAsync(new LoginCommand(
            "admin@nexuscrm.local",
            "ChangeMe!12345",
            null,
            null,
            null,
            null,
            null,
            null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Short_password_fails()
    {
        var result = await _validator.ValidateAsync(new LoginCommand(
            "admin@nexuscrm.local",
            "short",
            null,
            null,
            null,
            null,
            null,
            null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_email_fails()
    {
        var result = await _validator.ValidateAsync(new LoginCommand(
            "not-an-email",
            "ChangeMe!12345",
            null,
            null,
            null,
            null,
            null,
            null));

        result.IsValid.Should().BeFalse();
    }
}
