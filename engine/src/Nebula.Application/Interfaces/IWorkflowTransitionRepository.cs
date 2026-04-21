using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IWorkflowTransitionRepository
{
    Task<IReadOnlyList<WorkflowTransition>> ListByEntityAsync(string workflowType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestTransitionTimesAsync(
        string workflowType,
        IReadOnlyCollection<Guid> entityIds,
        CancellationToken ct = default);
    Task AddAsync(WorkflowTransition transition, CancellationToken ct = default);
}
