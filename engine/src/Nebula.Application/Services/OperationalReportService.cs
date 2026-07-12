using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class OperationalReportService : IOperationalReportService
{
    private readonly IOperationalReportProjectionRepository _repo;
    private readonly IBrokerInsightProjectionRepository _insightRepo;
    private readonly IDistributionScopeService _scope;

    public OperationalReportService(
        IOperationalReportProjectionRepository repo,
        IBrokerInsightProjectionRepository insightRepo,
        IDistributionScopeService scope)
    {
        _repo = repo;
        _insightRepo = insightRepo;
        _scope = scope;
    }

    public async Task<OperationalWorkloadReportDto> GetWorkloadAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(query, await ResolveAsync(query, user, ct), ct);
        var asOf = query.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var byOwner = rows.Where(r => r.OwnerUserId != null)
            .GroupBy(r => new { r.OwnerUserId, r.OwnerDisplayName })
            .Select(g => new CountByKeyDto(g.Key.OwnerUserId!.Value.ToString(), g.Key.OwnerDisplayName, g.Count()))
            .OrderByDescending(c => c.Count).Take(50).ToList();

        var byStatus = GroupCounts(rows, r => r.CurrentStatus);
        var byWorkflow = GroupCounts(rows, r => r.WorkflowType);

        var dueToday = rows.Where(r => r.IsDueToday)
            .OrderBy(r => r.OwnerDisplayName).Take(query.DrilldownLimit).Select(MapDrilldown).ToList();
        var overdue = rows.Where(r => r.IsOverdue)
            .OrderByDescending(r => r.DaysInStatus).Take(query.DrilldownLimit).Select(MapDrilldown).ToList();

        return new OperationalWorkloadReportDto(
            TotalOpen: rows.Count,
            DueToday: rows.Count(r => r.IsDueToday),
            Overdue: rows.Count(r => r.IsOverdue),
            Unassigned: rows.Count(r => r.OwnerUserId == null),
            ByOwner: byOwner,
            ByStatus: byStatus,
            ByWorkflowType: byWorkflow,
            DueTodayDrilldown: dueToday,
            OverdueDrilldown: overdue,
            AsOf: asOf,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    public async Task<WorkflowAgingReportDto> GetWorkflowAgingAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(query, await ResolveAsync(query, user, ct), ct);
        var asOf = query.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var byAgeBand = rows.Where(r => r.AgeBand != null)
            .GroupBy(r => r.AgeBand)
            .Select(g => new AgingBandDto(g.Key!, g.Count()))
            .OrderBy(b => AgeBandOrder(b.AgeBand)).ToList();

        var backlog = rows
            .OrderByDescending(r => r.DaysInStatus ?? 0)
            .Take(query.DrilldownLimit).Select(MapDrilldown).ToList();

        return new WorkflowAgingReportDto(
            TotalOpen: rows.Count,
            ByAgeBand: byAgeBand,
            ByWorkflowType: GroupCounts(rows, r => r.WorkflowType),
            ByStatus: GroupCounts(rows, r => r.CurrentStatus),
            BacklogDrilldown: backlog,
            AsOf: asOf,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    public async Task<DistributionRollupReportDto> GetDistributionRollupsAsync(
        DistributionRollupQuery query,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var reportQuery = new OperationalReportQuery(
            Region: null,
            LineOfBusiness: null,
            OwnerUserId: null,
            RootNodeId: query.RootNodeId,
            TerritoryId: query.TerritoryId,
            ProducerUserId: query.ProducerUserId,
            WorkflowType: null,
            AsOf: query.AsOf,
            DrilldownLimit: query.DrilldownLimit);
        var visibility = await ResolveAsync(reportQuery, user, ct);
        var groupBy = NormalizeGroupBy(query.GroupBy);
        var metricFamily = NormalizeMetricFamily(query.MetricFamily);
        var asOf = query.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (metricFamily != "Workflow")
            return await GetBrokerInsightRollupsAsync(query, visibility, groupBy, metricFamily, asOf, ct);

        var rows = await _repo.QueryAsync(reportQuery, visibility, ct);
        var groupedRows = rows
            .GroupBy(r => GroupKey(r, groupBy))
            .Where(g => g.Key.Key is not null)
            .Select(g =>
            {
                var list = g.ToList();
                return new DistributionRollupRowDto(
                    GroupKey: g.Key.Key!,
                    GroupLabel: g.Key.Label ?? g.Key.Key!,
                    GroupType: groupBy,
                    Metrics: MetricsFor(list),
                    DrilldownUrl: DrilldownUrl(groupBy, g.Key.Key!, asOf),
                    UnavailableReason: null);
            })
            .OrderByDescending(r => r.Metrics.RecordCount)
            .ThenBy(r => r.GroupLabel)
            .ToList();

        return new DistributionRollupReportDto(
            groupBy,
            metricFamily,
            asOf,
            DateTimeOffset.UtcNow,
            new DistributionScopeEchoDto(query.RootNodeId, query.TerritoryId, query.ProducerUserId),
            MetricsFor(rows),
            groupedRows);
    }

    private async Task<DistributionRollupReportDto> GetBrokerInsightRollupsAsync(
        DistributionRollupQuery query,
        ProjectionVisibility visibility,
        string groupBy,
        string metricFamily,
        DateOnly asOf,
        CancellationToken ct)
    {
        var periodStart = new DateOnly(asOf.Year, 1, 1);
        var metricQuery = new BrokerInsightProjectionQuery(
            BrokerId: null,
            MetricKey: null,
            PeriodStart: periodStart,
            PeriodEnd: asOf,
            Bucket: null,
            ProducerId: query.ProducerUserId,
            TerritoryId: query.TerritoryId,
            ProgramId: null,
            LineOfBusiness: null,
            Region: null,
            Page: 1,
            PageSize: Math.Max(query.DrilldownLimit, 500));
        var rows = (await _insightRepo.QueryAsync(metricQuery, visibility, ct))
            .Where(r => string.Equals(r.MetricFamily, metricFamily, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var groupedRows = rows
            .GroupBy(r => GroupKey(r, groupBy))
            .Where(g => g.Key.Key is not null)
            .Select(g =>
            {
                var list = g.ToList();
                return new DistributionRollupRowDto(
                    GroupKey: g.Key.Key!,
                    GroupLabel: g.Key.Label ?? g.Key.Key!,
                    GroupType: groupBy,
                    Metrics: MetricsFor(list, metricFamily),
                    DrilldownUrl: DrilldownUrl(groupBy, g.Key.Key!, asOf),
                    UnavailableReason: null);
            })
            .OrderByDescending(r => r.Metrics.RecordCount)
            .ThenBy(r => r.GroupLabel)
            .ToList();

        return new DistributionRollupReportDto(
            groupBy,
            metricFamily,
            asOf,
            DateTimeOffset.UtcNow,
            new DistributionScopeEchoDto(query.RootNodeId, query.TerritoryId, query.ProducerUserId),
            MetricsFor(rows, metricFamily),
            groupedRows);
    }

    private Task<ProjectionVisibility> ResolveAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct) =>
        _scope.ResolveAsync(new DistributionScopeRequest(query.RootNodeId, query.TerritoryId, query.ProducerUserId, query.AsOf), user, ct);

    private static List<CountByKeyDto> GroupCounts(IReadOnlyList<OperationalReportProjection> rows, Func<OperationalReportProjection, string?> selector) =>
        rows.Where(r => selector(r) != null)
            .GroupBy(selector)
            .Select(g => new CountByKeyDto(g.Key!, g.Key, g.Count()))
            .OrderByDescending(c => c.Count).ToList();

    private static int AgeBandOrder(string band) => band switch
    {
        "OnTrack" => 0,
        "ApproachingSla" => 1,
        "Overdue" => 2,
        _ => 3,
    };

    private static DistributionRollupMetricSetDto MetricsFor(IReadOnlyList<OperationalReportProjection> rows) => new(
        RecordCount: rows.Count,
        ProductionCount: rows.Count(r => r.SourceObjectType is "Policy" or "Submission"),
        WorkflowOpen: rows.Count(r => r.CurrentStatus is not null),
        WorkflowOverdue: rows.Count(r => r.IsOverdue),
        ActivityCount: rows.Count(r => r.SourceObjectType is "Task"));

    private static DistributionRollupMetricSetDto MetricsFor(IReadOnlyList<BrokerInsightProjection> rows, string metricFamily) => new(
        RecordCount: rows.Sum(r => r.SourceRecordCount),
        ProductionCount: metricFamily == "Production" ? rows.Sum(r => r.SourceRecordCount) : 0,
        WorkflowOpen: 0,
        WorkflowOverdue: 0,
        ActivityCount: metricFamily == "Activity" ? rows.Sum(r => r.SourceRecordCount) : 0);

    private static (string? Key, string? Label) GroupKey(OperationalReportProjection row, string groupBy) => groupBy switch
    {
        "Hierarchy" => (row.BrokerId?.ToString(), row.BrokerId?.ToString()),
        "Territory" => (row.TerritoryId?.ToString(), row.TerritoryId?.ToString()),
        "Producer" => (row.OwnerUserId?.ToString(), row.OwnerDisplayName ?? row.OwnerUserId?.ToString()),
        _ => (null, null)
    };

    private static (string? Key, string? Label) GroupKey(BrokerInsightProjection row, string groupBy) => groupBy switch
    {
        "Hierarchy" => (row.BrokerId.ToString(), row.BrokerName),
        "Territory" => (row.TerritoryId?.ToString(), row.TerritoryId?.ToString()),
        "Producer" => (row.ProducerId?.ToString(), row.ProducerId?.ToString()),
        _ => (null, null)
    };

    private static string NormalizeGroupBy(string groupBy) => groupBy.Trim() switch
    {
        "Territory" => "Territory",
        "Producer" => "Producer",
        _ => "Hierarchy"
    };

    private static string NormalizeMetricFamily(string metricFamily) => metricFamily.Trim() switch
    {
        "Workflow" => "Workflow",
        "Activity" => "Activity",
        _ => "Production"
    };

    private static string DrilldownUrl(string groupBy, string key, DateOnly asOf) => groupBy switch
    {
        "Territory" => $"/operational-reports?report=workload&territoryId={key}&asOf={asOf:yyyy-MM-dd}",
        "Producer" => $"/operational-reports?report=workload&producerUserId={key}&asOf={asOf:yyyy-MM-dd}",
        _ => $"/operational-reports?report=workload&rootNodeId={key}&asOf={asOf:yyyy-MM-dd}"
    };

    private static GlobalSearchResultDto MapDrilldown(OperationalReportProjection p) => new(
        ObjectType: p.SourceObjectType,
        ObjectId: p.SourceObjectId,
        Title: p.SourceObjectType,
        Subtitle: p.CurrentStatus is null ? null : $"{p.CurrentStatus}{(p.DaysInStatus is { } d ? $" · {d}d in status" : "")}",
        Status: p.CurrentStatus,
        OwnerUserId: p.OwnerUserId,
        OwnerDisplayName: p.OwnerDisplayName,
        LineOfBusiness: p.LineOfBusiness,
        Region: p.Region,
        MatchedFields: [],
        Snippet: p.AgeBand,
        TargetUrl: p.TargetUrl,
        Score: 1.0m,
        LastUpdatedAt: p.LastSourceUpdatedAt,
        IndexedAt: p.ProjectedAt);
}
