using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class WorkflowSlaThresholdRepository(AppDbContext db) : IWorkflowSlaThresholdRepository
{
    public async Task<WorkflowSlaThreshold?> GetThresholdAsync(
        string entityType,
        string status,
        string? lineOfBusiness,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(lineOfBusiness))
        {
            var exact = await db.WorkflowSlaThresholds.FirstOrDefaultAsync(
                threshold =>
                    threshold.EntityType == entityType
                    && threshold.Status == status
                    && threshold.LineOfBusiness == lineOfBusiness,
                ct);

            if (exact is not null)
                return exact;
        }

        return await db.WorkflowSlaThresholds.FirstOrDefaultAsync(
            threshold =>
                threshold.EntityType == entityType
                && threshold.Status == status
                && threshold.LineOfBusiness == null,
            ct);
    }
}
