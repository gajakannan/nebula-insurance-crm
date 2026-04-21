using Shouldly;
using Nebula.Application.Services;

namespace Nebula.Tests.Unit;

public class WorkflowStateMachineTests
{
    [Theory]
    [InlineData("Submission", "Received", "Triaging")]
    [InlineData("Submission", "BindRequested", "Bound")]
    [InlineData("Renewal", "Identified", "Outreach")]
    [InlineData("Renewal", "Quoted", "Completed")]
    public void IsValidTransition_WithAllowedTransition_ReturnsTrue(string workflowType, string from, string to)
    {
        WorkflowStateMachine.IsValidTransition(workflowType, from, to).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Submission", "Bound", "Quoted")]
    [InlineData("Submission", "Received", "WaitingOnBroker")]
    [InlineData("Submission", "Quoted", "Bound")]
    [InlineData("Renewal", "Completed", "Quoted")]
    [InlineData("Renewal", "Identified", "Completed")]
    [InlineData("Unknown", "Open", "Closed")]
    public void IsValidTransition_WithDisallowedTransition_ReturnsFalse(string workflowType, string from, string to)
    {
        WorkflowStateMachine.IsValidTransition(workflowType, from, to).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Submission", "Bound", true)]
    [InlineData("Submission", "Received", false)]
    [InlineData("Renewal", "Completed", true)]
    [InlineData("Renewal", "Identified", false)]
    [InlineData("Unknown", "Anything", false)]
    public void IsTerminalState_ReturnsExpectedValue(string workflowType, string state, bool expected)
    {
        WorkflowStateMachine.IsTerminalState(workflowType, state).ShouldBe(expected);
    }

    [Fact]
    public void GetAvailableTransitions_Submission_ReturnsConfiguredTargets()
    {
        var transitions = WorkflowStateMachine.GetAvailableTransitions("Submission", "Triaging");

        transitions.ShouldBe(["ReadyForUWReview", "WaitingOnBroker"]);
    }

    [Fact]
    public void ValidateRenewalTransition_ForDistributionUserIntoUnderwriterStage_ReturnsPolicyDenied()
    {
        var result = WorkflowStateMachine.ValidateRenewalTransition(
            "InReview",
            "Quoted",
            ["DistributionUser"]);

        result.ShouldBe("policy_denied");
    }

    [Fact]
    public void GetAvailableRenewalTransitions_FiltersByRole()
    {
        var transitions = WorkflowStateMachine.GetAvailableRenewalTransitions(
            "InReview",
            ["Underwriter"]);

        transitions.ShouldBe(["Lost", "Quoted"]);
    }
}
