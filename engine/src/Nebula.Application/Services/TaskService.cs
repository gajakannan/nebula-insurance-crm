using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class TaskService(
    ITaskRepository taskRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    IAuthorizationService authz,
    IUserProfileRepository userProfileRepo,
    BrokerScopeResolver scopeResolver,
    ILogger<TaskService> logger)
{
    private readonly ILogger<TaskService> _logger = logger;

    // Valid status transitions: (from, to)
    private static readonly HashSet<(string, string)> ValidTransitions =
    [
        ("Open", "InProgress"),
        ("InProgress", "Open"),
        ("InProgress", "Done"),
        ("Done", "Open"),
        ("Done", "InProgress"),
    ];

    public async Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(id, ct);
        if (task is null)
            return null;
        return await MapToDtoAsync(task, ct);
    }

    /// <summary>
    /// Fetch task by ID and verify the caller is authorized (Casbin + ownership).
    /// Returns (dto, null) on success, or (null, errorCode) on failure.
    /// Both not-found and not-authorized return "not_found" to prevent IDOR enumeration.
    /// </summary>
    public async Task<(TaskDto? Dto, string? ErrorCode)> GetByIdAuthorizedAsync(
        Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(id, ct);
        if (task is null)
            return (null, "not_found");

        if (!await AuthorizeTaskAsync(user, "read", task))
            return (null, "not_found"); // Normalize to 404 — prevent IDOR

        return (await MapToDtoAsync(task, ct), null);
    }

    public async Task<MyTasksResponseDto> GetMyTasksAsync(
        Guid assignedToUserId, string? callerDisplayName, int limit, ICurrentUserService user, CancellationToken ct = default)
    {
        var (tasks, totalCount) = await taskRepo.GetMyTasksAsync(assignedToUserId, limit, ct);
        var today = DateTime.UtcNow.Date;
        var summaries = tasks.Select(t => new TaskSummaryDto(
            t.Id, t.Title, t.Status, t.DueDate,
            t.LinkedEntityType, t.LinkedEntityId, null,
            t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "Done",
            callerDisplayName))
            .ToList();

        AuditBrokerUserRead(user, "broker.tasks", null);
        return new MyTasksResponseDto(summaries, totalCount);
    }

    /// <summary>
    /// BrokerUser variant: returns tasks scoped to the resolved broker entity (F0009 §12).
    /// Only tasks where LinkedEntityType='Broker' AND LinkedEntityId=resolvedBrokerId.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved.
    /// </summary>
    public async Task<MyTasksResponseDto> GetBrokerScopedTasksAsync(
        int limit, ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var (tasks, totalCount) = await taskRepo.GetBrokerScopedTasksAsync(resolvedBrokerId, limit, ct);
        var today = DateTime.UtcNow.Date;
        var summaries = tasks.Select(t => new TaskSummaryDto(
            t.Id, t.Title, t.Status, t.DueDate,
            t.LinkedEntityType, t.LinkedEntityId, null,
            t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "Done",
            null)) // assignee display name not returned to BrokerUser
            .ToList();

        AuditBrokerUserRead(user, "broker.tasks", resolvedBrokerId, resolvedBrokerId);
        return new MyTasksResponseDto(summaries, totalCount);
    }

    // F0004: Paginated task list (GET /tasks)
    public async Task<(TaskListResponseDto? Dto, string? ErrorCode)> GetTaskListAsync(
        TaskListQuery query, ICurrentUserService user, CancellationToken ct = default)
    {
        // View authorization check:
        // "assignedByMe" requires DistributionManager or Admin (only those roles appear
        //  in F0004 policy with creator-based access). We do a Casbin read check with
        //  creator == caller to determine eligibility.
        if (query.View == "assignedByMe")
        {
            var creatorAttrs = new Dictionary<string, object>
            {
                ["assignee"] = "__no_assignee__",
                ["creator"]  = user.UserId,
                ["subjectId"] = user.UserId,
            };
            var viewAuthorized = false;
            foreach (var role in user.Roles)
            {
                if (await authz.AuthorizeAsync(role, "task", "read", creatorAttrs))
                {
                    viewAuthorized = true;
                    break;
                }
            }
            if (!viewAuthorized)
                return (null, "view_not_authorized");
        }

        var (tasks, totalCount) = await taskRepo.GetTaskListAsync(query, ct);
        var today = DateTime.UtcNow.Date;

        // Batch-resolve display names for assignees and creators
        var userIds = tasks.SelectMany(t => new[] { t.AssignedToUserId, t.CreatedByUserId })
            .Distinct()
            .ToList();
        var profiles = new Dictionary<Guid, string?>();
        foreach (var uid in userIds)
        {
            if (!profiles.ContainsKey(uid))
            {
                var p = await userProfileRepo.GetByIdAsync(uid, ct);
                profiles[uid] = p?.DisplayName;
            }
        }

        // Batch-resolve linked entity names
        var entityNames = new Dictionary<(string, Guid), string?>();
        foreach (var t in tasks.Where(t => t.LinkedEntityType is not null && t.LinkedEntityId.HasValue))
        {
            var key = (t.LinkedEntityType!, t.LinkedEntityId!.Value);
            if (!entityNames.ContainsKey(key))
                entityNames[key] = await taskRepo.ResolveLinkedEntityNameAsync(t.LinkedEntityType, t.LinkedEntityId, ct);
        }

        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var page = Math.Max(1, query.Page);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = tasks.Select(t => new TaskListItemDto(
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
            t.AssignedToUserId, profiles.GetValueOrDefault(t.AssignedToUserId),
            t.CreatedByUserId, profiles.GetValueOrDefault(t.CreatedByUserId),
            t.LinkedEntityType, t.LinkedEntityId,
            t.LinkedEntityType is not null && t.LinkedEntityId.HasValue
                ? entityNames.GetValueOrDefault((t.LinkedEntityType, t.LinkedEntityId.Value))
                : null,
            t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != "Done",
            t.CreatedAt, t.UpdatedAt, t.CompletedAt))
            .ToList();

        return (new TaskListResponseDto(items, page, pageSize, totalCount, totalPages), null);
    }

    // F0003-S0001 / F0004: Create Task
    // F0004: DistributionManager/Admin can assign to any active internal user.
    public async Task<(TaskDto? Dto, string? ErrorCode)> CreateAsync(
        TaskCreateRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
    {
        // Determine if the caller is a manager-role (can cross-assign)
        var isManager = user.Roles.Contains("DistributionManager") || user.Roles.Contains("Admin");

        // Self-assignment guard: non-managers may only assign to themselves
        if (!isManager && dto.AssignedToUserId != user.UserId)
            return (null, "forbidden");

        // For managers assigning to others: validate the assignee exists and is active
        if (isManager && dto.AssignedToUserId != user.UserId)
        {
            var assigneeProfile = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
            if (assigneeProfile is null)
                return (null, "invalid_assignee");
            if (!assigneeProfile.IsActive)
                return (null, "inactive_assignee");
        }

        // LinkedEntity pairing guard (also validated by FluentValidation, but defense-in-depth)
        if ((dto.LinkedEntityType is not null) != (dto.LinkedEntityId is not null))
            return (null, "validation_error");

        // Resolve display names for timeline payload
        var assigneeProfile2 = dto.AssignedToUserId == user.UserId
            ? null
            : await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
        var assigneeDisplayName = dto.AssignedToUserId == user.UserId
            ? user.DisplayName
            : assigneeProfile2?.DisplayName;

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = "Open",
            Priority = dto.Priority ?? "Normal",
            DueDate = dto.DueDate,
            AssignedToUserId = dto.AssignedToUserId,
            LinkedEntityType = dto.LinkedEntityType,
            LinkedEntityId = dto.LinkedEntityId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await taskRepo.AddAsync(task, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Task",
            EntityId = task.Id,
            EventType = "TaskCreated",
            EventDescription = $"Task \"{task.Title}\" created",
            BrokerDescription = null, // InternalOnly
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                title = task.Title,
                assignedToUserId = task.AssignedToUserId,
                assignedToDisplayName = assigneeDisplayName,
                createdByUserId = user.UserId,
                createdByDisplayName = user.DisplayName,
                dueDate = task.DueDate,
                linkedEntityType = task.LinkedEntityType,
                linkedEntityId = task.LinkedEntityId,
            }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (await MapToDtoAsync(task, ct), null);
    }

    // F0003-S0002 / F0004: Update Task
    // F0004 additions:
    //   - creator-based access (Casbin already handles via creator attr)
    //   - status change restricted to assignee
    //   - reassignment: only creator can change assignedToUserId; emits TaskReassigned
    public async Task<(TaskDto? Dto, string? ErrorCode, string? TransitionFrom, string? TransitionTo)> UpdateAsync(
        Guid taskId, TaskUpdateRequestDto dto, IReadOnlySet<string> presentFields,
        uint rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(taskId, ct);
        if (task is null)
            return (null, "not_found", null, null);

        // Casbin authorization with both assignee and creator attributes
        if (!await AuthorizeTaskAsync(user, "update", task))
            return (null, "not_found", null, null); // Normalize to 404 — prevent IDOR

        // F0004: Only the assignee can change status
        if (dto.Status is not null && task.AssignedToUserId != user.UserId)
            return (null, "status_change_restricted", null, null);

        // F0004: Only the creator can reassign (change assignedToUserId)
        if (dto.AssignedToUserId.HasValue && task.CreatedByUserId != user.UserId)
            return (null, "forbidden", null, null);

        // Set RowVersion for optimistic concurrency
        task.RowVersion = rowVersion;

        var now = DateTime.UtcNow;
        var oldStatus = task.Status;
        var changedFields = new Dictionary<string, object?>();

        // Status transition validation
        if (dto.Status is not null && dto.Status != oldStatus)
        {
            if (!ValidTransitions.Contains((oldStatus, dto.Status)))
                return (null, "invalid_status_transition", oldStatus, dto.Status);
        }

        // Detect reassignment: assignedToUserId present and different from current
        var isReassignment = dto.AssignedToUserId.HasValue
            && dto.AssignedToUserId.Value != task.AssignedToUserId;

        if (isReassignment)
        {
            // Validate new assignee exists and is active
            var newAssigneeProfile = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId!.Value, ct);
            if (newAssigneeProfile is null)
                return (null, "invalid_assignee", null, null);
            if (!newAssigneeProfile.IsActive)
                return (null, "inactive_assignee", null, null);
        }

        // Apply present fields (non-reassignment mutations)
        if (dto.Title is not null)
        {
            if (dto.Title != task.Title)
                changedFields["title"] = new { from = task.Title, to = dto.Title };
            task.Title = dto.Title;
        }

        if (presentFields.Contains("description"))
        {
            if (dto.Description != task.Description)
                changedFields["description"] = new { from = task.Description, to = dto.Description };
            task.Description = dto.Description;
        }

        if (dto.Status is not null)
        {
            if (dto.Status != oldStatus)
                changedFields["status"] = new { from = oldStatus, to = dto.Status };
            task.Status = dto.Status;
        }

        if (dto.Priority is not null)
        {
            if (dto.Priority != task.Priority)
                changedFields["priority"] = new { from = task.Priority, to = dto.Priority };
            task.Priority = dto.Priority;
        }

        if (presentFields.Contains("dueDate"))
        {
            if (dto.DueDate != task.DueDate)
                changedFields["dueDate"] = new { from = task.DueDate, to = dto.DueDate };
            task.DueDate = dto.DueDate;
        }

        // CompletedAt handling for status transitions
        var previousCompletedAt = task.CompletedAt;
        if (dto.Status is not null && dto.Status != oldStatus)
        {
            if (dto.Status == "Done")
                task.CompletedAt = now;
            else if (oldStatus == "Done")
                task.CompletedAt = null;
        }

        task.UpdatedAt = now;
        task.UpdatedByUserId = user.UserId;

        // Emit appropriate timeline event
        string eventType;
        string eventDescription;
        string payloadJson;

        if (isReassignment)
        {
            var oldAssigneeProfile = await userProfileRepo.GetByIdAsync(task.AssignedToUserId, ct);
            var newAssigneeProfile = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId!.Value, ct);

            eventType = "TaskReassigned";
            eventDescription = $"Task reassigned from {oldAssigneeProfile?.DisplayName ?? task.AssignedToUserId.ToString()} to {newAssigneeProfile?.DisplayName ?? dto.AssignedToUserId.Value.ToString()}";
            payloadJson = JsonSerializer.Serialize(new
            {
                fromUserId = task.AssignedToUserId,
                fromDisplayName = oldAssigneeProfile?.DisplayName,
                toUserId = dto.AssignedToUserId!.Value,
                toDisplayName = newAssigneeProfile?.DisplayName,
            });

            task.AssignedToUserId = dto.AssignedToUserId.Value;
        }
        else if (dto.Status is not null && dto.Status != oldStatus && dto.Status == "Done")
        {
            eventType = "TaskCompleted";
            eventDescription = "Task completed";
            payloadJson = JsonSerializer.Serialize(new { completedAt = task.CompletedAt });
        }
        else if (dto.Status is not null && dto.Status != oldStatus && oldStatus == "Done")
        {
            eventType = "TaskReopened";
            eventDescription = "Task reopened";
            payloadJson = JsonSerializer.Serialize(new { previousCompletedAt });
        }
        else
        {
            eventType = "TaskUpdated";
            eventDescription = changedFields.Count > 0
                ? $"Task updated ({string.Join(", ", changedFields.Keys)})"
                : "Task updated";
            payloadJson = JsonSerializer.Serialize(new { changedFields });
        }

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Task",
            EntityId = task.Id,
            EventType = eventType,
            EventDescription = eventDescription,
            BrokerDescription = null, // InternalOnly
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = payloadJson,
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return (await MapToDtoAsync(task, ct), null, null, null);
    }

    // F0003-S0003 / F0004: Delete Task (soft delete)
    // F0004: Creator can also delete (Casbin handles via creator attr).
    public async Task<string?> DeleteAsync(
        Guid taskId, ICurrentUserService user, CancellationToken ct = default)
    {
        var task = await taskRepo.GetByIdAsync(taskId, ct);
        if (task is null)
            return "not_found";

        // Casbin authorization with both assignee and creator attributes
        if (!await AuthorizeTaskAsync(user, "delete", task))
            return "not_found"; // Normalize to 404 — prevent IDOR

        var now = DateTime.UtcNow;
        task.IsDeleted = true;
        task.DeletedAt = now;
        task.DeletedByUserId = user.UserId;
        task.UpdatedAt = now;
        task.UpdatedByUserId = user.UserId;

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Task",
            EntityId = task.Id,
            EventType = "TaskDeleted",
            EventDescription = "Task deleted",
            BrokerDescription = null, // InternalOnly
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new { }),
        }, ct);

        await unitOfWork.CommitAsync(ct);

        return null;
    }

    /// <summary>
    /// Check Casbin authorization for a task action against the fetched entity.
    /// F0004: Hydrates both assignee AND creator attributes for the enforcer.
    /// Used by Update/Delete/GetById to avoid a second DB fetch (TOCTOU fix).
    /// </summary>
    private async Task<bool> AuthorizeTaskAsync(ICurrentUserService user, string action, TaskItem task)
    {
        var attrs = new Dictionary<string, object>
        {
            ["assignee"]  = task.AssignedToUserId,
            ["creator"]   = task.CreatedByUserId,
            ["subjectId"] = user.UserId,
        };
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "task", action, attrs))
                return true;
        }
        return false;
    }

    private async Task<TaskDto> MapToDtoAsync(TaskItem t, CancellationToken ct)
    {
        var assigneeProfile = await userProfileRepo.GetByIdAsync(t.AssignedToUserId, ct);
        var creatorProfile = t.CreatedByUserId == t.AssignedToUserId
            ? assigneeProfile
            : await userProfileRepo.GetByIdAsync(t.CreatedByUserId, ct);
        var linkedEntityName = await taskRepo.ResolveLinkedEntityNameAsync(t.LinkedEntityType, t.LinkedEntityId, ct);

        return new TaskDto(
            t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
            t.AssignedToUserId, assigneeProfile?.DisplayName,
            t.CreatedByUserId, creatorProfile?.DisplayName,
            t.LinkedEntityType, t.LinkedEntityId, linkedEntityName,
            t.CreatedAt, t.UpdatedAt, t.CompletedAt, t.RowVersion);
    }

    private void AuditBrokerUserRead(ICurrentUserService user, string resource, Guid? entityId, Guid? resolvedBrokerId = null)
    {
        if (!user.Roles.Contains("BrokerUser")) return;
        _logger.LogInformation(
            "BrokerUser access: {Resource} by BrokerTenantId={BrokerTenantId} ResolvedBrokerId={ResolvedBrokerId} EntityId={EntityId} OccurredAt={OccurredAt}",
            resource,
            user.BrokerTenantId,
            resolvedBrokerId,
            entityId,
            DateTime.UtcNow);
    }
}
