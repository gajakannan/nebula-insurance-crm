namespace Nebula.Application.DTOs;

public record SubmissionBindRequestDto(
    string? IdempotencyKey = null);

public record SubmissionBindHandoffDto(
    Guid Id,
    Guid SubmissionId,
    string IdempotencyKey,
    string Status,
    Guid CorrelationId,
    DateTime RequestedAt,
    DateTime? CompletedAt);
