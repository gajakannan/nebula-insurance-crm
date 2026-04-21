namespace Nebula.Application.DTOs;

public record ContactCreateDto(
    Guid BrokerId,
    string FullName,
    string Email,
    string Phone,
    string? Role);
