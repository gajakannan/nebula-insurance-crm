using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class PolicyRepository(AppDbContext db) : IPolicyRepository
{
    private static readonly string[] OpenRenewalTerminalStates = ["Completed", "Lost"];

    public async Task<PaginatedResult<Policy>> ListAsync(
        PolicyListQuery query,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        var filtered = ApplyFilters(GetScopedQuery(query.CallerRoles, query.CallerRegions, query.CallerUserId, brokerScopeId), query);
        var totalCount = await filtered.CountAsync(ct);
        var data = await ApplySort(filtered, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Policy>(data, query.Page, query.PageSize, totalCount);
    }

    public async Task<Policy?> GetAccessibleByIdAsync(
        Guid id,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        await GetScopedQuery(user.Roles, user.Regions, user.UserId, brokerScopeId)
            .FirstOrDefaultAsync(policy => policy.Id == id, ct);

    public async Task<Policy?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default) =>
        await db.Policies
            .Include(policy => policy.Account)
            .Include(policy => policy.Broker)
            .Include(policy => policy.Carrier)
            .Include(policy => policy.Producer)
            .FirstOrDefaultAsync(policy => policy.Id == id, ct);

    public async Task<Policy?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default) =>
        await db.Policies
            .AsNoTracking()
            .Include(policy => policy.Account)
            .Include(policy => policy.Broker)
            .Include(policy => policy.Carrier)
            .Include(policy => policy.Producer)
            .Include(policy => policy.PredecessorPolicy)
            .FirstOrDefaultAsync(policy => policy.Id == id, ct);

    public async Task<CarrierRef?> GetCarrierByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.CarrierRefs.FirstOrDefaultAsync(carrier => carrier.Id == id && carrier.IsActive, ct);

    public async Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Accounts.FirstOrDefaultAsync(account => account.Id == id, ct);

    public async Task<Broker?> GetBrokerByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Brokers.FirstOrDefaultAsync(broker => broker.Id == id && broker.Status == "Active", ct);

    public async Task<bool> AccountExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Accounts.AnyAsync(account => account.Id == id, ct);

    public async Task<bool> BrokerExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Brokers.AnyAsync(broker => broker.Id == id && broker.Status == "Active", ct);

    public async Task<bool> ProducerExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.UserProfiles.AnyAsync(user => user.Id == id && user.IsActive, ct);

    public Task AddAsync(Policy policy, CancellationToken ct = default) =>
        db.Policies.AddAsync(policy, ct).AsTask();

    public Task AddVersionAsync(PolicyVersion version, CancellationToken ct = default) =>
        db.PolicyVersions.AddAsync(version, ct).AsTask();

    public Task AddEndorsementAsync(PolicyEndorsement endorsement, CancellationToken ct = default) =>
        db.PolicyEndorsements.AddAsync(endorsement, ct).AsTask();

    public async Task AddCoverageLinesAsync(IEnumerable<PolicyCoverageLine> coverageLines, CancellationToken ct = default) =>
        await db.PolicyCoverageLines.AddRangeAsync(coverageLines, ct);

    public async Task SetCoverageCurrentAsync(Guid policyId, Guid policyVersionId, CancellationToken ct = default)
    {
        await db.PolicyCoverageLines
            .Where(line => line.PolicyId == policyId)
            .ExecuteUpdateAsync(updates => updates.SetProperty(line => line.IsCurrent, false), ct);

        await db.PolicyCoverageLines
            .Where(line => line.PolicyId == policyId && line.PolicyVersionId == policyVersionId)
            .ExecuteUpdateAsync(updates => updates.SetProperty(line => line.IsCurrent, true), ct);
    }

    public async Task<PaginatedResult<PolicyVersion>> ListVersionsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PolicyVersions.AsNoTracking().Where(version => version.PolicyId == policyId);
        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(version => version.VersionNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<PolicyVersion>(data, page, pageSize, totalCount);
    }

    public async Task<PolicyVersion?> GetVersionAsync(Guid policyId, Guid versionId, CancellationToken ct = default) =>
        await db.PolicyVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.PolicyId == policyId && version.Id == versionId, ct);

    public async Task<PaginatedResult<PolicyEndorsement>> ListEndorsementsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.PolicyEndorsements
            .AsNoTracking()
            .Include(endorsement => endorsement.PolicyVersion)
            .Where(endorsement => endorsement.PolicyId == policyId);
        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(endorsement => endorsement.EndorsementNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<PolicyEndorsement>(data, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<PolicyCoverageLine>> ListCurrentCoverageLinesAsync(Guid policyId, CancellationToken ct = default) =>
        await db.PolicyCoverageLines
            .AsNoTracking()
            .Where(line => line.PolicyId == policyId && line.IsCurrent)
            .OrderBy(line => line.CoverageCode)
            .ToListAsync(ct);

    public async Task<PolicyAccountSummaryDto?> GetAccountSummaryAsync(
        Guid accountId,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        var policies = GetScopedQuery(user.Roles, user.Regions, user.UserId, brokerScopeId)
            .Where(policy => policy.AccountId == accountId);

        if (!await policies.AnyAsync(ct))
            return new PolicyAccountSummaryDto(accountId, 0, 0, 0, 0, null, null, null, 0, "USD", DateTime.UtcNow);

        var rows = await policies
            .Select(policy => new
            {
                policy.Id,
                policy.PolicyNumber,
                policy.CurrentStatus,
                policy.ExpirationDate,
                policy.TotalPremium,
                policy.PremiumCurrency,
            })
            .ToListAsync(ct);

        var next = rows
            .Where(policy => policy.CurrentStatus == PolicyStatuses.Issued && policy.ExpirationDate >= DateTime.UtcNow.Date)
            .OrderBy(policy => policy.ExpirationDate)
            .FirstOrDefault();

        var currencies = rows.Select(policy => policy.PremiumCurrency).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new PolicyAccountSummaryDto(
            accountId,
            rows.Count(policy => policy.CurrentStatus == PolicyStatuses.Issued),
            rows.Count(policy => policy.CurrentStatus == PolicyStatuses.Expired),
            rows.Count(policy => policy.CurrentStatus == PolicyStatuses.Cancelled),
            rows.Count(policy => policy.CurrentStatus == PolicyStatuses.Pending),
            next?.ExpirationDate,
            next?.Id,
            next?.PolicyNumber,
            rows.Where(policy => policy.CurrentStatus == PolicyStatuses.Issued).Sum(policy => policy.TotalPremium),
            currencies.Length <= 1 ? currencies.FirstOrDefault() ?? "USD" : "MIXED",
            DateTime.UtcNow);
    }

    public async Task<int> CountPoliciesForYearAsync(int year, CancellationToken ct = default) =>
        await db.Policies.CountAsync(policy => policy.CreatedAt.Year == year, ct);

    public async Task<int> CountVersionsAsync(Guid policyId, CancellationToken ct = default) =>
        await db.PolicyVersions.CountAsync(version => version.PolicyId == policyId, ct);

    public async Task<int> CountEndorsementsAsync(Guid policyId, CancellationToken ct = default) =>
        await db.PolicyEndorsements.CountAsync(endorsement => endorsement.PolicyId == policyId, ct);

    public async Task<int> CountCurrentCoverageLinesAsync(Guid policyId, CancellationToken ct = default) =>
        await db.PolicyCoverageLines.CountAsync(line => line.PolicyId == policyId && line.IsCurrent, ct);

    public async Task<int> CountOpenRenewalsAsync(Guid policyId, CancellationToken ct = default) =>
        await db.Renewals.CountAsync(renewal =>
            renewal.PolicyId == policyId && !OpenRenewalTerminalStates.Contains(renewal.CurrentStatus), ct);

    public async Task<Policy?> GetSuccessorPolicyAsync(Guid policyId, CancellationToken ct = default)
    {
        var successorPolicyId = await db.Renewals
            .Where(renewal => renewal.PolicyId == policyId && renewal.BoundPolicyId.HasValue)
            .OrderByDescending(renewal => renewal.UpdatedAt)
            .Select(renewal => renewal.BoundPolicyId)
            .FirstOrDefaultAsync(ct);

        return successorPolicyId.HasValue
            ? await db.Policies.AsNoTracking().FirstOrDefaultAsync(policy => policy.Id == successorPolicyId.Value, ct)
            : null;
    }

    public async Task<IReadOnlyList<Policy>> ListIssuedPoliciesExpiredBeforeAsync(
        DateTime today,
        int maxBatchSize,
        CancellationToken ct = default) =>
        await db.Policies
            .Include(policy => policy.Account)
            .Include(policy => policy.Broker)
            .Include(policy => policy.Carrier)
            .Where(policy =>
                !policy.IsDeleted
                && policy.CurrentStatus == PolicyStatuses.Issued
                && policy.ExpirationDate < today.Date)
            .OrderBy(policy => policy.ExpirationDate)
            .ThenBy(policy => policy.PolicyNumber)
            .Take(maxBatchSize)
            .ToListAsync(ct);

    private IQueryable<Policy> GetScopedQuery(
        IReadOnlyList<string> roles,
        IReadOnlyList<string> callerRegions,
        Guid callerUserId,
        Guid? brokerScopeId)
    {
        var policies = db.Policies
            .AsNoTracking()
            .Include(policy => policy.Account)
            .Include(policy => policy.Broker)
            .Include(policy => policy.Carrier)
            .Include(policy => policy.Producer)
            .AsQueryable();

        if (HasRole(roles, "Admin"))
            return policies;

        var regions = NormalizeRegions(callerRegions);
        var includeRegional = HasRole(roles, "DistributionUser") || HasRole(roles, "DistributionManager");
        var includeUnderwriter = HasRole(roles, "Underwriter");
        var includeRelationshipManager = HasRole(roles, "RelationshipManager");
        var includeProgramManager = HasRole(roles, "ProgramManager");
        var includeBrokerUser = HasRole(roles, "BrokerUser") && brokerScopeId.HasValue;

        return policies.Where(policy =>
            (includeRegional && policy.Account.Region != null && regions.Contains(policy.Account.Region))
            || includeUnderwriter
            || (includeRelationshipManager && policy.Broker.ManagedByUserId == callerUserId)
            || includeProgramManager
            || (includeBrokerUser && policy.BrokerId == brokerScopeId));
    }

    private static IQueryable<Policy> ApplyFilters(IQueryable<Policy> query, PolicyListQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(policy =>
                EF.Functions.ILike(policy.PolicyNumber, $"%{search}%")
                || EF.Functions.ILike(policy.AccountDisplayNameAtLink, $"%{search}%")
                || EF.Functions.ILike(policy.Carrier.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            var statuses = filters.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
            query = query.Where(policy => statuses.Contains(policy.CurrentStatus));
        }

        if (!string.IsNullOrWhiteSpace(filters.LineOfBusiness))
            query = query.Where(policy => policy.LineOfBusiness == filters.LineOfBusiness);

        if (filters.CarrierId.HasValue)
            query = query.Where(policy => policy.CarrierId == filters.CarrierId.Value);

        if (filters.BrokerOfRecordId.HasValue)
            query = query.Where(policy => policy.BrokerId == filters.BrokerOfRecordId.Value);

        if (filters.AccountId.HasValue)
            query = query.Where(policy => policy.AccountId == filters.AccountId.Value);

        if (filters.ExpiringBefore.HasValue)
            query = query.Where(policy => policy.ExpirationDate <= filters.ExpiringBefore.Value);

        return query;
    }

    private static IQueryable<Policy> ApplySort(IQueryable<Policy> query, PolicyListQuery filters)
    {
        var descending = string.Equals(filters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sort = filters.SortBy.Trim().ToLowerInvariant();

        return (sort, descending) switch
        {
            ("effectivedate", false) => query.OrderBy(policy => policy.EffectiveDate).ThenBy(policy => policy.PolicyNumber),
            ("effectivedate", true) => query.OrderByDescending(policy => policy.EffectiveDate).ThenByDescending(policy => policy.PolicyNumber),
            ("expirationdate", true) => query.OrderByDescending(policy => policy.ExpirationDate).ThenByDescending(policy => policy.PolicyNumber),
            ("status", false) => query.OrderBy(policy => policy.CurrentStatus).ThenBy(policy => policy.PolicyNumber),
            ("status", true) => query.OrderByDescending(policy => policy.CurrentStatus).ThenByDescending(policy => policy.PolicyNumber),
            ("premium", false) => query.OrderBy(policy => policy.TotalPremium).ThenBy(policy => policy.PolicyNumber),
            ("premium", true) => query.OrderByDescending(policy => policy.TotalPremium).ThenByDescending(policy => policy.PolicyNumber),
            _ => query.OrderBy(policy => policy.ExpirationDate).ThenBy(policy => policy.PolicyNumber),
        };
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static List<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public static class PolicyStatuses
{
    public const string Pending = "Pending";
    public const string Issued = "Issued";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}
