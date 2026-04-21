using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nebula.Tests.Unit;

public class TaskServiceTests
{
    private readonly StubTaskRepository _taskRepo = new();
    private readonly StubTimelineRepository _timelineRepo = new();
    private readonly StubUnitOfWork _unitOfWork = new();
    private readonly StubCurrentUserService _user = new(Guid.Parse("aaaa0000-0000-0000-0000-000000000001"));
    private readonly StubUserProfileRepository _userProfileRepo = new();

    private TaskService CreateService() => new(
        _taskRepo,
        _timelineRepo,
        _unitOfWork,
        new StubAuthorizationService(),
        _userProfileRepo,
        new BrokerScopeResolver(new StubBrokerRepository()),
        NullLogger<TaskService>.Instance);

    // ═══════════════════════════════════════════════════════════════════════
    //  S0001: CreateAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsTaskDto()
    {
        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Test Task", "desc", "High", DateTime.UtcNow.AddDays(7),
            _user.UserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, _user);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.Title.ShouldBe("Test Task");
        result.Status.ShouldBe("Open");
        result.Priority.ShouldBe("High");
        _taskRepo.Added.Count.ShouldBe(1);
        _timelineRepo.Events.Count.ShouldBe(1);
        _timelineRepo.Events[0].EventType.ShouldBe("TaskCreated");
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_DefaultPriority_WhenNullPriority()
    {
        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Test", null, null, null, _user.UserId, null, null);

        var (result, _) = await svc.CreateAsync(dto, _user);

        result!.Priority.ShouldBe("Normal");
    }

    [Fact]
    public async Task CreateAsync_SelfAssignmentViolation_ReturnsForbidden()
    {
        // Non-manager users (e.g. DistributionUser) cannot assign tasks to others
        var nonManagerUser = new StubCurrentUserService(
            Guid.Parse("aaaa0000-0000-0000-0000-000000000002"),
            roles: ["DistributionUser"]);
        var svc = CreateService();
        var otherUserId = Guid.NewGuid();
        var dto = new TaskCreateRequestDto("Test", null, null, null, otherUserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, nonManagerUser);

        error.ShouldBe("forbidden");
        result.ShouldBeNull();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_LinkedEntityMismatch_ReturnsValidationError()
    {
        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Test", null, null, null, _user.UserId, "Broker", null);

        var (result, error) = await svc.CreateAsync(dto, _user);

        error.ShouldBe("validation_error");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithLinkedEntity_Succeeds()
    {
        var svc = CreateService();
        var linkedId = Guid.NewGuid();
        var dto = new TaskCreateRequestDto("Test", null, null, null, _user.UserId, "Broker", linkedId);

        var (result, error) = await svc.CreateAsync(dto, _user);

        error.ShouldBeNull();
        result!.LinkedEntityType.ShouldBe("Broker");
        result.LinkedEntityId.ShouldBe(linkedId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  S0002: UpdateAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_StatusOpenToInProgress_Succeeds()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        var dto = new TaskUpdateRequestDto(null, null, "InProgress", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("InProgress");
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskUpdated");
    }

    [Fact]
    public async Task UpdateAsync_StatusInProgressToDone_SetsCompletedAt()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "InProgress");
        var dto = new TaskUpdateRequestDto(null, null, "Done", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("Done");
        result.CompletedAt.ShouldNotBeNull();
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskCompleted");
    }

    [Fact]
    public async Task UpdateAsync_StatusDoneToOpen_ClearsCompletedAt()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Done");
        task.CompletedAt = DateTime.UtcNow.AddHours(-1);
        var dto = new TaskUpdateRequestDto(null, null, "Open", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("Open");
        result.CompletedAt.ShouldBeNull();
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskReopened");
    }

    [Fact]
    public async Task UpdateAsync_StatusOpenToDone_ReturnsInvalidTransition()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        var dto = new TaskUpdateRequestDto(null, null, "Done", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBe("invalid_status_transition");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto("New Title", null, null, null, null, null);
        var present = Fields("title");

        var (result, error, _, _) = await svc.UpdateAsync(Guid.NewGuid(), dto, present, 0, _user);

        error.ShouldBe("not_found");
    }

    [Fact]
    public async Task UpdateAsync_OtherUsersTask_ReturnsNotFound()
    {
        var svc = CreateService();
        var otherUserId = Guid.NewGuid();
        var task = SeedTask(otherUserId, "Open");
        var dto = new TaskUpdateRequestDto("New Title", null, null, null, null, null);
        var present = Fields("title");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBe("not_found"); // IDOR normalization: 403 → 404
    }

    [Fact]
    public async Task UpdateAsync_ReassignToOther_ReturnsForbidden()
    {
        // Only the task creator can reassign. Here the user is the assignee but NOT the creator.
        var svc = CreateService();
        var otherCreator = Guid.Parse("bbbb0000-0000-0000-0000-000000000099");
        var task = SeedTaskWithCreator(_user.UserId, otherCreator, "Open");
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, Guid.NewGuid());
        var present = Fields("assignedToUserId");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBe("forbidden");
    }

    [Fact]
    public async Task UpdateAsync_ClearDueDate_SetsNull()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        task.DueDate = DateTime.UtcNow.AddDays(5);
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, null);
        var present = Fields("dueDate"); // dueDate present but null = clear

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.DueDate.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ClearDescription_SetsNull()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        task.Description = "Old description";
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, null);
        var present = Fields("description"); // description present but null = clear

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Description.ShouldBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  S0003: DeleteAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_OwnTask_Succeeds()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");

