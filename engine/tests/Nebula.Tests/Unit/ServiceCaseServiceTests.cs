using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Application.Validators;
using Nebula.Domain.Entities;
using Shouldly;

namespace Nebula.Tests.Unit;

public class ServiceCaseServiceTests
{
    private readonly F0024ServiceCaseRepository _serviceCaseRepo = new();
    private readonly F0024TaskRepository _taskRepo = new();
    private readonly F0024TimelineRepository _timelineRepo = new();
    private readonly F0024UnitOfWork _unitOfWork = new();
    private readonly F0024UserProfileRepository _userProfileRepo = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _accountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _policyId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ServiceCaseServiceTests()
    {
        _serviceCaseRepo.AccountIds.Add(_accountId);
        _serviceCaseRepo.PolicyAccountIds[_policyId] = _accountId;
        _userProfileRepo.Seed(new UserProfile
        {
            Id = _userId,
            DisplayName = "Sarah Chen",
            Email = "sarah.chen@nebula.local",
            Department = "Distribution",
            IsActive = true,
        });
    }

    [Fact]
    public async Task CreateAsync_ValidPolicyContext_CreatesIntakeCaseTransitionAndTimeline()
    {
        var svc = CreateService();

        var (result, error) = await svc.CreateAsync(CreateRequest(), User());

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.CaseNumber.ShouldBe("SC-2026-000001");
        result.Status.ShouldBe("Intake");
        result.Transitions.Count.ShouldBe(1);
        result.Transitions[0].FromStatus.ShouldBeNull();
        result.Transitions[0].ToStatus.ShouldBe("Intake");
        result.ClaimReference!.CarrierClaimNumber.ShouldBe("CLM-123");
        _serviceCaseRepo.Added.Count.ShouldBe(1);
        _timelineRepo.Events.Select(e => e.EntityType).ShouldBe(["Account", "Policy"]);
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task TransitionAsync_ResolvedWithoutResolution_ReturnsMissingResolutionSummary()
    {
        var svc = CreateService();
        var serviceCase = SeedCase(status: "InProgress");

        var (result, error) = await svc.TransitionAsync(
            serviceCase.Id,
            new ServiceCaseTransitionRequestDto("Resolved", null, null, null, null),
            User());

        result.ShouldBeNull();
        error.ShouldBe("missing_resolution_summary");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task TransitionAsync_WaitingWithoutReason_ReturnsMissingWaitingReason()
    {
        var svc = CreateService();
        var serviceCase = SeedCase(status: "InProgress");

        var (result, error) = await svc.TransitionAsync(
            serviceCase.Id,
            new ServiceCaseTransitionRequestDto("Waiting", null, null, null, null),
            User());

        result.ShouldBeNull();
        error.ShouldBe("missing_waiting_reason");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task TransitionAsync_ClosedWithoutResolution_ReturnsMissingResolutionSummary()
    {
        var svc = CreateService();
        var serviceCase = SeedCase(status: "Resolved");

        var (result, error) = await svc.TransitionAsync(
            serviceCase.Id,
            new ServiceCaseTransitionRequestDto("Closed", null, null, null, null),
            User());

        result.ShouldBeNull();
        error.ShouldBe("missing_resolution_summary");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAsync_ClosedCase_ReturnsClosedServiceCase()
    {
        var svc = CreateService();
        var serviceCase = SeedCase(status: "Closed");

        var (result, error) = await svc.UpdateAsync(
            serviceCase.Id,
            new ServiceCaseUpdateRequestDto("New summary", null, null, null, null, null, null, null),
            User());

        result.ShouldBeNull();
        error.ShouldBe("closed_service_case");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateFollowUpTaskAsync_ValidRequest_CreatesTaskLinkAndTimeline()
    {
        var svc = CreateService();
        var serviceCase = SeedCase(status: "Waiting");

        var (result, error) = await svc.CreateFollowUpTaskAsync(
            serviceCase.Id,
            new ServiceCaseFollowUpTaskRequestDto("Call carrier", "Ask for status", _userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), "High", null),
            User());

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.LinkedEntityType.ShouldBe("ServiceCase");
        result.LinkedEntityId.ShouldBe(serviceCase.Id);
        serviceCase.TaskLinks.Count.ShouldBe(1);
        _taskRepo.Added.Count.ShouldBe(1);
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskCreated");
        _timelineRepo.Events.ShouldContain(e => e.EventType == "ServiceCaseFollowUpTaskCreated");
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public void TransitionValidator_UnsupportedStatus_Fails()
    {
        var validator = new ServiceCaseTransitionRequestValidator();

        var result = validator.Validate(new ServiceCaseTransitionRequestDto("Reopened", null, null, null, null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Unsupported service case status.");
    }

    [Fact]
    public void CreateValidator_MissingDueDate_Fails()
    {
        var validator = new ServiceCaseCreateRequestValidator();
        var request = CreateRequest() with { DueDate = null };

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Due date is required.");
    }

    [Fact]
    public void ClaimReferenceValidator_FutureDateOfLoss_Fails()
    {
        var validator = new ServiceCaseClaimReferenceUpdateRequestValidator();
        var request = new ServiceCaseClaimReferenceUpdateRequestDto(null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), null, null, null, null);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Date of loss cannot be in the future.");
    }

    private ServiceCaseService CreateService() => new(
        _serviceCaseRepo,
        _taskRepo,
        _timelineRepo,
        _unitOfWork,
        new F0024AuthorizationService(),
        _userProfileRepo);

    private F0024CurrentUser User() => new(_userId, ["Admin"]);

    private ServiceCaseCreateRequestDto CreateRequest() => new(
        _accountId,
        _policyId,
        "Water damage claim support",
        "Help coordinate carrier claim reference.",
        "ClaimSupport",
        "High",
        _userId,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
        "Follow up with carrier.",
        new ServiceCaseClaimReferenceUpdateRequestDto("CLM-123", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Jane Claimant", "Water damage", "Carrier desk", null));

    private ServiceCase SeedCase(string status)
    {
        var now = DateTime.UtcNow;
        var serviceCase = new ServiceCase
        {
            CaseNumber = "SC-2026-000099",
            AccountId = _accountId,
            PolicyId = _policyId,
            Summary = "Seeded case",
            Type = "Service",
            Status = status,
            Priority = "Medium",
            OwnerUserId = _userId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = _userId,
            UpdatedByUserId = _userId,
        };
        _serviceCaseRepo.Seed(serviceCase);
        return serviceCase;
    }
}

internal class F0024ServiceCaseRepository : IServiceCaseRepository
{
    private readonly Dictionary<Guid, ServiceCase> _cases = new();
    public HashSet<Guid> AccountIds { get; } = [];
    public Dictionary<Guid, Guid> PolicyAccountIds { get; } = new();
    public HashSet<Guid> CommunicationIds { get; } = [];
    public List<ServiceCase> Added { get; } = [];

    public Task AddAsync(ServiceCase serviceCase, CancellationToken ct = default)
    {
        Added.Add(serviceCase);
        _cases[serviceCase.Id] = serviceCase;
        return Task.CompletedTask;
    }

    public void Seed(ServiceCase serviceCase) => _cases[serviceCase.Id] = serviceCase;

    public Task<ServiceCase?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_cases.GetValueOrDefault(id));

    public Task<PaginatedResult<ServiceCase>> ListAsync(ServiceCaseListQuery query, CancellationToken ct = default)
    {
        var data = _cases.Values.ToList();
        return Task.FromResult(new PaginatedResult<ServiceCase>(data, query.Page, query.PageSize, data.Count));
    }

    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default) =>
        Task.FromResult(AccountIds.Contains(accountId));

    public Task<bool> PolicyExistsAsync(Guid policyId, CancellationToken ct = default) =>
        Task.FromResult(PolicyAccountIds.ContainsKey(policyId));

    public Task<bool> PolicyBelongsToAccountAsync(Guid policyId, Guid accountId, CancellationToken ct = default) =>
        Task.FromResult(PolicyAccountIds.TryGetValue(policyId, out var actualAccountId) && actualAccountId == accountId);

    public Task<bool> CommunicationExistsAsync(Guid communicationEventId, CancellationToken ct = default) =>
        Task.FromResult(CommunicationIds.Contains(communicationEventId));

    public Task<string> NextCaseNumberAsync(CancellationToken ct = default) =>
        Task.FromResult("SC-2026-000001");
}

internal class F0024TaskRepository : ITaskRepository
{
    public List<TaskItem> Added { get; } = [];

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<TaskItem?>(null);
    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetMyTasksAsync(Guid assignedToUserId, int limit, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<TaskItem>, int)>(([], 0));
    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetBrokerScopedTasksAsync(Guid brokerId, int limit, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<TaskItem>, int)>(([], 0));
    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetTaskListAsync(TaskListQuery query, CancellationToken ct = default) => Task.FromResult<(IReadOnlyList<TaskItem>, int)>(([], 0));

    public Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        Added.Add(task);
        return Task.CompletedTask;
    }

    public Task<string?> ResolveLinkedEntityNameAsync(string? entityType, Guid? entityId, CancellationToken ct = default) => Task.FromResult<string?>(null);
}

internal class F0024TimelineRepository : ITimelineRepository
{
    public List<ActivityTimelineEvent> Events { get; } = [];

    public Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default)
    {
        Events.Add(evt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(string entityType, Guid? entityId, int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);
    public Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new PaginatedResult<ActivityTimelineEvent>([], page, pageSize, 0));
    public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);
}

internal class F0024UnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }
    public Task CommitAsync(CancellationToken ct = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }
}

internal record F0024CurrentUser(Guid UserId, IReadOnlyList<string> Roles) : ICurrentUserService
{
    public string? DisplayName => "Sarah Chen";
    public IReadOnlyList<string> Regions => ["West"];
    public string? BrokerTenantId => null;
}

internal class F0024AuthorizationService : IAuthorizationService
{
    public Task<bool> AuthorizeAsync(string userRole, string resourceType, string action, IDictionary<string, object>? resourceAttributes = null) =>
        Task.FromResult(userRole == "Admin" || userRole == "DistributionManager" || userRole == "DistributionUser");
}

internal class F0024UserProfileRepository : IUserProfileRepository
{
    private readonly Dictionary<Guid, UserProfile> _profiles = new();
    public void Seed(UserProfile profile) => _profiles[profile.Id] = profile;
    public Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(_profiles.GetValueOrDefault(userId));
    public Task<IReadOnlyList<UserProfile>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UserProfile>>(_profiles.Values.Where(p => userIds.Contains(p.Id)).ToList());
    public Task<IReadOnlyList<UserProfile>> SearchAsync(string query, bool activeOnly, int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserProfile>>([]);
}
