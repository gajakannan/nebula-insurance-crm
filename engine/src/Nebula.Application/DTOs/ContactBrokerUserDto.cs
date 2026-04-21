namespace Nebula.Application.DTOs;

/// <summary>
/// Contact response DTO for BrokerUser audience (F0009 §8 / BROKER-VISIBILITY-MATRIX.md).
/// Excludes InternalOnly fields: RowVersion.
/// </summary>
public record ContactBrokerUserDto(
    Guid Id,
    Guid BrokerId,
    string FullName,
    string? Email,
    string? Phone,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt);
