namespace Nebula.Application.DTOs;

public record SubmissionApprovalDecisionDto(
    Guid Id,
    Guid SubmissionId,
    string Decision,
    Guid ApproverUserId,
    string Reason,
    IReadOnlyList<string> BlockingConditions,
    DateTime DecidedAt);

public record SubmissionApprovalRequestDto(
    string Decision,
    string Reason,
    IReadOnlyList<string>? BlockingConditions = null);
