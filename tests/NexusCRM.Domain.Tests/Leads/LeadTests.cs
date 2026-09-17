using FluentAssertions;
using NexusCRM.Domain.Leads;

namespace NexusCRM.Domain.Tests.Leads;

public sealed class LeadTests
{
    [Fact]
    public void MoveToStage_RaisesEvent_AndUpdatesStatus()
    {
        var pipelineId = Guid.NewGuid();
        var stageA = Guid.NewGuid();
        var stageWon = Guid.NewGuid();

        var lead = Lead.Create(Guid.NewGuid(), pipelineId, stageA, "Acme opportunity", source: "Web");
        lead.MoveToStage(stageWon, isWon: true, isLost: false);

        lead.StageId.Should().Be(stageWon);
        lead.Status.Should().Be(LeadStatus.Won);
        lead.DomainEvents.OfType<LeadStageChangedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void SetScore_RejectsOutOfRange()
    {
        var lead = Lead.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Lead");
        var act = () => lead.SetScore(140);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
