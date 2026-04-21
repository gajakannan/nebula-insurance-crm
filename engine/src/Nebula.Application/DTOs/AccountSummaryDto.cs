namespace Nebula.Application.DTOs;

public record AccountSummaryDto(
    Guid Id,
    string DisplayName,
    string Status,
    string? BrokerOfRecordName,
    string? PrimaryProducerDisplayName,
    string? TerritoryCode,
    string? Region,
    int ActivePolicyCount,
    int OpenSubmissionCount,
    int RenewalDueCount,
    DateTime? LastActivityAt,
    string RowVersion);
