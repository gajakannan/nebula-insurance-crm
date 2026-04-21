namespace Nebula.Application.DTOs;

public record RenewalListItemDto(
    Guid Id,
    Guid AccountId,
    string AccountDisplayName,
    string AccountStatus,
    Guid? AccountSurvivorId,
    string AccountName,
    string AccountIndustry,
    string AccountPrimaryState,
    string BrokerName,
    string BrokerLicenseNumber,
    string BrokerState,
    string PolicyNumber,
    string? Carrier,
    string? LineOfBusiness,
    string CurrentStatus,
    DateTime PolicyExpirationDate,
    DateTime TargetOutreachDate,
    Guid AssignedToUserId,
    string? AssignedUserDisplayName,
    string? Urgency,
    string RowVersion);
