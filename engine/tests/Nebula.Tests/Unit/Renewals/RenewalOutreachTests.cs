using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.Renewals;

public class RenewalOutreachTests
{
    // --- draft (F0038-S0005) ---

    [Fact]
    public async Task PersistDraft_PersistsInternalOnlyEvent_AndDoesNotTransition()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Identified"));
        var renewalId = repos.Renewals.Single().Id;

        var (dto, error, _) = await svc.PersistOutreachDraftAsync(renewalId, DraftReq("Hi, your policy renews soon — can we connect?"), Underwriter(user));

        error.ShouldBeNull();
        dto.ShouldNotBeNull();
        dto!.InternalOnly.ShouldBeTrue();
        var evt = repos.Timeline.Events.ShouldHaveSingleItem();
        evt.EventType.ShouldBe("RenewalOutreachDrafted");
        evt.BrokerDescription.ShouldBeNull(); // InternalOnly
        repos.Transitions.Items.ShouldBeEmpty(); // drafting never transitions
        repos.Renewals.Single().CurrentStatus.ShouldBe("Identified");
    }

    [Fact]
    public async Task PersistDraft_RejectsForbiddenContent_PersistsNothing()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Identified"));
        var renewalId = repos.Renewals.Single().Id;

        var (dto, error, _) = await svc.PersistOutreachDraftAsync(renewalId, DraftReq("Your annual premium is due next month."), Underwriter(user));

        dto.ShouldBeNull();
        error.ShouldBe("content_constraint");
        repos.Timeline.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task PersistDraft_InvalidState_WhenRenewalNotIdentifiedOrOutreach()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "InReview"));
        var renewalId = repos.Renewals.Single().Id;

        var (_, error, _) = await svc.PersistOutreachDraftAsync(renewalId, DraftReq("Hi there."), Underwriter(user));
        error.ShouldBe("invalid_state");
    }

    // --- mock-send (F0038-S0006) ---

    [Fact]
    public async Task MockSend_CommitsTransition_AndSimulatedSendEvent_NoSmtp()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Identified"));
        var renewalId = repos.Renewals.Single().Id;

        var (dto, error, _) = await svc.OutreachMockSendAsync(renewalId, MockSendReq("Hi, your policy renews soon — can we connect?"), 0, Underwriter(user));

        error.ShouldBeNull();
        dto.ShouldNotBeNull();
        dto!.Transition.FromState.ShouldBe("Identified");
        dto.Transition.ToState.ShouldBe("Outreach");
        repos.Renewals.Single().CurrentStatus.ShouldBe("Outreach");
        repos.Transitions.Items.ShouldHaveSingleItem();
        var sent = repos.Timeline.Events.ShouldHaveSingleItem();
        sent.EventType.ShouldBe("RenewalOutreachMockSent"); // "sent (simulated)" — no transport invoked
        repos.Uow.CommitCount.ShouldBe(1); // atomic commit
    }

    [Fact]
    public async Task MockSend_DeniesNonUnderwriter_ViaStateMachineDefense()
    {
        // Even if this reaches the service (the Casbin gate is the first line), the state-machine
        // exception is Underwriter/Admin-only — a Distribution assignee is denied.
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Identified"));
        var renewalId = repos.Renewals.Single().Id;

        var (_, error, _) = await svc.OutreachMockSendAsync(renewalId, MockSendReq("Hi."), 0, new TestUser(user, ["DistributionUser"]));

        error.ShouldBe("policy_denied");
        repos.Transitions.Items.ShouldBeEmpty();
        repos.Renewals.Single().CurrentStatus.ShouldBe("Identified");
    }

    [Fact]
    public async Task MockSend_InvalidTransition_WhenNotIdentified()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Outreach"));
        var renewalId = repos.Renewals.Single().Id;

        var (_, error, _) = await svc.OutreachMockSendAsync(renewalId, MockSendReq("Hi."), 0, Underwriter(user));
        error.ShouldBe("invalid_transition");
    }

    [Fact]
    public async Task MockSend_PreconditionFailed_OnRowVersionMismatch()
    {
        var user = Guid.NewGuid();
        var (svc, repos) = NewService();
        repos.Renewals.Seed(NewRenewal(user, "Identified"));
        var renewalId = repos.Renewals.Single().Id;

        var (_, error, _) = await svc.OutreachMockSendAsync(renewalId, MockSendReq("Hi."), 9, Underwriter(user));
        error.ShouldBe("precondition_failed");
    }

    // --- helpers ---

    private sealed record Repos(
        StubRenewalRepository Renewals,
        StubWorkflowTransitionRepository Transitions,
        StubTimelineRepository Timeline,
        StubUnitOfWork Uow);

    private static (RenewalService Svc, Repos Repos) NewService()
    {
        var renewals = new StubRenewalRepository();
        var transitions = new StubWorkflowTransitionRepository();
        var timeline = new StubTimelineRepository();
        var uow = new StubUnitOfWork();
        var svc = new RenewalService(renewals, null!, transitions, timeline, null!, null!, null!, null!, uow);
        return (svc, new Repos(renewals, transitions, timeline, uow));
    }

    private static Renewal NewRenewal(Guid assignedTo, string status) => new()
    {
        Id = Guid.NewGuid(),
        CurrentStatus = status,
        AssignedToUserId = assignedTo,
        AccountDisplayNameAtLink = "Acme Manufacturing",
        AccountStatusAtRead = "Active",
        RowVersion = 0,
    };

    private static OutreachProvenanceDto Provenance() =>
        new(Guid.NewGuid().ToString(), "mock-deterministic-1", "renewal-outreach", "1.0.0", "sha256:abc");

    private static RenewalOutreachDraftRequestDto DraftReq(string body) => new(body, Provenance());

    private static RenewalOutreachMockSendRequestDto MockSendReq(string body) => new(body, Provenance());

    private static ICurrentUserService Underwriter(Guid userId) => new TestUser(userId, ["Underwriter"]);

    private sealed class TestUser(Guid userId, IReadOnlyList<string> roles) : ICurrentUserService
    {
        public Guid UserId => userId;
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => roles;
        public IReadOnlyList<string> Regions => [];
        public string? BrokerTenantId => null;
    }
}
