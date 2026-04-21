using Nebula.Domain.Entities;

using Nebula.Application.Common;

namespace Nebula.Application.Interfaces;

public interface ITimelineRepository
{
    Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(string entityType, Guid? entityId, int limit, CancellationToken ct = default);
    /// <summary>
    /// Paginated variant for internal roles (F0002-S0007).
    /// Returns page/pageSize/totalCount metadata alongside events.
    /// </summary>
    Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default);
    /// <summary>
    /// BrokerUser variant: returns only approved event types with non-null BrokerDescription,
    /// scoped to the specified broker entity IDs (F0009 §8.1).
    /// </summary>
    Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default);
    Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default);
}
