namespace Nebula.Application.DTOs;

public sealed record WorkQueueDto(
    Guid Id,
    string Name,
    string WorkType,
    string Status,
    bool IsFallback,
    string? Description,
    int ActiveMemberCount,
    int OpenItemCount,
    uint RowVersion);

public sealed record WorkQueueUpsertRequestDto(
    string Name,
    string WorkType,
    string Status,
    string? Description,
    IReadOnlyList<QueueMemberUpsertRequestDto> Members);

public sealed record QueueMemberUpsertRequestDto(
    Guid UserProfileId,
    string Role,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);

public sealed record AssignmentRuleDto(
    Guid Id,
    Guid WorkQueueId,
    string RuleType,
    int Precedence,
    int Version,
    string Status,
    string ConditionsJson,
    string OutcomeJson,
    uint RowVersion);

public sealed record AssignmentRuleUpsertRequestDto(
    Guid WorkQueueId,
    string RuleType,
    int Precedence,
    string Status,
    string ConditionsJson,
    string OutcomeJson);

public sealed record CoverageWindowDto(
    Guid Id,
    Guid CoveredUserId,
    Guid BackupUserId,
    Guid? WorkQueueId,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? Reason,
    uint RowVersion);

public sealed record CoverageWindowUpsertRequestDto(
    Guid CoveredUserId,
    Guid BackupUserId,
    Guid? WorkQueueId,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string? Reason);

public sealed record QueueWorkItemDto(
    Guid Id,
    Guid WorkQueueId,
    string SourceType,
    Guid SourceId,
    Guid? AssignedToUserId,
    string QueueStatus,
    DateTime RoutedAt,
    string? RuleVersion,
    string? MatchReason,
    uint RowVersion);

public sealed record QueueReassignmentRequestDto(Guid AssignedToUserId, string Reason);

public sealed record QueueRebalanceRequestDto(string Strategy, int? MaxItems, string Reason);

public sealed record QueueRouteRequestDto(string SourceType, Guid SourceId);

public sealed record RoutingEventDto(
    Guid Id,
    Guid? QueueWorkItemId,
    string SourceType,
    Guid SourceId,
    string Outcome,
    string ReasonCode,
    Guid? ActorUserId,
    DateTime OccurredAt,
    string DecisionPayloadJson);
