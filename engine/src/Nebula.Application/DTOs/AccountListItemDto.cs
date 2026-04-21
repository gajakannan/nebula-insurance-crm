namespace Nebula.Application.DTOs;

public record AccountListItemDto(
    Guid Id,
    string DisplayName,
    string? LegalName,
    string? TaxId,
    string Status,
    Guid? BrokerOfRecordId,
    string? BrokerOfRecordName,
    string? TerritoryCode,
    string? Region,
    string? PrimaryLineOfBusiness,
    DateTime? LastActivityAt,
    int? ActivePolicyCount,
    int? OpenSubmissionCount,
    int? RenewalDueCount,
    string RowVersion);
