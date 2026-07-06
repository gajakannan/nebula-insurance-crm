using Nebula.Application.Common;

namespace Nebula.Application.DTOs;

public sealed record BrokerInsightScorecardQuery(
    Guid? BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? ProducerId,
    Guid? TerritoryId,
    Guid? ProgramId,
    string? LineOfBusiness,
    string? Region,
    int Page,
    int PageSize);

public sealed record BrokerInsightTrendQuery(
    Guid BrokerId,
    string MetricKey,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Bucket,
    int Page,
    int PageSize);

public sealed record BrokerInsightBenchmarkQuery(
    Guid BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeerSet);

public sealed record BrokerInsightSnapshotQuery(
    Guid BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);

public sealed record BrokerInsightFilterContextDto(
    Guid? ProducerId,
    Guid? TerritoryId,
    Guid? ProgramId,
    string? LineOfBusiness,
    string? Region);

public sealed record BrokerInsightMetricCardDto(
    string MetricKey,
    string Label,
    decimal? Value,
    decimal? ComparisonValue,
    string Unit,
    int Denominator,
    int SourceRecordCount,
    string Status,
    bool DrilldownAvailable,
    DateTimeOffset LastRefreshedAt);

public sealed record BrokerInsightScorecardDto(
    Guid BrokerId,
    string BrokerName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? ComparisonPeriodStart,
    DateOnly? ComparisonPeriodEnd,
    BrokerInsightFilterContextDto? Filters,
    IReadOnlyList<BrokerInsightMetricCardDto> Metrics,
    bool PartialData,
    DateTimeOffset GeneratedAt);

public sealed record BrokerInsightTrendPointDto(
    DateOnly BucketStart,
    DateOnly BucketEnd,
    decimal? Value,
    int Denominator,
    int SourceRecordCount,
    string Status);

public sealed record BrokerInsightTrendDto(
    Guid BrokerId,
    string MetricKey,
    string Bucket,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<BrokerInsightTrendPointDto> Points,
    IReadOnlyList<GlobalSearchResultDto> SourceRows,
    bool PartialData,
    DateTimeOffset GeneratedAt);

public sealed record BrokerInsightPeerSetDto(
    string Type,
    int VisiblePeerCount,
    int MinimumPeerCount,
    string Status);

public sealed record BrokerInsightBenchmarkMetricDto(
    string MetricKey,
    decimal? BrokerValue,
    int Denominator,
    decimal? PeerMedian,
    int? Rank,
    decimal? Percentile,
    decimal? Variance,
    string Status);

public sealed record BrokerInsightBenchmarkDto(
    Guid BrokerId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    BrokerInsightPeerSetDto PeerSet,
    IReadOnlyList<BrokerInsightBenchmarkMetricDto> Metrics,
    DateTimeOffset GeneratedAt);

public sealed record BrokerInsightSnapshotItemDto(
    string Label,
    string Value,
    int SourceRecordCount);

public sealed record BrokerInsightSnapshotDto(
    Guid BrokerId,
    string BrokerName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<BrokerInsightSnapshotItemDto> Highlights,
    IReadOnlyList<BrokerInsightSnapshotItemDto> Risks,
    string? ActivitySummary,
    string? OpportunitySummary,
    IReadOnlyList<GlobalSearchResultDto> SourceLinks,
    bool PartialData,
    DateTimeOffset GeneratedAt);

public sealed record BrokerInsightProjectionQuery(
    Guid? BrokerId,
    string? MetricKey,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Bucket,
    Guid? ProducerId,
    Guid? TerritoryId,
    Guid? ProgramId,
    string? LineOfBusiness,
    string? Region,
    int Page,
    int PageSize);

public static class BrokerInsightQueryDefaults
{
    public const int MinimumPeerCount = 5;
    public static readonly string[] MetricKeys =
    [
        "quoteCount",
        "bindCount",
        "quoteToBindRate",
        "retentionRate",
        "openPipelineCount",
        "activityCount",
        "productionAmount"
    ];

    public static readonly string[] Buckets = ["day", "week", "month", "quarter"];
    public static readonly string[] PeerSets = ["visibleBrokerGroup", "producer", "territory", "program", "region"];
}
