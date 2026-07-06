using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class DistributionScopeService(IDistributionScopeRepository repo) : IDistributionScopeService
{
    private static readonly string[] ExternalRoles = ["BrokerUser", "ExternalUser"];
    private static readonly string[] FullScopeRoles = ["Admin"];

    public async Task<ProjectionVisibility> ResolveAsync(
        DistributionScopeRequest request,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var asOf = request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var roles = user.Roles.ToList();
        var regions = user.Regions.ToList();
        var explanations = new List<string>();

        if (roles.Any(r => ExternalRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            return Empty(user, roles, regions, asOf, "external_denied");
        }

        var nodeIds = new HashSet<Guid>();
        var brokerIds = new HashSet<Guid>();
        var territoryIds = new HashSet<Guid>();
        var producerUserIds = new HashSet<Guid>();
        var requestedScope = request.RootNodeId.HasValue || request.TerritoryId.HasValue || request.ProducerUserId.HasValue;
        var seeAll = roles.Any(r => FullScopeRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (request.RootNodeId.HasValue)
        {
            var hierarchy = await repo.ResolveHierarchyScopeAsync(request.RootNodeId.Value, ct);
            if (!hierarchy.Found)
                return Empty(user, roles, regions, asOf, "root_not_found");

            nodeIds.UnionWith(hierarchy.DistributionNodeIds);
            brokerIds.UnionWith(hierarchy.BrokerIds);
            explanations.Add("hierarchy_scope");
        }

        if (request.TerritoryId.HasValue)
        {
            territoryIds.Add(request.TerritoryId.Value);
            brokerIds.UnionWith(await repo.ListBrokerIdsForTerritoryAsync(request.TerritoryId.Value, asOf, ct));
            explanations.Add("territory_scope");
        }

        if (request.ProducerUserId.HasValue)
        {
            producerUserIds.Add(request.ProducerUserId.Value);
            explanations.Add("producer_scope");
        }

        if (seeAll)
        {
            explanations.Add("admin_full_scope");
        }
        else
        {
            var authority = await repo.ResolveAuthorityScopeAsync(user.UserId, roles, regions, asOf, ct);
            explanations.AddRange(authority.ExplanationCodes);

            if (requestedScope)
            {
                if (!IntersectOrFail(nodeIds, authority.DistributionNodeIds)
                    || !IntersectOrFail(brokerIds, authority.BrokerIds)
                    || !IntersectOrFail(territoryIds, authority.TerritoryIds)
                    || !IntersectOrFail(producerUserIds, authority.ProducerUserIds))
                {
                    return Empty(user, roles, regions, asOf, "requested_scope_outside_authority");
                }
            }
            else
            {
                nodeIds.UnionWith(authority.DistributionNodeIds);
                brokerIds.UnionWith(authority.BrokerIds);
                territoryIds.UnionWith(authority.TerritoryIds);
                producerUserIds.UnionWith(authority.ProducerUserIds);
            }

            if (brokerIds.Count == 0 && territoryIds.Count == 0 && producerUserIds.Count == 0 && regions.Count == 0)
                explanations.Add("owner_scope");
            else
                explanations.Add("constrained_scope");
        }

        return new ProjectionVisibility(
            SeeAll: seeAll,
            UserId: user.UserId,
            Roles: roles,
            Regions: regions,
            DistributionNodeIds: nodeIds,
            BrokerIds: brokerIds,
            TerritoryIds: territoryIds,
            ProducerUserIds: producerUserIds,
            AsOf: asOf,
            HasScope: true,
            ExplanationCodes: explanations);
    }

    public async Task<bool> CanReadDistributionNodeAsync(Guid nodeId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct)
    {
        var visibility = await ResolveAsync(new DistributionScopeRequest(nodeId, null, null, asOf), user, ct);
        return visibility.HasScope;
    }

    public async Task<bool> CanReadTerritoryAsync(Guid territoryId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct)
    {
        var visibility = await ResolveAsync(new DistributionScopeRequest(null, territoryId, null, asOf), user, ct);
        return visibility.HasScope;
    }

    public async Task<bool> CanReadBrokerAsync(Guid brokerId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct)
    {
        var visibility = await ResolveAsync(new DistributionScopeRequest(brokerId, null, null, asOf), user, ct);
        return visibility.HasScope;
    }

    public async Task<bool> CanReadProducerAsync(Guid producerUserId, ICurrentUserService user, DateOnly? asOf, CancellationToken ct)
    {
        var visibility = await ResolveAsync(new DistributionScopeRequest(null, null, producerUserId, asOf), user, ct);
        return visibility.HasScope;
    }

    private static bool IntersectOrFail(HashSet<Guid> requested, IReadOnlySet<Guid> authority)
    {
        if (requested.Count == 0)
            return true;

        requested.IntersectWith(authority);
        return requested.Count > 0;
    }

    private static ProjectionVisibility Empty(
        ICurrentUserService user,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> regions,
        DateOnly asOf,
        string explanation) => new(
            SeeAll: false,
            UserId: user.UserId,
            Roles: roles,
            Regions: regions,
            DistributionNodeIds: new HashSet<Guid>(),
            BrokerIds: new HashSet<Guid>(),
            TerritoryIds: new HashSet<Guid>(),
            ProducerUserIds: new HashSet<Guid>(),
            AsOf: asOf,
            HasScope: false,
            ExplanationCodes: [explanation]);
}
