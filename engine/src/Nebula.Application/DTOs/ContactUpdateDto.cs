namespace Nebula.Application.DTOs;

public record ContactUpdateDto(
    string FullName,
    string Email,
    string Phone,
    string? Role);