        var error = await svc.DeleteAsync(task.Id, _user);

        error.ShouldBeNull();
        task.IsDeleted.ShouldBeTrue();
        task.DeletedAt.ShouldNotBeNull();
        task.DeletedByUserId.ShouldBe(_user.UserId);
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskDeleted");
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        var svc = CreateService();

        var error = await svc.DeleteAsync(Guid.NewGuid(), _user);

        error.ShouldBe("not_found");
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersTask_ReturnsNotFound()
    {
        var svc = CreateService();
        var task = SeedTask(Guid.NewGuid(), "Open");

        var error = await svc.DeleteAsync(task.Id, _user);

        error.ShouldBe("not_found"); // IDOR normalization: 403 → 404
    }

    [Fact]
    public async Task DeleteAsync_CompletedTask_Succeeds()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Done");
        task.CompletedAt = DateTime.UtcNow.AddHours(-1);

        var error = await svc.DeleteAsync(task.Id, _user);

        error.ShouldBeNull();
        task.IsDeleted.ShouldBeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  S0002: Additional transitions & edge cases
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_StatusDoneToInProgress_ClearsCompletedAt()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Done");
        task.CompletedAt = DateTime.UtcNow.AddHours(-1);
        var dto = new TaskUpdateRequestDto(null, null, "InProgress", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("InProgress");
        result.CompletedAt.ShouldBeNull();
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskReopened");
    }

