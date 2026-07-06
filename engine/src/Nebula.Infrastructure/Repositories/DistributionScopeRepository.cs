using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class DistributionScopeRepository(AppDbContext db) : IDistributionScopeRepository
{
    private static readonly string[] ManagerRoles = ["DistributionManager", "ProgramManager"];
    private static readonly string[] ProducerRoles = ["RelationshipManager", "DistributionUser", "Underwriter"];

    public async Task<DistributionHierarchyScope> ResolveHierarchyScopeAsync(Guid rootNodeId, CancellationToken ct)
    {
        var root = await db.DistributionNodes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == rootNodeId, ct);

        if (root is null)
            return new DistributionHierarchyScope(false, new HashSet<Guid>(), new HashSet<Guid>());

        var selfPrefix = Combine(root.AncestryPath, root.Id);
        var childPrefix = selfPrefix + "/";
        var nodes = await db.DistributionNodes.AsNoTracking()
            .Where(n => n.Id == root.Id || (n.IsActive && (n.AncestryPath == selfPrefix || n.AncestryPath.StartsWith(childPrefix))))
            .Select(n => new { n.Id, n.NodeType })
            .ToListAsync(ct);

        return new DistributionHierarchyScope(
            true,
            nodes.Select(n => n.Id).ToHashSet(),
            nodes.Where(n => n.NodeType == "Broker").Select(n => n.Id).ToHashSet());
    }

    public async Task<IReadOnlySet<Guid>> ListBrokerIdsForTerritoryAsync(Guid territoryId, DateOnly asOf, CancellationToken ct)
    {
        var brokerIds = await db.TerritoryAssignments.AsNoTracking()
            .Where(a => a.TerritoryId == territoryId
                        && a.MemberType == "Broker"
                        && a.EffectiveFrom <= asOf
                        && (a.EffectiveTo == null || a.EffectiveTo > asOf))
            .Select(a => a.MemberId)
            .ToListAsync(ct);

        return brokerIds.ToHashSet();
    }

    public async Task<DistributionAuthorityScope> ResolveAuthorityScopeAsync(
        Guid userId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> regions,
        DateOnly asOf,
        CancellationToken ct)
    {
        var brokerIds = new HashSet<Guid>();
        var producerUserIds = new HashSet<Guid>();
        var explanations = new List<string>();
        var hasManagerScope = roles.Any(r => ManagerRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
        var hasProducerScope = roles.Any(r => ProducerRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (hasManagerScope || hasProducerScope)
        {
            var managedBrokerIds = await db.Brokers.AsNoTracking()
                .Where(b => b.ManagedByUserId == userId)
                .Select(b => b.Id)
                .ToListAsync(ct);
            brokerIds.UnionWith(managedBrokerIds);

            if (managedBrokerIds.Count > 0)
                explanations.Add("managed_broker_authority");
        }

        if (hasManagerScope && regions.Count > 0)
        {
            var regionBrokerIds = await db.BrokerRegions.AsNoTracking()
                .Where(r => regions.Contains(r.Region))
                .Select(r => r.BrokerId)
                .ToListAsync(ct);
            brokerIds.UnionWith(regionBrokerIds);

            if (regionBrokerIds.Count > 0)
                explanations.Add("region_broker_authority");
        }

        if (hasProducerScope)
        {
            producerUserIds.Add(userId);
            explanations.Add("producer_user_authority");

            var producerBrokerIds = await db.ProducerOwnership.AsNoTracking()
                .Where(o => o.ProducerNodeId == userId
                            && o.ScopeType == "BrokerRelationship"
                            && o.EffectiveFrom <= asOf
                            && (o.EffectiveTo == null || o.EffectiveTo > asOf))
                .Select(o => o.ScopeId)
                .ToListAsync(ct);
            brokerIds.UnionWith(producerBrokerIds);

            var assignedBrokerIds = await db.TerritoryAssignments.AsNoTracking()
                .Where(a => a.MemberType == "Producer"
                            && a.MemberId == userId
                            && a.EffectiveFrom <= asOf
                            && (a.EffectiveTo == null || a.EffectiveTo > asOf))
                .Join(
                    db.TerritoryAssignments.AsNoTracking().Where(a =>
                        a.MemberType == "Broker"
                        && a.EffectiveFrom <= asOf
                        && (a.EffectiveTo == null || a.EffectiveTo > asOf)),
                    producer => producer.TerritoryId,
                    broker => broker.TerritoryId,
                    (_, broker) => broker.MemberId)
                .ToListAsync(ct);
            brokerIds.UnionWith(assignedBrokerIds);

            if (producerBrokerIds.Count > 0 || assignedBrokerIds.Count > 0)
                explanations.Add("producer_broker_authority");
        }

        var territoryIds = await db.TerritoryAssignments.AsNoTracking()
            .Where(a => a.MemberType == "Broker"
                        && brokerIds.Contains(a.MemberId)
                        && a.EffectiveFrom <= asOf
                        && (a.EffectiveTo == null || a.EffectiveTo > asOf))
            .Select(a => a.TerritoryId)
            .ToListAsync(ct);

        var nodeIds = await db.DistributionNodes.AsNoTracking()
            .Where(n => n.IsActive && brokerIds.Contains(n.Id))
            .Select(n => n.Id)
            .ToListAsync(ct);

        if (territoryIds.Count > 0)
            explanations.Add("territory_authority");

        return new DistributionAuthorityScope(
            nodeIds.ToHashSet(),
            brokerIds,
            territoryIds.ToHashSet(),
            producerUserIds,
            explanations);
    }

    private static string Combine(string ancestryPath, Guid id) =>
        string.IsNullOrWhiteSpace(ancestryPath) ? $"/{id}" : $"{ancestryPath}/{id}";
}
