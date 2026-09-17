using FluentAssertions;
using NexusCRM.Domain.Identity;

namespace NexusCRM.Domain.Tests.Identity;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void PasswordPolicy_RequiresComplexity()
    {
        var options = new PasswordPolicyOptions();

        PasswordPolicy.IsSatisfiedBy("short", options).Should().BeFalse();
        PasswordPolicy.IsSatisfiedBy("alllowercase1!", options).Should().BeFalse();
        PasswordPolicy.IsSatisfiedBy("ChangeMe!12345", options).Should().BeTrue();
    }

    [Fact]
    public void EnforceRefreshTokenLimits_RevokesOldestActiveSessions()
    {
        var user = User.Create("sec@nexuscrm.local", "Sec", "hash");
        for (var i = 0; i < 5; i++)
        {
            user.IssueRefreshToken($"hash-{i}", "device", "127.0.0.1", TimeSpan.FromDays(14));
        }

        user.EnforceRefreshTokenLimits(2);

        user.RefreshTokens.Count(t => t.IsActive).Should().Be(2);
        user.RefreshTokens.Count(t => t.IsRevoked).Should().Be(3);
    }

    [Fact]
    public void RecordFailedLogin_LocksAfterThreshold()
    {
        var user = User.Create("lock@nexuscrm.local", "Lock", "hash");
        for (var i = 0; i < 4; i++)
        {
            user.RecordFailedLogin(maxFailedAttempts: 5, lockoutMinutes: 15);
            user.IsLockedOut.Should().BeFalse();
        }

        user.RecordFailedLogin(maxFailedAttempts: 5, lockoutMinutes: 15);
        user.IsLockedOut.Should().BeTrue();
    }
}
