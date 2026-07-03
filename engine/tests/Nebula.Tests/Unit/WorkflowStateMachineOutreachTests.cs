using Shouldly;
using Nebula.Application.Services;

namespace Nebula.Tests.Unit;

public class WorkflowStateMachineOutreachTests
{
    [Fact]
    public void Underwriter_MayMoveIdentifiedToOutreach_OnMockSendPath()
    {
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Identified", "Outreach", ["Underwriter"])
            .ShouldBeNull();
    }

    [Fact]
    public void Admin_MayMoveIdentifiedToOutreach_OnMockSendPath()
    {
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Identified", "Outreach", ["Admin"])
            .ShouldBeNull();
    }

    [Fact]
    public void Distribution_IsDenied_OnMockSendPath()
    {
        // The general Identified->Outreach owner is Distribution, but the mock-send path is
        // Underwriter-only (ADR-028 §3). Distribution must NOT get in through this path.
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Identified", "Outreach", ["DistributionUser"])
            .ShouldBe("policy_denied");
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Identified", "Outreach", ["DistributionManager"])
            .ShouldBe("policy_denied");
    }

    [Fact]
    public void OnlyIdentifiedToOutreach_IsPermitted_OnThisPath()
    {
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Outreach", "InReview", ["Underwriter"])
            .ShouldBe("invalid_transition");
        WorkflowStateMachine.ValidateOutreachMockSendTransition("Identified", "InReview", ["Underwriter"])
            .ShouldBe("invalid_transition");
    }
}
