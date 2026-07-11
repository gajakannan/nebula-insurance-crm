using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class DistributionScopeService(
    IDistributionScopeRepository repo,
    ILogger<DistributionScopeService>? logger = null) : IDistributionScopeService
{
    // NullLogger keeps unit tests that construct the service without DI (no logger) allocation-free.
    private readonly ILogger _log = logger ?? NullLogger<DistributionScopeService>.Instance;

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

        var requestedScope = request.RootNodeId.HasValue || request.TerritoryId.HasValue || request.ProducerUserId.HasValue;
        var seeAll = roles.Any(r => FullScopeRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        // Authority union — the caller's default-visible scope. Owner/region are implicit via UserId/Regions;
        // these sets are OR-ed with them in the repositories. Always the authority union, never the request.
        var authorityNodeIds = new HashSet<Guid>();
        var authorityBrokerIds = new HashSet<Guid>();
        var authorityTerritoryIds = new HashSet<Guid>();
        var authorityProducerIds = new HashSet<Guid>();

        if (seeAll)
        {
            explanations.Add("admin_full_scope");
        }
        else
        {
            var authority = await repo.ResolveAuthorityScopeAsync(user.UserId, roles, regions, asOf, ct);
            explanations.AddRange(authority.ExplanationCodes);
            authorityNodeIds.UnionWith(authority.DistributionNodeIds);
            authorityBrokerIds.UnionWith(authority.BrokerIds);
            authorityTerritoryIds.UnionWith(authority.TerritoryIds);
            authorityProducerIds.UnionWith(authority.ProducerUserIds);
        }

        // Requested narrowing — explicit filters clamped to authority (fail closed via IntersectOrFail).
        // Applied as an AND filter ON TOP of the authority union in the repositories, so an explicit filter
        // narrows the slice without dropping managed-broker rows that are out-of-region or not owned.
        IReadOnlySet<Guid>? requestedBrokerIds = null;
        IReadOnlySet<Guid>? requestedTerritoryIds = null;
        IReadOnlySet<Guid>? requestedProducerIds = null;

        if (requestedScope)
        {
            var reqNodeIds = new HashSet<Guid>();
            var reqBrokerIds = new HashSet<Guid>();
            var reqTerritoryIds = new HashSet<Guid>();
            var reqProducerIds = new HashSet<Guid>();

            if (request.RootNodeId.HasValue)
            {
                var hierarchy = await repo.ResolveHierarchyScopeAsync(request.RootNodeId.Value, ct);
                if (!hierarchy.Found)
                    return Empty(user, roles, regions, asOf, "root_not_found");

                reqNodeIds.UnionWith(hierarchy.DistributionNodeIds);
                reqBrokerIds.UnionWith(hierarchy.BrokerIds);
                explanations.Add("hierarchy_scope");
            }

            if (request.TerritoryId.HasValue)
            {
                reqTerritoryIds.Add(request.TerritoryId.Value);
                reqBrokerIds.UnionWith(await repo.ListBrokerIdsForTerritoryAsync(request.TerritoryId.Value, asOf, ct));
                explanations.Add("territory_scope");
            }

            if (request.ProducerUserId.HasValue)
            {
                reqProducerIds.Add(request.ProducerUserId.Value);
                explanations.Add("producer_scope");
            }

            if (!seeAll)
            {
                // Fail closed: every requested dimension must intersect the caller's authority.
                // IntersectOrFail also narrows each requested set to its authorized subset.
                if (!IntersectOrFail(reqNodeIds, authorityNodeIds)
                    || !IntersectOrFail(reqBrokerIds, authorityBrokerIds)
                    || !IntersectOrFail(reqTerritoryIds, authorityTerritoryIds)
                    || !IntersectOrFail(reqProducerIds, authorityProducerIds))
                {
                    // SEC-L1: server-side detection signal for scope probing. Records who/what dimensions were
                    // requested out-of-authority; the caller still gets a no-leak empty scope (no existence
                    // is disclosed to the client).
                    _log.LogWarning(
                        "Distribution scope denied for user {UserId} (roles {Roles}): requested out-of-authority scope " +
                        "[rootNode={HasRoot} territory={HasTerritory} producer={HasProducer}] asOf {AsOf}",
                        user.UserId, string.Join(",", roles), request.RootNodeId.HasValue,
                        request.TerritoryId.HasValue, request.ProducerUserId.HasValue, asOf);
                    return Empty(user, roles, regions, asOf, "requested_scope_outside_authority");
                }
            }

            requestedBrokerIds = reqBrokerIds;
            requestedTerritoryIds = reqTerritoryIds;
            requestedProducerIds = reqProducerIds;
        }

        explanations.Add(seeAll
            ? "admin_scope"
            : authorityBrokerIds.Count == 0 && authorityTerritoryIds.Count == 0 && authorityProducerIds.Count == 0 && regions.Count == 0
                ? "owner_scope"
                : "constrained_scope");

        return new ProjectionVisibility(
            SeeAll: seeAll,
            UserId: user.UserId,
            Roles: roles,
            Regions: regions,
            DistributionNodeIds: authorityNodeIds,
            BrokerIds: authorityBrokerIds,
            TerritoryIds: authorityTerritoryIds,
            ProducerUserIds: authorityProducerIds,
            AsOf: asOf,
            HasScope: true,
            ExplanationCodes: explanations,
            ExplicitScopeRequested: requestedScope,
            RequestedBrokerIds: requestedBrokerIds,
            RequestedTerritoryIds: requestedTerritoryIds,
            RequestedProducerUserIds: requestedProducerIds);
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
        // WHY: a broker is a distribution node (NodeType == "Broker") and shares its GUID, so a broker id is
        // a valid RootNodeId. Resolving the hierarchy rooted at the broker node and intersecting with the
        // caller's authority is exactly the broker-visibility check (see CanReadBrokerAsync_ReturnsFalseForHiddenSibling).
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
