namespace Nebula.Application.DTOs;

/// <summary>
/// Broker response DTO for BrokerUser audience (F0009 §8 / BROKER-VISIBILITY-MATRIX.md).
/// Excludes InternalOnly fields: RowVersion, IsDeactivated.
/// </summary>
public record BrokerBrokerUserDto(
    Guid Id,
    string LegalName,
    string LicenseNumber,
    string State,
    string Status,
    string? Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime UpdatedAt);
