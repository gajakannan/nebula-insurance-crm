using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class ServiceCaseService(
    IServiceCaseRepository serviceCaseRepo,
    ITaskRepository taskRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    IAuthorizationService authz,
    IUserProfileRepository userProfileRepo)
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["Intake"] = ["InProgress", "Waiting"],
        ["InProgress"] = ["Waiting", "Resolved"],
        ["Waiting"] = ["InProgress", "Resolved"],
        ["Resolved"] = ["Closed"],
    };

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> CreateAsync(
        ServiceCaseCreateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "create", ct))
            return (null, "forbidden");

        if (!await serviceCaseRepo.AccountExistsAsync(dto.AccountId, ct))
            return (null, "account_not_found");

        if (dto.PolicyId.HasValue)
        {
            if (!await serviceCaseRepo.PolicyExistsAsync(dto.PolicyId.Value, ct))
                return (null, "policy_not_found");
            if (!await serviceCaseRepo.PolicyBelongsToAccountAsync(dto.PolicyId.Value, dto.AccountId, ct))
                return (null, "policy_account_mismatch");
        }

        var ownerError = await ValidateOwnerAsync(dto.OwnerUserId, user, ct);
        if (ownerError is not null)
            return (null, ownerError);

        var now = DateTime.UtcNow;
        var serviceCase = new ServiceCase
        {
            CaseNumber = await serviceCaseRepo.NextCaseNumberAsync(ct),
            AccountId = dto.AccountId,
            PolicyId = dto.PolicyId,
            Summary = dto.Summary.Trim(),
            Description = TrimToNull(dto.Description),
            Type = dto.Type,
            Status = "Intake",
            Priority = dto.Priority,
            OwnerUserId = dto.OwnerUserId,
            DueDate = dto.DueDate,
            FollowUpSummary = TrimToNull(dto.FollowUpSummary),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        if (dto.ClaimReference is not null && HasClaimReferenceData(dto.ClaimReference))
            serviceCase.ClaimReference = BuildClaimReference(serviceCase.Id, dto.ClaimReference, user.UserId, now);

        serviceCase.Transitions.Add(new ServiceCaseTransition
        {
            ServiceCaseId = serviceCase.Id,
            FromStatus = null,
            ToStatus = "Intake",
            ActorUserId = user.UserId,
            OccurredAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        });

        await serviceCaseRepo.AddAsync(serviceCase, ct);
        await AddTimelineAsync(serviceCase, "ServiceCaseCreated", $"Service case created: {serviceCase.Summary}", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(ServiceCaseListResponseDto? Dto, string? ErrorCode)> ListAsync(
        ServiceCaseListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", ct))
            return (null, "forbidden");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var result = await serviceCaseRepo.ListAsync(query with { Page = page, PageSize = pageSize }, ct);
        var ownerProfiles = await userProfileRepo.GetByIdsAsync(result.Data.Select(c => c.OwnerUserId).Distinct().ToArray(), ct);
        var ownerDisplayNames = ownerProfiles.ToDictionary(p => p.Id, p => p.DisplayName);
        return (new ServiceCaseListResponseDto(
            result.Data.Select(serviceCase => MapToDto(serviceCase, ownerDisplayNames.GetValueOrDefault(serviceCase.OwnerUserId))).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages), null);
    }

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> GetByIdAsync(
        Guid serviceCaseId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        return serviceCase is null ? (null, "not_found") : (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid serviceCaseId,
        ServiceCaseUpdateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "update", ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        if (serviceCase is null)
            return (null, "not_found");
        if (serviceCase.Status == "Closed")
            return (null, "closed_service_case");
        if (IsStale(serviceCase, dto.RowVersion))
            return (null, "concurrency_conflict");

        if (dto.OwnerUserId.HasValue && dto.OwnerUserId.Value != serviceCase.OwnerUserId)
        {
            if (!await AuthorizeAsync(user, "assign", ct))
                return (null, "forbidden");
            var ownerError = await ValidateOwnerAsync(dto.OwnerUserId.Value, user, ct);
            if (ownerError is not null)
                return (null, ownerError);
            serviceCase.OwnerUserId = dto.OwnerUserId.Value;
        }

        if (dto.Summary is not null)
            serviceCase.Summary = dto.Summary.Trim();
        if (dto.Description is not null)
            serviceCase.Description = TrimToNull(dto.Description);
        if (dto.Priority is not null)
            serviceCase.Priority = dto.Priority;
        if (dto.DueDate.HasValue)
            serviceCase.DueDate = dto.DueDate;
        if (dto.FollowUpSummary is not null)
            serviceCase.FollowUpSummary = TrimToNull(dto.FollowUpSummary);
        if (dto.ResolutionSummary is not null)
            serviceCase.ResolutionSummary = TrimToNull(dto.ResolutionSummary);

        var now = DateTime.UtcNow;
        serviceCase.UpdatedAt = now;
        serviceCase.UpdatedByUserId = user.UserId;
        await AddTimelineAsync(serviceCase, "ServiceCaseUpdated", $"Service case updated: {serviceCase.Summary}", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> TransitionAsync(
        Guid serviceCaseId,
        ServiceCaseTransitionRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "transition", ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        if (serviceCase is null)
            return (null, "not_found");
        if (serviceCase.Status == "Closed")
            return (null, "closed_service_case");
        if (IsStale(serviceCase, dto.RowVersion))
            return (null, "concurrency_conflict");
        if (!AllowedTransitions.TryGetValue(serviceCase.Status, out var allowed) || !allowed.Contains(dto.ToStatus))
            return (null, "invalid_transition");
        if (dto.ToStatus == "Waiting" && string.IsNullOrWhiteSpace(dto.ReasonCode) && string.IsNullOrWhiteSpace(dto.Note))
            return (null, "missing_waiting_reason");
        if ((dto.ToStatus == "Resolved" || dto.ToStatus == "Closed") && string.IsNullOrWhiteSpace(dto.ResolutionSummary) && string.IsNullOrWhiteSpace(serviceCase.ResolutionSummary))
            return (null, "missing_resolution_summary");

        var now = DateTime.UtcNow;
        var fromStatus = serviceCase.Status;
        serviceCase.Status = dto.ToStatus;
        serviceCase.UpdatedAt = now;
        serviceCase.UpdatedByUserId = user.UserId;
        if (dto.ResolutionSummary is not null)
            serviceCase.ResolutionSummary = TrimToNull(dto.ResolutionSummary);
        if (dto.ToStatus == "Resolved")
            serviceCase.ResolvedAt ??= now;
        if (dto.ToStatus == "Closed")
            serviceCase.ClosedAt ??= now;

        serviceCase.Transitions.Add(new ServiceCaseTransition
        {
            ServiceCaseId = serviceCase.Id,
            FromStatus = fromStatus,
            ToStatus = dto.ToStatus,
            ActorUserId = user.UserId,
            OccurredAt = now,
            ReasonCode = TrimToNull(dto.ReasonCode),
            Note = TrimToNull(dto.Note),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        });

        await AddTimelineAsync(serviceCase, "ServiceCaseTransitioned", $"Service case moved from {fromStatus} to {dto.ToStatus}", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> UpdateClaimReferenceAsync(
        Guid serviceCaseId,
        ServiceCaseClaimReferenceUpdateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "update_claim_reference", ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        if (serviceCase is null)
            return (null, "not_found");
        if (serviceCase.Status == "Closed")
            return (null, "closed_service_case");
        if (IsStale(serviceCase, dto.RowVersion))
            return (null, "concurrency_conflict");

        var now = DateTime.UtcNow;
        if (serviceCase.ClaimReference is null)
            serviceCase.ClaimReference = BuildClaimReference(serviceCase.Id, dto, user.UserId, now);
        else
        {
            ApplyClaimReference(serviceCase.ClaimReference, dto);
            serviceCase.ClaimReference.UpdatedAt = now;
            serviceCase.ClaimReference.UpdatedByUserId = user.UserId;
        }

        serviceCase.UpdatedAt = now;
        serviceCase.UpdatedByUserId = user.UserId;
        await AddTimelineAsync(serviceCase, "ServiceCaseClaimReferenceUpdated", "Service case claim reference updated", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(ServiceCaseDto? Dto, string? ErrorCode)> LinkCommunicationAsync(
        Guid serviceCaseId,
        ServiceCaseCommunicationLinkRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "link_communication", ct) || !await AuthorizeCommunicationAsync(user, "read", ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        if (serviceCase is null)
            return (null, "not_found");
        if (serviceCase.Status == "Closed")
            return (null, "closed_service_case");
        if (IsStale(serviceCase, dto.RowVersion))
            return (null, "concurrency_conflict");
        if (!await serviceCaseRepo.CommunicationExistsAsync(dto.CommunicationEventId, ct))
            return (null, "communication_not_found");
        if (serviceCase.CommunicationLinks.Any(l => l.CommunicationEventId == dto.CommunicationEventId))
            return (null, "duplicate_communication_link");

        var now = DateTime.UtcNow;
        serviceCase.CommunicationLinks.Add(new ServiceCaseCommunicationLink
        {
            ServiceCaseId = serviceCase.Id,
            CommunicationEventId = dto.CommunicationEventId,
            LinkType = dto.LinkType ?? "Context",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        });
        serviceCase.UpdatedAt = now;
        serviceCase.UpdatedByUserId = user.UserId;

        await AddTimelineAsync(serviceCase, "ServiceCaseCommunicationLinked", "Communication linked to service case", user, now, ct, dto.CommunicationEventId);
        await unitOfWork.CommitAsync(ct);
        return (await MapToDtoAsync(serviceCase, ct), null);
    }

    public async Task<(TaskDto? Dto, string? ErrorCode)> CreateFollowUpTaskAsync(
        Guid serviceCaseId,
        ServiceCaseFollowUpTaskRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "create_follow_up", ct) || !await AuthorizeTaskAsync(user, "create", dto.AssignedToUserId, ct))
            return (null, "forbidden");

        var serviceCase = await serviceCaseRepo.GetByIdWithDetailsAsync(serviceCaseId, ct);
        if (serviceCase is null)
            return (null, "not_found");
        if (serviceCase.Status == "Closed")
            return (null, "closed_service_case");
        if (IsStale(serviceCase, dto.RowVersion))
            return (null, "concurrency_conflict");

        var ownerError = await ValidateOwnerAsync(dto.AssignedToUserId, user, ct);
        if (ownerError is not null)
            return (null, ownerError);

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = TrimToNull(dto.Description),
            Status = "Open",
            Priority = dto.Priority ?? "Normal",
            DueDate = dto.DueDate?.ToDateTime(TimeOnly.MinValue),
            AssignedToUserId = dto.AssignedToUserId,
            LinkedEntityType = "ServiceCase",
            LinkedEntityId = serviceCase.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await taskRepo.AddAsync(task, ct);
        serviceCase.TaskLinks.Add(new ServiceCaseTaskLink
        {
            ServiceCaseId = serviceCase.Id,
            TaskId = task.Id,
            Relationship = "FollowUp",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        });
        serviceCase.UpdatedAt = now;
        serviceCase.UpdatedByUserId = user.UserId;

        await AddTaskTimelineAsync(serviceCase, task, user, now, ct);
        await AddTimelineAsync(serviceCase, "ServiceCaseFollowUpTaskCreated", $"Follow-up task created: {task.Title}", user, now, ct, task.Id);
        await unitOfWork.CommitAsync(ct);
        return (MapTaskToDto(task), null);
    }

    private async Task<string?> ValidateOwnerAsync(Guid ownerUserId, ICurrentUserService user, CancellationToken ct)
    {
        var isManager = user.Roles.Contains("DistributionManager") || user.Roles.Contains("Admin");
        if (!isManager && ownerUserId != user.UserId)
            return "forbidden";

        var owner = await userProfileRepo.GetByIdAsync(ownerUserId, ct);
        if (owner is null)
            return "invalid_assignee";
        if (!owner.IsActive)
            return "inactive_assignee";

        return null;
    }

    private async Task AddTimelineAsync(
        ServiceCase serviceCase,
        string eventType,
        string description,
        ICurrentUserService user,
        DateTime occurredAt,
        CancellationToken ct,
        Guid? relatedId = null)
    {
        await timelineRepo.AddEventAsync(BuildTimelineEvent(serviceCase, "Account", serviceCase.AccountId, eventType, description, user, occurredAt, relatedId), ct);
        if (serviceCase.PolicyId.HasValue)
            await timelineRepo.AddEventAsync(BuildTimelineEvent(serviceCase, "Policy", serviceCase.PolicyId.Value, eventType, description, user, occurredAt, relatedId), ct);
    }

    private static ActivityTimelineEvent BuildTimelineEvent(
        ServiceCase serviceCase,
        string entityType,
        Guid entityId,
        string eventType,
        string description,
        ICurrentUserService user,
        DateTime occurredAt,
        Guid? relatedId)
    {
        return new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            EventDescription = description,
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                serviceCaseId = serviceCase.Id,
                serviceCase.CaseNumber,
                serviceCase.Status,
                serviceCase.Priority,
                serviceCase.OwnerUserId,
                serviceCase.AccountId,
                serviceCase.PolicyId,
                relatedId,
            }),
        };
    }

    private async Task AddTaskTimelineAsync(ServiceCase serviceCase, TaskItem task, ICurrentUserService user, DateTime occurredAt, CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Task",
            EntityId = task.Id,
            EventType = "TaskCreated",
            EventDescription = $"Task \"{task.Title}\" created from service case {serviceCase.CaseNumber}",
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                serviceCaseId = serviceCase.Id,
                serviceCase.CaseNumber,
                title = task.Title,
                assignedToUserId = task.AssignedToUserId,
            }),
        }, ct);
    }

    private async Task<bool> AuthorizeAsync(ICurrentUserService user, string action, CancellationToken ct)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "service_case", action, new Dictionary<string, object>
            {
                ["subjectId"] = user.UserId,
            }))
                return true;
        }

        return false;
    }

    private async Task<bool> AuthorizeCommunicationAsync(ICurrentUserService user, string action, CancellationToken ct)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "communication_event", action, new Dictionary<string, object>
            {
                ["subjectId"] = user.UserId,
            }))
                return true;
        }

        return false;
    }

    private async Task<bool> AuthorizeTaskAsync(ICurrentUserService user, string action, Guid assigneeId, CancellationToken ct)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "task", action, new Dictionary<string, object>
            {
                ["assignee"] = assigneeId,
                ["creator"] = user.UserId,
            }))
                return true;
        }

        return false;
    }

    private static bool IsStale(ServiceCase serviceCase, uint? rowVersion) =>
        rowVersion.HasValue && serviceCase.RowVersion != 0 && serviceCase.RowVersion != rowVersion.Value;

    private static bool HasClaimReferenceData(ServiceCaseClaimReferenceUpdateRequestDto dto) =>
        dto.DateOfLoss.HasValue
        || !string.IsNullOrWhiteSpace(dto.CarrierClaimNumber)
        || !string.IsNullOrWhiteSpace(dto.ClaimantDisplayName)
        || !string.IsNullOrWhiteSpace(dto.LossSummary)
        || !string.IsNullOrWhiteSpace(dto.CarrierContactReference);

    private static ServiceCaseClaimReference BuildClaimReference(
        Guid serviceCaseId,
        ServiceCaseClaimReferenceUpdateRequestDto dto,
        Guid userId,
        DateTime now)
    {
        var claimReference = new ServiceCaseClaimReference
        {
            ServiceCaseId = serviceCaseId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        ApplyClaimReference(claimReference, dto);
        return claimReference;
    }

    private static void ApplyClaimReference(ServiceCaseClaimReference claimReference, ServiceCaseClaimReferenceUpdateRequestDto dto)
    {
        claimReference.CarrierClaimNumber = TrimToNull(dto.CarrierClaimNumber);
        claimReference.DateOfLoss = dto.DateOfLoss;
        claimReference.ClaimantDisplayName = TrimToNull(dto.ClaimantDisplayName);
        claimReference.LossSummary = TrimToNull(dto.LossSummary);
        claimReference.CarrierContactReference = TrimToNull(dto.CarrierContactReference);
    }

    private async Task<ServiceCaseDto> MapToDtoAsync(ServiceCase serviceCase, CancellationToken ct)
    {
        var owner = await userProfileRepo.GetByIdAsync(serviceCase.OwnerUserId, ct);
        return MapToDto(serviceCase, owner?.DisplayName);
    }

    private static ServiceCaseDto MapToDto(ServiceCase serviceCase, string? ownerDisplayName) => new(
        serviceCase.Id,
        serviceCase.CaseNumber,
        serviceCase.AccountId,
        serviceCase.PolicyId,
        serviceCase.Summary,
        serviceCase.Description,
        serviceCase.Type,
        serviceCase.Status,
        serviceCase.Priority,
        serviceCase.OwnerUserId,
        ownerDisplayName,
        serviceCase.Account?.Name,
        serviceCase.Policy?.PolicyNumber,
        serviceCase.DueDate,
        serviceCase.FollowUpSummary,
        serviceCase.ClaimReference is not null,
        LatestActivityAt(serviceCase),
        serviceCase.ClaimReference is null
            ? null
            : new ServiceCaseClaimReferenceDto(
                serviceCase.ClaimReference.CarrierClaimNumber,
                serviceCase.ClaimReference.DateOfLoss,
                serviceCase.ClaimReference.ClaimantDisplayName,
                serviceCase.ClaimReference.LossSummary,
                serviceCase.ClaimReference.CarrierContactReference,
                serviceCase.ClaimReference.UpdatedByUserId,
                serviceCase.ClaimReference.UpdatedAt),
        serviceCase.CommunicationLinks
            .OrderBy(l => l.CreatedAt)
            .Select(l => new ServiceCaseCommunicationLinkDto(l.CommunicationEventId, l.LinkType, l.CreatedByUserId, l.CreatedAt))
            .ToList(),
        serviceCase.TaskLinks
            .OrderBy(l => l.CreatedAt)
            .Select(l => new ServiceCaseTaskLinkDto(l.TaskId, l.Relationship, l.CreatedByUserId, l.CreatedAt))
            .ToList(),
        serviceCase.Transitions
            .OrderBy(t => t.OccurredAt)
            .Select(t => new ServiceCaseTransitionDto(t.FromStatus, t.ToStatus, t.ActorUserId, t.OccurredAt, t.ReasonCode, t.Note))
            .ToList(),
        serviceCase.ResolvedAt,
        serviceCase.ClosedAt,
        serviceCase.ResolutionSummary,
        serviceCase.CreatedByUserId,
        serviceCase.CreatedAt,
        serviceCase.UpdatedAt,
        serviceCase.RowVersion);

    private static DateTime? LatestActivityAt(ServiceCase serviceCase)
    {
        var timestamps = new List<DateTime?> { serviceCase.UpdatedAt, serviceCase.CreatedAt, serviceCase.ResolvedAt, serviceCase.ClosedAt };
        timestamps.AddRange(serviceCase.Transitions.Select(t => (DateTime?)t.OccurredAt));
        timestamps.AddRange(serviceCase.CommunicationLinks.Select(l => (DateTime?)l.CreatedAt));
        timestamps.AddRange(serviceCase.TaskLinks.Select(l => (DateTime?)l.CreatedAt));
        timestamps.Add(serviceCase.ClaimReference?.UpdatedAt);
        return timestamps.Where(t => t.HasValue).Max();
    }

    private static TaskDto MapTaskToDto(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.DueDate,
        task.AssignedToUserId,
        null,
        task.CreatedByUserId,
        null,
        task.LinkedEntityType,
        task.LinkedEntityId,
        null,
        task.CreatedAt,
        task.UpdatedAt,
        task.CompletedAt,
        task.RowVersion);

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
