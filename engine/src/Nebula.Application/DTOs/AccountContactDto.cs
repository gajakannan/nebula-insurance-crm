namespace Nebula.Application.DTOs;

public record AccountContactDto(
    Guid Id,
    Guid AccountId,
    string FullName,
    string? Role,
    string? Email,
    string? Phone,
    bool IsPrimary,
    string RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt);
