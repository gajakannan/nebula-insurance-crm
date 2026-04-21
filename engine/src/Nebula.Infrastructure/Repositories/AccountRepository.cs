using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class AccountRepository(AppDbContext db) : IAccountRepository
{
    private static readonly string[] SubmissionTerminalStatusCodes = OpportunityStatusCatalog.SubmissionStatuses
        .Where(status => status.IsTerminal)
        .Select(status => status.Code)
        .ToArray();

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Accounts.FirstOrDefaultAsync(account => account.Id == id, ct);

    public async Task<Account?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default) =>
        await db.Accounts
            .Include(account => account.BrokerOfRecord)
            .Include(account => account.PrimaryProducer)
            .Include(account => account.MergedInto)
            .FirstOrDefaultAsync(account => account.Id == id, ct);

    public async Task<Account?> GetAccessibleByIdAsync(
        Guid id,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default) =>
        await GetScopedQuery(user, brokerScopeId)
            .FirstOrDefaultAsync(account => account.Id == id, ct);

    public async Task<PaginatedResult<Account>> ListAsync(
        AccountListQuery query,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        var filtered = ApplyFilters(GetScopedQuery(user, brokerScopeId), query, user);
        var totalCount = await filtered.CountAsync(ct);
        var data = await ApplySort(filtered, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Account>(data, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, AccountSummaryProjection>> GetSummaryProjectionAsync(
        IReadOnlyCollection<Guid> accountIds,
        ICurrentUserService user,
        Guid? brokerScopeId,
        CancellationToken ct = default)
    {
        if (accountIds.Count == 0)
            return new Dictionary<Guid, AccountSummaryProjection>();

        var activePolicyCounts = await db.Policies
            .Where(policy =>
                accountIds.Contains(policy.AccountId)
                && !policy.IsDeleted
                && policy.CurrentStatus == "Active")
            .GroupBy(policy => policy.AccountId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, ct);

        var submissionCounts = await GetScopedSubmissionsQuery(user, brokerScopeId)
            .Where(submission =>
                accountIds.Contains(submission.AccountId)
                && !SubmissionTerminalStatusCodes.Contains(submission.CurrentStatus))
            .GroupBy(submission => submission.AccountId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, ct);

        var renewalCounts = await GetScopedRenewalsQuery(user, brokerScopeId)
            .Where(renewal =>
                accountIds.Contains(renewal.AccountId)
                && renewal.CurrentStatus != "Completed"
                && renewal.CurrentStatus != "Lost")
            .GroupBy(renewal => renewal.AccountId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, ct);

        var lastActivity = await db.ActivityTimelineEvents
            .Where(evt => evt.EntityType == "Account" && accountIds.Contains(evt.EntityId))
            .GroupBy(evt => evt.EntityId)
            .Select(group => new { group.Key, LastActivityAt = group.Max(evt => evt.OccurredAt) })
            .ToDictionaryAsync(item => item.Key, item => (DateTime?)item.LastActivityAt, ct);

        var projection = new Dictionary<Guid, AccountSummaryProjection>(accountIds.Count);
        foreach (var accountId in accountIds)
        {
            projection[accountId] = new AccountSummaryProjection(
                activePolicyCounts.GetValueOrDefault(accountId),
                submissionCounts.GetValueOrDefault(accountId),
                renewalCounts.GetValueOrDefault(accountId),
                lastActivity.GetValueOrDefault(accountId));
        }

        return projection;
    }

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        db.Accounts.Add(account);
        return Task.CompletedTask;
    }

    public async Task<PaginatedResult<Policy>> ListPoliciesAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Policies
            .AsNoTracking()
            .Where(policy => policy.AccountId == accountId);

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(policy => policy.ExpirationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Policy>(data, page, pageSize, totalCount);
    }

    public async Task<AccountMergeImpactProjection> GetMergeImpactAsync(Guid accountId, CancellationToken ct = default)
    {
        var submissionCount = await db.Submissions
            .Where(submission => submission.AccountId == accountId)
            .CountAsync(ct);

        var policyCount = await db.Policies
            .Where(policy => policy.AccountId == accountId)
            .CountAsync(ct);

        var renewalCount = await db.Renewals
            .Where(renewal => renewal.AccountId == accountId)
            .CountAsync(ct);

        var contactCount = await db.AccountContacts
            .Where(contact => contact.AccountId == accountId)
            .CountAsync(ct);

        var timelineCount = await db.ActivityTimelineEvents
            .Where(evt => evt.EntityType == "Account" && evt.EntityId == accountId)
            .CountAsync(ct);

        return new AccountMergeImpactProjection(
            submissionCount,
            policyCount,
            renewalCount,
            contactCount,
            timelineCount);
    }

    public async Task PropagateFallbackStateAsync(
        Guid accountId,
        string displayName,
        string status,
        Guid? survivorAccountId,
        CancellationToken ct = default)
    {
        await db.Submissions
            .Where(submission => submission.AccountId == accountId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(submission => submission.AccountDisplayNameAtLink, displayName)
                .SetProperty(submission => submission.AccountStatusAtRead, status)
                .SetProperty(submission => submission.AccountSurvivorId, survivorAccountId), ct);

        await db.Renewals
            .Where(renewal => renewal.AccountId == accountId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(renewal => renewal.AccountDisplayNameAtLink, displayName)
                .SetProperty(renewal => renewal.AccountStatusAtRead, status)
                .SetProperty(renewal => renewal.AccountSurvivorId, survivorAccountId), ct);

        await db.Policies
            .Where(policy => policy.AccountId == accountId)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(policy => policy.AccountDisplayNameAtLink, displayName)
                .SetProperty(policy => policy.AccountStatusAtRead, status)
                .SetProperty(policy => policy.AccountSurvivorId, survivorAccountId), ct);
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<bool> ExistsActiveTaxIdAsync(string taxId, Guid? excludeAccountId, CancellationToken ct = default)
    {
        var normalizedTaxId = NormalizeTaxId(taxId);
        return await db.Accounts.AnyAsync(account =>
            account.Status == AccountStatuses.Active
            && account.TaxId != null
            && account.TaxId == normalizedTaxId
            && (!excludeAccountId.HasValue || account.Id != excludeAccountId.Value), ct);
    }

    private IQueryable<Account> GetScopedQuery(ICurrentUserService user, Guid? brokerScopeId)
    {
        var accounts = db.Accounts
            .AsNoTracking()
            .Include(account => account.BrokerOfRecord)
            .Include(account => account.PrimaryProducer)
            .Include(account => account.MergedInto)
            .AsQueryable();

        if (HasRole(user.Roles, "Admin"))
            return accounts;

        var regions = NormalizeRegions(user.Regions);
        var includeRegional = HasRole(user.Roles, "DistributionUser") || HasRole(user.Roles, "DistributionManager");
        var includeUnderwriter = HasRole(user.Roles, "Underwriter");
        var includeRelationshipManager = HasRole(user.Roles, "RelationshipManager");
        var includeProgramManager = HasRole(user.Roles, "ProgramManager");
        var includeBrokerUser = HasRole(user.Roles, "BrokerUser") && brokerScopeId.HasValue;
        var managedProgramIds = db.Programs
            .Where(program => program.ManagedByUserId == user.UserId)
            .Select(program => program.Id);

        return accounts.Where(account =>
            (includeRegional
                && regions.Count > 0
                && account.Region != null
                && regions.Contains(account.Region))
            || (includeUnderwriter
                && (db.Submissions.Any(submission => submission.AccountId == account.Id && submission.AssignedToUserId == user.UserId)
                    || db.Renewals.Any(renewal => renewal.AccountId == account.Id && renewal.AssignedToUserId == user.UserId)))
            || (includeRelationshipManager
                && account.BrokerOfRecord != null
                && account.BrokerOfRecord.ManagedByUserId == user.UserId)
            || (includeProgramManager
                && db.Submissions.Any(submission =>
                    submission.AccountId == account.Id
                    && submission.ProgramId.HasValue
                    && managedProgramIds.Contains(submission.ProgramId.Value)))
            || (includeBrokerUser
                && (account.BrokerOfRecordId == brokerScopeId
                    || db.Submissions.Any(submission => submission.AccountId == account.Id && submission.BrokerId == brokerScopeId)
                    || db.Renewals.Any(renewal => renewal.AccountId == account.Id && renewal.BrokerId == brokerScopeId)
                    || db.Policies.Any(policy => policy.AccountId == account.Id && policy.BrokerId == brokerScopeId))));
    }

    private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, AccountListQuery filters, ICurrentUserService user)
    {
        var requestedStatuses = ParseStatuses(filters.Status);
        if (requestedStatuses.Count == 0)
        {
            requestedStatuses = [AccountStatuses.Active, AccountStatuses.Inactive];
        }
        else if (!filters.IncludeRemoved)
        {
            requestedStatuses.Remove(AccountStatuses.Deleted);
        }

        if (requestedStatuses.Count > 0)
            query = query.Where(account => requestedStatuses.Contains(account.Status));

        if (!string.IsNullOrWhiteSpace(filters.Query))
        {
            var search = filters.Query.Trim();
            query = query.Where(account =>
                EF.Functions.ILike(account.Name, $"%{search}%")
                || (account.LegalName != null && EF.Functions.ILike(account.LegalName, $"%{search}%"))
                || (account.TaxId != null && EF.Functions.ILike(account.TaxId, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(filters.TerritoryCode))
            query = query.Where(account => account.TerritoryCode == filters.TerritoryCode);

        if (!string.IsNullOrWhiteSpace(filters.Region))
            query = query.Where(account => account.Region == filters.Region);

        if (filters.BrokerOfRecordId.HasValue)
            query = query.Where(account => account.BrokerOfRecordId == filters.BrokerOfRecordId.Value);

        if (!string.IsNullOrWhiteSpace(filters.PrimaryLineOfBusiness))
            query = query.Where(account => account.PrimaryLineOfBusiness == filters.PrimaryLineOfBusiness);

        return query;
    }

    private static IQueryable<Account> ApplySort(IQueryable<Account> query, AccountListQuery filters)
    {
        var descending = string.Equals(filters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sort = filters.Sort.Trim().ToLowerInvariant();

        return (sort, descending) switch
        {
            ("status", false) => query.OrderBy(account => account.Status).ThenBy(account => account.Name),
            ("status", true) => query.OrderByDescending(account => account.Status).ThenByDescending(account => account.Name),
            ("territorycode", false) => query.OrderBy(account => account.TerritoryCode).ThenBy(account => account.Name),
            ("territorycode", true) => query.OrderByDescending(account => account.TerritoryCode).ThenByDescending(account => account.Name),
            _ => query.OrderBy(account => account.Name),
        };
    }

    private IQueryable<Submission> GetScopedSubmissionsQuery(ICurrentUserService user, Guid? brokerScopeId)
    {
        var submissions = db.Submissions.AsNoTracking().AsQueryable();
        if (HasRole(user.Roles, "Admin"))
            return submissions;

        var regions = NormalizeRegions(user.Regions);

        return submissions.Where(submission =>
            ((HasRole(user.Roles, "DistributionUser") || HasRole(user.Roles, "Underwriter")) && submission.AssignedToUserId == user.UserId)
            || (HasRole(user.Roles, "DistributionManager")
                && submission.Account.Region != null
                && regions.Contains(submission.Account.Region))
            || (HasRole(user.Roles, "RelationshipManager") && submission.Broker.ManagedByUserId == user.UserId)
            || (HasRole(user.Roles, "ProgramManager")
                && submission.ProgramId.HasValue
                && db.Programs.Any(program => program.Id == submission.ProgramId.Value && program.ManagedByUserId == user.UserId))
            || (HasRole(user.Roles, "BrokerUser") && brokerScopeId.HasValue && submission.BrokerId == brokerScopeId.Value));
    }

    private IQueryable<Renewal> GetScopedRenewalsQuery(ICurrentUserService user, Guid? brokerScopeId)
    {
        var renewals = db.Renewals.AsNoTracking().AsQueryable();
        if (HasRole(user.Roles, "Admin"))
            return renewals;

        var regions = NormalizeRegions(user.Regions);

        return renewals.Where(renewal =>
            ((HasRole(user.Roles, "DistributionUser") || HasRole(user.Roles, "Underwriter")) && renewal.AssignedToUserId == user.UserId)
            || (HasRole(user.Roles, "DistributionManager")
                && renewal.Account.Region != null
                && regions.Contains(renewal.Account.Region))
            || (HasRole(user.Roles, "RelationshipManager") && renewal.Broker.ManagedByUserId == user.UserId)
            || (HasRole(user.Roles, "ProgramManager"))
            || (HasRole(user.Roles, "BrokerUser") && brokerScopeId.HasValue && renewal.BrokerId == brokerScopeId.Value));
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> ParseStatuses(string? statuses)
    {
        if (string.IsNullOrWhiteSpace(statuses))
            return [];

        return statuses
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizeTaxId(string taxId) =>
        taxId.Trim().ToUpperInvariant();
}
