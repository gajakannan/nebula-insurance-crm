using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class RenewalRepository(AppDbContext db) : IRenewalRepository
{
    private static readonly string[] RenewalTerminalStatusCodes = OpportunityStatusCatalog.RenewalStatuses
        .Where(status => status.IsTerminal)
        .Select(status => status.Code)
        .ToArray();

    public async Task<Renewal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Renewals.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Renewal?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default) =>
        await db.Renewals
            .Include(r => r.Account)
            .Include(r => r.Broker)
            .Include(r => r.Policy)
            .Include(r => r.AssignedToUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task AddAsync(Renewal renewal, CancellationToken ct = default) =>
        db.Renewals.AddAsync(renewal, ct).AsTask();

    public async Task<bool> HasActiveRenewalForPolicyAsync(Guid policyId, CancellationToken ct = default) =>
        await db.Renewals.AnyAsync(
            renewal =>
                renewal.PolicyId == policyId
                && !renewal.IsDeleted
                && !RenewalTerminalStatusCodes.Contains(renewal.CurrentStatus),
            ct);

    public async Task<PaginatedResult<Renewal>> ListAsync(RenewalListQuery query, CancellationToken ct = default)
    {
        var filtered = await ApplyFiltersAsync(GetScopedQuery(query), query, ct);
        var totalCount = await filtered.CountAsync(ct);
        var data = await ApplySort(filtered, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Renewal>(data, query.Page, query.PageSize, totalCount);
    }

    public Task UpdateAsync(Renewal renewal, CancellationToken ct = default) =>
        Task.CompletedTask;

    private IQueryable<Renewal> GetScopedQuery(RenewalListQuery query)
    {
        var renewals = db.Renewals
            .AsNoTracking()
            .Include(renewal => renewal.Account)
            .Include(renewal => renewal.Broker)
            .Include(renewal => renewal.Policy)
            .Include(renewal => renewal.AssignedToUser)
            .AsQueryable();

        if (HasRole(query.CallerRoles, "Admin"))
            return renewals;

        var normalizedRegions = NormalizeRegions(query.CallerRegions);
        var hasRegions = normalizedRegions.Length != 0;
        var includeAssigned = HasRole(query.CallerRoles, "DistributionUser") || HasRole(query.CallerRoles, "Underwriter");
        var includeRegion = HasRole(query.CallerRoles, "DistributionManager");
        var includeManagedBroker = HasRole(query.CallerRoles, "RelationshipManager");
        var includeProgramManager = HasRole(query.CallerRoles, "ProgramManager");

        return renewals.Where(renewal =>
            (includeAssigned && renewal.AssignedToUserId == query.CallerUserId)
            || (includeRegion && hasRegions && normalizedRegions.Contains(renewal.Account.Region))
            || (includeManagedBroker && renewal.Broker.ManagedByUserId == query.CallerUserId)
            || includeProgramManager);
    }

    private async Task<IQueryable<Renewal>> ApplyFiltersAsync(
        IQueryable<Renewal> query,
        RenewalListQuery filters,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            var statuses = filters.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (statuses.Length > 0)
                query = query.Where(renewal => statuses.Contains(renewal.CurrentStatus));
        }
        else if (!filters.IncludeTerminal)
        {
            query = query.Where(renewal => !RenewalTerminalStatusCodes.Contains(renewal.CurrentStatus));
        }

        if (filters.AccountId.HasValue)
            query = query.Where(renewal => renewal.AccountId == filters.AccountId.Value);

        if (filters.BrokerId.HasValue)
            query = query.Where(renewal => renewal.BrokerId == filters.BrokerId.Value);

        if (filters.AssignedToUserId.HasValue)
            query = query.Where(renewal => renewal.AssignedToUserId == filters.AssignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(filters.LineOfBusiness))
            query = query.Where(renewal => renewal.LineOfBusiness == filters.LineOfBusiness);

        query = ApplyDueWindowFilter(query, filters.DueWindow);

        if (!string.IsNullOrWhiteSpace(filters.Urgency))
        {
            var thresholds = await LoadRenewalIdentifiedThresholdsAsync(ct);
            query = ApplyUrgencyFilter(query, filters.Urgency!, thresholds);
        }

        return query;
    }

    private IQueryable<Renewal> ApplyDueWindowFilter(IQueryable<Renewal> query, string? dueWindow)
    {
        var today = DateTime.UtcNow.Date;

        return dueWindow switch
        {
            "45" => query.Where(renewal => renewal.PolicyExpirationDate >= today && renewal.PolicyExpirationDate < today.AddDays(46)),
            "60" => query.Where(renewal => renewal.PolicyExpirationDate >= today && renewal.PolicyExpirationDate < today.AddDays(61)),
            "90" => query.Where(renewal => renewal.PolicyExpirationDate >= today && renewal.PolicyExpirationDate < today.AddDays(91)),
            "overdue" => query.Where(renewal => renewal.CurrentStatus == "Identified" && renewal.TargetOutreachDate < today),
            _ => query,
        };
    }

    private IQueryable<Renewal> ApplyUrgencyFilter(
        IQueryable<Renewal> query,
        string urgency,
        IReadOnlyDictionary<string, RenewalThresholdWindow> thresholds)
    {
        var today = DateTime.UtcNow.Date;
        var specificLobs = thresholds.Keys.Where(key => !string.IsNullOrEmpty(key)).ToArray();
        var identified = query.Where(renewal => renewal.CurrentStatus == "Identified");
        IQueryable<Renewal>? matching = null;

        foreach (var entry in thresholds)
        {
            matching = AppendUrgencySegment(
                matching,
                identified,
                string.IsNullOrEmpty(entry.Key) ? null : entry.Key,
                specificLobs,
                entry.Value,
                today,
                urgency);
        }

        if (!thresholds.ContainsKey(string.Empty))
        {
            matching = AppendUrgencySegment(
                matching,
                identified,
                null,
                specificLobs,
                new RenewalThresholdWindow(60, 90),
                today,
                urgency);
        }

        return matching ?? identified.Where(_ => false);
    }

    private async Task<IReadOnlyDictionary<string, RenewalThresholdWindow>> LoadRenewalIdentifiedThresholdsAsync(CancellationToken ct)
    {
        var thresholds = await db.WorkflowSlaThresholds
            .Where(threshold => threshold.EntityType == "renewal" && threshold.Status == "Identified")
            .Select(threshold => new
            {
                Key = threshold.LineOfBusiness ?? string.Empty,
                threshold.WarningDays,
                threshold.TargetDays,
            })
            .ToListAsync(ct);

        return thresholds.ToDictionary(
            threshold => threshold.Key,
            threshold => new RenewalThresholdWindow(threshold.WarningDays, threshold.TargetDays),
            StringComparer.Ordinal);
    }

    private static IQueryable<Renewal> ApplySort(IQueryable<Renewal> query, RenewalListQuery filters)
    {
        var sort = filters.Sort.Trim().ToLowerInvariant();
        var descending = string.Equals(filters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sort, descending) switch
        {
            ("accountname", false) => query.OrderBy(renewal => renewal.AccountDisplayNameAtLink).ThenBy(renewal => renewal.PolicyExpirationDate),
            ("accountname", true) => query.OrderByDescending(renewal => renewal.AccountDisplayNameAtLink).ThenByDescending(renewal => renewal.PolicyExpirationDate),
            ("currentstatus", false) => query.OrderBy(renewal => renewal.CurrentStatus).ThenBy(renewal => renewal.PolicyExpirationDate),
            ("currentstatus", true) => query.OrderByDescending(renewal => renewal.CurrentStatus).ThenByDescending(renewal => renewal.PolicyExpirationDate),
            ("assignedtouserid", false) => query.OrderBy(renewal => renewal.AssignedToUserId).ThenBy(renewal => renewal.PolicyExpirationDate),
            ("assignedtouserid", true) => query.OrderByDescending(renewal => renewal.AssignedToUserId).ThenByDescending(renewal => renewal.PolicyExpirationDate),
            _ => query.OrderBy(renewal => renewal.PolicyExpirationDate).ThenBy(renewal => renewal.AccountDisplayNameAtLink),
        };
    }

    private static IQueryable<Renewal> AppendUrgencySegment(
        IQueryable<Renewal>? current,
        IQueryable<Renewal> source,
        string? lineOfBusiness,
        string[] specificLobs,
        RenewalThresholdWindow threshold,
        DateTime today,
        string urgency)
    {
        var lowerBound = today.AddDays(threshold.TargetDays);
        var upperBound = today.AddDays(threshold.TargetDays + threshold.WarningDays);
        IQueryable<Renewal> segment;

        if (lineOfBusiness is null)
        {
            segment = string.Equals(urgency, "overdue", StringComparison.OrdinalIgnoreCase)
                ? source.Where(renewal =>
                    !specificLobs.Contains(renewal.LineOfBusiness ?? string.Empty)
                    && renewal.PolicyExpirationDate < lowerBound)
                : source.Where(renewal =>
                    !specificLobs.Contains(renewal.LineOfBusiness ?? string.Empty)
                    && renewal.PolicyExpirationDate >= lowerBound
                    && renewal.PolicyExpirationDate < upperBound);
        }
        else
        {
            segment = string.Equals(urgency, "overdue", StringComparison.OrdinalIgnoreCase)
                ? source.Where(renewal =>
                    renewal.LineOfBusiness == lineOfBusiness
                    && renewal.PolicyExpirationDate < lowerBound)
                : source.Where(renewal =>
                    renewal.LineOfBusiness == lineOfBusiness
                    && renewal.PolicyExpirationDate >= lowerBound
                    && renewal.PolicyExpirationDate < upperBound);
        }

        return current is null ? segment : current.Concat(segment);
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static string[] NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private sealed record RenewalThresholdWindow(int WarningDays, int TargetDays);
}
