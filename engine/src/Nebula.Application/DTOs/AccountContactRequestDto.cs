namespace Nebula.Application.DTOs;

public record AccountContactRequestDto(
    string FullName,
    string? Role,
    string? Email,
    string? Phone,
    bool IsPrimary);
