namespace Nebula.Application.DTOs;

public record TaskCreateRequestDto(
    string Title,
    string? Description,
    string? Priority,
    DateTime? DueDate,
    Guid AssignedToUserId,
    string? LinkedEntityType,
    Guid? LinkedEntityId);
