namespace Nebula.Application.DTOs;

public record TaskDto(
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
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    uint RowVersion);
