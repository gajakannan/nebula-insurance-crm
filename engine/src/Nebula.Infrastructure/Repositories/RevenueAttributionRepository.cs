using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class RevenueAttributionRepository(AppDbContext db) : IRevenueAttributionRepository
{
    public async Task<RevenueAttributionRollupResponseDto> GetRollupsAsync(
        RevenueAttributionRollupQuery query,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        var projections = ApplySourceVisibility(db.RevenueAttributionProjections.AsNoTracking(), user, brokerScopeId)
            .Where(p => p.PolicyPeriodStart <= query.EndDate && p.PolicyPeriodEnd >= query.StartDate);

        if (query.ProducerId.HasValue)
            projections = projections.Where(p => p.ProducerId == query.ProducerId.Value);
        if (query.BrokerId.HasValue)
            projections = projections.Where(p => p.BrokerId == query.BrokerId.Value);
        if (query.TerritoryId.HasValue)
            projections = projections.Where(p => p.TerritoryId == query.TerritoryId.Value);
        if (query.CarrierMarketId.HasValue)
            projections = projections.Where(p => p.CarrierMarketId == query.CarrierMarketId.Value);

        var sourceRows = await projections.ToListAsync(ct);
        var labels = await ResolveGroupLabelsAsync(query.GroupBy, sourceRows, ct);
        var rows = sourceRows
            .GroupBy(p => GroupKey(query.GroupBy, p))
            .Select(group => new RevenueAttributionRollupRowDto(
                group.Key ?? "unassigned",
                group.Key is not null && labels.TryGetValue(group.Key, out var label) ? label : "Unassigned",
                group.Sum(p => p.ExpectedGrossCommission),
                group.Sum(p => p.ApprovedAdjustmentTotal),
                group.Sum(p => p.AdjustedExpectedCommission),
                group.Sum(p => p.ProducerAllocationAmount),
                group.Count(),
                group.Sum(p => p.UnresolvedExceptionCount)))
            .OrderBy(row => row.GroupLabel)
            .ToList();

        return new RevenueAttributionRollupResponseDto(query, rows);
    }

    private IQueryable<RevenueAttributionProjection> ApplySourceVisibility(
        IQueryable<RevenueAttributionProjection> query,
        ICurrentUserService user,
        Guid? brokerScopeId)
    {
        if (HasRole(user.Roles, "Admin") || HasRole(user.Roles, "ProgramManager"))
            return query;

        var normalizedRegions = user.Regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .ToList();
        var includeRegional = HasRole(user.Roles, "DistributionUser") || HasRole(user.Roles, "DistributionManager");
        var includeUnderwriter = HasRole(user.Roles, "Underwriter");
        var includeRelationshipManager = HasRole(user.Roles, "RelationshipManager");
        var includeBrokerUser = HasRole(user.Roles, "BrokerUser") && brokerScopeId.HasValue;

        return query.Where(projection => db.Policies.Any(policy =>
            policy.Id == projection.PolicyId
            && (
                (includeRegional && policy.Account.Region != null && normalizedRegions.Contains(policy.Account.Region))
                || includeUnderwriter
                || (includeRelationshipManager && policy.Broker.ManagedByUserId == user.UserId)
                || (includeBrokerUser && policy.BrokerId == brokerScopeId)
            )));
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private async Task<IReadOnlyDictionary<string, string>> ResolveGroupLabelsAsync(
        string groupBy,
        IReadOnlyList<RevenueAttributionProjection> sourceRows,
        CancellationToken ct)
    {
        if (groupBy == "producer")
        {
            var ids = sourceRows.Where(row => row.ProducerId.HasValue).Select(row => row.ProducerId!.Value).Distinct().ToList();
            return await db.UserProfiles.AsNoTracking()
                .Where(userProfile => ids.Contains(userProfile.Id))
                .ToDictionaryAsync(userProfile => userProfile.Id.ToString(), userProfile => userProfile.DisplayName, ct);
        }

        if (groupBy == "broker")
        {
            var ids = sourceRows.Where(row => row.BrokerId.HasValue).Select(row => row.BrokerId!.Value).Distinct().ToList();
            return await db.Brokers.AsNoTracking()
                .Where(broker => ids.Contains(broker.Id))
                .ToDictionaryAsync(broker => broker.Id.ToString(), broker => broker.LegalName, ct);
        }

        if (groupBy == "territory")
        {
            var ids = sourceRows.Where(row => row.TerritoryId.HasValue).Select(row => row.TerritoryId!.Value).Distinct().ToList();
            return await db.Territories.AsNoTracking()
                .Where(territory => ids.Contains(territory.Id))
                .ToDictionaryAsync(territory => territory.Id.ToString(), territory => territory.Name, ct);
        }

        if (groupBy == "carrierMarket")
        {
            var ids = sourceRows.Select(row => row.CarrierMarketId).Distinct().ToList();
            var marketLabels = await db.CarrierMarkets.AsNoTracking()
                .Where(carrierMarket => ids.Contains(carrierMarket.Id))
                .ToDictionaryAsync(carrierMarket => carrierMarket.Id.ToString(), carrierMarket => carrierMarket.Name, ct);
            var missingIds = ids.Where(id => !marketLabels.ContainsKey(id.ToString())).ToList();
            if (missingIds.Count == 0)
                return marketLabels;

            var carrierRefLabels = await db.CarrierRefs.AsNoTracking()
                .Where(carrierRef => missingIds.Contains(carrierRef.Id))
                .ToDictionaryAsync(carrierRef => carrierRef.Id.ToString(), carrierRef => carrierRef.Name, ct);

            foreach (var label in carrierRefLabels)
                marketLabels[label.Key] = label.Value;

            return marketLabels;
        }

        return sourceRows
            .Select(row => row.PolicyPeriodStart.ToString("yyyy-MM"))
            .Distinct()
            .ToDictionary(period => period, period => period);
    }

    private static string? GroupKey(string groupBy, RevenueAttributionProjection projection) =>
        groupBy == "producer" ? projection.ProducerId?.ToString() :
        groupBy == "broker" ? projection.BrokerId?.ToString() :
        groupBy == "territory" ? projection.TerritoryId?.ToString() :
        groupBy == "carrierMarket" ? projection.CarrierMarketId.ToString() :
        projection.PolicyPeriodStart.ToString("yyyy-MM");
}
