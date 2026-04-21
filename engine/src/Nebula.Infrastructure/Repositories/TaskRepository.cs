using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetMyTasksAsync(
        Guid assignedToUserId, int limit, CancellationToken ct = default)
    {
        var query = db.Tasks
            .Where(t => t.AssignedToUserId == assignedToUserId && t.Status != "Done");

        var totalCount = await query.CountAsync(ct);

        var tasks = await query
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .Take(limit)
            .ToListAsync(ct);

        return (tasks, totalCount);
    }

    public async Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetBrokerScopedTasksAsync(
        Guid brokerId, int limit, CancellationToken ct = default)
    {
        // F0009 §12: tasks where LinkedEntityType='Broker' AND LinkedEntityId=brokerId AND IsDeleted=false.
        // The global IsDeleted query filter handles the soft-delete guard; IsDeleted=false is
        // also applied explicitly here in case the filter is bypassed.
        var query = db.Tasks
            .Where(t => t.LinkedEntityType == "Broker" && t.LinkedEntityId == brokerId && !t.IsDeleted);

        var totalCount = await query.CountAsync(ct);
        var tasks = await query
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate)
            .Take(limit)
            .ToListAsync(ct);

        return (tasks, totalCount);
    }

    public async Task<(IReadOnlyList<TaskItem> Tasks, int TotalCount)> GetTaskListAsync(
        TaskListQuery query, CancellationToken ct = default)
    {
        var q = db.Tasks.AsQueryable();

        // View-based scope: myWork = assigned to caller, assignedByMe = created by caller AND assigned to someone else
        if (query.View == "assignedByMe")
            q = q.Where(t => t.CreatedByUserId == query.CallerUserId && t.AssignedToUserId != query.CallerUserId);
        else // default: myWork
            q = q.Where(t => t.AssignedToUserId == query.CallerUserId);

        // Optional filters
        if (query.StatusFilter is { Length: > 0 })
            q = q.Where(t => query.StatusFilter.Contains(t.Status));

        if (query.PriorityFilter is { Length: > 0 })
            q = q.Where(t => query.PriorityFilter.Contains(t.Priority));

        if (query.DueDateFrom.HasValue)
            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value >= query.DueDateFrom.Value);

        if (query.DueDateTo.HasValue)
            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value <= query.DueDateTo.Value);

        if (query.Overdue == true)
        {
            var today = DateTime.UtcNow.Date;
            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "Done");
        }

        if (query.AssigneeId.HasValue)
            q = q.Where(t => t.AssignedToUserId == query.AssigneeId.Value);

        if (query.LinkedEntityTypeFilter is { Length: > 0 })
            q = q.Where(t => t.LinkedEntityType != null && query.LinkedEntityTypeFilter.Contains(t.LinkedEntityType));

        if (query.CreatedById.HasValue)
            q = q.Where(t => t.CreatedByUserId == query.CreatedById.Value);

        // Total count before pagination
        var totalCount = await q.CountAsync(ct);

        // Sorting
        q = (query.Sort?.ToLowerInvariant(), query.SortDir?.ToLowerInvariant()) switch
        {
            ("duedate", "desc")   => q.OrderByDescending(t => t.DueDate.HasValue ? 0 : 1).ThenByDescending(t => t.DueDate),
            ("duedate", _)        => q.OrderBy(t => t.DueDate.HasValue ? 0 : 1).ThenBy(t => t.DueDate),
            ("priority", "desc")  => q.OrderByDescending(t => t.Priority),
            ("priority", _)       => q.OrderBy(t => t.Priority),
            ("createdat", "desc") => q.OrderByDescending(t => t.CreatedAt),
            ("createdat", _)      => q.OrderBy(t => t.CreatedAt),
            ("status", "desc")    => q.OrderByDescending(t => t.Status),
            ("status", _)         => q.OrderBy(t => t.Status),
            _                     => q.OrderBy(t => t.DueDate.HasValue ? 0 : 1).ThenBy(t => t.DueDate),
        };

        // Pagination
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var page = Math.Max(1, query.Page);
        var tasks = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (tasks, totalCount);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        await db.Tasks.AddAsync(task, ct);
    }

    public async Task<string?> ResolveLinkedEntityNameAsync(string? entityType, Guid? entityId, CancellationToken ct = default)
    {
        if (entityType is null || entityId is null)
            return null;

        return entityType switch
        {
            "Broker" => await db.Brokers
                .Where(b => b.Id == entityId.Value)
                .Select(b => b.LegalName)
                .FirstOrDefaultAsync(ct),
            "Submission" => await db.Submissions
                .Where(s => s.Id == entityId.Value)
                .Select(s => s.Account.Name)
                .FirstOrDefaultAsync(ct),
            "Renewal" => await db.Renewals
                .Where(r => r.Id == entityId.Value)
                .Select(r => r.Account.Name)
                .FirstOrDefaultAsync(ct),
            _ => null,
        };
    }
}
