using FluentAssertions;
using NexusCRM.Domain.Identity;

namespace NexusCRM.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void RecordFailedLogin_LocksAccount_AfterThreshold()
    {
        var user = User.Create("a@b.com", "Ada", "hash");

        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(maxFailedAttempts: 5, lockoutMinutes: 10);
        }

        user.IsLockedOut.Should().BeTrue();
        user.DomainEvents.OfType<UserLockedOutDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void RefreshToken_Rotation_MarksPreviousRevoked()
    {
        var user = User.Create("a@b.com", "Ada", "hash");
        var first = user.IssueRefreshToken("hash-1", "chrome", "127.0.0.1", TimeSpan.FromDays(1));
        var second = user.IssueRefreshToken("hash-2", "chrome", "127.0.0.1", TimeSpan.FromDays(1));

        first.Revoke("Rotated", second.TokenHash);

        first.IsRevoked.Should().BeTrue();
        first.ReplacedByTokenHash.Should().Be("hash-2");
        second.IsActive.Should().BeTrue();
    }

    [Fact]
    public void JoinTenant_SetsDefaultMembership()
    {
        var user = User.Create("a@b.com", "Ada", "hash");
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        user.JoinTenant(tenantId, roleId, isDefault: true);

        user.Memberships.Should().ContainSingle();
        user.Memberships.Single().IsDefault.Should().BeTrue();
        user.GetMembership(tenantId)!.RoleId.Should().Be(roleId);
    }
}
