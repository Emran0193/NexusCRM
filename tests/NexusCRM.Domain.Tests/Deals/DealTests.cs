using FluentAssertions;
using NexusCRM.Domain.Deals;

namespace NexusCRM.Domain.Tests.Deals;

public sealed class DealTests
{
    [Fact]
    public void MoveToWon_ClosesDeal_AndBumpsRowVersion()
    {
        var deal = Deal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Enterprise license",
            250000m);

        var before = deal.RowVersion.ToArray();
        deal.MoveToStage(Guid.NewGuid(), isWon: true, isLost: false);

        deal.Status.Should().Be(DealStatus.Won);
        deal.ClosedAtUtc.Should().NotBeNull();
        deal.RowVersion.Should().NotEqual(before);
        deal.DomainEvents.OfType<DealStageChangedDomainEvent>().Should().ContainSingle();
    }
}
