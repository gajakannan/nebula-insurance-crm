namespace Nebula.Application.DTOs;

public record AccountCreateRequestDto(
    string DisplayName,
    string? LegalName,
    string? TaxId,
    string? Industry,
    string? PrimaryLineOfBusiness,
    Guid? BrokerOfRecordId,
    Guid? PrimaryProducerUserId,
    string? TerritoryCode,
    string? Region,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    Guid? LinkFromSubmissionId,
    Guid? LinkFromPolicyId);
