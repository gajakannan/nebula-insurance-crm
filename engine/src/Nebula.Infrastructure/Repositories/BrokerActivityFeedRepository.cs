using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

/// <summary>
/// Query-time, no-leak projection for the existing internal Broker timeline feed.
/// Visibility is applied before count, ordering, and pagination.
/// </summary>
public sealed class BrokerActivityFeedRepository(AppDbContext db) : IBrokerActivityFeedRepository
{
    public async Task<PaginatedResult<TimelineEventDto>> ListAsync(
        Guid? brokerId,
        int page,
        int pageSize,
        ProjectionVisibility visibility,
        CancellationToken ct = default)
    {
        var query =
            from timelineEvent in db.ActivityTimelineEvents.AsNoTracking()
            join broker in db.Brokers.AsNoTracking()
                on timelineEvent.EntityId equals broker.Id
            where timelineEvent.EntityType == "Broker"
            select new { TimelineEvent = timelineEvent, BrokerName = broker.LegalName };

        if (!visibility.HasScope)
        {
            query = query.Where(_ => false);
        }
        else if (!visibility.SeeAll)
        {
            var visibleBrokerIds = visibility.BrokerIds.ToList();
            query = query.Where(row => visibleBrokerIds.Contains(row.TimelineEvent.EntityId));
        }

        if (brokerId.HasValue)
            query = query.Where(row => row.TimelineEvent.EntityId == brokerId.Value);

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(row => row.TimelineEvent.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(row => new TimelineEventDto(
                row.TimelineEvent.Id,
                row.TimelineEvent.EntityType,
                row.TimelineEvent.EntityId,
                row.TimelineEvent.EventType,
                row.TimelineEvent.EventDescription,
                row.BrokerName,
                row.TimelineEvent.ActorDisplayName ?? "Unknown User",
                row.TimelineEvent.OccurredAt))
            .ToListAsync(ct);

        return new PaginatedResult<TimelineEventDto>(data, page, pageSize, totalCount);
    }
}
