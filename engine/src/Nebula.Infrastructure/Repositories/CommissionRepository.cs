using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class CommissionRepository(AppDbContext db) : ICommissionRepository
{
    public async Task<PaginatedResult<ExpectedCommission>> SearchExpectedCommissionsAsync(
        CommissionSearchQuery query,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var commissions = ApplySourceVisibility(db.ExpectedCommissions
            .Include(e => e.Adjustments)
            .AsNoTracking(), user, brokerScopeId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            commissions = commissions.Where(e =>
                e.Id.ToString().ToLower().Contains(search)
                || e.PolicyId.ToString().ToLower().Contains(search)
                || e.CarrierMarketId.ToString().ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            commissions = commissions.Where(e => e.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.ExceptionState))
            commissions = commissions.Where(e => e.ExceptionState == query.ExceptionState);
        if (query.PolicyId.HasValue)
            commissions = commissions.Where(e => e.PolicyId == query.PolicyId.Value);
        if (query.CarrierMarketId.HasValue)
            commissions = commissions.Where(e => e.CarrierMarketId == query.CarrierMarketId.Value);

        var totalCount = await commissions.CountAsync(ct);
        var data = await commissions
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<ExpectedCommission>(data, page, pageSize, totalCount);
    }

    public Task<ExpectedCommission?> GetExpectedCommissionAsync(
        Guid expectedCommissionId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        ApplySourceVisibility(db.ExpectedCommissions
            .Include(e => e.Adjustments)
            .AsNoTracking(), user, brokerScopeId)
            .FirstOrDefaultAsync(e => e.Id == expectedCommissionId, ct);

    public Task<ExpectedCommission?> GetExpectedCommissionForMutationAsync(
        Guid expectedCommissionId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        ApplySourceVisibility(db.ExpectedCommissions
            .Include(e => e.Adjustments), user, brokerScopeId)
            .FirstOrDefaultAsync(e => e.Id == expectedCommissionId, ct);

    public async Task<IReadOnlyDictionary<Guid, CommissionDisplayContextDto>> GetDisplayContextsAsync(
        IReadOnlyCollection<Guid> policyIds,
        CancellationToken ct = default)
    {
        if (policyIds.Count == 0)
            return new Dictionary<Guid, CommissionDisplayContextDto>();

        return await db.Policies
            .AsNoTracking()
            .Include(policy => policy.Broker)
            .Include(policy => policy.Carrier)
            .Include(policy => policy.Producer)
            .Where(policy => policyIds.Contains(policy.Id))
            .Select(policy => new CommissionDisplayContextDto(
                policy.Id,
                policy.PolicyNumber,
                policy.AccountDisplayNameAtLink,
                policy.Carrier.Name,
                policy.Broker.LegalName,
                policy.ProducerUserId,
                policy.Producer != null ? policy.Producer.DisplayName : null,
                policy.LineOfBusiness))
            .ToDictionaryAsync(context => context.PolicyId, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetProducerDisplayNamesAsync(
        IReadOnlyCollection<Guid> producerIds,
        CancellationToken ct = default)
    {
        if (producerIds.Count == 0)
            return new Dictionary<Guid, string>();

        return await db.UserProfiles
            .AsNoTracking()
            .Where(userProfile => producerIds.Contains(userProfile.Id))
            .ToDictionaryAsync(userProfile => userProfile.Id, userProfile => userProfile.DisplayName, ct);
    }

    public async Task<IReadOnlyList<CommissionSchedule>> ListSchedulesAsync(Guid? carrierMarketId, CancellationToken ct = default)
    {
        var query = db.CommissionSchedules.AsNoTracking().AsQueryable();
        if (carrierMarketId.HasValue)
            query = query.Where(s => s.CarrierMarketId == carrierMarketId.Value);
        return await query.OrderByDescending(s => s.EffectiveFrom).ToListAsync(ct);
    }

    public Task<CommissionSchedule?> GetScheduleAsync(Guid scheduleId, CancellationToken ct = default) =>
        db.CommissionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

    public Task<bool> ScheduleOverlapsAsync(CommissionSchedule schedule, Guid? excludingId, CancellationToken ct = default) =>
        db.CommissionSchedules.AnyAsync(existing =>
            (!excludingId.HasValue || existing.Id != excludingId.Value)
            && existing.CarrierMarketId == schedule.CarrierMarketId
            && existing.LineOfBusiness == schedule.LineOfBusiness
            && existing.State == schedule.State
            && existing.ProductCode == schedule.ProductCode
            && existing.EffectiveFrom <= (schedule.EffectiveTo ?? DateOnly.MaxValue)
            && (existing.EffectiveTo ?? DateOnly.MaxValue) >= schedule.EffectiveFrom, ct);

    public Task AddScheduleAsync(CommissionSchedule schedule, CancellationToken ct = default) =>
        db.CommissionSchedules.AddAsync(schedule, ct).AsTask();

    public async Task<IReadOnlyList<ProducerSplitAssignment>> ListPolicySplitsAsync(Guid policyId, CancellationToken ct = default) =>
        await db.ProducerSplitAssignments
            .Include(s => s.Participants)
            .AsNoTracking()
            .Where(s => s.PolicyId == policyId)
            .OrderByDescending(s => s.EffectiveFrom)
            .ToListAsync(ct);

    public Task<bool> PolicyIsVisibleAsync(
        Guid policyId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        ApplyPolicyVisibility(db.Policies.AsNoTracking(), user, brokerScopeId)
            .AnyAsync(policy => policy.Id == policyId, ct);

    public Task<ProducerSplitAssignment?> GetActiveSplitAsync(Guid policyId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken ct = default)
    {
        var targetEnd = effectiveTo ?? DateOnly.MaxValue;
        return db.ProducerSplitAssignments
            .Include(s => s.Participants)
            .Where(s => s.PolicyId == policyId && s.EffectiveFrom <= targetEnd && (s.EffectiveTo ?? DateOnly.MaxValue) >= effectiveFrom)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> SplitOverlapsAsync(Guid policyId, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid? excludingId, CancellationToken ct = default)
    {
        var targetEnd = effectiveTo ?? DateOnly.MaxValue;
        return db.ProducerSplitAssignments.AnyAsync(existing =>
            (!excludingId.HasValue || existing.Id != excludingId.Value)
            && existing.PolicyId == policyId
            && existing.EffectiveFrom <= targetEnd
            && (existing.EffectiveTo ?? DateOnly.MaxValue) >= effectiveFrom, ct);
    }

    public Task AddSplitAsync(ProducerSplitAssignment assignment, CancellationToken ct = default) =>
        db.ProducerSplitAssignments.AddAsync(assignment, ct).AsTask();

    public Task AddExpectedCommissionAsync(ExpectedCommission expectedCommission, CancellationToken ct = default) =>
        db.ExpectedCommissions.AddAsync(expectedCommission, ct).AsTask();

    public Task<CommissionAdjustment?> GetAdjustmentAsync(
        Guid adjustmentId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        ApplyAdjustmentSourceVisibility(db.CommissionAdjustments
            .Include(a => a.ExpectedCommission)
            .ThenInclude(e => e!.Adjustments), user, brokerScopeId)
            .FirstOrDefaultAsync(a => a.Id == adjustmentId, ct);

    public async Task<IReadOnlyList<CommissionAdjustment>> ListAdjustmentsAsync(
        Guid expectedCommissionId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        await ApplyAdjustmentSourceVisibility(db.CommissionAdjustments
            .AsNoTracking()
            .Include(a => a.ExpectedCommission), user, brokerScopeId)
            .Where(a => a.ExpectedCommissionId == expectedCommissionId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(ct);

    public Task AddAdjustmentAsync(CommissionAdjustment adjustment, CancellationToken ct = default) =>
        db.CommissionAdjustments.AddAsync(adjustment, ct).AsTask();

    public async Task UpsertProjectionAsync(RevenueAttributionProjection projection, CancellationToken ct = default)
    {
        var existing = await db.RevenueAttributionProjections.FirstOrDefaultAsync(p => p.ExpectedCommissionId == projection.ExpectedCommissionId, ct);
        if (existing is null)
        {
            await db.RevenueAttributionProjections.AddAsync(projection, ct);
            return;
        }

        existing.ExpectedGrossCommission = projection.ExpectedGrossCommission;
        existing.PolicyId = projection.PolicyId;
        existing.ProducerId = projection.ProducerId;
        existing.BrokerId = projection.BrokerId;
        existing.TerritoryId = projection.TerritoryId;
        existing.CarrierMarketId = projection.CarrierMarketId;
        existing.PolicyPeriodStart = projection.PolicyPeriodStart;
        existing.PolicyPeriodEnd = projection.PolicyPeriodEnd;
        existing.LineOfBusiness = projection.LineOfBusiness;
        existing.ApprovedAdjustmentTotal = projection.ApprovedAdjustmentTotal;
        existing.AdjustedExpectedCommission = projection.AdjustedExpectedCommission;
        existing.ProducerAllocationAmount = projection.ProducerAllocationAmount;
        existing.UnresolvedExceptionCount = projection.UnresolvedExceptionCount;
        existing.SourceRefreshedAt = projection.SourceRefreshedAt;
        existing.UpdatedAt = projection.UpdatedAt;
        existing.UpdatedByUserId = projection.UpdatedByUserId;
    }

    private IQueryable<ExpectedCommission> ApplySourceVisibility(
        IQueryable<ExpectedCommission> query,
        ICurrentUserService user,
        Guid? brokerScopeId)
    {
        var visiblePolicies = ApplyPolicyVisibility(db.Policies.AsNoTracking(), user, brokerScopeId);
        return query.Where(expected => visiblePolicies.Any(policy => policy.Id == expected.PolicyId));
    }

    private IQueryable<CommissionAdjustment> ApplyAdjustmentSourceVisibility(
        IQueryable<CommissionAdjustment> query,
        ICurrentUserService user,
        Guid? brokerScopeId)
    {
        var visiblePolicies = ApplyPolicyVisibility(db.Policies.AsNoTracking(), user, brokerScopeId);
        return query.Where(adjustment =>
            adjustment.ExpectedCommission != null
            && visiblePolicies.Any(policy => policy.Id == adjustment.ExpectedCommission.PolicyId));
    }

    private static IQueryable<Policy> ApplyPolicyVisibility(
        IQueryable<Policy> query,
        ICurrentUserService user,
        Guid? brokerScopeId)
    {
        if (HasRole(user.Roles, "Admin") || HasRole(user.Roles, "ProgramManager"))
            return query;

        var normalizedRegions = NormalizeRegions(user.Regions);
        var includeRegional = HasRole(user.Roles, "DistributionUser") || HasRole(user.Roles, "DistributionManager");
        var includeUnderwriter = HasRole(user.Roles, "Underwriter");
        var includeRelationshipManager = HasRole(user.Roles, "RelationshipManager");
        var includeBrokerUser = HasRole(user.Roles, "BrokerUser") && brokerScopeId.HasValue;

        return query.Where(policy =>
            (includeRegional && policy.Account.Region != null && normalizedRegions.Contains(policy.Account.Region))
            || includeUnderwriter
            || (includeRelationshipManager && policy.Broker.ManagedByUserId == user.UserId)
            || (includeBrokerUser && policy.BrokerId == brokerScopeId));
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static List<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .ToList();
}