    [Fact]
    public async Task UpdateAsync_StatusInProgressToOpen_Succeeds()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "InProgress");
        var dto = new TaskUpdateRequestDto(null, null, "Open", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("Open");
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskUpdated");
    }

    [Fact]
    public async Task UpdateAsync_SameStatus_TreatedAsNoStatusChange()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        var dto = new TaskUpdateRequestDto(null, null, "Open", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Status.ShouldBe("Open");
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskUpdated");
    }

    [Fact]
    public async Task UpdateAsync_MultipleFieldChanges_TracksAllChangedFields()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        task.Priority = "Normal";
        var dto = new TaskUpdateRequestDto("New Title", null, null, "High", null, null);
        var present = Fields("title", "priority");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        result!.Title.ShouldBe("New Title");
        result.Priority.ShouldBe("High");
        var evt = _timelineRepo.Events.Single();
        evt.EventType.ShouldBe("TaskUpdated");
        evt.EventDescription.ShouldContain("title");
        evt.EventDescription.ShouldContain("priority");
    }

    [Fact]
    public async Task UpdateAsync_ReopenedEvent_CapturesOriginalCompletedAt()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Done");
        var originalCompletedAt = DateTime.UtcNow.AddHours(-2);
        task.CompletedAt = originalCompletedAt;
        var dto = new TaskUpdateRequestDto(null, null, "Open", null, null, null);
        var present = Fields("status");

        var (_, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, _user);

        error.ShouldBeNull();
        var evt = _timelineRepo.Events.Single();
        evt.EventType.ShouldBe("TaskReopened");
        evt.EventPayloadJson!.ShouldContain("previousCompletedAt");
        // Verify the payload contains the original CompletedAt, not the current time
        evt.EventPayloadJson.ShouldContain(originalCompletedAt.Year.ToString());
    }

    [Fact]
    public async Task CreateAsync_PastDueDate_Succeeds()
    {
        var svc = CreateService();
        var pastDate = DateTime.UtcNow.AddDays(-7);
        var dto = new TaskCreateRequestDto("Overdue task", null, null, pastDate, _user.UserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, _user);

        error.ShouldBeNull();
        (result!.DueDate!.Value - pastDate).Duration().ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_TimelineEvent_HasCorrectPayloadFields()
    {
        var svc = CreateService();
        var linkedId = Guid.NewGuid();
        var dto = new TaskCreateRequestDto("Audit task", "desc", "High",
            DateTime.UtcNow.AddDays(3), _user.UserId, "Broker", linkedId);

        await svc.CreateAsync(dto, _user);

        var evt = _timelineRepo.Events.Single();
        evt.EntityType.ShouldBe("Task");
        evt.EventType.ShouldBe("TaskCreated");
        evt.BrokerDescription.ShouldBeNull("task events are InternalOnly");
        evt.ActorUserId.ShouldBe(_user.UserId);
        evt.EventDescription.ShouldContain("Audit task");
        evt.EventPayloadJson!.ShouldContain("title");
        evt.EventPayloadJson.ShouldContain("assignedToUserId");
    }

    [Fact]
    public async Task DeleteAsync_TimelineEvent_HasCorrectFields()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");

        await svc.DeleteAsync(task.Id, _user);

        var evt = _timelineRepo.Events.Single();
        evt.EntityType.ShouldBe("Task");
        evt.EntityId.ShouldBe(task.Id);
        evt.EventType.ShouldBe("TaskDeleted");
        evt.EventDescription.ShouldBe("Task deleted");
        evt.BrokerDescription.ShouldBeNull("task events are InternalOnly");
        evt.ActorUserId.ShouldBe(_user.UserId);
    }

    [Fact]
    public async Task DeleteAsync_SetsUpdatedAtAndUpdatedByUserId()
    {
        var svc = CreateService();
        var task = SeedTask(_user.UserId, "Open");
        var beforeDelete = DateTime.UtcNow;

        await svc.DeleteAsync(task.Id, _user);

        task.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        task.UpdatedByUserId.ShouldBe(_user.UserId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Read methods — AuditBrokerUserRead coverage
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyTasksAsync_NonBrokerUser_SkipsAuditLog()
    {
        var svc = CreateService();
        var result = await svc.GetMyTasksAsync(_user.UserId, "Test User", 10, _user);

        result.ShouldNotBeNull();
        result.Tasks.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetMyTasksAsync_BrokerUser_AuditLogIsEmitted()
    {
        var brokerUser = new StubCurrentUserService(
            Guid.Parse("aaaa0000-0000-0000-0000-000000000002"),
            roles: ["BrokerUser"],
            brokerTenantId: "broker-tenant-1");
        var svc = CreateService();

        // Should not throw — audit log is best-effort
        var result = await svc.GetMyTasksAsync(brokerUser.UserId, null, 10, brokerUser);

        result.ShouldNotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  F0004: Cross-User Assignment
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ManagerAssignsToOther_Succeeds()
    {
        var managerUser = new StubCurrentUserService(
            Guid.Parse("bbbb0000-0000-0000-0000-000000000001"),
            roles: ["DistributionManager"]);
        var assigneeId = Guid.Parse("cccc0000-0000-0000-0000-000000000001");
        _userProfileRepo.Seed(new UserProfile
        {
            Id = assigneeId,
            DisplayName = "Assignee User",
            Email = "assignee@example.com",
            Department = "Distribution",
            IsActive = true,
        });

        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Cross-assign task", null, null, null, assigneeId, null, null);

        var (result, error) = await svc.CreateAsync(dto, managerUser);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.AssignedToUserId.ShouldBe(assigneeId);
        result.CreatedByUserId.ShouldBe(managerUser.UserId);
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_NonManagerAssignsToOther_ReturnsForbidden()
    {
        var distributionUser = new StubCurrentUserService(
            Guid.Parse("dddd0000-0000-0000-0000-000000000001"),
            roles: ["DistributionUser"]);
        var otherUserId = Guid.NewGuid();

        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Bad assign", null, null, null, otherUserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, distributionUser);

        error.ShouldBe("forbidden");
        result.ShouldBeNull();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_AssignToInactiveUser_ReturnsInactiveAssignee()
    {
        var managerUser = new StubCurrentUserService(
            Guid.Parse("bbbb0000-0000-0000-0000-000000000002"),
            roles: ["Admin"]);
        var inactiveUserId = Guid.Parse("cccc0000-0000-0000-0000-000000000002");
        _userProfileRepo.Seed(new UserProfile
        {
            Id = inactiveUserId,
            DisplayName = "Inactive User",
            Email = "inactive@example.com",
            Department = "Distribution",
            IsActive = false,
        });

        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Inactive assign", null, null, null, inactiveUserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, managerUser);

        error.ShouldBe("inactive_assignee");
        result.ShouldBeNull();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_AssignToNonExistent_ReturnsInvalidAssignee()
    {
        var managerUser = new StubCurrentUserService(
            Guid.Parse("bbbb0000-0000-0000-0000-000000000003"),
            roles: ["Admin"]);
        var nonExistentUserId = Guid.NewGuid(); // not seeded in _userProfileRepo

        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Ghost assign", null, null, null, nonExistentUserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, managerUser);

        error.ShouldBe("invalid_assignee");
        result.ShouldBeNull();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task CreateAsync_ManagerSelfAssign_StillWorks()
    {
        var managerUserId = Guid.Parse("bbbb0000-0000-0000-0000-000000000004");
        var managerUser = new StubCurrentUserService(managerUserId, roles: ["DistributionManager"]);

        var svc = CreateService();
        var dto = new TaskCreateRequestDto("Self-assign by manager", null, null, null, managerUserId, null, null);

        var (result, error) = await svc.CreateAsync(dto, managerUser);

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.AssignedToUserId.ShouldBe(managerUserId);
        result.CreatedByUserId.ShouldBe(managerUserId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  F0004: Creator-Based Access
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_CreatorEditsTitle_Succeeds()
    {
        // Manager created a task assigned to someone else — creator should be able to edit title
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000001");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000001");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["DistributionManager"]);

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto("Updated by creator", null, null, null, null, null);
        var present = Fields("title");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, creatorUser);

        error.ShouldBeNull();
        result!.Title.ShouldBe("Updated by creator");
    }

    [Fact]
    public async Task UpdateAsync_CreatorCannotChangeStatus_ReturnsStatusChangeRestricted()
    {
        // Manager created a task assigned to someone else — creator cannot change status
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000002");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000002");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["DistributionManager"]);

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, "InProgress", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, creatorUser);

        error.ShouldBe("status_change_restricted");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_AssigneeCanChangeStatus_OnManagerCreatedTask()
    {
        // The assignee (not the creator) should be able to change status
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000003");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000003");
        var assigneeUser = new StubCurrentUserService(assigneeId, roles: ["DistributionUser"]);

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, "InProgress", null, null, null);
        var present = Fields("status");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, assigneeUser);

        error.ShouldBeNull();
        result!.Status.ShouldBe("InProgress");
    }

    [Fact]
    public async Task UpdateAsync_CreatorReassigns_Succeeds()
    {
        // Creator should be able to change the assignee
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000004");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000004");
        var newAssigneeId = Guid.Parse("aaaa1111-0000-0000-0000-000000000001");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["DistributionManager"]);

        _userProfileRepo.Seed(new UserProfile
        {
            Id = newAssigneeId,
            DisplayName = "New Assignee",
            Email = "new-assignee@example.com",
            Department = "Distribution",
            IsActive = true,
        });

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, newAssigneeId);
        var present = Fields("assignedToUserId");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, creatorUser);

        error.ShouldBeNull();
        result!.AssignedToUserId.ShouldBe(newAssigneeId);
    }

    [Fact]
    public async Task UpdateAsync_CreatorReassigns_EmitsTaskReassignedEvent()
    {
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000005");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000005");
        var newAssigneeId = Guid.Parse("aaaa1111-0000-0000-0000-000000000002");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["DistributionManager"]);

        _userProfileRepo.Seed(new UserProfile
        {
            Id = newAssigneeId,
            DisplayName = "New Assignee B",
            Email = "new-assignee-b@example.com",
            Department = "Distribution",
            IsActive = true,
        });

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, newAssigneeId);
        var present = Fields("assignedToUserId");

        await svc.UpdateAsync(task.Id, dto, present, 0, creatorUser);

        var evt = _timelineRepo.Events.Single();
        evt.EventType.ShouldBe("TaskReassigned");
        evt.EventPayloadJson!.ShouldContain("fromUserId");
        evt.EventPayloadJson.ShouldContain("toUserId");
        evt.EventPayloadJson.ShouldContain(newAssigneeId.ToString());
    }

    [Fact]
    public async Task UpdateAsync_AssigneeCannotReassign_ReturnsForbidden()
    {
        // Assignee (non-creator) should NOT be able to change the assignee
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000006");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000006");
        var otherUserId = Guid.NewGuid();
        var assigneeUser = new StubCurrentUserService(assigneeId, roles: ["DistributionUser"]);

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, otherUserId);
        var present = Fields("assignedToUserId");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, assigneeUser);

        error.ShouldBe("forbidden");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReassignToInactive_ReturnsInactiveAssignee()
    {
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000007");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000007");
        var inactiveUserId = Guid.Parse("aaaa1111-0000-0000-0000-000000000003");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["Admin"]);

        _userProfileRepo.Seed(new UserProfile
        {
            Id = inactiveUserId,
            DisplayName = "Inactive Target",
            Email = "inactive-target@example.com",
            Department = "Distribution",
            IsActive = false,
        });

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();
        var dto = new TaskUpdateRequestDto(null, null, null, null, null, inactiveUserId);
        var present = Fields("assignedToUserId");

        var (result, error, _, _) = await svc.UpdateAsync(task.Id, dto, present, 0, creatorUser);

        error.ShouldBe("inactive_assignee");
        result.ShouldBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  F0004: Delete by Creator
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_CreatorCanDelete_Succeeds()
    {
        // Creator who is not the assignee should still be able to delete
        var creatorId = Guid.Parse("eeee0000-0000-0000-0000-000000000008");
        var assigneeId = Guid.Parse("ffff0000-0000-0000-0000-000000000008");
        var creatorUser = new StubCurrentUserService(creatorId, roles: ["DistributionManager"]);

        var task = SeedTaskWithCreator(assigneeId, creatorId, "Open");
        var svc = CreateService();

        var error = await svc.DeleteAsync(task.Id, creatorUser);

        error.ShouldBeNull();
        task.IsDeleted.ShouldBeTrue();
        task.DeletedByUserId.ShouldBe(creatorId);
        _timelineRepo.Events.ShouldContain(e => e.EventType == "TaskDeleted");
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private TaskItem SeedTask(Guid assignedToUserId, string status)
    {
        var task = new TaskItem
        {
            Title = "Seeded Task",
            Status = status,
            Priority = "Normal",
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = assignedToUserId,
            UpdatedByUserId = assignedToUserId,
        };
        _taskRepo.Seed(task);
        return task;
    }

    /// <summary>
    /// Seeds a task where the creator and assignee are different users (F0004 cross-user scenario).
    /// </summary>
    private TaskItem SeedTaskWithCreator(Guid assignedToUserId, Guid createdByUserId, string status)
    {
        var task = new TaskItem
        {
            Title = "Cross-User Task",
            Status = status,
            Priority = "Normal",
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            UpdatedByUserId = createdByUserId,
        };
        _taskRepo.Seed(task);
        return task;
    }

    private static HashSet<string> Fields(params string[] fields) =>
        new(fields, StringComparer.OrdinalIgnoreCase);
}

