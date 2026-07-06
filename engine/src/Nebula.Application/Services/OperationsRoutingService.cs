using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class OperationsRoutingService(
    IQueueAssignmentRepository queueRepo,
    ITaskRoutingSource taskSource,
    ISubmissionRoutingSource submissionSource,
    IRenewalRoutingSource renewalSource,
    IDistributionOwnershipResolver ownershipResolver,
    ITimelineRepository timelineRepo,
    IUserProfileRepository userProfileRepo,
    IAuthorizationService authz,
    IUnitOfWork unitOfWork)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(IReadOnlyList<WorkQueueDto> Items, int TotalCount)> ListQueuesAsync(
        string? workType, string? status, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", null))
            return ([], 0);

        var (queues, total) = await queueRepo.ListQueuesAsync(workType, status, page, pageSize, ct);
        return (queues.Select(MapQueue).ToList(), total);
    }

    public async Task<(WorkQueueDto? Dto, string? ErrorCode)> GetQueueAsync(
        Guid queueId, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", queueId))
            return (null, "policy_denied");

        var queue = await queueRepo.GetQueueAsync(queueId, ct);
        return queue is null ? (null, "queue_not_found") : (MapQueue(queue), null);
    }

    public async Task<(WorkQueueDto? Dto, string? ErrorCode)> UpsertQueueAsync(
        Guid? queueId, WorkQueueUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "manage", queueId))
            return (null, "policy_denied");

        var now = DateTime.UtcNow;
        WorkQueue queue;
        var isCreate = !queueId.HasValue;

        if (isCreate)
        {
            queue = new WorkQueue
            {
                CreatedAt = now,
                CreatedByUserId = user.UserId,
            };
            await queueRepo.AddQueueAsync(queue, ct);
        }
        else
        {
            var existingQueue = await queueRepo.GetQueueForUpdateAsync(queueId.GetValueOrDefault(), ct);
            if (existingQueue is null)
                return (null, "queue_not_found");
            queue = existingQueue;
            if (rowVersion.HasValue && queue.RowVersion != rowVersion.Value)
                return (null, "concurrency_conflict");
        }

        if (await queueRepo.QueueNameExistsAsync(dto.Name, dto.WorkType, queueId, ct))
            return (null, "assignment_rule_conflict");

        if (!isCreate && queue.Status == "Active" && dto.Status == "Inactive" && await queueRepo.HasOpenItemsAsync(queue.Id, ct))
            return (null, "queue_inactive");

        queue.Name = dto.Name.Trim();
        queue.WorkType = dto.WorkType;
        queue.Status = dto.Status;
        queue.Description = dto.Description;
        queue.UpdatedAt = now;
        queue.UpdatedByUserId = user.UserId;

        var requestedMembers = dto.Members.ToList();
        foreach (var existing in queue.Members.ToList())
        {
            var requested = requestedMembers.FirstOrDefault(member => member.UserProfileId == existing.UserProfileId);
            if (requested is not null)
            {
                existing.Role = requested.Role;
                existing.EffectiveFrom = requested.EffectiveFrom;
                existing.EffectiveTo = requested.EffectiveTo;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = user.UserId;
                requestedMembers.Remove(requested);
                continue;
            }

            existing.IsDeleted = true;
            existing.DeletedAt = now;
            existing.DeletedByUserId = user.UserId;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = user.UserId;
        }

        foreach (var member in requestedMembers)
        {
            queue.Members.Add(new WorkQueueMember
            {
                WorkQueueId = queue.Id,
                UserProfileId = member.UserProfileId,
                Role = member.Role,
                EffectiveFrom = member.EffectiveFrom,
                EffectiveTo = member.EffectiveTo,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = user.UserId,
                UpdatedByUserId = user.UserId,
            });
        }

        await AddDecisionAsync(null, dto.WorkType, queue.Id, isCreate ? "QueueCreated" : "QueueUpdated", "queue_mutation", user, new { queue.Id, queue.Name }, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapQueue(queue), null);
    }

    public async Task<(IReadOnlyList<AssignmentRuleDto> Items, int TotalCount)> ListRulesAsync(
        Guid? queueId, string? status, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", queueId))
            return ([], 0);

        var rules = await queueRepo.ListRulesAsync(queueId, status, ct);
        return (rules.Select(MapRule).ToList(), rules.Count);
    }

    public async Task<(AssignmentRuleDto? Dto, string? ErrorCode)> UpsertRuleAsync(
        Guid? ruleId, AssignmentRuleUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "manage", dto.WorkQueueId))
            return (null, "policy_denied");

        var queue = await queueRepo.GetQueueAsync(dto.WorkQueueId, ct);
        if (queue is null)
            return (null, "queue_not_found");

        var now = DateTime.UtcNow;
        AssignmentRule rule;
        if (ruleId.HasValue)
        {
            rule = await queueRepo.GetRuleForUpdateAsync(ruleId.Value, ct)
                ?? null!;
            if (rule is null)
                return (null, "not_found");
            if (rowVersion.HasValue && rule.RowVersion != rowVersion.Value)
                return (null, "concurrency_conflict");
            rule.Version += 1;
        }
        else
        {
            rule = new AssignmentRule
            {
                WorkQueueId = dto.WorkQueueId,
                CreatedAt = now,
                CreatedByUserId = user.UserId,
            };
            await queueRepo.AddRuleAsync(rule, ct);
        }

        rule.RuleType = dto.RuleType;
        rule.Precedence = dto.Precedence;
        rule.Status = dto.Status;
        rule.ConditionsJson = dto.ConditionsJson;
        rule.OutcomeJson = dto.OutcomeJson;
        rule.ActivatedAt = dto.Status == "Active" ? now : rule.ActivatedAt;
        rule.ActivatedByUserId = dto.Status == "Active" ? user.UserId : rule.ActivatedByUserId;
        rule.UpdatedAt = now;
        rule.UpdatedByUserId = user.UserId;

        await AddDecisionAsync(null, queue.WorkType, queue.Id, "AssignmentRuleVersioned", "assignment_rule_versioned", user, new { rule.Id, rule.RuleType, rule.Version }, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapRule(rule), null);
    }

    public async Task<(IReadOnlyList<CoverageWindowDto> Items, int TotalCount)> ListCoverageWindowsAsync(
        Guid? queueId, Guid? coveredUserId, string? status, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", queueId))
            return ([], 0);

        var windows = await queueRepo.ListCoverageWindowsAsync(queueId, coveredUserId, status, ct);
        return (windows.Select(MapCoverage).ToList(), windows.Count);
    }

    public async Task<(CoverageWindowDto? Dto, string? ErrorCode)> UpsertCoverageWindowAsync(
        Guid? coverageWindowId, CoverageWindowUpsertRequestDto dto, uint? rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "manage", dto.WorkQueueId))
            return (null, "policy_denied");

        if (await queueRepo.CoverageOverlapsAsync(dto.CoveredUserId, dto.BackupUserId, dto.WorkQueueId, dto.StartsAt, dto.EndsAt, coverageWindowId, ct))
            return (null, "coverage_window_overlap");

        var now = DateTime.UtcNow;
        CoverageWindow window;
        if (coverageWindowId.HasValue)
        {
            window = await queueRepo.GetCoverageForUpdateAsync(coverageWindowId.Value, ct)
                ?? null!;
            if (window is null)
                return (null, "not_found");
            if (rowVersion.HasValue && window.RowVersion != rowVersion.Value)
                return (null, "concurrency_conflict");
        }
        else
        {
            window = new CoverageWindow
            {
                CreatedAt = now,
                CreatedByUserId = user.UserId,
            };
            await queueRepo.AddCoverageWindowAsync(window, ct);
        }

        window.CoveredUserId = dto.CoveredUserId;
        window.BackupUserId = dto.BackupUserId;
        window.WorkQueueId = dto.WorkQueueId;
        window.StartsAt = dto.StartsAt;
        window.EndsAt = dto.EndsAt;
        window.Status = dto.Status;
        window.Reason = dto.Reason;
        window.UpdatedAt = now;
        window.UpdatedByUserId = user.UserId;

        await AddDecisionAsync(null, "Coverage", window.Id, "CoverageWindowUpdated", "coverage_window_updated", user, new { window.Id, window.CoveredUserId, window.BackupUserId }, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapCoverage(window), null);
    }

    public async Task<(QueueWorkItemDto? Dto, string? ErrorCode)> RouteAsync(
        string sourceType, Guid sourceId, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "assign", null))
            return (null, "policy_denied");

        var source = await ResolveSourceAsync(sourceType, sourceId, ct);
        if (source is null)
            return (null, "not_found");

        var existing = await queueRepo.GetActiveQueueItemAsync(sourceType, sourceId, ct);
        if (existing is not null)
        {
            await AddDecisionAsync(existing.Id, sourceType, sourceId, "Skipped", "duplicate_route_event", user, new { existing.Id }, DateTime.UtcNow, ct);
            await unitOfWork.CommitAsync(ct);
            return (MapQueueItem(existing), null);
        }

        var now = DateTime.UtcNow;
        var (queue, assignee, reason, ruleVersion) = await ResolveRouteAsync(source, now, ct);
        if (queue is null)
            return (null, "routing_no_match");

        var item = new QueueWorkItem
        {
            WorkQueueId = queue.Id,
            SourceType = sourceType,
            SourceId = sourceId,
            AssignedToUserId = assignee,
            QueueStatus = assignee.HasValue ? "Assigned" : "Open",
            RoutedAt = now,
            RuleVersion = ruleVersion,
            MatchReason = reason,
            IdempotencyKey = $"{sourceType}:{sourceId}:{ruleVersion ?? "fallback"}",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await queueRepo.AddQueueItemAsync(item, ct);
        if (assignee.HasValue)
            await AssignSourceAsync(sourceType, sourceId, assignee.Value, user.UserId, now, ct);
        await AddDecisionAsync(item.Id, sourceType, sourceId, queue.IsFallback ? "Fallback" : "Routed", reason, user, new { queue.Id, assignee }, now, ct);
        await AddTimelineAsync(sourceType, sourceId, queue.IsFallback ? "RoutingFallbackApplied" : "WorkRouted", $"Work routed to {queue.Name}", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapQueueItem(item), null);
    }

    public async Task<(IReadOnlyList<QueueWorkItemDto> Items, int TotalCount)> ListQueueItemsAsync(
        Guid queueId, string? status, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", queueId))
            return ([], 0);

        var (items, total) = await queueRepo.ListQueueItemsAsync(queueId, status, page, pageSize, ct);
        return (items.Select(MapQueueItem).ToList(), total);
    }

    public async Task<(QueueWorkItemDto? Dto, string? ErrorCode)> ReassignAsync(
        Guid queueItemId, QueueReassignmentRequestDto dto, uint rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        var item = await queueRepo.GetQueueItemForUpdateAsync(queueItemId, ct);
        if (item is null)
            return (null, "not_found");
        if (!await AuthorizeAsync(user, "assign", item.WorkQueueId))
            return (null, "policy_denied");
        if (item.RowVersion != rowVersion)
            return (null, "concurrency_conflict");
        if (item.QueueStatus == "Closed")
            return (null, "queue_item_closed");

        var target = await userProfileRepo.GetByIdAsync(dto.AssignedToUserId, ct);
        if (target is null)
            return (null, "invalid_assignee");
        if (!target.IsActive)
            return (null, "inactive_assignee");

        var now = DateTime.UtcNow;
        item.AssignedToUserId = dto.AssignedToUserId;
        item.QueueStatus = "Assigned";
        item.UpdatedAt = now;
        item.UpdatedByUserId = user.UserId;
        await AssignSourceAsync(item.SourceType, item.SourceId, dto.AssignedToUserId, user.UserId, now, ct);
        await AddDecisionAsync(item.Id, item.SourceType, item.SourceId, "Reassigned", "manual_override", user, new { item.Id, dto.AssignedToUserId, dto.Reason }, now, ct);
        await AddTimelineAsync(item.SourceType, item.SourceId, "QueueWorkItemReassigned", $"Queue work reassigned to {target.DisplayName}", user, now, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapQueueItem(item), null);
    }

    public async Task<(IReadOnlyList<QueueWorkItemDto>? Items, string? ErrorCode)> RebalanceAsync(
        Guid queueId, QueueRebalanceRequestDto dto, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "assign", queueId))
            return (null, "policy_denied");

        var queue = await queueRepo.GetQueueAsync(queueId, ct);
        if (queue is null)
            return (null, "queue_not_found");

        var (items, _) = await queueRepo.ListQueueItemsAsync(queueId, "Open", 1, dto.MaxItems ?? 25, ct);
        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            await AddDecisionAsync(item.Id, item.SourceType, item.SourceId, "Rebalanced", dto.Strategy, user, new { item.Id, dto.Reason }, now, ct);
        }
        await unitOfWork.CommitAsync(ct);
        return (items.Select(MapQueueItem).ToList(), null);
    }

    public async Task<(IReadOnlyList<RoutingEventDto> Items, int TotalCount)> ListRoutingEventsAsync(
        string? sourceType, Guid? sourceId, Guid? queueItemId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await AuthorizeAsync(user, "read", null))
            return ([], 0);

        var (events, total) = await queueRepo.ListRoutingEventsAsync(sourceType, sourceId, queueItemId, page, pageSize, ct);
        return (events.Select(MapEvent).ToList(), total);
    }

    private async Task<(WorkQueue? Queue, Guid? Assignee, string Reason, string? RuleVersion)> ResolveRouteAsync(RoutingSourceSummary source, DateTime now, CancellationToken ct)
    {
        if (source.CurrentAssigneeId.HasValue)
        {
            var coverage = (await queueRepo.ListActiveCoverageAsync(source.CurrentAssigneeId.Value, source.SourceType, now, ct)).FirstOrDefault();
            if (coverage is not null)
            {
                var queue = coverage.WorkQueueId.HasValue
                    ? await queueRepo.GetQueueAsync(coverage.WorkQueueId.Value, ct)
                    : await queueRepo.GetFallbackQueueAsync(source.SourceType, ct);
                return (queue, coverage.BackupUserId, "coverage_window_match", "coverage");
            }
        }

        var owner = await ownershipResolver.ResolveOwnerAsync(source, ct);
        if (owner.HasValue)
        {
            var rules = await queueRepo.ListActiveRulesAsync(source.SourceType, ct);
            var rule = rules.FirstOrDefault(r => r.RuleType == "TerritoryOwnership" || r.RuleType == "WorkloadBalance");
            if (rule is not null)
                return (rule.WorkQueue, owner, "rule_match", $"{rule.RuleType}:{rule.Version}");
        }

        var fallback = await queueRepo.GetFallbackQueueAsync(source.SourceType, ct);
        return (fallback, null, "no_rule_match_fallback", "fallback");
    }

    private Task<RoutingSourceSummary?> ResolveSourceAsync(string sourceType, Guid sourceId, CancellationToken ct) =>
        sourceType switch
        {
            "Task" => taskSource.GetSummaryAsync(sourceId, ct),
            "Submission" => submissionSource.GetSummaryAsync(sourceId, ct),
            "Renewal" => renewalSource.GetSummaryAsync(sourceId, ct),
            _ => Task.FromResult<RoutingSourceSummary?>(null),
        };

    private Task<bool> AssignSourceAsync(string sourceType, Guid sourceId, Guid assignedToUserId, Guid actorUserId, DateTime now, CancellationToken ct) =>
        sourceType switch
        {
            "Task" => taskSource.AssignAsync(sourceId, assignedToUserId, actorUserId, now, ct),
            "Submission" => submissionSource.AssignAsync(sourceId, assignedToUserId, actorUserId, now, ct),
            "Renewal" => renewalSource.AssignAsync(sourceId, assignedToUserId, actorUserId, now, ct),
            _ => Task.FromResult(false),
        };

    private async Task<bool> AuthorizeAsync(ICurrentUserService user, string action, Guid? queueId)
    {
        var attrs = new Dictionary<string, object>
        {
            ["subjectId"] = user.UserId,
            ["queueId"] = queueId?.ToString() ?? "",
        };

        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, "queue", action, attrs))
                return true;
        }

        return false;
    }

    private async Task AddDecisionAsync(Guid? queueItemId, string sourceType, Guid sourceId, string outcome, string reason, ICurrentUserService user, object payload, DateTime now, CancellationToken ct)
    {
        await queueRepo.AddRoutingDecisionAsync(new RoutingDecisionLog
        {
            QueueWorkItemId = queueItemId,
            SourceType = sourceType,
            SourceId = sourceId,
            Outcome = outcome,
            ReasonCode = reason,
            ActorUserId = user.UserId,
            OccurredAt = now,
            DecisionPayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        }, ct);
    }

    private async Task AddTimelineAsync(string sourceType, Guid sourceId, string eventType, string description, ICurrentUserService user, DateTime now, CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = sourceType,
            EntityId = sourceId,
            EventType = eventType,
            EventDescription = description,
            BrokerDescription = null,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
        }, ct);
    }

    private static WorkQueueDto MapQueue(WorkQueue queue) => new(
        queue.Id,
        queue.Name,
        queue.WorkType,
        queue.Status,
        queue.IsFallback,
        queue.Description,
        queue.Members.Count(m => !m.IsDeleted && (m.EffectiveTo == null || m.EffectiveTo > DateTime.UtcNow)),
        queue.QueueWorkItems.Count(i => i.QueueStatus == "Open" || i.QueueStatus == "Assigned" || i.QueueStatus == "InProgress"),
        queue.RowVersion);

    private static AssignmentRuleDto MapRule(AssignmentRule rule) => new(
        rule.Id, rule.WorkQueueId, rule.RuleType, rule.Precedence, rule.Version, rule.Status,
        rule.ConditionsJson, rule.OutcomeJson, rule.RowVersion);

    private static CoverageWindowDto MapCoverage(CoverageWindow window) => new(
        window.Id, window.CoveredUserId, window.BackupUserId, window.WorkQueueId,
        window.StartsAt, window.EndsAt, window.Status, window.Reason, window.RowVersion);

    private static QueueWorkItemDto MapQueueItem(QueueWorkItem item) => new(
        item.Id, item.WorkQueueId, item.SourceType, item.SourceId, item.AssignedToUserId,
        item.QueueStatus, item.RoutedAt, item.RuleVersion, item.MatchReason, item.RowVersion);

    private static RoutingEventDto MapEvent(RoutingDecisionLog log) => new(
        log.Id, log.QueueWorkItemId, log.SourceType, log.SourceId, log.Outcome, log.ReasonCode,
        log.ActorUserId, log.OccurredAt, log.DecisionPayloadJson);
}
