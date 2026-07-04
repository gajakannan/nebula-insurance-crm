using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class BrokerInsightService : IBrokerInsightService
{
    private readonly IBrokerInsightProjectionRepository _repo;

    public BrokerInsightService(IBrokerInsightProjectionRepository repo) => _repo = repo;

    public async Task<PaginatedResult<BrokerInsightScorecardDto>> GetScorecardsAsync(
        BrokerInsightScorecardQuery query,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(ToProjectionQuery(query), ProjectionVisibilityResolver.For(user), ct);
        var grouped = rows.GroupBy(r => new { r.BrokerId, r.BrokerName })
            .Select(g => MapScorecard(g.Key.BrokerId, g.Key.BrokerName, query, g.ToList()))
            .ToList();

        return new PaginatedResult<BrokerInsightScorecardDto>(
            grouped,
            query.Page,
            query.PageSize,
            grouped.Count);
    }

    public async Task<BrokerInsightTrendDto?> GetTrendAsync(
        BrokerInsightTrendQuery query,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(new BrokerInsightProjectionQuery(
            BrokerId: query.BrokerId,
            MetricKey: query.MetricKey,
            PeriodStart: query.PeriodStart,
            PeriodEnd: query.PeriodEnd,
            Bucket: query.Bucket,
            ProducerId: null,
            TerritoryId: null,
            ProgramId: null,
            LineOfBusiness: null,
            Region: null,
            Page: query.Page,
            PageSize: query.PageSize), ProjectionVisibilityResolver.For(user), ct);

        if (rows.Count == 0)
            return null;

        var points = rows
            .GroupBy(r => new { r.PeriodStart, r.PeriodEnd })
            .Select(g =>
            {
                var first = g.First();
                return new BrokerInsightTrendPointDto(
                    first.PeriodStart,
                    first.PeriodEnd,
                    first.Value,
                    g.Sum(r => r.Denominator),
                    g.Sum(r => r.SourceRecordCount),
                    MergeStatus(g));
            })
            .OrderBy(p => p.BucketStart)
            .ToList();

        return new BrokerInsightTrendDto(
            query.BrokerId,
            query.MetricKey,
            query.Bucket,
            query.PeriodStart,
            query.PeriodEnd,
            points,
            SourceRows: [],
            PartialData: rows.Any(IsPartial),
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    public async Task<BrokerInsightBenchmarkDto?> GetBenchmarkAsync(
        BrokerInsightBenchmarkQuery query,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(new BrokerInsightProjectionQuery(
            BrokerId: null,
            MetricKey: null,
            PeriodStart: query.PeriodStart,
            PeriodEnd: query.PeriodEnd,
            Bucket: null,
            ProducerId: null,
            TerritoryId: null,
            ProgramId: null,
            LineOfBusiness: null,
            Region: null,
            Page: 1,
            PageSize: 200), ProjectionVisibilityResolver.For(user), ct);

        if (!rows.Any(r => r.BrokerId == query.BrokerId))
            return null;

        var visiblePeerCount = rows.Select(r => r.BrokerId).Distinct().Count();
        var hasEnoughPeers = visiblePeerCount >= BrokerInsightQueryDefaults.MinimumPeerCount;
        var metrics = BrokerInsightQueryDefaults.MetricKeys
            .Select(metricKey => MapBenchmarkMetric(rows, query.BrokerId, metricKey, hasEnoughPeers))
            .ToList();

        return new BrokerInsightBenchmarkDto(
            query.BrokerId,
            query.PeriodStart,
            query.PeriodEnd,
            new BrokerInsightPeerSetDto(
                query.PeerSet,
                visiblePeerCount,
                BrokerInsightQueryDefaults.MinimumPeerCount,
                visiblePeerCount == 0 ? "NoData" : hasEnoughPeers ? "Available" : "InsufficientPeers"),
            metrics,
            DateTimeOffset.UtcNow);
    }

    public async Task<BrokerInsightSnapshotDto?> GetSnapshotAsync(
        BrokerInsightSnapshotQuery query,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var rows = await _repo.QueryAsync(new BrokerInsightProjectionQuery(
            BrokerId: query.BrokerId,
            MetricKey: null,
            PeriodStart: query.PeriodStart,
            PeriodEnd: query.PeriodEnd,
            Bucket: null,
            ProducerId: null,
            TerritoryId: null,
            ProgramId: null,
            LineOfBusiness: null,
            Region: null,
            Page: 1,
            PageSize: 200), ProjectionVisibilityResolver.For(user), ct);

        if (rows.Count == 0)
            return null;

        var brokerName = rows.First().BrokerName;
        var highlights = rows
            .Where(r => r.Value.HasValue && r.Value.Value >= 0)
            .OrderByDescending(r => r.SourceRecordCount)
            .Take(3)
            .Select(r => new BrokerInsightSnapshotItemDto(r.MetricLabel, FormatValue(r), r.SourceRecordCount))
            .ToList();
        var risks = rows
            .Where(r => IsPartial(r) || r.Denominator == 0)
            .Take(3)
            .Select(r => new BrokerInsightSnapshotItemDto(r.MetricLabel, r.Denominator == 0 ? "No authorized denominator" : r.ProjectionStatus, r.SourceRecordCount))
            .ToList();

        return new BrokerInsightSnapshotDto(
            query.BrokerId,
            brokerName,
            query.PeriodStart,
            query.PeriodEnd,
            highlights,
            risks,
            ActivitySummary: SummaryFor(rows, "Activity"),
            OpportunitySummary: SummaryFor(rows, "Pipeline"),
            SourceLinks: [],
            PartialData: rows.Any(IsPartial),
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    private static BrokerInsightProjectionQuery ToProjectionQuery(BrokerInsightScorecardQuery query) => new(
        BrokerId: query.BrokerId,
        MetricKey: null,
        PeriodStart: query.PeriodStart,
        PeriodEnd: query.PeriodEnd,
        Bucket: null,
        ProducerId: query.ProducerId,
        TerritoryId: query.TerritoryId,
        ProgramId: query.ProgramId,
        LineOfBusiness: query.LineOfBusiness,
        Region: query.Region,
        Page: query.Page,
        PageSize: query.PageSize);

    private static BrokerInsightScorecardDto MapScorecard(
        Guid brokerId,
        string brokerName,
        BrokerInsightScorecardQuery query,
        IReadOnlyList<BrokerInsightProjection> rows)
    {
        var metrics = BrokerInsightQueryDefaults.MetricKeys
            .Select(key => new { Key = key, Row = rows.FirstOrDefault(r => r.MetricKey == key) })
            .Select(metric => metric.Row is null ? EmptyMetricCard(metric.Key) : MapMetricCard(metric.Row))
            .ToList();

        var comparisonStart = rows.Select(r => r.ComparisonPeriodStart).FirstOrDefault(d => d.HasValue);
        var comparisonEnd = rows.Select(r => r.ComparisonPeriodEnd).FirstOrDefault(d => d.HasValue);

        return new BrokerInsightScorecardDto(
            brokerId,
            brokerName,
            query.PeriodStart,
            query.PeriodEnd,
            comparisonStart,
            comparisonEnd,
            new BrokerInsightFilterContextDto(query.ProducerId, query.TerritoryId, query.ProgramId, query.LineOfBusiness, query.Region),
            metrics,
            rows.Any(IsPartial),
            DateTimeOffset.UtcNow);
    }

    private static BrokerInsightMetricCardDto MapMetricCard(BrokerInsightProjection row) => new(
        row.MetricKey,
        row.MetricLabel,
        row.Denominator == 0 && row.Unit == "percentage" ? null : row.Value,
        row.ComparisonValue,
        row.Unit,
        row.Denominator,
        row.SourceRecordCount,
        row.ProjectionStatus,
        row.SourceRecordCount > 0,
        row.LastSourceUpdatedAt);

    private static BrokerInsightMetricCardDto EmptyMetricCard(string metricKey) => new(
        metricKey,
        LabelFor(metricKey),
        null,
        null,
        UnitFor(metricKey),
        0,
        0,
        "NoData",
        false,
        DateTimeOffset.UtcNow);

    private static BrokerInsightBenchmarkMetricDto MapBenchmarkMetric(
        IReadOnlyList<BrokerInsightProjection> rows,
        Guid brokerId,
        string metricKey,
        bool hasEnoughPeers)
    {
        var metricRows = rows.Where(r => r.MetricKey == metricKey).ToList();
        var broker = metricRows.FirstOrDefault(r => r.BrokerId == brokerId);
        if (broker is null)
            return new BrokerInsightBenchmarkMetricDto(metricKey, null, 0, null, null, null, null, "NoData");

        if (!hasEnoughPeers)
            return new BrokerInsightBenchmarkMetricDto(metricKey, broker.Value, broker.Denominator, null, null, null, null, "Suppressed");

        var values = metricRows.Where(r => r.Value.HasValue).Select(r => r.Value!.Value).Order().ToList();
        var median = Median(values);
        int? rank = broker.Value.HasValue ? values.Count(v => v > broker.Value.Value) + 1 : null;
        decimal? percentile = rank.HasValue && values.Count > 0
            ? Math.Round((decimal)(values.Count - rank.Value + 1) / values.Count * 100m, 2)
            : null;

        return new BrokerInsightBenchmarkMetricDto(
            metricKey,
            broker.Value,
            broker.Denominator,
            median,
            rank,
            percentile,
            broker.Value.HasValue && median.HasValue ? broker.Value.Value - median.Value : null,
            "Available");
    }

    private static decimal? Median(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0) return null;
        var mid = values.Count / 2;
        return values.Count % 2 == 0 ? (values[mid - 1] + values[mid]) / 2 : values[mid];
    }

    private static string MergeStatus(IEnumerable<BrokerInsightProjection> rows)
    {
        var list = rows.ToList();
        if (list.Any(r => r.ProjectionStatus == "Unavailable")) return "Unavailable";
        if (list.Any(IsPartial)) return "Partial";
        return list.Sum(r => r.SourceRecordCount) == 0 ? "NoData" : "Available";
    }

    private static bool IsPartial(BrokerInsightProjection row) => row.ProjectionStatus is "Partial" or "Unavailable";

    private static string FormatValue(BrokerInsightProjection row) => row.Unit switch
    {
        "percentage" => row.Value.HasValue ? $"{row.Value:0.##}%" : "N/A",
        "currency" => row.Value.HasValue ? $"{row.Value:0.##}" : "N/A",
        _ => row.Value.HasValue ? $"{row.Value:0.##}" : "N/A",
    };

    private static string? SummaryFor(IEnumerable<BrokerInsightProjection> rows, string family)
    {
        var familyRows = rows.Where(r => r.MetricFamily == family).ToList();
        if (familyRows.Count == 0) return null;
        var totalSources = familyRows.Sum(r => r.SourceRecordCount);
        return $"{family} metrics include {totalSources} authorized source record(s).";
    }

    private static string LabelFor(string metricKey) => metricKey switch
    {
        "quoteCount" => "Quote count",
        "bindCount" => "Bind count",
        "quoteToBindRate" => "Quote-to-bind rate",
        "retentionRate" => "Retention rate",
        "openPipelineCount" => "Open pipeline",
        "activityCount" => "Activity count",
        "productionAmount" => "Production",
        _ => metricKey,
    };

    private static string UnitFor(string metricKey) => metricKey switch
    {
        "quoteToBindRate" or "retentionRate" => "percentage",
        "productionAmount" => "currency",
        _ => "count",
    };
}