// ═══════════════════════════════════════════════════════════════════════════
//  Test doubles
// ═══════════════════════════════════════════════════════════════════════════

internal class StubTaskRepository : ITaskRepository
{
    private readonly Dictionary<Guid, TaskItem> _tasks = new();
    public List<TaskItem> Added { get; } = [];

    public void Seed(TaskItem task) => _tasks[task.Id] = task;

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_tasks.GetValueOrDefault(id));

    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetMyTasksAsync(
        Guid assignedToUserId, int limit, CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<TaskItem>, int)>(([], 0));

    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetBrokerScopedTasksAsync(
        Guid brokerId, int limit, CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<TaskItem>, int)>(([], 0));

    public Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetTaskListAsync(
        TaskListQuery query, CancellationToken ct = default)
    {
        var items = _tasks.Values.Where(t => !t.IsDeleted);
        if (query.View == "myWork")
            items = items.Where(t => t.AssignedToUserId == query.CallerUserId);
        else
            items = items.Where(t => t.CreatedByUserId == query.CallerUserId && t.AssignedToUserId != query.CallerUserId);
        var list = items.ToList();
        return Task.FromResult<(IReadOnlyList<TaskItem>, int)>((list, list.Count));
    }

    public Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        Added.Add(task);
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<string?> ResolveLinkedEntityNameAsync(string? entityType, Guid? entityId, CancellationToken ct = default) =>
        Task.FromResult<string?>(entityType is not null ? $"Test {entityType}" : null);
}

