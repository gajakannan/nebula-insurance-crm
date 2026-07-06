using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Services;

public class TaskRoutingSource(AppDbContext db) : ITaskRoutingSource
{
    public async Task<RoutingSourceSummary?> GetSummaryAsync(Guid sourceId, CancellationToken ct = default) =>
        await db.Tasks
            .Where(t => t.Id == sourceId)
            .Select(t => new RoutingSourceSummary(
                "Task",
                t.Id,
                t.AssignedToUserId,
                t.LinkedEntityType == "Broker" ? t.LinkedEntityId : null,
                t.LinkedEntityType == "Account" ? t.LinkedEntityId : null,
                null,
                null,
                null,
                t.DueDate))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> AssignAsync(Guid sourceId, Guid assignedToUserId, Guid actorUserId, DateTime now, CancellationToken ct = default)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == sourceId, ct);
        if (task is null)
            return false;

        task.AssignedToUserId = assignedToUserId;
        task.UpdatedAt = now;
        task.UpdatedByUserId = actorUserId;
        return true;
    }
}

public class SubmissionRoutingSource(AppDbContext db) : ISubmissionRoutingSource
{
    public async Task<RoutingSourceSummary?> GetSummaryAsync(Guid sourceId, CancellationToken ct = default) =>
        await db.Submissions
            .Where(s => s.Id == sourceId)
            .Select(s => new RoutingSourceSummary(
                "Submission",
                s.Id,
                s.AssignedToUserId,
                s.BrokerId,
                s.AccountId,
                s.ProgramId,
                s.Account.Region,
                s.LineOfBusiness,
                s.EffectiveDate))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> AssignAsync(Guid sourceId, Guid assignedToUserId, Guid actorUserId, DateTime now, CancellationToken ct = default)
    {
        var submission = await db.Submissions.FirstOrDefaultAsync(s => s.Id == sourceId, ct);
        if (submission is null)
            return false;

        submission.AssignedToUserId = assignedToUserId;
        submission.UpdatedAt = now;
        submission.UpdatedByUserId = actorUserId;
        return true;
    }
}

public class RenewalRoutingSource(AppDbContext db) : IRenewalRoutingSource
{
    public async Task<RoutingSourceSummary?> GetSummaryAsync(Guid sourceId, CancellationToken ct = default) =>
        await db.Renewals
            .Where(r => r.Id == sourceId)
            .Select(r => new RoutingSourceSummary(
                "Renewal",
                r.Id,
                r.AssignedToUserId,
                r.BrokerId,
                r.AccountId,
                null,
                r.Account.Region,
                r.LineOfBusiness,
                r.TargetOutreachDate))
            .FirstOrDefaultAsync(ct);

    public async Task<bool> AssignAsync(Guid sourceId, Guid assignedToUserId, Guid actorUserId, DateTime now, CancellationToken ct = default)
    {
        var renewal = await db.Renewals.FirstOrDefaultAsync(r => r.Id == sourceId, ct);
        if (renewal is null)
            return false;

        renewal.AssignedToUserId = assignedToUserId;
        renewal.UpdatedAt = now;
        renewal.UpdatedByUserId = actorUserId;
        return true;
    }
}

public class DistributionOwnershipResolver(AppDbContext db) : IDistributionOwnershipResolver
{
    public async Task<Guid?> ResolveOwnerAsync(RoutingSourceSummary source, CancellationToken ct = default)
    {
        if (source.ProgramId.HasValue)
        {
            return await db.Programs
                .Where(p => p.Id == source.ProgramId.Value)
                .Select(p => p.ManagedByUserId)
                .FirstOrDefaultAsync(ct);
        }

        return source.CurrentAssigneeId;
    }
}
