using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit;

public class WorkflowServiceTests
{
    private readonly StubWorkflowTransitionRepository _transitionRepo = new();
    private readonly StubTimelineRepository _timelineRepo = new();
    private readonly StubUnitOfWork _unitOfWork = new();
    private readonly StubCurrentUserService _user = new(Guid.Parse("bbbb0000-0000-0000-0000-000000000001"));

    [Fact]
    public async Task SubmissionService_GetByIdAsync_MapsDto()
    {
        var repo = new StubSubmissionRepository();
        var now = DateTime.UtcNow;
        var account = NewAccount(now);
        var broker = NewBroker(now);
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        var submission = new Submission
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            ProgramId = Guid.NewGuid(),
            LineOfBusiness = "Cyber",
            CurrentStatus = "Received",
            EffectiveDate = now.Date.AddDays(30),
            ExpirationDate = now.Date.AddDays(395),
            PremiumEstimate = 125000m,
            AssignedToUserId = _user.UserId,
            Account = account,
            Broker = broker,
            AssignedToUser = assignee,
            CreatedAt = now,
            CreatedByUserId = _user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = _user.UserId,
        };
        repo.Seed(submission);

        var service = CreateSubmissionService(repo, assignee);

        var result = await service.GetByIdAsync(submission.Id, _user);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(submission.Id);
        result.CurrentStatus.ShouldBe("Received");
        result.LineOfBusiness.ShouldBe("Cyber");
    }

    [Fact]
    public async Task SubmissionService_GetTransitionsAsync_MapsTransitions()
    {
        var repo = new StubSubmissionRepository();
        var submissionId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow.AddDays(-1);
        _transitionRepo.Seed(new WorkflowTransition
        {
            WorkflowType = "Submission",
            EntityId = submissionId,
            FromState = "Received",
            ToState = "Triaging",
            Reason = "Initial review",
            ActorUserId = _user.UserId,
            OccurredAt = occurredAt,
        });

        var service = CreateSubmissionService(repo);

        var result = await service.GetTransitionsAsync(submissionId);

        result.Count().ShouldBe(1);
        result[0].WorkflowType.ShouldBe("Submission");
        result[0].Reason.ShouldBe("Initial review");
    }

    [Fact]
    public async Task SubmissionService_TransitionAsync_NotFound_ReturnsNotFound()
    {
        var repo = new StubSubmissionRepository();
        var service = CreateSubmissionService(repo);

        var (result, error, _) = await service.TransitionAsync(
            Guid.NewGuid(),
            new WorkflowTransitionRequestDto("Triaging", "review"),
            0,
            _user);

        result.ShouldBeNull();
        error.ShouldBe("not_found");
    }

    [Fact]
    public async Task SubmissionService_TransitionAsync_InvalidTransition_ReturnsInvalidTransition()
    {
        var repo = new StubSubmissionRepository();
        var now = DateTime.UtcNow;
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        repo.Seed(new Submission
        {
            Account = NewAccount(now),
            Broker = NewBroker(now),
            AssignedToUser = assignee,
            AccountId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            CurrentStatus = "Received",
            EffectiveDate = now.Date,
            PremiumEstimate = 50000m,
            AssignedToUserId = _user.UserId,
            CreatedAt = now,
            CreatedByUserId = _user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateSubmissionService(repo, assignee);
        var seeded = repo.Single();

        var (result, error, _) = await service.TransitionAsync(
            seeded.Id,
            new WorkflowTransitionRequestDto("Bound", "too early"),
            seeded.RowVersion,
            _user);

        result.ShouldBeNull();
        error.ShouldBe("invalid_transition");
        seeded.CurrentStatus.ShouldBe("Received");
        _transitionRepo.Items.ShouldBeEmpty();
        _timelineRepo.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmissionService_TransitionAsync_ValidTransition_PersistsTransitionAndTimeline()
    {
        var repo = new StubSubmissionRepository();
        var now = DateTime.UtcNow;
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        repo.Seed(new Submission
        {
            Account = NewAccount(now),
            Broker = NewBroker(now),
            AssignedToUser = assignee,
            AccountId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            CurrentStatus = "Received",
            EffectiveDate = now.Date,
            PremiumEstimate = 50000m,
            AssignedToUserId = _user.UserId,
            CreatedAt = now.AddDays(-3),
            CreatedByUserId = _user.UserId,
            UpdatedAt = now.AddDays(-3),
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateSubmissionService(repo, assignee);
        var seeded = repo.Single();

        var (result, error, _) = await service.TransitionAsync(
            seeded.Id,
            new WorkflowTransitionRequestDto("Triaging", "queue"),
            seeded.RowVersion,
            _user);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.FromState.ShouldBe("Received");
        result.ToState.ShouldBe("Triaging");
        seeded.CurrentStatus.ShouldBe("Triaging");
        seeded.UpdatedByUserId.ShouldBe(_user.UserId);
        _transitionRepo.Items.Count.ShouldBe(1);
        var timelineEvent = _timelineRepo.Events.ShouldHaveSingleItem();
        timelineEvent.EntityType.ShouldBe("Submission");
        timelineEvent.EventType.ShouldBe("SubmissionTransitioned");
        timelineEvent.ActorUserId.ShouldBe(_user.UserId);
        timelineEvent.EventDescription.ShouldContain("Note: queue");
    }

    [Fact]
    public async Task SubmissionService_UpdateAsync_ClearOptionalFields_SetsNullAndTracksChanges()
    {
        var repo = new StubSubmissionRepository();
        var now = DateTime.UtcNow;
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        repo.Seed(new Submission
        {
            Account = NewAccount(now),
            Broker = NewBroker(now),
            AssignedToUser = assignee,
            AccountId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            LineOfBusiness = "Cyber",
            CurrentStatus = "Received",
            EffectiveDate = now.Date.AddDays(30),
            ExpirationDate = now.Date.AddDays(395),
            PremiumEstimate = 125000m,
            Description = "Existing submission notes",
            AssignedToUserId = _user.UserId,
            CreatedAt = now.AddDays(-2),
            CreatedByUserId = _user.UserId,
            UpdatedAt = now.AddDays(-2),
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateSubmissionService(repo, assignee);
        var seeded = repo.Single();

        var (result, error) = await service.UpdateAsync(
            seeded.Id,
            new SubmissionUpdateDto(null, null, null, null, null, null),
            Fields("programId", "lineOfBusiness", "expirationDate", "premiumEstimate", "description"),
            seeded.RowVersion,
            _user);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.ProgramId.ShouldBeNull();
        result.LineOfBusiness.ShouldBeNull();
        result.ExpirationDate.ShouldBeNull();
        result.PremiumEstimate.ShouldBeNull();
        result.Description.ShouldBeNull();
        seeded.ProgramId.ShouldBeNull();
        seeded.LineOfBusiness.ShouldBeNull();
        seeded.ExpirationDate.ShouldBeNull();
        seeded.PremiumEstimate.ShouldBeNull();
        seeded.Description.ShouldBeNull();

        var timelineEvent = _timelineRepo.Events.ShouldHaveSingleItem();
        timelineEvent.EventType.ShouldBe("SubmissionUpdated");
        timelineEvent.EventPayloadJson.ShouldContain("\"programId\"");
        timelineEvent.EventPayloadJson.ShouldContain("\"description\"");
    }

    [Fact]
    public async Task SubmissionService_UpdateAsync_OmittedOptionalFields_PreserveExistingValues()
    {
        var repo = new StubSubmissionRepository();
        var now = DateTime.UtcNow;
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        var existingProgramId = Guid.NewGuid();
        repo.Seed(new Submission
        {
            Account = NewAccount(now),
            Broker = NewBroker(now),
            AssignedToUser = assignee,
            AccountId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            ProgramId = existingProgramId,
            LineOfBusiness = "Cyber",
            CurrentStatus = "Received",
            EffectiveDate = now.Date.AddDays(30),
            ExpirationDate = now.Date.AddDays(395),
            PremiumEstimate = 125000m,
            Description = "Existing submission notes",
            AssignedToUserId = _user.UserId,
            CreatedAt = now.AddDays(-2),
            CreatedByUserId = _user.UserId,
            UpdatedAt = now.AddDays(-2),
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateSubmissionService(repo, assignee);
        var seeded = repo.Single();
        var updatedEffectiveDate = seeded.EffectiveDate.AddDays(10);

        var (result, error) = await service.UpdateAsync(
            seeded.Id,
            new SubmissionUpdateDto(null, null, updatedEffectiveDate, null, null, null),
            Fields("effectiveDate"),
            seeded.RowVersion,
            _user);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.EffectiveDate.ShouldBe(updatedEffectiveDate);
        result.ProgramId.ShouldBe(existingProgramId);
        result.LineOfBusiness.ShouldBe("Cyber");
        result.ExpirationDate.ShouldNotBeNull();
        result.PremiumEstimate.ShouldBe(125000m);
        result.Description.ShouldBe("Existing submission notes");
    }

    [Fact]
    public async Task RenewalService_GetByIdAsync_MapsDto()
    {
        var repo = new StubRenewalRepository();
        var now = DateTime.UtcNow;
        var account = NewAccount(now);
        var broker = NewBroker(now);
        var policy = NewPolicy(now, account, broker);
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        var renewal = new Renewal
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            PolicyId = policy.Id,
            LineOfBusiness = "Property",
            CurrentStatus = "Identified",
            PolicyExpirationDate = now.Date.AddDays(45),
            TargetOutreachDate = now.Date.AddDays(5),
            AssignedToUserId = _user.UserId,
            Account = account,
            Broker = broker,
            Policy = policy,
            AssignedToUser = assignee,
            CreatedAt = now,
            CreatedByUserId = _user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = _user.UserId,
        };
        repo.Seed(renewal);

        var service = CreateRenewalService(repo);

        var result = await service.GetByIdAsync(renewal.Id, _user);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(renewal.Id);
        result.CurrentStatus.ShouldBe("Identified");
        result.PolicyId.ShouldBe(renewal.PolicyId);
        result.PolicyExpirationDate.ShouldBe(renewal.PolicyExpirationDate);
        result.TargetOutreachDate.ShouldBe(renewal.TargetOutreachDate);
        result.LineOfBusiness.ShouldBe("Property");
    }

    [Fact]
    public async Task RenewalService_GetTransitionsAsync_MapsTransitions()
    {
        var repo = new StubRenewalRepository();
        var renewalId = Guid.NewGuid();
        _transitionRepo.Seed(new WorkflowTransition
        {
            WorkflowType = "Renewal",
            EntityId = renewalId,
            FromState = "Identified",
            ToState = "Outreach",
            Reason = "ready",
            ActorUserId = _user.UserId,
            OccurredAt = DateTime.UtcNow,
        });

        var service = CreateRenewalService(repo);

        var result = await service.GetTransitionsAsync(renewalId);

        result.Count().ShouldBe(1);
        result[0].WorkflowType.ShouldBe("Renewal");
        result[0].ToState.ShouldBe("Outreach");
    }

    [Fact]
    public async Task RenewalService_TransitionAsync_NotFound_ReturnsNotFound()
    {
        var repo = new StubRenewalRepository();
        var service = CreateRenewalService(repo);

        var (result, error, missingItems) = await service.TransitionAsync(
            Guid.NewGuid(),
            new RenewalTransitionRequestDto("Outreach", "seed"),
            0,
            _user);

        result.ShouldBeNull();
        error.ShouldBe("not_found");
        missingItems.ShouldBeNull();
    }

    [Fact]
    public async Task RenewalService_TransitionAsync_InvalidTransition_ReturnsInvalidTransition()
    {
        var repo = new StubRenewalRepository();
        var now = DateTime.UtcNow;
        var account = NewAccount(now);
        var broker = NewBroker(now);
        var policy = NewPolicy(now, account, broker);
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        repo.Seed(new Renewal
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            PolicyId = policy.Id,
            CurrentStatus = "Identified",
            PolicyExpirationDate = now.Date.AddDays(30),
            TargetOutreachDate = now.Date.AddDays(-60),
            AssignedToUserId = _user.UserId,
            Account = account,
            Broker = broker,
            Policy = policy,
            AssignedToUser = assignee,
            CreatedAt = now,
            CreatedByUserId = _user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateRenewalService(repo);
        var seeded = repo.Single();

        var (result, error, missingItems) = await service.TransitionAsync(
            seeded.Id,
            new RenewalTransitionRequestDto("Completed", "too early"),
            seeded.RowVersion,
            _user);

        result.ShouldBeNull();
        error.ShouldBe("invalid_transition");
        missingItems.ShouldBeNull();
        seeded.CurrentStatus.ShouldBe("Identified");
        _transitionRepo.Items.ShouldBeEmpty();
        _timelineRepo.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task RenewalService_TransitionAsync_ValidTransition_PersistsTransitionAndTimeline()
    {
        var repo = new StubRenewalRepository();
        var now = DateTime.UtcNow;
        var account = NewAccount(now);
        var broker = NewBroker(now);
        var policy = NewPolicy(now, account, broker);
        var assignee = NewUserProfile(_user.UserId, "DistributionUser");
        repo.Seed(new Renewal
        {
            AccountId = account.Id,
            BrokerId = broker.Id,
            PolicyId = policy.Id,
            CurrentStatus = "Identified",
            PolicyExpirationDate = now.Date.AddDays(30),
            TargetOutreachDate = now.Date.AddDays(-60),
            AssignedToUserId = _user.UserId,
            Account = account,
            Broker = broker,
            Policy = policy,
            AssignedToUser = assignee,
            CreatedAt = now.AddDays(-2),
            CreatedByUserId = _user.UserId,
            UpdatedAt = now.AddDays(-2),
            UpdatedByUserId = _user.UserId,
        });

        var service = CreateRenewalService(repo);
        var seeded = repo.Single();

        var (result, error, missingItems) = await service.TransitionAsync(
            seeded.Id,
            new RenewalTransitionRequestDto("Outreach", "start"),
            seeded.RowVersion,
            _user);

        error.ShouldBeNull();
        missingItems.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.FromState.ShouldBe("Identified");
        result.ToState.ShouldBe("Outreach");
        seeded.CurrentStatus.ShouldBe("Outreach");
        _transitionRepo.Items.Count.ShouldBe(1);
        var timelineEvent = _timelineRepo.Events.ShouldHaveSingleItem();
        timelineEvent.EntityType.ShouldBe("Renewal");
        timelineEvent.EventType.ShouldBe("RenewalTransitioned");
    }

    private SubmissionService CreateSubmissionService(
        StubSubmissionRepository repo,
        UserProfile? assignedUser = null)
    {
        var userProfileRepo = new StubUserProfileRepository();
        if (assignedUser is not null)
            userProfileRepo.Seed(assignedUser);

        return new SubmissionService(
            repo,
            _transitionRepo,
            _timelineRepo,
            new StubBrokerRepository(),
            new StubReferenceDataRepository(),
            userProfileRepo,
            new StubSubmissionDocumentChecklistReader(),
            _unitOfWork);
    }

    private RenewalService CreateRenewalService(StubRenewalRepository repo)
    {
        var userProfileRepo = new StubUserProfileRepository();
        userProfileRepo.Seed(NewUserProfile(_user.UserId, "DistributionUser"));

        return new RenewalService(
            repo,
            _transitionRepo,
            _timelineRepo,
            new StubReferenceDataRepository(),
            userProfileRepo,
            new StubWorkflowSlaThresholdRepository(),
            _unitOfWork);
    }

    private Account NewAccount(DateTime now) => new()
    {
        Name = "Test Account",
        Industry = "Technology",
        PrimaryState = "CA",
        Region = "West",
        Status = "Active",
        CreatedAt = now,
        UpdatedAt = now,
        CreatedByUserId = _user.UserId,
        UpdatedByUserId = _user.UserId,
    };

    private Broker NewBroker(DateTime now) => new()
    {
        LegalName = "Test Broker",
        LicenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
        State = "CA",
        Status = "Active",
        CreatedAt = now,
        UpdatedAt = now,
        CreatedByUserId = _user.UserId,
        UpdatedByUserId = _user.UserId,
    };

    private Policy NewPolicy(DateTime now, Account account, Broker broker) => new()
    {
        PolicyNumber = $"POL-{Guid.NewGuid():N}"[..12],
        AccountId = account.Id,
        BrokerId = broker.Id,
        CarrierId = Guid.NewGuid(),
        Carrier = NewCarrierRef("Acme Carrier"),
        LineOfBusiness = "Property",
        EffectiveDate = now.Date.AddMonths(-11),
        ExpirationDate = now.Date.AddDays(45),
        TotalPremium = 125000m,
        PremiumCurrency = "USD",
        CurrentStatus = "Issued",
        Account = account,
        Broker = broker,
        CreatedAt = now,
        CreatedByUserId = _user.UserId,
        UpdatedAt = now,
        UpdatedByUserId = _user.UserId,
    };

    private static CarrierRef NewCarrierRef(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{name} {Guid.NewGuid():N}",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static UserProfile NewUserProfile(Guid id, string role) => new()
    {
        Id = id,
        IdpIssuer = "test",
        IdpSubject = "test",
        Email = "test@example.com",
        DisplayName = "Test User",
        Department = "Distribution",
        RolesJson = $"[\"{role}\"]",
        RegionsJson = "[\"West\"]",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static HashSet<string> Fields(params string[] fields) =>
        new(fields, StringComparer.OrdinalIgnoreCase);
}

internal sealed class StubSubmissionRepository : ISubmissionRepository
{
    private readonly Dictionary<Guid, Submission> _submissions = new();

    public void Seed(Submission submission) => _submissions[submission.Id] = submission;
    public Submission Single() => _submissions.Values.Single();

    public Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_submissions.GetValueOrDefault(id));

    public Task<Submission?> GetByIdWithIncludesAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_submissions.GetValueOrDefault(id));

    public Task AddAsync(Submission submission, CancellationToken ct = default)
    {
        _submissions[submission.Id] = submission;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Submission submission, CancellationToken ct = default)
    {
        _submissions[submission.Id] = submission;
        return Task.CompletedTask;
    }

    public Task<PaginatedResult<Submission>> ListAsync(
        SubmissionListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default) =>
        Task.FromResult(new PaginatedResult<Submission>([], query.Page, query.PageSize, 0));

    public Task<IReadOnlyDictionary<Guid, bool>> GetStaleFlagsAsync(
        IReadOnlyCollection<Guid> submissionIds,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, bool>>(submissionIds.ToDictionary(id => id, _ => false));
}

internal sealed class StubRenewalRepository : IRenewalRepository
{
    private readonly Dictionary<Guid, Renewal> _renewals = new();

    public void Seed(Renewal renewal) => _renewals[renewal.Id] = renewal;
    public Renewal Single() => _renewals.Values.Single();

    public Task<Renewal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_renewals.GetValueOrDefault(id));

    public Task<Renewal?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_renewals.GetValueOrDefault(id));

    public Task AddAsync(Renewal renewal, CancellationToken ct = default)
    {
        _renewals[renewal.Id] = renewal;
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveRenewalForPolicyAsync(Guid policyId, CancellationToken ct = default) =>
        Task.FromResult(_renewals.Values.Any(renewal =>
            renewal.PolicyId == policyId
            && renewal.CurrentStatus is not ("Completed" or "Lost")));

    public Task<PaginatedResult<Renewal>> ListAsync(RenewalListQuery query, CancellationToken ct = default) =>
        Task.FromResult(new PaginatedResult<Renewal>([], query.Page, query.PageSize, 0));

    public Task UpdateAsync(Renewal renewal, CancellationToken ct = default)
    {
        _renewals[renewal.Id] = renewal;
        return Task.CompletedTask;
    }
}

internal sealed class StubWorkflowTransitionRepository : IWorkflowTransitionRepository
{
    public List<WorkflowTransition> Items { get; } = [];

    public void Seed(WorkflowTransition transition) => Items.Add(transition);

    public Task<IReadOnlyList<WorkflowTransition>> ListByEntityAsync(string workflowType, Guid entityId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<WorkflowTransition>>(Items
            .Where(t => t.WorkflowType == workflowType && t.EntityId == entityId)
            .ToList());

    public Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestTransitionTimesAsync(
        string workflowType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, DateTime>>(Items
            .Where(t => t.WorkflowType == workflowType && entityIds.Contains(t.EntityId))
            .GroupBy(t => t.EntityId)
            .ToDictionary(group => group.Key, group => group.Max(item => item.OccurredAt)));

    public Task AddAsync(WorkflowTransition transition, CancellationToken ct = default)
    {
        Items.Add(transition);
        return Task.CompletedTask;
    }
}

internal sealed class StubReferenceDataRepository : IReferenceDataRepository
{
    private readonly Dictionary<Guid, Policy> _policies = new();

    public void Seed(Policy policy) => _policies[policy.Id] = policy;

    public Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Account>>([]);
    public Task<IReadOnlyList<MGA>> GetMgasAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MGA>>([]);
    public Task<IReadOnlyList<Nebula.Domain.Entities.Program>> GetProgramsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Nebula.Domain.Entities.Program>>([]);
    public Task<IReadOnlyList<ReferenceSubmissionStatus>> GetSubmissionStatusesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ReferenceSubmissionStatus>>([]);
    public Task<IReadOnlyList<ReferenceRenewalStatus>> GetRenewalStatusesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ReferenceRenewalStatus>>([]);
    public Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Account?>(null);
    public Task<Policy?> GetPolicyByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_policies.GetValueOrDefault(id));
    public Task<Nebula.Domain.Entities.Program?> GetProgramByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<Nebula.Domain.Entities.Program?>(null);
}

internal sealed class StubWorkflowSlaThresholdRepository : IWorkflowSlaThresholdRepository
{
    public Task<WorkflowSlaThreshold?> GetThresholdAsync(
        string entityType,
        string status,
        string? lineOfBusiness,
        CancellationToken ct = default) =>
        Task.FromResult<WorkflowSlaThreshold?>(new WorkflowSlaThreshold
        {
            EntityType = entityType,
            Status = status,
            LineOfBusiness = lineOfBusiness,
            WarningDays = 60,
            TargetDays = 90,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
}

internal sealed class StubSubmissionDocumentChecklistReader : ISubmissionDocumentChecklistReader
{
    public Task<IReadOnlyList<SubmissionDocumentCheckDto>> GetChecklistAsync(Guid submissionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SubmissionDocumentCheckDto>>([]);
}
