using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class WorkflowTransitionRepository(AppDbContext db) : IWorkflowTransitionRepository
{
    public async Task<IReadOnlyList<WorkflowTransition>> ListByEntityAsync(
        string workflowType, Guid entityId, CancellationToken ct = default) =>
        await db.WorkflowTransitions
            .Where(wt => wt.WorkflowType == workflowType && wt.EntityId == entityId)
            .OrderBy(wt => wt.OccurredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestTransitionTimesAsync(
        string workflowType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken ct = default)
    {
        if (entityIds.Count == 0)
            return new Dictionary<Guid, DateTime>();

        return await db.WorkflowTransitions
            .Where(wt => wt.WorkflowType == workflowType && entityIds.Contains(wt.EntityId))
            .GroupBy(wt => wt.EntityId)
            .Select(group => new
            {
                EntityId = group.Key,
                LatestOccurredAt = group.Max(item => item.OccurredAt),
            })
            .ToDictionaryAsync(item => item.EntityId, item => item.LatestOccurredAt, ct);
    }

    public async Task AddAsync(WorkflowTransition transition, CancellationToken ct = default)
    {
        db.WorkflowTransitions.Add(transition);
        await Task.CompletedTask;
    }
}
