namespace Nebula.Application.DTOs;

/// <summary>
/// Query object for the GET /tasks paginated list endpoint (F0004).
/// </summary>
public record TaskListQuery(
    /// <summary>"myWork" (assignee == caller) or "assignedByMe" (creator == caller)</summary>
    string View,
    Guid CallerUserId,
    string[]? StatusFilter,
    string[]? PriorityFilter,
    DateTime? DueDateFrom,
    DateTime? DueDateTo,
    bool? Overdue,
    Guid? AssigneeId,
    string[]? LinkedEntityTypeFilter,
    Guid? CreatedById,
    /// <summary>Sort field: "dueDate", "priority", "createdAt", "status"</summary>
    string Sort,
    /// <summary>Sort direction: "asc" or "desc"</summary>
    string SortDir,
    int Page,
    int PageSize);