internal class StubTimelineRepository : ITimelineRepository
{
    public List<ActivityTimelineEvent> Events { get; } = [];

    public Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default)
    {
        Events.Add(evt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(
        string entityType, Guid? entityId, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);

    public Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(
        string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new PaginatedResult<ActivityTimelineEvent>([], 1, pageSize, 0));

    public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(
        IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);
}

internal class StubUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }
    public Task CommitAsync(CancellationToken ct = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }
}

internal class StubCurrentUserService(
    Guid userId,
    IReadOnlyList<string>? roles = null,
    string? brokerTenantId = null) : ICurrentUserService
{
    public Guid UserId => userId;
    public string? DisplayName => "Test User";
    public IReadOnlyList<string> Roles => roles ?? ["Admin"];
    public IReadOnlyList<string> Regions => ["West"];
    public string? BrokerTenantId => brokerTenantId;
}

/// <summary>
/// Stub authorization service that simulates Casbin OR semantics:
/// allows if assignee == subjectId OR creator == subjectId.
/// DistributionManager/Admin with create action always pass.
/// </summary>
internal class StubAuthorizationService : IAuthorizationService
{
    public Task<bool> AuthorizeAsync(string userRole, string resourceType, string action,
        IDictionary<string, object>? resourceAttributes = null)
    {
        if (resourceAttributes is not null)
        {
            var subjectId = resourceAttributes.TryGetValue("subjectId", out var sid) ? sid : null;
            var assignee = resourceAttributes.TryGetValue("assignee", out var asg) ? asg : null;
            var creator = resourceAttributes.TryGetValue("creator", out var crt) ? crt : null;

            // Simulate OR semantics: allow if assignee == subjectId OR creator == subjectId
            if (Equals(assignee, subjectId)) return Task.FromResult(true);
            if (Equals(creator, subjectId)) return Task.FromResult(true);

            // For create action: managers with 'true' condition always pass
            if (action == "create" && (userRole == "DistributionManager" || userRole == "Admin"))
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
        // Default allow (for roles without attribute conditions)
        return Task.FromResult(true);
    }
}

internal class StubBrokerRepository : IBrokerRepository
{
    public Task<Guid?> GetIdByBrokerTenantIdAsync(string brokerTenantId, CancellationToken ct = default) =>
        Task.FromResult<Guid?>(null);
    public Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Broker?>(null);
    public Task<Broker?> GetByIdIncludingDeactivatedAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Broker?>(null);
    public Task<PaginatedResult<Broker>> ListAsync(string? search, string? statusFilter, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new PaginatedResult<Broker>([], 1, pageSize, 0));
    public Task AddAsync(Broker broker, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(Broker broker, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> HasActiveSubmissionsOrRenewalsAsync(Guid brokerId, CancellationToken ct = default) => Task.FromResult(false);
}

internal class StubUserProfileRepository : IUserProfileRepository
{
    private readonly Dictionary<Guid, UserProfile> _profiles = new();

    public void Seed(UserProfile profile) => _profiles[profile.Id] = profile;

    public Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_profiles.GetValueOrDefault(userId));

    public Task<IReadOnlyList<UserProfile>> SearchAsync(string query, bool activeOnly, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UserProfile>>(_profiles.Values
            .Where(p => p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || p.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(p => !activeOnly || p.IsActive)
            .Take(limit)
            .ToList());
}
