using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetMyTasksAsync(Guid assignedToUserId, int limit, CancellationToken ct = default);
    /// <summary>
    /// BrokerUser variant: tasks scoped to a specific broker entity link (F0009 §12).
    /// Returns only tasks where LinkedEntityType='Broker' AND LinkedEntityId=brokerId AND IsDeleted=false.
    /// </summary>
    Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetBrokerScopedTasksAsync(Guid brokerId, int limit, CancellationToken ct = default);
    /// <summary>
    /// F0004: Paginated, filtered task list for the GET /tasks endpoint.
    /// Supports myWork (assignee) and assignedByMe (creator) views with full filter/sort/page.
    /// </summary>
    Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetTaskListAsync(TaskListQuery query, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    /// <summary>
    /// F0004: Resolves a display name for a linked entity (Broker→LegalName, Submission/Renewal→Account.Name).
    /// Returns null if the entity doesn't exist or the type is unknown.
    /// </summary>
    Task<string?> ResolveLinkedEntityNameAsync(string? entityType, Guid? entityId, CancellationToken ct = default);
}
