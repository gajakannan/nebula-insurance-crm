using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class CommunicationService(
    ICommunicationRepository communicationRepo,
    ITaskRepository taskRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    IAuthorizationService authz,
    IUserProfileRepository userProfileRepo)
{
    public async Task<(CommunicationEventDto? Dto, string? ErrorCode)> CreateAsync(
        CommunicationEventCreateRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "create", ct))
            return (null, "forbidden");

        var normalizedLinks = NormalizeLinks(dto.Links);
        if (normalizedLinks.Count != dto.Links.Count)
            return (null, "duplicate_link");

        foreach (var link in normalizedLinks)
        {
            if (!await communicationRepo.LinkedEntityExistsAsync(link.EntityType, link.EntityId, ct))
                return (null, "not_found");
        }

        var now = DateTime.UtcNow;
        var communication = new CommunicationEvent
        {
            Type = dto.Type,
            Direction = dto.Direction,
            Summary = dto.Summary.Trim(),
            Body = string.IsNullOrWhiteSpace(dto.Body) ? null : dto.Body.Trim(),
            OccurredAt = dto.OccurredAt,
            Visibility = "InternalOnly",
            EmailProvider = dto.EmailReference?.Provider,
            EmailMessageId = dto.EmailReference?.MessageId,
            EmailSubject = dto.EmailReference?.Subject,
            EmailSentAt = dto.EmailReference?.SentAt,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        foreach (var link in normalizedLinks)
        {
            communication.Links.Add(new CommunicationLink
            {
                EntityType = link.EntityType,
                EntityId = link.EntityId,
                IsPrimary = link.IsPrimary,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = user.UserId,
                UpdatedByUserId = user.UserId,
            });
        }

        foreach (var participant in dto.Participants)
        {
            communication.Participants.Add(new CommunicationParticipant
            {
                DisplayName = participant.DisplayName.Trim(),
                Email = participant.Email,
                ParticipantType = participant.ParticipantType,
                Role = participant.Role,
                LinkedEntityType = participant.LinkedEntityType,
                LinkedEntityId = participant.LinkedEntityId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = user.UserId,
                UpdatedByUserId = user.UserId,
            });
        }

        await communicationRepo.AddAsync(communication, ct);
        foreach (var link in normalizedLinks)
            await AddCommunicationTimelineAsync(link.EntityType, link.EntityId, communication, "CommunicationCaptured", user, now, ct);

        if (dto.FollowUp is not null)
        {
            var (task, error) = await CreateFollowUpTaskInternalAsync(communication, dto.FollowUp, user, now, ct);
            if (error is not null)
                return (null, error);

            communication.FollowUpTaskLinks.Add(new CommunicationFollowUpTaskLink
            {
                TaskId = task!.Id,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = user.UserId,
                UpdatedByUserId = user.UserId,
            });

            var primary = normalizedLinks.First(l => l.IsPrimary);
            await AddFollowUpTimelineAsync(primary.EntityType, primary.EntityId, communication.Id, task, user, now, ct);
        }

        await unitOfWork.CommitAsync(ct);
        return (MapToDto(communication), null);
    }

    public async Task<(CommunicationHistoryResponseDto? Dto, string? ErrorCode)> ListAsync(
        CommunicationHistoryQuery query, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", ct))
            return (null, "forbidden");

        if (!await communicationRepo.LinkedEntityExistsAsync(query.EntityType, query.EntityId, ct))
            return (null, "not_found");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var result = await communicationRepo.ListByEntityAsync(query.EntityType, query.EntityId, page, pageSize, ct);
        return (new CommunicationHistoryResponseDto(
            result.Data.Select(MapToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages), null);
    }

    public async Task<(CommunicationEventDto? Dto, string? ErrorCode)> GetByIdAsync(
        Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", ct))
            return (null, "forbidden");

        var communication = await communicationRepo.GetByIdWithDetailsAsync(id, ct);
        return communication is null ? (null, "not_found") : (MapToDto(communication), null);
    }

    public async Task<(TaskDto? Dto, string? ErrorCode)> CreateFollowUpTaskAsync(
        Guid communicationId,
        CommunicationEventFollowUpRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", ct) || !await AuthorizeAsync(user, "create_follow_up", ct))
            return (null, "forbidden");

        var communication = await communicationRepo.GetByIdWithDetailsAsync(communicationId, ct);
        if (communication is null)
            return (null, "not_found");

        var now = DateTime.UtcNow;
        var (task, error) = await CreateFollowUpTaskInternalAsync(communication, dto, user, now, ct);
        if (error is not null)
            return (null, error);

        communication.FollowUpTaskLinks.Add(new CommunicationFollowUpTaskLink
        {
            TaskId = task!.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        });

        var primary = communication.Links.FirstOrDefault(l => l.IsPrimary) ?? communication.Links.First();
        await AddFollowUpTimelineAsync(primary.EntityType, primary.EntityId, communication.Id, task, user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapTaskToDto(task), null);
    }

    public async Task<(CommunicationEventDto? Dto, string? ErrorCode)> CorrectOrRedactAsync(
        Guid communicationId,
        CommunicationEventCorrectionRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var action = dto.Action == "Redact" ? "redact" : "correct";
        if (!await AuthorizeAsync(user, "read", ct) || !await AuthorizeAsync(user, action, ct))
            return (null, "forbidden");

        var communication = await communicationRepo.GetByIdWithDetailsAsync(communicationId, ct);
        if (communication is null)
            return (null, "not_found");

        var now = DateTime.UtcNow;
        var correction = new CommunicationCorrection
        {
            CommunicationEventId = communication.Id,
            Action = dto.Action,
            Reason = dto.Reason.Trim(),
            PreviousSummary = communication.Summary,
            PreviousBody = communication.Body,
            NewSummary = dto.Action == "Correct" ? dto.Summary ?? communication.Summary : null,
            NewBody = dto.Action == "Correct" ? dto.Body ?? communication.Body : null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        if (dto.Action == "Redact")
        {
            communication.RedactedAt = now;
            communication.RedactedByUserId = user.UserId;
            communication.RedactionReason = dto.Reason.Trim();
        }
        else
        {
            communication.Summary = dto.Summary ?? communication.Summary;
            communication.Body = dto.Body ?? communication.Body;
        }

        communication.UpdatedAt = now;
        communication.UpdatedByUserId = user.UserId;
        await communicationRepo.AddCorrectionAsync(correction, ct);

        foreach (var link in communication.Links)
        {
            await AddCommunicationTimelineAsync(
                link.EntityType,
                link.EntityId,
                communication,
                dto.Action == "Redact" ? "CommunicationRedacted" : "CommunicationCorrected",
                user,
                now,
                ct,
                dto.Reason);
        }

        await unitOfWork.CommitAsync(ct);
        return (MapToDto(communication), null);
    }

    private async Task<(TaskItem? Task, string? ErrorCode)> CreateFollowUpTaskInternalAsync(
        CommunicationEvent communication,
        CommunicationEventFollowUpRequestDto dto,
        ICurrentUserService user,
        DateTime now,
        CancellationToken ct)
    {
        if (!await AuthorizeAsync(user, "create_follow_up", ct))
            return (null, "forbidden");

        if (!await communicationRepo.LinkedEntityExistsAsync(dto.LinkedEntityType, dto.LinkedEntityId, ct))
            return (null, "not_found");

        var isManager = user.Roles.Contains("DistributionManager") || user.Roles.Contains("Admin");
        if (!isManager && dto.AssignedToUserId != user.UserId)
            return (null, "forbidden");

        if (isManager && dto.AssignedToUserId != user.UserId)
        {
            var assignee = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
            if (assignee is null)
                return (null, "invalid_assignee");
            if (!assignee.IsActive)
                return (null, "inactive_assignee");
        }

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
            EventDescription = $"Task \"{task.Title}\" created from communication",
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                title = task.Title,
                linkedEntityType = task.LinkedEntityType,
                linkedEntityId = task.LinkedEntityId,
                communicationId = communication.Id,
            }),
        }, ct);

        return (task, null);
    }

    private async Task AddCommunicationTimelineAsync(
        string entityType,
        Guid entityId,
        CommunicationEvent communication,
        string eventType,
        ICurrentUserService user,
        DateTime occurredAt,
        CancellationToken ct,
        string? reason = null)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            EventDescription = eventType switch
            {
                "CommunicationCorrected" => $"Communication corrected: {communication.Summary}",
                "CommunicationRedacted" => "Communication redacted",
                _ => $"Communication captured: {communication.Summary}",
            },
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                communicationId = communication.Id,
                communicationType = communication.Type,
                summary = communication.RedactedAt.HasValue ? "[Redacted]" : communication.Summary,
                primaryEntityType = communication.Links.FirstOrDefault(l => l.IsPrimary)?.EntityType,
                primaryEntityId = communication.Links.FirstOrDefault(l => l.IsPrimary)?.EntityId,
                reason,
            }),
        }, ct);
    }

    private async Task AddFollowUpTimelineAsync(
        string entityType,
        Guid entityId,
        Guid communicationId,
        TaskItem task,
        ICurrentUserService user,
        DateTime occurredAt,
        CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = "CommunicationFollowUpTaskCreated",
            EventDescription = $"Follow-up task created: {task.Title}",
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                communicationId,
                taskId = task.Id,
                taskTitle = task.Title,
            }),
        }, ct);
    }

    private async Task<bool> AuthorizeAsync(ICurrentUserService user, string action, CancellationToken ct)
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

    private static IReadOnlyList<CommunicationLinkDto> NormalizeLinks(IReadOnlyList<CommunicationLinkDto> links)
    {
        return links
            .GroupBy(l => (l.EntityType, l.EntityId))
            .Select(g => g.First())
            .ToList();
    }

    private static CommunicationEventDto MapToDto(CommunicationEvent communication)
    {
        var isRedacted = communication.RedactedAt.HasValue;
        return new CommunicationEventDto(
            communication.Id,
            communication.Type,
            communication.Direction,
            isRedacted ? "[Redacted]" : communication.Summary,
            isRedacted ? null : communication.Body,
            communication.OccurredAt,
            communication.Visibility,
            communication.EmailMessageId is null && communication.EmailSubject is null
                ? null
                : new CommunicationEmailReferenceDto(
                    communication.EmailProvider,
                    communication.EmailMessageId,
                    communication.EmailSubject,
                    communication.EmailSentAt),
            communication.Participants.Select(p => new CommunicationParticipantDto(
                p.DisplayName, p.Email, p.ParticipantType, p.Role, p.LinkedEntityType, p.LinkedEntityId)).ToList(),
            communication.Links.Select(l => new CommunicationLinkDto(l.EntityType, l.EntityId, l.IsPrimary)).ToList(),
            communication.FollowUpTaskLinks.Select(l => l.TaskId).ToList(),
            isRedacted,
            communication.RedactedAt,
            communication.CreatedByUserId,
            communication.CreatedAt,
            communication.UpdatedAt,
            communication.RowVersion);
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
}
