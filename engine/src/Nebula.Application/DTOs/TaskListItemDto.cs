namespace Nebula.Application.DTOs;

public record TaskListItemDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    Guid AssignedToUserId,
    string? AssignedToDisplayName,
    Guid CreatedByUserId,
    string? CreatedByDisplayName,
    string? LinkedEntityType,
    Guid? LinkedEntityId,
    string? LinkedEntityName,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt);
