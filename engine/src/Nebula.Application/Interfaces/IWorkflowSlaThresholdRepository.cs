using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IWorkflowSlaThresholdRepository
{
    Task<WorkflowSlaThreshold?> GetThresholdAsync(
        string entityType,
        string status,
        string? lineOfBusiness,
        CancellationToken ct = default);
}
