using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class TimelineService(
    ITimelineRepository timelineRepo,
    IBrokerActivityFeedRepository brokerActivityFeed,
    IDistributionScopeService distributionScope,
    BrokerScopeResolver scopeResolver,
    ILogger<TimelineService> logger)
{
    private readonly ILogger<TimelineService> _logger = logger;

    public async Task<PaginatedResult<TimelineEventDto>> ListEventsPagedAsync(
        string entityType, Guid? entityId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        var result = await timelineRepo.ListEventsPagedAsync(entityType, entityId, page, pageSize, ct);
        AuditBrokerUserRead(user, "broker.timeline", entityId);
        var mapped = result.Data.Select(MapToDto).ToList();
        return new PaginatedResult<TimelineEventDto>(mapped, result.Page, result.PageSize, result.TotalCount);
    }

    /// <summary>
    /// Internal Broker projection used by the existing dashboard/Broker 360 feed and F0040.
    /// The canonical distribution scope is resolved for every read and applied by the repository
    /// before count, order, or pagination so hidden rows cannot influence the response envelope.
    /// </summary>
    public async Task<PaginatedResult<TimelineEventDto>> ListBrokerActivityPagedAsync(
        Guid? brokerId,
        int page,
        int pageSize,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visibility = await distributionScope.ResolveAsync(
            new DistributionScopeRequest(null, null, null, null),
            user,
            ct);

        return await brokerActivityFeed.ListAsync(brokerId, page, pageSize, visibility, ct);
    }

    private static TimelineEventDto MapToDto(ActivityTimelineEvent e) => new(
        e.Id, e.EntityType, e.EntityId, e.EventType,
        e.EventDescription, null, e.ActorDisplayName ?? "Unknown User", e.OccurredAt);

    /// <summary>
    /// BrokerUser variant: returns approved event types only with BrokerDescription (F0009-S0004 §8.1).
    /// Scoped to the resolved broker entity within the authenticated user's broker_tenant_id scope.
    /// Throws BrokerScopeUnresolvableException if scope cannot be resolved.
    /// </summary>
    public async Task<IReadOnlyList<TimelineBrokerUserEventDto>> ListEventsForBrokerUserAsync(
        int limit, ICurrentUserService user, CancellationToken ct = default)
    {
        var resolvedBrokerId = await scopeResolver.ResolveAsync(user, ct);
        var events = await timelineRepo.ListEventsForBrokerUserAsync([resolvedBrokerId], limit, ct);
        AuditBrokerUserRead(user, "broker.timeline", resolvedBrokerId, resolvedBrokerId);
        return events.Select(e => new TimelineBrokerUserEventDto(
            e.Id, e.EntityType, e.EntityId, e.EventType,
            e.BrokerDescription, e.ActorDisplayName, e.OccurredAt))
            .ToList();
    }

    private void AuditBrokerUserRead(ICurrentUserService user, string resource, Guid? entityId, Guid? resolvedBrokerId = null)
    {
        if (!user.Roles.Contains("BrokerUser")) return;
        _logger.LogInformation(
            "BrokerUser access: {Resource} by BrokerTenantId={BrokerTenantId} ResolvedBrokerId={ResolvedBrokerId} EntityId={EntityId} OccurredAt={OccurredAt}",
            resource,
            user.BrokerTenantId,
            resolvedBrokerId,
            entityId,
            DateTime.UtcNow);
    }
}
