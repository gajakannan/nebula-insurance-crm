using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<DashboardKpisDto> GetKpisAsync(ICurrentUserService user, int periodDays = 90, CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 90);

        var terminalSubmissionStatuses = await db.ReferenceSubmissionStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);
        var terminalRenewalStatuses = await db.ReferenceRenewalStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);

        var scopedBrokers = GetScopedBrokerQuery(user);
        var scopedSubmissions = GetScopedSubmissionQuery(user);
        var scopedRenewals = GetScopedRenewalQuery(user);

        var activeBrokers = await scopedBrokers.CountAsync(b => b.Status == "Active", ct);
        var openSubmissions = await scopedSubmissions
            .CountAsync(s => !terminalSubmissionStatuses.Contains(s.CurrentStatus), ct);

        var windowStart = DateTime.UtcNow.AddDays(-periodDays);

        // Renewal rate: % of renewals reaching Completed out of all exited renewals in the selected window.
        var exitedRenewals = await scopedRenewals
            .Where(r => terminalRenewalStatuses.Contains(r.CurrentStatus) && r.UpdatedAt >= windowStart)
            .ToListAsync(ct);

        double? renewalRate = exitedRenewals.Count > 0
            ? Math.Round(exitedRenewals.Count(r => r.CurrentStatus == "Completed") * 100.0 / exitedRenewals.Count, 1)
            : null;

        // Avg turnaround: mean days from Submission.CreatedAt to first terminal transition
        var scopedSubmissionIds = scopedSubmissions.Select(submission => submission.Id);

        var terminalTransitions = await db.WorkflowTransitions
            .Where(wt => wt.WorkflowType == "Submission"
                && scopedSubmissionIds.Contains(wt.EntityId)
                && terminalSubmissionStatuses.Contains(wt.ToState)
                && wt.OccurredAt >= windowStart)
            .GroupBy(wt => wt.EntityId)
            .Select(g => new { EntityId = g.Key, FirstTerminal = g.Min(wt => wt.OccurredAt) })
            .ToListAsync(ct);

        double? avgTurnaroundDays = null;
        if (terminalTransitions.Count > 0)
        {
            var submissionIds = terminalTransitions.Select(t => t.EntityId).ToList();
            var submissions = await scopedSubmissions
                .Where(s => submissionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.CreatedAt, ct);

            var turnarounds = terminalTransitions
                .Where(t => submissions.ContainsKey(t.EntityId))
                .Select(t => (t.FirstTerminal - submissions[t.EntityId]).TotalDays)
                .ToList();

            if (turnarounds.Count > 0)
                avgTurnaroundDays = Math.Round(turnarounds.Average(), 1);
        }

        return new DashboardKpisDto(activeBrokers, openSubmissions, renewalRate, avgTurnaroundDays);
    }

    public async Task<DashboardOpportunitiesDto> GetOpportunitiesAsync(ICurrentUserService user, int periodDays = 180, CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);
        var windowStart = DateTime.UtcNow.AddDays(-periodDays);

        var submissionStatuses = await db.ReferenceSubmissionStatuses
            .Where(s => !s.IsTerminal)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
            .ToListAsync(ct);

        var submissionTerminalStatuses = await db.ReferenceSubmissionStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);

        var scopedSubmissions = GetWindowedScopedSubmissionQuery(user, windowStart);
        var scopedRenewals = GetWindowedScopedRenewalQuery(user, windowStart);

        var submissionCounts = await scopedSubmissions
            .Where(s => !submissionTerminalStatuses.Contains(s.CurrentStatus))
            .GroupBy(s => s.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var submissionOpportunities = submissionStatuses
            .Select(s => new OpportunityStatusCountDto(
                s.Code,
                submissionCounts.GetValueOrDefault(s.Code, 0),
                s.ColorGroup ?? "intake"))
            .ToList();

        var renewalStatuses = await db.ReferenceRenewalStatuses
            .Where(s => !s.IsTerminal)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
            .ToListAsync(ct);

        var renewalTerminalStatuses = await db.ReferenceRenewalStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);

        var renewalCounts = await scopedRenewals
            .Where(r => !renewalTerminalStatuses.Contains(r.CurrentStatus))
            .GroupBy(r => r.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var renewalOpportunities = renewalStatuses
            .Select(s => new OpportunityStatusCountDto(
                s.Code,
                renewalCounts.GetValueOrDefault(s.Code, 0),
                s.ColorGroup ?? "intake"))
            .ToList();

        return new DashboardOpportunitiesDto(submissionOpportunities, renewalOpportunities);
    }

    public async Task<OpportunityFlowDto> GetOpportunityFlowAsync(
        ICurrentUserService user,
        string entityType,
        int periodDays,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);

        var normalizedEntityType = entityType.Trim().ToLowerInvariant();
        var windowEnd = DateTime.UtcNow;
        var windowStart = windowEnd.AddDays(-periodDays);

        string workflowType;
        List<StatusMeta> statuses;
        Dictionary<string, int> currentCounts;
        List<CurrentEntityState> currentEntities;

        if (normalizedEntityType == "submission")
        {
            statuses = await db.ReferenceSubmissionStatuses
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
                .ToListAsync(ct);

            var scopedSubmissions = GetWindowedScopedSubmissionQuery(user, windowStart);

            currentCounts = await scopedSubmissions
                .GroupBy(s => s.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

            currentEntities = await scopedSubmissions
                .Select(s => new CurrentEntityState(s.Id, s.CurrentStatus, s.CreatedAt))
                .ToListAsync(ct);

            workflowType = "Submission";
        }
        else if (normalizedEntityType == "renewal")
        {
            statuses = await db.ReferenceRenewalStatuses
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
                .ToListAsync(ct);

            var scopedRenewals = GetWindowedScopedRenewalQuery(user, windowStart);

            currentCounts = await scopedRenewals
                .GroupBy(r => r.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

            currentEntities = await scopedRenewals
                .Select(r => new CurrentEntityState(r.Id, r.CurrentStatus, r.CreatedAt))
                .ToListAsync(ct);

            workflowType = "Renewal";
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(entityType), "entityType must be 'submission' or 'renewal'.");
        }

        var scopedEntityIds = currentEntities.Select(entity => entity.EntityId).ToList();
        var linkRows = scopedEntityIds.Count == 0
            ? []
            : await db.WorkflowTransitions
                .Where(wt => wt.WorkflowType == workflowType
                    && scopedEntityIds.Contains(wt.EntityId)
                    && wt.OccurredAt >= windowStart
                    && wt.OccurredAt <= windowEnd
                    && wt.FromState != wt.ToState)
                .GroupBy(wt => new { wt.FromState, wt.ToState })
                .Select(g => new OpportunityFlowLinkDto(g.Key.FromState, g.Key.ToState, g.Count()))
                .ToListAsync(ct);

        var inflowByStatus = linkRows
            .GroupBy(l => l.TargetStatus)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        var outflowByStatus = linkRows
            .GroupBy(l => l.SourceStatus)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

        var knownStatuses = statuses.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownStatuses = linkRows
            .SelectMany(l => new[] { l.SourceStatus, l.TargetStatus })
            .Where(s => !knownStatuses.Contains(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .Select((status, index) => new StatusMeta(status, status, false, (short)(1000 + index), "decision"))
            .ToList();

        var allStatuses = statuses.Concat(unknownStatuses).ToList();
        var allTransitions = scopedEntityIds.Count == 0
            ? []
            : await db.WorkflowTransitions
                .Where(wt => wt.WorkflowType == workflowType && scopedEntityIds.Contains(wt.EntityId))
                .Select(wt => new CurrentStatusTransition(wt.EntityId, wt.ToState, wt.OccurredAt))
                .ToListAsync(ct);

        var avgDwellDaysByStatus = BuildAverageDwellDaysByStatus(currentEntities, allTransitions, windowEnd);

        var initialNodes = allStatuses.Select(s => new OpportunityFlowNodeDto(
            s.Code,
            s.DisplayName,
            s.IsTerminal,
            s.DisplayOrder,
            s.ColorGroup ?? (s.IsTerminal ? "decision" : "intake"),
            currentCounts.GetValueOrDefault(s.Code, 0),
            inflowByStatus.GetValueOrDefault(s.Code, 0),
            outflowByStatus.GetValueOrDefault(s.Code, 0),
            avgDwellDaysByStatus.GetValueOrDefault(s.Code)))
            .ToList();

        var emphasisByStatus = OpportunityFlowNodeEmphasisCalculator.Compute(initialNodes);
        var nodes = initialNodes
            .Select(node => node with
            {
                Emphasis = node.IsTerminal
                    ? null
                    : emphasisByStatus.GetValueOrDefault(node.Status)
            })
            .ToList();

        return new OpportunityFlowDto(
            normalizedEntityType,
            periodDays,
            windowStart,
            windowEnd,
            nodes,
            linkRows.OrderByDescending(l => l.Count).ThenBy(l => l.SourceStatus).ThenBy(l => l.TargetStatus).ToList());
    }

    public async Task<OpportunityItemsDto> GetOpportunityItemsAsync(
        ICurrentUserService user,
        string entityType,
        string status,
        CancellationToken ct = default)
    {
        if (entityType == "submission")
        {
            var query = GetScopedSubmissionQuery(user)
                .Include(s => s.Account)
                .Where(s => s.CurrentStatus == status);

            var totalCount = await query.CountAsync(ct);

            var items = await query.Take(5).ToListAsync(ct);
            var itemIds = items.Select(item => item.Id).ToList();
            var lastTransitions = itemIds.Count == 0
                ? new Dictionary<Guid, DateTime>()
                : await db.WorkflowTransitions
                    .Where(wt => wt.WorkflowType == "Submission" && wt.ToState == status && itemIds.Contains(wt.EntityId))
                    .GroupBy(wt => wt.EntityId)
                    .Select(g => new { EntityId = g.Key, LastTransition = g.Max(wt => wt.OccurredAt) })
                    .ToDictionaryAsync(g => g.EntityId, g => g.LastTransition, ct);

            var userIds = items.Select(s => s.AssignedToUserId).Distinct().ToList();
            var users = await db.UserProfiles
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

            var miniCards = items.Select(s =>
            {
                var daysInStatus = lastTransitions.TryGetValue(s.Id, out var transitionDate)
                    ? (int)(DateTime.UtcNow - transitionDate).TotalDays
                    : (int)(DateTime.UtcNow - s.CreatedAt).TotalDays;

                users.TryGetValue(s.AssignedToUserId, out var user);
                var initials = GetInitials(user?.DisplayName);

                return new OpportunityMiniCardDto(s.Id, s.Account.Name, (double)s.PremiumEstimate, daysInStatus, initials, user?.DisplayName);
            }).OrderByDescending(c => c.DaysInStatus).ToList();

            return new OpportunityItemsDto(miniCards, totalCount);
        }
        else // renewal
        {
            var query = GetScopedRenewalQuery(user)
                .Include(r => r.Account)
                .Where(r => r.CurrentStatus == status);

            var totalCount = await query.CountAsync(ct);

            var items = await query.Take(5).ToListAsync(ct);
            var itemIds = items.Select(item => item.Id).ToList();
            var lastTransitions = itemIds.Count == 0
                ? new Dictionary<Guid, DateTime>()
                : await db.WorkflowTransitions
                    .Where(wt => wt.WorkflowType == "Renewal" && wt.ToState == status && itemIds.Contains(wt.EntityId))
                    .GroupBy(wt => wt.EntityId)
                    .Select(g => new { EntityId = g.Key, LastTransition = g.Max(wt => wt.OccurredAt) })
                    .ToDictionaryAsync(g => g.EntityId, g => g.LastTransition, ct);

            var userIds = items.Select(r => r.AssignedToUserId).Distinct().ToList();
            var users = await db.UserProfiles
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

            var miniCards = items.Select(r =>
            {
                var daysInStatus = lastTransitions.TryGetValue(r.Id, out var transitionDate)
                    ? (int)(DateTime.UtcNow - transitionDate).TotalDays
                    : (int)(DateTime.UtcNow - r.CreatedAt).TotalDays;

                users.TryGetValue(r.AssignedToUserId, out var user);
                var initials = GetInitials(user?.DisplayName);

                return new OpportunityMiniCardDto(r.Id, r.Account.Name, null, daysInStatus, initials, user?.DisplayName);
            }).OrderByDescending(c => c.DaysInStatus).ToList();

            return new OpportunityItemsDto(miniCards, totalCount);
        }
    }

    public async Task<OpportunityBreakdownDto> GetOpportunityBreakdownAsync(
        ICurrentUserService user,
        string entityType,
        string status,
        string groupBy,
        int periodDays,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);
        var normalizedEntityType = entityType.Trim().ToLowerInvariant();
        var normalizedGroupBy = groupBy.Trim().ToLowerInvariant();
        var windowStart = DateTime.UtcNow.AddDays(-periodDays);

        List<BreakdownAggregation> rawGroups = normalizedEntityType switch
        {
            "submission" => await GetSubmissionBreakdownGroupsAsync(user, status, normalizedGroupBy, windowStart, ct),
            "renewal" => await GetRenewalBreakdownGroupsAsync(user, status, normalizedGroupBy, windowStart, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), "entityType must be 'submission' or 'renewal'."),
        };

        var groups = rawGroups
            .Select(raw => MapBreakdownGroup(raw.Key, raw.Count, normalizedGroupBy))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new OpportunityBreakdownDto(
            normalizedEntityType,
            status,
            GroupByValueToContractFormat(normalizedGroupBy),
            periodDays,
            groups,
            groups.Sum(group => group.Count));
    }

    private async Task<List<BreakdownAggregation>> GetSubmissionBreakdownGroupsAsync(
        ICurrentUserService user,
        string status,
        string groupBy,
        DateTime windowStart,
        CancellationToken ct)
    {
        var scopedSubmissions = GetWindowedScopedSubmissionQuery(user, windowStart)
            .Where(submission => submission.CurrentStatus == status);

        return groupBy switch
        {
            "assigneduser" => await (
                from submission in scopedSubmissions
                join assignee in db.UserProfiles on submission.AssignedToUserId equals assignee.Id into userJoin
                from assignee in userJoin.DefaultIfEmpty()
                group submission by assignee.DisplayName into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "broker" => await (
                from submission in scopedSubmissions
                join broker in db.Brokers on submission.BrokerId equals broker.Id into brokerJoin
                from broker in brokerJoin.DefaultIfEmpty()
                group submission by broker.LegalName into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "program" => await (
                from submission in scopedSubmissions
                join program in db.Programs on submission.ProgramId equals program.Id into programJoin
                from program in programJoin.DefaultIfEmpty()
                group submission by program.Name into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "lineofbusiness" => await scopedSubmissions
                .GroupBy(submission => submission.LineOfBusiness)
                .Select(grouped => new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "brokerstate" => await (
                from submission in scopedSubmissions
                join broker in db.Brokers on submission.BrokerId equals broker.Id into brokerJoin
                from broker in brokerJoin.DefaultIfEmpty()
                group submission by broker.State into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(groupBy), "Unsupported breakdown groupBy."),
        };
    }

    private async Task<List<BreakdownAggregation>> GetRenewalBreakdownGroupsAsync(
        ICurrentUserService user,
        string status,
        string groupBy,
        DateTime windowStart,
        CancellationToken ct)
    {
        var scopedRenewals = GetWindowedScopedRenewalQuery(user, windowStart)
            .Where(renewal => renewal.CurrentStatus == status);

        return groupBy switch
        {
            "assigneduser" => await (
                from renewal in scopedRenewals
                join assignee in db.UserProfiles on renewal.AssignedToUserId equals assignee.Id into userJoin
                from assignee in userJoin.DefaultIfEmpty()
                group renewal by assignee.DisplayName into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "broker" => await (
                from renewal in scopedRenewals
                join broker in db.Brokers on renewal.BrokerId equals broker.Id into brokerJoin
                from broker in brokerJoin.DefaultIfEmpty()
                group renewal by broker.LegalName into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "program" => await (
                from renewal in scopedRenewals
                join submission in db.Submissions on renewal.RenewalSubmissionId equals submission.Id into submissionJoin
                from submission in submissionJoin.DefaultIfEmpty()
                join program in db.Programs on submission.ProgramId equals program.Id into programJoin
                from program in programJoin.DefaultIfEmpty()
                group renewal by program.Name into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "lineofbusiness" => await scopedRenewals
                .GroupBy(renewal => renewal.LineOfBusiness)
                .Select(grouped => new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            "brokerstate" => await (
                from renewal in scopedRenewals
                join broker in db.Brokers on renewal.BrokerId equals broker.Id into brokerJoin
                from broker in brokerJoin.DefaultIfEmpty()
                group renewal by broker.State into grouped
                select new BreakdownAggregation(grouped.Key, grouped.Count()))
                .ToListAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(groupBy), "Unsupported breakdown groupBy."),
        };
    }

    private static OpportunityBreakdownGroupDto MapBreakdownGroup(string? key, int count, string normalizedGroupBy)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new OpportunityBreakdownGroupDto(null, "Unknown", count);

        return normalizedGroupBy == "lineofbusiness"
            ? new OpportunityBreakdownGroupDto(key, LineOfBusinessCatalog.GetDisplayLabel(key), count)
            : new OpportunityBreakdownGroupDto(key, key, count);
    }

    private static string GroupByValueToContractFormat(string normalizedGroupBy) =>
        normalizedGroupBy switch
        {
            "assigneduser" => "assignedUser",
            "lineofbusiness" => "lineOfBusiness",
            "brokerstate" => "brokerState",
            _ => normalizedGroupBy,
        };

    private static readonly IReadOnlyList<OutcomeDefinition> OutcomeDefinitions =
    [
        new("bound", "Bound", "solid"),
        new("no_quote", "No Quote", "red_dashed"),
        new("declined", "Declined", "red_dashed"),
        new("expired", "Expired", "gray_dotted"),
        new("lost_competitor", "Lost to Competitor", "red_dashed"),
    ];

    public async Task<OpportunityOutcomesDto> GetOpportunityOutcomesAsync(
        ICurrentUserService user,
        int periodDays,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);
        var windowStart = DateTime.UtcNow.AddDays(-periodDays);
        var includeSubmissions = IncludesEntityType(entityTypes, "submission");
        var includeRenewals = IncludesEntityType(entityTypes, "renewal");

        var submissionTerminalStatuses = await db.ReferenceSubmissionStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToHashSetAsync(ct);
        var renewalTerminalStatuses = await db.ReferenceRenewalStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToHashSetAsync(ct);

        var scopedSubmissionIds = includeSubmissions
            ? GetScopedSubmissionQuery(user).Select(submission => submission.Id)
            : Enumerable.Empty<Guid>().AsQueryable();
        var scopedRenewalIds = includeRenewals
            ? GetScopedRenewalQuery(user).Select(renewal => renewal.Id)
            : Enumerable.Empty<Guid>().AsQueryable();

        var submissionTransitions = includeSubmissions
            ? await db.WorkflowTransitions
                .Where(wt =>
                    wt.WorkflowType == "Submission"
                    && scopedSubmissionIds.Contains(wt.EntityId)
                    && wt.OccurredAt >= windowStart
                    && submissionTerminalStatuses.Contains(wt.ToState))
                .Select(wt => new ExitTransition(wt.EntityId, wt.ToState, wt.OccurredAt))
                .ToListAsync(ct)
            : [];

        var renewalTransitions = includeRenewals
            ? await db.WorkflowTransitions
                .Where(wt =>
                    wt.WorkflowType == "Renewal"
                    && scopedRenewalIds.Contains(wt.EntityId)
                    && wt.OccurredAt >= windowStart
                    && renewalTerminalStatuses.Contains(wt.ToState))
                .Select(wt => new ExitTransition(wt.EntityId, wt.ToState, wt.OccurredAt))
                .ToListAsync(ct)
            : [];

        var firstSubmissionExits = submissionTransitions
            .GroupBy(t => t.EntityId)
            .Select(g => g.OrderBy(t => t.ExitAtUtc).First())
            .ToList();
        var firstRenewalExits = renewalTransitions
            .GroupBy(t => t.EntityId)
            .Select(g => g.OrderBy(t => t.ExitAtUtc).First())
            .ToList();

        var submissionIds = firstSubmissionExits.Select(e => e.EntityId).ToList();
        var renewalIds = firstRenewalExits.Select(e => e.EntityId).ToList();

        var submissionCreatedAt = !includeSubmissions || submissionIds.Count == 0
            ? new Dictionary<Guid, DateTime>()
            : await GetScopedSubmissionQuery(user)
                .Where(s => submissionIds.Contains(s.Id))
                .Select(s => new { s.Id, s.CreatedAt })
                .ToDictionaryAsync(s => s.Id, s => s.CreatedAt, ct);

        var renewalCreatedAt = !includeRenewals || renewalIds.Count == 0
            ? new Dictionary<Guid, DateTime>()
            : await GetScopedRenewalQuery(user)
                .Where(r => renewalIds.Contains(r.Id))
                .Select(r => new { r.Id, r.CreatedAt })
                .ToDictionaryAsync(r => r.Id, r => r.CreatedAt, ct);

        var exits = new List<OutcomeExitEntry>();

        foreach (var exit in firstSubmissionExits)
        {
            if (!submissionCreatedAt.TryGetValue(exit.EntityId, out var createdAt))
                continue;

            var outcomeKey = MapOutcomeKey("submission", exit.ExitStatus);
            if (outcomeKey is null)
                continue;

            var daysToExit = Math.Max(0, (int)(exit.ExitAtUtc - createdAt).TotalDays);
            exits.Add(new OutcomeExitEntry(outcomeKey, daysToExit));
        }

        foreach (var exit in firstRenewalExits)
        {
            if (!renewalCreatedAt.TryGetValue(exit.EntityId, out var createdAt))
                continue;

            var outcomeKey = MapOutcomeKey("renewal", exit.ExitStatus);
            if (outcomeKey is null)
                continue;

            var daysToExit = Math.Max(0, (int)(exit.ExitAtUtc - createdAt).TotalDays);
            exits.Add(new OutcomeExitEntry(outcomeKey, daysToExit));
        }

        var grouped = exits
            .GroupBy(e => e.OutcomeKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalExits = exits.Count;

        var outcomes = OutcomeDefinitions.Select(definition =>
        {
            var entries = grouped.GetValueOrDefault(definition.Key, []);
            var count = entries.Count;
            var percent = totalExits == 0
                ? 0
                : Math.Round(count * 100.0 / totalExits, 1);
            double? avgDays = count == 0
                ? null
                : Math.Round(entries.Average(e => e.DaysToExit), 1);

            return new OpportunityOutcomeDto(
                definition.Key,
                definition.Label,
                definition.BranchStyle,
                count,
                percent,
                avgDays);
        }).ToList();

        return new OpportunityOutcomesDto(periodDays, totalExits, outcomes);
    }

    public async Task<OpportunityItemsDto> GetOpportunityOutcomeItemsAsync(
        ICurrentUserService user,
        string outcomeKey,
        int periodDays,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);
        var normalizedOutcomeKey = outcomeKey.Trim().ToLowerInvariant();

        if (!OutcomeDefinitions.Any(o => o.Key == normalizedOutcomeKey))
            throw new ArgumentOutOfRangeException(nameof(outcomeKey), "Unsupported outcome key.");

        var windowStart = DateTime.UtcNow.AddDays(-periodDays);
        var includeSubmissions = IncludesEntityType(entityTypes, "submission");
        var includeRenewals = IncludesEntityType(entityTypes, "renewal");

        var submissionItems = includeSubmissions
            ? await GetOutcomeSubmissionItemsAsync(user, normalizedOutcomeKey, windowStart, ct)
            : [];
        var renewalItems = includeRenewals
            ? await GetOutcomeRenewalItemsAsync(user, normalizedOutcomeKey, windowStart, ct)
            : [];

        var combined = submissionItems
            .Concat(renewalItems)
            .OrderByDescending(i => i.DaysInStatus)
            .ThenBy(i => i.EntityName)
            .ToList();

        return new OpportunityItemsDto(combined.Take(5).ToList(), combined.Count);
    }

    private async Task<IReadOnlyList<OpportunityMiniCardDto>> GetOutcomeSubmissionItemsAsync(
        ICurrentUserService user,
        string outcomeKey,
        DateTime windowStart,
        CancellationToken ct)
    {
        var statuses = GetOutcomeStatuses(outcomeKey, "submission");
        if (statuses.Count == 0)
            return [];

        var scopedSubmissionIds = GetScopedSubmissionQuery(user).Select(submission => submission.Id);

        var transitions = await db.WorkflowTransitions
            .Where(wt =>
                wt.WorkflowType == "Submission"
                && scopedSubmissionIds.Contains(wt.EntityId)
                && wt.OccurredAt >= windowStart
                && statuses.Contains(wt.ToState))
            .Select(wt => new ExitTransition(wt.EntityId, wt.ToState, wt.OccurredAt))
            .ToListAsync(ct);

        if (transitions.Count == 0)
            return [];

        var firstExits = transitions
            .GroupBy(t => t.EntityId)
            .Select(g => g.OrderBy(t => t.ExitAtUtc).First())
            .ToDictionary(e => e.EntityId, e => e);
        var entityIds = firstExits.Keys.ToList();

        var submissions = await GetScopedSubmissionQuery(user)
            .Include(s => s.Account)
            .Where(s => entityIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.CreatedAt,
                s.PremiumEstimate,
                AccountName = s.Account.Name,
                s.AssignedToUserId,
            })
            .ToListAsync(ct);

        var userIds = submissions.Select(s => s.AssignedToUserId).Distinct().ToList();
        Dictionary<Guid, string?> users = userIds.Count == 0
            ? []
            : await db.UserProfiles
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => (string?)u.DisplayName, ct);

        return submissions.Select(s =>
        {
            var exit = firstExits[s.Id];
            var daysToExit = Math.Max(0, (int)(exit.ExitAtUtc - s.CreatedAt).TotalDays);
            users.TryGetValue(s.AssignedToUserId, out var displayName);

            return new OpportunityMiniCardDto(
                s.Id,
                s.AccountName,
                (double?)s.PremiumEstimate,
                daysToExit,
                GetInitials(displayName),
                displayName);
        }).ToList();
    }

    private async Task<IReadOnlyList<OpportunityMiniCardDto>> GetOutcomeRenewalItemsAsync(
        ICurrentUserService user,
        string outcomeKey,
        DateTime windowStart,
        CancellationToken ct)
    {
        var statuses = GetOutcomeStatuses(outcomeKey, "renewal");
        if (statuses.Count == 0)
            return [];

        var scopedRenewalIds = GetScopedRenewalQuery(user).Select(renewal => renewal.Id);

        var transitions = await db.WorkflowTransitions
            .Where(wt =>
                wt.WorkflowType == "Renewal"
                && scopedRenewalIds.Contains(wt.EntityId)
                && wt.OccurredAt >= windowStart
                && statuses.Contains(wt.ToState))
            .Select(wt => new ExitTransition(wt.EntityId, wt.ToState, wt.OccurredAt))
            .ToListAsync(ct);

        if (transitions.Count == 0)
            return [];

        var firstExits = transitions
            .GroupBy(t => t.EntityId)
            .Select(g => g.OrderBy(t => t.ExitAtUtc).First())
            .ToDictionary(e => e.EntityId, e => e);
        var entityIds = firstExits.Keys.ToList();

        var renewals = await GetScopedRenewalQuery(user)
            .Include(r => r.Account)
            .Where(r => entityIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.CreatedAt,
                AccountName = r.Account.Name,
                r.AssignedToUserId,
            })
            .ToListAsync(ct);

        var userIds = renewals.Select(r => r.AssignedToUserId).Distinct().ToList();
        Dictionary<Guid, string?> users = userIds.Count == 0
            ? []
            : await db.UserProfiles
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => (string?)u.DisplayName, ct);

        return renewals.Select(r =>
        {
            var exit = firstExits[r.Id];
            var daysToExit = Math.Max(0, (int)(exit.ExitAtUtc - r.CreatedAt).TotalDays);
            users.TryGetValue(r.AssignedToUserId, out var displayName);

            return new OpportunityMiniCardDto(
                r.Id,
                r.AccountName,
                null,
                daysToExit,
                GetInitials(displayName),
                displayName);
        }).ToList();
    }

    private static List<string> GetOutcomeStatuses(string outcomeKey, string workflowType) =>
        (workflowType, outcomeKey) switch
        {
            ("submission", "bound") => ["Bound"],
            ("submission", "no_quote") => ["NotQuoted"],
            ("submission", "declined") => ["Declined"],
            ("submission", "expired") => ["Expired"],
            ("submission", "lost_competitor") => ["Lost", "Withdrawn"],
            ("renewal", "bound") => ["Completed"],
            ("renewal", "no_quote") => [],
            ("renewal", "declined") => [],
            ("renewal", "expired") => [],
            ("renewal", "lost_competitor") => ["Lost"],
            _ => [],
        };

    private static string? MapOutcomeKey(string workflowType, string statusCode) =>
        (workflowType, statusCode) switch
        {
            ("submission", "Bound") => "bound",
            ("submission", "NotQuoted") => "no_quote",
            ("submission", "Declined") => "declined",
            ("submission", "Expired") => "expired",
            ("submission", "Lost") => "lost_competitor",
            ("submission", "Withdrawn") => "lost_competitor",
            ("renewal", "Completed") => "bound",
            ("renewal", "Lost") => "lost_competitor",
            _ => null,
        };

    private static bool IncludesEntityType(IReadOnlyCollection<string>? entityTypes, string entityType) =>
        entityTypes is null || entityTypes.Count == 0 || entityTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<NudgeCardDto>> GetNudgesAsync(Guid userId, CancellationToken ct = default)
    {
        var nudges = new List<NudgeCardDto>();
        var today = DateTime.UtcNow.Date;

        var terminalSubmissionStatuses = await db.ReferenceSubmissionStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);
        var terminalRenewalStatuses = await db.ReferenceRenewalStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);

        // Priority 1: Overdue tasks assigned to this user (oldest DueDate first).
        // Scoped to AssignedToUserId — only tasks the user owns are surfaced.
        var overdueTasks = await db.Tasks
            .Where(t => t.AssignedToUserId == userId && t.Status != "Done"
                && t.DueDate.HasValue && t.DueDate.Value < today)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .ToListAsync(ct);

        foreach (var task in overdueTasks)
        {
            var daysOverdue = (int)(today - task.DueDate!.Value).TotalDays;
            nudges.Add(new NudgeCardDto(
                "OverdueTask", task.Title,
                $"{daysOverdue} day{(daysOverdue != 1 ? "s" : "")} overdue",
                task.LinkedEntityType ?? "Task", task.LinkedEntityId ?? task.Id,
                task.Title, daysOverdue, "Review Now"));
        }

        if (nudges.Count >= 10) return nudges.Take(10).ToList();

        // Priority 2: Stale submissions assigned to this user (>5 days in current status).
        // Scoped to AssignedToUserId — only submissions the user owns are surfaced.
        // Staleness is computed from the most recent WorkflowTransition into the submission's
        // current status, not from UpdatedAt. Falls back to CreatedAt when no matching
        // transition record exists (e.g. submission never changed state).
        var candidateSubData = await db.Submissions
            .Where(s => s.AssignedToUserId == userId
                && !terminalSubmissionStatuses.Contains(s.CurrentStatus))
            .Select(s => new { s.Id, s.CurrentStatus, AccountName = s.Account.Name, s.CreatedAt })
            .ToListAsync(ct);

        if (candidateSubData.Count > 0)
        {
            var candidateIds = candidateSubData.Select(x => x.Id).ToList();

            var allSubTransitions = await db.WorkflowTransitions
                .Where(wt => wt.WorkflowType == "Submission" && candidateIds.Contains(wt.EntityId))
                .Select(wt => new { wt.EntityId, wt.ToState, wt.OccurredAt })
                .ToListAsync(ct);

            var transitionsBySubmission = allSubTransitions
                .GroupBy(wt => wt.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var staleSubmissions = candidateSubData
                .Select(s =>
                {
                    var enteredCurrentStatus = transitionsBySubmission.TryGetValue(s.Id, out var transitions)
                        ? transitions
                            .Where(wt => wt.ToState == s.CurrentStatus)
                            .Select(wt => wt.OccurredAt)
                            .DefaultIfEmpty(s.CreatedAt)
                            .Max()
                        : s.CreatedAt;
                    var daysInStatus = (int)(DateTime.UtcNow - enteredCurrentStatus).TotalDays;
                    return new { s.Id, s.CurrentStatus, s.AccountName, DaysInStatus = daysInStatus };
                })
                .Where(x => x.DaysInStatus > 5)
                .OrderByDescending(x => x.DaysInStatus)
                .Take(10 - nudges.Count)
                .ToList();

            foreach (var sub in staleSubmissions)
            {
                nudges.Add(new NudgeCardDto(
                    "StaleSubmission", $"Follow up on {sub.AccountName}",
                    $"{sub.DaysInStatus} days in {sub.CurrentStatus}",
                    "Submission", sub.Id, sub.AccountName, sub.DaysInStatus, "Take Action"));
            }
        }

        if (nudges.Count >= 10) return nudges.Take(10).ToList();

        // Priority 3: Upcoming renewals assigned to this user (expiring within 14 days, non-terminal).
        // Scoped to AssignedToUserId — only renewals the user owns are surfaced.
        var fourteenDaysFromNow = today.AddDays(14);
        var upcomingRenewals = await db.Renewals
            .Where(r => r.AssignedToUserId == userId
                && !terminalRenewalStatuses.Contains(r.CurrentStatus)
                && r.PolicyExpirationDate >= today && r.PolicyExpirationDate <= fourteenDaysFromNow)
            .OrderBy(r => r.PolicyExpirationDate)
            .Select(r => new { r.Id, r.CurrentStatus, AccountName = r.Account.Name, r.PolicyExpirationDate })
            .Take(10 - nudges.Count)
            .ToListAsync(ct);

        foreach (var ren in upcomingRenewals)
        {
            var daysUntil = (int)(ren.PolicyExpirationDate - today).TotalDays;
            nudges.Add(new NudgeCardDto(
                "UpcomingRenewal", $"Renewal for {ren.AccountName}",
                $"Due in {daysUntil} day{(daysUntil != 1 ? "s" : "")}",
                "Renewal", ren.Id, ren.AccountName, daysUntil, "Start Outreach"));
        }

        return nudges.Take(10).ToList();
    }

    public async Task<IReadOnlyList<NudgeCardDto>> GetNudgesForBrokerUserAsync(
        IReadOnlyList<Guid> brokerIds, CancellationToken ct = default)
    {
        // F0009 §14: BrokerUser sees only OverdueTask nudges for tasks linked to their broker(s).
        // StaleSubmission and UpcomingRenewal types are excluded entirely.
        var today = DateTime.UtcNow.Date;

        var overdueTasks = await db.Tasks
            .Where(t => t.LinkedEntityType == "Broker"
                && brokerIds.Contains(t.LinkedEntityId!.Value)
                && t.Status != "Done"
                && t.DueDate.HasValue && t.DueDate.Value < today)
            .OrderBy(t => t.DueDate)
            .Take(3)
            .ToListAsync(ct);

        var nudges = overdueTasks.Select(task =>
        {
            var daysOverdue = (int)(today - task.DueDate!.Value).TotalDays;
            return new NudgeCardDto(
                "OverdueTask", task.Title,
                $"{daysOverdue} day{(daysOverdue != 1 ? "s" : "")} overdue",
                "Broker", task.LinkedEntityId!.Value,
                task.Title, daysOverdue, "Review Now");
        }).ToList();

        return nudges;
    }

    private static readonly (string Key, string Label, int Min, int Max)[] AgingBuckets =
    [
        ("0-2", "0\u20132 days", 0, 2),
        ("3-5", "3\u20135 days", 3, 5),
        ("6-10", "6\u201310 days", 6, 10),
        ("11-20", "11\u201320 days", 11, 20),
        ("21+", "21+ days", 21, int.MaxValue),
    ];

    public async Task<OpportunityAgingDto> GetOpportunityAgingAsync(
        ICurrentUserService user,
        string entityType,
        int periodDays,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);

        var normalizedEntityType = entityType.Trim().ToLowerInvariant();
        var windowStart = DateTime.UtcNow.AddDays(-periodDays);

        List<StatusMeta> statuses;
        List<EntityAgingEntry> entities;

        if (normalizedEntityType == "submission")
        {
            statuses = await db.ReferenceSubmissionStatuses
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
                .ToListAsync(ct);

            var candidates = await GetWindowedScopedSubmissionQuery(user, windowStart)
                .Select(s => new { s.Id, s.CurrentStatus, s.CreatedAt })
                .ToListAsync(ct);

            var candidateIds = candidates.Select(c => c.Id).ToList();
            var transitions = await db.WorkflowTransitions
                .Where(wt => wt.WorkflowType == "Submission" && candidateIds.Contains(wt.EntityId))
                .Select(wt => new { wt.EntityId, wt.ToState, wt.OccurredAt })
                .ToListAsync(ct);

            var transitionLookup = transitions
                .GroupBy(wt => wt.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            entities = candidates.Select(c =>
            {
                var enteredCurrent = transitionLookup.TryGetValue(c.Id, out var txns)
                    ? txns.Where(t => t.ToState == c.CurrentStatus).Select(t => t.OccurredAt).DefaultIfEmpty(c.CreatedAt).Max()
                    : c.CreatedAt;
                return new EntityAgingEntry(c.CurrentStatus, (int)(DateTime.UtcNow - enteredCurrent).TotalDays);
            }).ToList();
        }
        else if (normalizedEntityType == "renewal")
        {
            statuses = await db.ReferenceRenewalStatuses
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
                .ToListAsync(ct);

            var candidates = await GetWindowedScopedRenewalQuery(user, windowStart)
                .Select(r => new { r.Id, r.CurrentStatus, r.CreatedAt })
                .ToListAsync(ct);

            var candidateIds = candidates.Select(c => c.Id).ToList();
            var transitions = await db.WorkflowTransitions
                .Where(wt => wt.WorkflowType == "Renewal" && candidateIds.Contains(wt.EntityId))
                .Select(wt => new { wt.EntityId, wt.ToState, wt.OccurredAt })
                .ToListAsync(ct);

            var transitionLookup = transitions
                .GroupBy(wt => wt.EntityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            entities = candidates.Select(c =>
            {
                var enteredCurrent = transitionLookup.TryGetValue(c.Id, out var txns)
                    ? txns.Where(t => t.ToState == c.CurrentStatus).Select(t => t.OccurredAt).DefaultIfEmpty(c.CreatedAt).Max()
                    : c.CreatedAt;
                return new EntityAgingEntry(c.CurrentStatus, (int)(DateTime.UtcNow - enteredCurrent).TotalDays);
            }).ToList();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(entityType), "entityType must be 'submission' or 'renewal'.");
        }

        var groupedByStatus = entities
            .GroupBy(e => e.Status)
            .ToDictionary(g => g.Key, g => g.ToList());

        var thresholdRows = await db.WorkflowSlaThresholds
            .Where(threshold => threshold.EntityType == normalizedEntityType)
            .Select(threshold => new WorkflowSlaThresholdEntry(
                threshold.Status,
                threshold.LineOfBusiness,
                new WorkflowSlaThresholdDto(threshold.WarningDays, threshold.TargetDays)))
            .ToListAsync(ct);

        var thresholds = thresholdRows
            .GroupBy(row => row.Status, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => row.LineOfBusiness is null ? 0 : 1)
                    .ThenBy(row => row.Threshold.TargetDays)
                    .ThenBy(row => row.Threshold.WarningDays)
                    .First()
                    .Threshold,
                StringComparer.OrdinalIgnoreCase);

        var agingStatuses = statuses.Select(s =>
        {
            var statusEntities = groupedByStatus.GetValueOrDefault(s.Code, []);
            thresholds.TryGetValue(s.Code, out var threshold);

            OpportunityAgingSlaDto? sla = null;
            if (threshold is not null)
            {
                var bands = new SlaStatusBandsDto(
                    statusEntities.Count(entity => entity.DaysInStatus <= threshold.WarningDays),
                    statusEntities.Count(entity => entity.DaysInStatus > threshold.WarningDays && entity.DaysInStatus <= threshold.TargetDays),
                    statusEntities.Count(entity => entity.DaysInStatus > threshold.TargetDays));
                sla = OpportunityAgingSlaDto.From(threshold, bands);
            }

            var buckets = AgingBuckets.Select(b =>
            {
                var count = statusEntities.Count(e => e.DaysInStatus >= b.Min && e.DaysInStatus <= b.Max);
                return new OpportunityAgingBucketDto(b.Key, b.Label, count);
            }).ToList();

            return new OpportunityAgingStatusDto(
                s.Code, s.DisplayName, s.ColorGroup ?? "intake", s.DisplayOrder, sla, buckets, statusEntities.Count);
        }).ToList();

        return new OpportunityAgingDto(normalizedEntityType, periodDays, agingStatuses);
    }

    public async Task<OpportunityHierarchyDto> GetOpportunityHierarchyAsync(
        ICurrentUserService user,
        int periodDays,
        CancellationToken ct = default)
    {
        periodDays = NormalizePeriodDays(periodDays, 180);

        // Submissions — include all statuses (active + terminal) for composition views
        var submissionStatuses = await db.ReferenceSubmissionStatuses
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
            .ToListAsync(ct);

        var submissionCounts = await GetScopedSubmissionQuery(user)
            .GroupBy(s => s.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        // Renewals — include all statuses (active + terminal) for composition views
        var renewalStatuses = await db.ReferenceRenewalStatuses
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StatusMeta(s.Code, s.DisplayName, s.IsTerminal, s.DisplayOrder, s.ColorGroup))
            .ToListAsync(ct);

        var renewalCounts = await GetScopedRenewalQuery(user)
            .GroupBy(r => r.CurrentStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        var submissionChildren = BuildHierarchyChildren("submission", submissionStatuses, submissionCounts);
        var renewalChildren = BuildHierarchyChildren("renewal", renewalStatuses, renewalCounts);

        var submissionTotal = submissionChildren.Sum(c => c.Count);
        var renewalTotal = renewalChildren.Sum(c => c.Count);

        var root = new OpportunityHierarchyNodeDto(
            "root", "All Opportunities", submissionTotal + renewalTotal,
            Children:
            [
                new OpportunityHierarchyNodeDto("submission", "Submissions", submissionTotal, "entityType", Children: submissionChildren),
                new OpportunityHierarchyNodeDto("renewal", "Renewals", renewalTotal, "entityType", Children: renewalChildren),
            ]);

        return new OpportunityHierarchyDto(periodDays, root);
    }

    private static List<OpportunityHierarchyNodeDto> BuildHierarchyChildren(
        string entityType,
        List<StatusMeta> statuses,
        Dictionary<string, int> counts)
    {
        return statuses
            .GroupBy(s => s.ColorGroup ?? "intake")
            .Select(colorGrouping =>
            {
                var statusChildren = colorGrouping
                    .Select(s => new OpportunityHierarchyNodeDto(
                        $"{entityType}:{colorGrouping.Key}:{s.Code}",
                        s.DisplayName,
                        counts.GetValueOrDefault(s.Code, 0),
                        "status",
                        colorGrouping.Key))
                    .ToList();

                var groupLabel = char.ToUpperInvariant(colorGrouping.Key[0]) + colorGrouping.Key[1..];

                return new OpportunityHierarchyNodeDto(
                    $"{entityType}:{colorGrouping.Key}",
                    groupLabel,
                    statusChildren.Sum(c => c.Count),
                    "colorGroup",
                    colorGrouping.Key,
                    statusChildren);
            })
            .ToList();
    }

    private IQueryable<Broker> GetScopedBrokerQuery(ICurrentUserService user)
    {
        var brokers = db.Brokers.AsQueryable();
        if (HasRole(user, "Admin"))
            return brokers;

        var includeAssigned = HasRole(user, "DistributionUser") || HasRole(user, "Underwriter");
        var includeRegion = HasRole(user, "DistributionManager") || HasRole(user, "Underwriter");
        var includeManagedBroker = HasRole(user, "RelationshipManager");
        var includeManagedProgram = HasRole(user, "ProgramManager");

        var normalizedRegions = NormalizeRegions(user.Regions);
        var managedProgramIds = db.Programs
            .Where(program => program.ManagedByUserId == user.UserId)
            .Select(program => program.Id);
        var programScopedSubmissionIds = db.Submissions
            .Where(submission => submission.ProgramId.HasValue && managedProgramIds.Contains(submission.ProgramId.Value))
            .Select(submission => submission.Id);
        var programScopedBrokerIds = db.Submissions
            .Where(submission => submission.ProgramId.HasValue && managedProgramIds.Contains(submission.ProgramId.Value))
            .Select(submission => submission.BrokerId)
            .Union(db.Renewals
                .Where(renewal => renewal.RenewalSubmissionId.HasValue && programScopedSubmissionIds.Contains(renewal.RenewalSubmissionId.Value))
                .Select(renewal => renewal.BrokerId))
            .Union(db.Brokers
                .Where(broker => broker.PrimaryProgramId.HasValue && managedProgramIds.Contains(broker.PrimaryProgramId.Value))
                .Select(broker => broker.Id));
        var assignedBrokerIds = db.Submissions
            .Where(submission => submission.AssignedToUserId == user.UserId)
            .Select(submission => submission.BrokerId)
            .Union(db.Renewals
                .Where(renewal => renewal.AssignedToUserId == user.UserId)
                .Select(renewal => renewal.BrokerId));

        return brokers.Where(broker =>
            (includeAssigned && assignedBrokerIds.Contains(broker.Id))
            || (includeRegion
                && normalizedRegions.Count > 0
                && broker.BrokerRegions.Any(region => normalizedRegions.Contains(region.Region)))
            || (includeManagedBroker && broker.ManagedByUserId == user.UserId)
            || (includeManagedProgram && programScopedBrokerIds.Contains(broker.Id)));
    }

    private IQueryable<Submission> GetScopedSubmissionQuery(ICurrentUserService user)
    {
        var submissions = db.Submissions.AsQueryable();
        if (HasRole(user, "Admin"))
            return submissions;

        var includeAssigned = HasRole(user, "DistributionUser") || HasRole(user, "Underwriter");
        var includeRegion = HasRole(user, "DistributionManager") || HasRole(user, "Underwriter");
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
                && submission.Broker.BrokerRegions.Any(region => normalizedRegions.Contains(region.Region)))
            || (includeManagedBroker && submission.Broker.ManagedByUserId == user.UserId)
            || (includeManagedProgram
                && submission.ProgramId.HasValue
                && managedProgramIds.Contains(submission.ProgramId.Value)));
    }

    private IQueryable<Submission> GetWindowedScopedSubmissionQuery(ICurrentUserService user, DateTime windowStart) =>
        GetScopedSubmissionQuery(user).Where(submission => submission.CreatedAt >= windowStart);

    private IQueryable<Renewal> GetScopedRenewalQuery(ICurrentUserService user)
    {
        var renewals = db.Renewals.AsQueryable();
        if (HasRole(user, "Admin"))
            return renewals;

        var includeAssigned = HasRole(user, "DistributionUser") || HasRole(user, "Underwriter");
        var includeRegion = HasRole(user, "DistributionManager") || HasRole(user, "Underwriter");
        var includeManagedBroker = HasRole(user, "RelationshipManager");
        var includeManagedProgram = HasRole(user, "ProgramManager");

        var normalizedRegions = NormalizeRegions(user.Regions);
        var managedProgramIds = db.Programs
            .Where(program => program.ManagedByUserId == user.UserId)
            .Select(program => program.Id);
        var programScopedSubmissionIds = db.Submissions
            .Where(submission => submission.ProgramId.HasValue && managedProgramIds.Contains(submission.ProgramId.Value))
            .Select(submission => submission.Id);

        return renewals.Where(renewal =>
            (includeAssigned && renewal.AssignedToUserId == user.UserId)
            || (includeRegion
                && normalizedRegions.Count > 0
                && renewal.Broker.BrokerRegions.Any(region => normalizedRegions.Contains(region.Region)))
            || (includeManagedBroker && renewal.Broker.ManagedByUserId == user.UserId)
            || (includeManagedProgram
                && renewal.RenewalSubmissionId.HasValue
                && programScopedSubmissionIds.Contains(renewal.RenewalSubmissionId.Value)));
    }

    private IQueryable<Renewal> GetWindowedScopedRenewalQuery(ICurrentUserService user, DateTime windowStart) =>
        GetScopedRenewalQuery(user).Where(renewal => renewal.CreatedAt >= windowStart);

    private static bool HasRole(ICurrentUserService user, string role) =>
        user.Roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static List<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private sealed record EntityAgingEntry(string Status, int DaysInStatus);
    private sealed record BreakdownAggregation(string? Key, int Count);
    private sealed record CurrentEntityState(Guid EntityId, string Status, DateTime CreatedAt);
    private sealed record CurrentStatusTransition(Guid EntityId, string ToState, DateTime OccurredAt);
    private sealed record WorkflowSlaThresholdEntry(string Status, string? LineOfBusiness, WorkflowSlaThresholdDto Threshold);
    private sealed record ExitTransition(Guid EntityId, string ExitStatus, DateTime ExitAtUtc);
    private sealed record OutcomeExitEntry(string OutcomeKey, int DaysToExit);
    private sealed record OutcomeDefinition(string Key, string Label, string BranchStyle);

    private static string? GetInitials(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return null;

        var initials = string.Concat(displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part[0]))
            .ToUpperInvariant();

        return initials.Length switch
        {
            0 => null,
            <= 2 => initials,
            _ => initials[..2],
        };
    }

    private sealed record StatusMeta(
        string Code,
        string DisplayName,
        bool IsTerminal,
        short DisplayOrder,
        string? ColorGroup);

    private static int NormalizePeriodDays(int periodDays, int defaultDays)
    {
        if (periodDays <= 0)
            return defaultDays;
        return Math.Min(periodDays, 730);
    }

    private static Dictionary<string, double> BuildAverageDwellDaysByStatus(
        IReadOnlyList<CurrentEntityState> entities,
        IReadOnlyList<CurrentStatusTransition> transitions,
        DateTime nowUtc)
    {
        var enteredLookup = transitions
            .GroupBy(t => new { t.EntityId, t.ToState })
            .ToDictionary(
                g => (g.Key.EntityId, Status: g.Key.ToState),
                g => g.Max(t => t.OccurredAt));

        return entities
            .GroupBy(entity => entity.Status)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var dwellDays = g.Select(entity =>
                    {
                        var enteredAt = enteredLookup.TryGetValue((entity.EntityId, entity.Status), out var value)
                            ? value
                            : entity.CreatedAt;

                        return Math.Max(0, (nowUtc - enteredAt).TotalDays);
                    });

                    return Math.Round(dwellDays.Average(), 1);
                },
                StringComparer.OrdinalIgnoreCase);
    }
}
