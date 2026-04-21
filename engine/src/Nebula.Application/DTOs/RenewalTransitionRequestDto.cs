namespace Nebula.Application.DTOs;

public record RenewalTransitionRequestDto(
    string ToState,
    string? Reason,
    string? ReasonCode = null,
    string? ReasonDetail = null,
    Guid? BoundPolicyId = null,
    Guid? RenewalSubmissionId = null);
