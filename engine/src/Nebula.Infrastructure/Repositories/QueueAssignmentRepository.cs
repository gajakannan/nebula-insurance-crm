using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class QueueAssignmentRepository(AppDbContext db) : IQueueAssignmentRepository
{
    public async Task<(IReadOnlyList<WorkQueue> Queues, int TotalCount)> ListQueuesAsync(
        string? workType, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.WorkQueues
            .Include(q => q.Members)
            .Include(q => q.QueueWorkItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(workType))
            query = query.Where(q => q.WorkType == workType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(q => q.Status == status);

        var total = await query.CountAsync(ct);
        var size = Math.Max(1, Math.Min(pageSize, 100));
        var currentPage = Math.Max(1, page);
        var items = await query
            .OrderByDescending(q => q.IsFallback)
            .ThenBy(q => q.WorkType)
            .ThenBy(q => q.Name)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<WorkQueue?> GetQueueAsync(Guid queueId, CancellationToken ct = default) =>
        db.WorkQueues
            .Include(q => q.Members)
            .Include(q => q.AssignmentRules)
            .Include(q => q.CoverageWindows)
            .FirstOrDefaultAsync(q => q.Id == queueId, ct);

    public Task<WorkQueue?> GetQueueForUpdateAsync(Guid queueId, CancellationToken ct = default) =>
        db.WorkQueues
            .Include(q => q.Members)
            .Include(q => q.AssignmentRules)
            .Include(q => q.CoverageWindows)
            .FirstOrDefaultAsync(q => q.Id == queueId, ct);

    public Task<WorkQueue?> GetFallbackQueueAsync(string sourceType, CancellationToken ct = default) =>
        db.WorkQueues.FirstOrDefaultAsync(q =>
            q.WorkType == sourceType && q.IsFallback && q.Status == "Active", ct);

    public Task<bool> QueueNameExistsAsync(string name, string workType, Guid? excludingQueueId, CancellationToken ct = default) =>
        db.WorkQueues.AnyAsync(q =>
            q.Name == name && q.WorkType == workType && (!excludingQueueId.HasValue || q.Id != excludingQueueId.Value), ct);

    public Task<bool> HasOpenItemsAsync(Guid queueId, CancellationToken ct = default) =>
        db.QueueWorkItems.AnyAsync(i =>
            i.WorkQueueId == queueId && (i.QueueStatus == "Open" || i.QueueStatus == "Assigned" || i.QueueStatus == "InProgress"), ct);

    public async Task AddQueueAsync(WorkQueue queue, CancellationToken ct = default) =>
        await db.WorkQueues.AddAsync(queue, ct);

    public async Task<IReadOnlyList<AssignmentRule>> ListRulesAsync(Guid? queueId, string? status, CancellationToken ct = default)
    {
        var query = db.AssignmentRules.Include(r => r.WorkQueue).AsQueryable();
        if (queueId.HasValue)
            query = query.Where(r => r.WorkQueueId == queueId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        return await query.OrderBy(r => r.Precedence).ThenByDescending(r => r.Version).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AssignmentRule>> ListActiveRulesAsync(string sourceType, CancellationToken ct = default) =>
        await db.AssignmentRules
            .Include(r => r.WorkQueue)
            .Where(r => r.Status == "Active" && (r.WorkQueue.WorkType == sourceType || r.WorkQueue.WorkType == "Mixed"))
            .OrderBy(r => r.Precedence)
            .ThenByDescending(r => r.Version)
            .ToListAsync(ct);

    public Task<AssignmentRule?> GetRuleForUpdateAsync(Guid ruleId, CancellationToken ct = default) =>
        db.AssignmentRules.Include(r => r.WorkQueue).FirstOrDefaultAsync(r => r.Id == ruleId, ct);

    public async Task AddRuleAsync(AssignmentRule rule, CancellationToken ct = default) =>
        await db.AssignmentRules.AddAsync(rule, ct);

    public async Task<IReadOnlyList<CoverageWindow>> ListCoverageWindowsAsync(Guid? queueId, Guid? coveredUserId, string? status, CancellationToken ct = default)
    {
        var query = db.CoverageWindows.AsQueryable();
        if (queueId.HasValue)
            query = query.Where(w => w.WorkQueueId == queueId.Value);
        if (coveredUserId.HasValue)
            query = query.Where(w => w.CoveredUserId == coveredUserId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(w => w.Status == status);

        return await query.OrderBy(w => w.StartsAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CoverageWindow>> ListActiveCoverageAsync(Guid coveredUserId, string sourceType, DateTime now, CancellationToken ct = default) =>
        await db.CoverageWindows
            .Include(w => w.WorkQueue)
            .Where(w => w.CoveredUserId == coveredUserId
                && w.Status == "Active"
                && w.StartsAt <= now
                && w.EndsAt > now
                && (w.WorkQueueId == null || w.WorkQueue!.WorkType == sourceType || w.WorkQueue.WorkType == "Mixed"))
            .ToListAsync(ct);

    public Task<CoverageWindow?> GetCoverageForUpdateAsync(Guid coverageWindowId, CancellationToken ct = default) =>
        db.CoverageWindows.FirstOrDefaultAsync(w => w.Id == coverageWindowId, ct);

    public Task<bool> CoverageOverlapsAsync(Guid coveredUserId, Guid backupUserId, Guid? queueId, DateTime startsAt, DateTime endsAt, Guid? excludingId, CancellationToken ct = default) =>
        db.CoverageWindows.AnyAsync(w =>
            w.CoveredUserId == coveredUserId
            && w.BackupUserId == backupUserId
            && w.WorkQueueId == queueId
            && w.Status != "Cancelled"
            && (!excludingId.HasValue || w.Id != excludingId.Value)
            && startsAt < w.EndsAt
            && endsAt > w.StartsAt, ct);

    public async Task AddCoverageWindowAsync(CoverageWindow window, CancellationToken ct = default) =>
        await db.CoverageWindows.AddAsync(window, ct);

    public Task<QueueWorkItem?> GetActiveQueueItemAsync(string sourceType, Guid sourceId, CancellationToken ct = default) =>
        db.QueueWorkItems.FirstOrDefaultAsync(i =>
            i.SourceType == sourceType
            && i.SourceId == sourceId
            && (i.QueueStatus == "Open" || i.QueueStatus == "Assigned" || i.QueueStatus == "InProgress"), ct);

    public Task<QueueWorkItem?> GetQueueItemForUpdateAsync(Guid queueItemId, CancellationToken ct = default) =>
        db.QueueWorkItems.Include(i => i.WorkQueue).FirstOrDefaultAsync(i => i.Id == queueItemId, ct);

    public async Task AddQueueItemAsync(QueueWorkItem item, CancellationToken ct = default) =>
        await db.QueueWorkItems.AddAsync(item, ct);

    public async Task<(IReadOnlyList<QueueWorkItem> Items, int TotalCount)> ListQueueItemsAsync(Guid queueId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.QueueWorkItems.Where(i => i.WorkQueueId == queueId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(i => i.QueueStatus == status);

        var total = await query.CountAsync(ct);
        var size = Math.Max(1, Math.Min(pageSize, 100));
        var currentPage = Math.Max(1, page);
        var items = await query
            .OrderBy(i => i.RoutedAt)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task AddRoutingDecisionAsync(RoutingDecisionLog decision, CancellationToken ct = default) =>
        await db.RoutingDecisionLogs.AddAsync(decision, ct);

    public async Task<(IReadOnlyList<RoutingDecisionLog> Events, int TotalCount)> ListRoutingEventsAsync(
        string? sourceType, Guid? sourceId, Guid? queueItemId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.RoutingDecisionLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(sourceType))
            query = query.Where(e => e.SourceType == sourceType);
        if (sourceId.HasValue)
            query = query.Where(e => e.SourceId == sourceId.Value);
        if (queueItemId.HasValue)
            query = query.Where(e => e.QueueWorkItemId == queueItemId.Value);

        var total = await query.CountAsync(ct);
        var size = Math.Max(1, Math.Min(pageSize, 100));
        var currentPage = Math.Max(1, page);
        var events = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(ct);
        return (events, total);
    }
}
