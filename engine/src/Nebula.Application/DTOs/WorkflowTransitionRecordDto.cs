namespace Nebula.Application.DTOs;

public record WorkflowTransitionRecordDto(
    Guid Id,
    string WorkflowType,
    Guid EntityId,
    string? FromState,
    string ToState,
    string? Reason,
    DateTime OccurredAt);
