namespace Nebula.Application.DTOs;

/// <summary>Validated operational-report query. Filters bounded; AsOf defaults to today.</summary>
public sealed record OperationalReportQuery(
    string? Region,
    string? LineOfBusiness,
    Guid? OwnerUserId,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    string? WorkflowType,
    DateOnly? AsOf,
    int DrilldownLimit);

public sealed record CountByKeyDto(string Key, string? Label, int Count);

public sealed record AgingBandDto(string AgeBand, int Count);

/// <summary>Daily operational workload report (S0005).</summary>
public sealed record OperationalWorkloadReportDto(
    int TotalOpen,
    int DueToday,
    int Overdue,
    int Unassigned,
    IReadOnlyList<CountByKeyDto> ByOwner,
    IReadOnlyList<CountByKeyDto> ByStatus,
    IReadOnlyList<CountByKeyDto> ByWorkflowType,
    IReadOnlyList<GlobalSearchResultDto> DueTodayDrilldown,
    IReadOnlyList<GlobalSearchResultDto> OverdueDrilldown,
    DateOnly AsOf,
    DateTimeOffset GeneratedAt);

/// <summary>Workflow aging and backlog report (S0006).</summary>
public sealed record WorkflowAgingReportDto(
    int TotalOpen,
    IReadOnlyList<AgingBandDto> ByAgeBand,
    IReadOnlyList<CountByKeyDto> ByWorkflowType,
    IReadOnlyList<CountByKeyDto> ByStatus,
    IReadOnlyList<GlobalSearchResultDto> BacklogDrilldown,
    DateOnly AsOf,
    DateTimeOffset GeneratedAt);

public sealed record DistributionRollupQuery(
    string GroupBy,
    string MetricFamily,
    DateOnly? AsOf,
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId,
    int DrilldownLimit);

public sealed record DistributionScopeEchoDto(
    Guid? RootNodeId,
    Guid? TerritoryId,
    Guid? ProducerUserId);

public sealed record DistributionRollupMetricSetDto(
    int RecordCount,
    int ProductionCount,
    int WorkflowOpen,
    int WorkflowOverdue,
    int ActivityCount);

public sealed record DistributionRollupRowDto(
    string GroupKey,
    string GroupLabel,
    string GroupType,
    DistributionRollupMetricSetDto Metrics,
    string? DrilldownUrl,
    string? UnavailableReason);

public sealed record DistributionRollupReportDto(
    string GroupBy,
    string MetricFamily,
    DateOnly AsOf,
    DateTimeOffset GeneratedAt,
    DistributionScopeEchoDto? Scope,
    DistributionRollupMetricSetDto Totals,
    IReadOnlyList<DistributionRollupRowDto> Rows);
