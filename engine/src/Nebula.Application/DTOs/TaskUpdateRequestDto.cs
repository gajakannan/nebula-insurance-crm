namespace Nebula.Application.DTOs;

public record TaskUpdateRequestDto(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    DateTime? DueDate,
    Guid? AssignedToUserId);
