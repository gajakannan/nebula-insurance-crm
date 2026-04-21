namespace Nebula.Application.DTOs;

public record TaskSummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTime? DueDate,
    string? LinkedEntityType,
    Guid? LinkedEntityId,
    string? LinkedEntityName,
    bool IsOverdue,
    string? AssignedToDisplayName);
