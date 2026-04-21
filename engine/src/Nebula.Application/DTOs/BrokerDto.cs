namespace Nebula.Application.DTOs;

public record BrokerDto(
    Guid Id,
    string LegalName,
    string LicenseNumber,
    string State,
    string Status,
    string? Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint RowVersion,
    bool IsDeactivated);
