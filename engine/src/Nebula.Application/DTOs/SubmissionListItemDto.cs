namespace Nebula.Application.DTOs;

public record SubmissionListItemDto(
    Guid Id,
    Guid AccountId,
    string AccountDisplayName,
    string AccountStatus,
    Guid? AccountSurvivorId,
    string AccountName,
    string BrokerName,
    string? LineOfBusiness,
    string CurrentStatus,
    DateTime EffectiveDate,
    Guid AssignedToUserId,
    string? AssignedToDisplayName,
    DateTime CreatedAt,
    bool IsStale);
