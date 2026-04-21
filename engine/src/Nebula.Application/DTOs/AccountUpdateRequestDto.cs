namespace Nebula.Application.DTOs;

public record AccountUpdateRequestDto(
    string? DisplayName,
    string? LegalName,
    string? TaxId,
    string? Industry,
    string? PrimaryLineOfBusiness,
    string? TerritoryCode,
    string? Region,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);
