namespace Nebula.Application.DTOs;

public record AccountSummaryProjection(
    int ActivePolicyCount,
    int OpenSubmissionCount,
    int RenewalDueCount,
    DateTime? LastActivityAt);
