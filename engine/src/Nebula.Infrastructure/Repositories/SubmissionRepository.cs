using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public async Task<Submission?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Submissions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Submission?> GetByIdWithIncludesAsync(Guid id, CancellationToken ct = default) =>
        await db.Submissions
            .Include(s => s.Account)
            .Include(s => s.Broker)
            .Include(s => s.Program)
            .Include(s => s.AssignedToUser)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Submission submission, CancellationToken ct = default) =>
        await db.Submissions.AddAsync(submission, ct);

    public Task UpdateAsync(Submission submission, CancellationToken ct = default) =>
        Task.CompletedTask;

    public async Task<PaginatedResult<Submission>> ListAsync(
        SubmissionListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var filteredQuery = ApplyFilters(GetScopedQuery(user), query);

        if (query.Stale.HasValue)
        {
            var candidates = await filteredQuery
                .Select(submission => new SubmissionStaleInfo(
                    submission.Id,
                    submission.CurrentStatus,
                    submission.CreatedAt))
                .ToListAsync(ct);

            var staleFlags = await BuildStaleFlagsAsync(candidates, ct);
            var matchingIds = staleFlags
                .Where(item => item.Value == query.Stale.Value)
                .Select(item => item.Key)
                .ToList();

            if (matchingIds.Count == 0)
                return new PaginatedResult<Submission>([], query.Page, query.PageSize, 0);

            filteredQuery = filteredQuery.Where(submission => matchingIds.Contains(submission.Id));
        }

        var totalCount = await filteredQuery.CountAsync(ct);
        var data = await ApplySort(filteredQuery, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Submission>(data, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, bool>> GetStaleFlagsAsync(
        IReadOnlyCollection<Guid> submissionIds,
        CancellationToken ct = default)
    {
        if (submissionIds.Count == 0)
            return new Dictionary<Guid, bool>();

        var submissions = await db.Submissions
            .Where(submission => submissionIds.Contains(submission.Id))
            .Select(submission => new SubmissionStaleInfo(
                submission.Id,
                submission.CurrentStatus,
                submission.CreatedAt))
            .ToListAsync(ct);

        return await BuildStaleFlagsAsync(submissions, ct);
    }

    private IQueryable<Submission> GetScopedQuery(ICurrentUserService user)
    {
        var submissions = db.Submissions
            .AsNoTracking()
            .Include(submission => submission.Account)
            .Include(submission => submission.Broker)
            .Include(submission => submission.Program)
            .Include(submission => submission.AssignedToUser)
            .AsQueryable();

        if (HasRole(user, "Admin"))
            return submissions;

        var includeAssigned = HasRole(user, "DistributionUser") || HasRole(user, "Underwriter");
        var includeRegion = HasRole(user, "DistributionManager");
        var includeManagedBroker = HasRole(user, "RelationshipManager");
        var includeManagedProgram = HasRole(user, "ProgramManager");

        var normalizedRegions = NormalizeRegions(user.Regions);
        var managedProgramIds = db.Programs
            .Where(program => program.ManagedByUserId == user.UserId)
            .Select(program => program.Id);

        return submissions.Where(submission =>
            (includeAssigned && submission.AssignedToUserId == user.UserId)
            || (includeRegion
                && normalizedRegions.Count > 0
                && normalizedRegions.Contains(submission.Account.Region))
            || (includeManagedBroker && submission.Broker.ManagedByUserId == user.UserId)
            || (includeManagedProgram
                && submission.ProgramId.HasValue
                && managedProgramIds.Contains(submission.ProgramId.Value)));
    }

    private static IQueryable<Submission> ApplyFilters(
        IQueryable<Submission> query,
        SubmissionListQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            var statuses = filters.Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (statuses.Length > 0)
                query = query.Where(submission => statuses.Contains(submission.CurrentStatus));
        }

        if (filters.BrokerId.HasValue)
            query = query.Where(submission => submission.BrokerId == filters.BrokerId.Value);

        if (filters.AccountId.HasValue)
            query = query.Where(submission => submission.AccountId == filters.AccountId.Value);

        if (filters.AssignedToUserId.HasValue)
            query = query.Where(submission => submission.AssignedToUserId == filters.AssignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(filters.LineOfBusiness))
            query = query.Where(submission => submission.LineOfBusiness == filters.LineOfBusiness);

        return query;
    }

    private static IQueryable<Submission> ApplySort(
        IQueryable<Submission> query,
        SubmissionListQuery filters)
    {
        var sort = filters.Sort.Trim().ToLowerInvariant();
        var descending = string.Equals(filters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sort, descending) switch
        {
            ("accountname", false) => query.OrderBy(submission => submission.AccountDisplayNameAtLink).ThenBy(submission => submission.CreatedAt),
            ("accountname", true) => query.OrderByDescending(submission => submission.AccountDisplayNameAtLink).ThenByDescending(submission => submission.CreatedAt),
            ("brokername", false) => query.OrderBy(submission => submission.Broker.LegalName).ThenBy(submission => submission.CreatedAt),
            ("brokername", true) => query.OrderByDescending(submission => submission.Broker.LegalName).ThenByDescending(submission => submission.CreatedAt),
            ("currentstatus", false) => query.OrderBy(submission => submission.CurrentStatus).ThenBy(submission => submission.CreatedAt),
            ("currentstatus", true) => query.OrderByDescending(submission => submission.CurrentStatus).ThenByDescending(submission => submission.CreatedAt),
            ("effectivedate", false) => query.OrderBy(submission => submission.EffectiveDate).ThenBy(submission => submission.CreatedAt),
            ("effectivedate", true) => query.OrderByDescending(submission => submission.EffectiveDate).ThenByDescending(submission => submission.CreatedAt),
            ("createdat", false) => query.OrderBy(submission => submission.CreatedAt),
            _ => query.OrderByDescending(submission => submission.CreatedAt),
        };
    }

    private async Task<IReadOnlyDictionary<Guid, bool>> BuildStaleFlagsAsync(
        IReadOnlyCollection<SubmissionStaleInfo> submissions,
        CancellationToken ct)
    {
        if (submissions.Count == 0)
            return new Dictionary<Guid, bool>();

        var thresholds = await db.WorkflowSlaThresholds
            .Where(threshold => threshold.EntityType == "submission")
            .ToDictionaryAsync(threshold => threshold.Status, ct);

        var latestTransitionTimes = await db.WorkflowTransitions
            .Where(transition =>
                transition.WorkflowType == "Submission"
                && submissions.Select(submission => submission.Id).Contains(transition.EntityId))
            .GroupBy(transition => transition.EntityId)
            .Select(group => new
            {
                EntityId = group.Key,
                LatestOccurredAt = group.Max(item => item.OccurredAt),
            })
            .ToDictionaryAsync(item => item.EntityId, item => item.LatestOccurredAt, ct);

        var now = DateTime.UtcNow;
        var flags = new Dictionary<Guid, bool>(submissions.Count);

        foreach (var submission in submissions)
        {
            if (OpportunityStatusCatalog.SubmissionTerminalStatusCodes.Contains(submission.CurrentStatus)
                || string.Equals(submission.CurrentStatus, "ReadyForUWReview", StringComparison.Ordinal))
            {
                flags[submission.Id] = false;
                continue;
            }

            if (!thresholds.TryGetValue(submission.CurrentStatus, out var threshold))
            {
                flags[submission.Id] = false;
                continue;
            }

            var referenceTimestamp = latestTransitionTimes.TryGetValue(submission.Id, out var latestOccurredAt)
                ? latestOccurredAt
                : submission.CreatedAt;

            flags[submission.Id] = (now - referenceTimestamp).TotalHours > threshold.TargetDays * 24;
        }

        return flags;
    }

    private static bool HasRole(ICurrentUserService user, string role) =>
        user.Roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static List<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private sealed record SubmissionStaleInfo(Guid Id, string CurrentStatus, DateTime CreatedAt);
}
