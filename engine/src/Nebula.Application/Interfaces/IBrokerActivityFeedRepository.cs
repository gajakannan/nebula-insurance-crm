using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

/// <summary>
/// Focused read projection for authorization-scoped internal Broker timeline activity.
/// Keeping this separate from <see cref="ITimelineRepository"/> avoids coupling timeline
/// writers and unrelated entity feeds to the F0040 Broker projection contract.
/// </summary>
public interface IBrokerActivityFeedRepository
{
    Task<PaginatedResult<TimelineEventDto>> ListAsync(
        Guid? brokerId,
        int page,
        int pageSize,
        ProjectionVisibility visibility,
        CancellationToken ct = default);
}
