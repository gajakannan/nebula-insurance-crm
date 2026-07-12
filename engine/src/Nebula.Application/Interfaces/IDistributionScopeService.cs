using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDistributionScopeService
{
    Task<ProjectionVisibility> ResolveAsync(
        DistributionScopeRequest request,
        ICurrentUserService user,
        CancellationToken ct);

    Task<bool> CanReadDistributionNodeAsync(Guid nodeId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct);
    Task<bool> CanReadTerritoryAsync(Guid territoryId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct);
    Task<bool> CanReadBrokerAsync(Guid brokerId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct);
    Task<bool> CanReadProducerAsync(Guid producerUserId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct);
}

public interface IDistributionScopeRepository
{
    Task<DistributionHierarchyScope> ResolveHierarchyScopeAsync(Guid rootNodeId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> ListBrokerIdsForTerritoryAsync(Guid territoryId, DateOnly asOf, CancellationToken ct);
    Task<DistributionAuthorityScope> ResolveAuthorityScopeAsync(
        Guid userId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> regions,
        DateOnly asOf,
        CancellationToken ct);
}

public sealed record DistributionHierarchyScope(
    bool Found,
    IReadOnlySet<Guid> DistributionNodeIds,
    IReadOnlySet<Guid> BrokerIds);

public sealed record DistributionAuthorityScope(
    IReadOnlySet<Guid> DistributionNodeIds,
    IReadOnlySet<Guid> BrokerIds,
    IReadOnlySet<Guid> TerritoryIds,
    IReadOnlySet<Guid> ProducerUserIds,
    IReadOnlyList<string> ExplanationCodes);
