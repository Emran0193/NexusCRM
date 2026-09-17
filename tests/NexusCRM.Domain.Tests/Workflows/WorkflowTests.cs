using FluentAssertions;
using NexusCRM.Domain.Workflows;

namespace NexusCRM.Domain.Tests.Workflows;

public sealed class WorkflowTests
{
    [Fact]
    public void Matches_RequiresAllConditions()
    {
        var workflow = WorkflowDefinition.Create(
            Guid.NewGuid(),
            "Qualified follow-up",
            "demo",
            WorkflowTriggerType.LeadStageChanged);

        workflow.AddCondition("ToStageName", WorkflowOperator.Equals, "Qualified");
        workflow.AddCondition("Score", WorkflowOperator.GreaterThan, "50");
        workflow.AddAction(WorkflowActionType.LogNotification, value: "hi");

        workflow.Matches(new Dictionary<string, string?>
        {
            ["ToStageName"] = "Qualified",
            ["Score"] = "80"
        }).Should().BeTrue();

        workflow.Matches(new Dictionary<string, string?>
        {
            ["ToStageName"] = "Qualified",
            ["Score"] = "10"
        }).Should().BeFalse();
    }

    [Fact]
    public void DisabledWorkflow_DoesNotMatch()
    {
        var workflow = WorkflowDefinition.Create(
            Guid.NewGuid(),
            "Disabled",
            "demo",
            WorkflowTriggerType.DealStageChanged);

        workflow.AddAction(WorkflowActionType.LogNotification, value: "x");
        workflow.Disable();

        workflow.Matches(new Dictionary<string, string?>()).Should().BeFalse();
    }
}
