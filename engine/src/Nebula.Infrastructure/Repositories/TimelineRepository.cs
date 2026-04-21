using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class TimelineRepository(AppDbContext db) : ITimelineRepository
{
    public async Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(
        string entityType, Guid? entityId, int limit, CancellationToken ct = default)
    {
        var query = db.ActivityTimelineEvents
            .Where(e => e.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(e => e.EntityId == entityId.Value);

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(
        string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ActivityTimelineEvents
            .Where(e => e.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(e => e.EntityId == entityId.Value);

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<ActivityTimelineEvent>(data, page, pageSize, totalCount);
    }

    // Approved BrokerUser-visible event types (F0009 §8.1 / BROKER-VISIBILITY-MATRIX.md).
    private static readonly string[] BrokerUserApprovedEventTypes =
    [
        "BrokerCreated", "BrokerUpdated", "BrokerStatusChanged", "ContactAdded", "ContactUpdated"
    ];

    public async Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(
        IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default)
    {
        // F0009 §8.1 contract:
        //   1. EntityType = 'Broker' AND EntityId IN resolved broker scope
        //   2. EventType IN approved types
        //   3. Only events with non-null BrokerDescription returned
        return await db.ActivityTimelineEvents
            .Where(e =>
                e.EntityType == "Broker" &&
                brokerIds.Contains(e.EntityId) &&
                BrokerUserApprovedEventTypes.Contains(e.EventType) &&
                e.BrokerDescription != null)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default)
    {
        db.ActivityTimelineEvents.Add(evt);
        return Task.CompletedTask;
    }
}
