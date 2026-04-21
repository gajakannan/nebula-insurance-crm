namespace Nebula.Application.DTOs;

public record AccountReferenceDto(
    Guid Id,
    string Name,
    string Status,
    string? Industry);
