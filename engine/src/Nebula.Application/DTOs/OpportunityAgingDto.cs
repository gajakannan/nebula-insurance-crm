namespace Nebula.Application.DTOs;

public record OpportunityAgingDto(
    string EntityType,
    int PeriodDays,
    IReadOnlyList<OpportunityAgingStatusDto> Statuses);

public record OpportunityAgingStatusDto(
    string Status,
    string Label,
    string ColorGroup,
    short DisplayOrder,
    OpportunityAgingSlaDto? Sla,
    IReadOnlyList<OpportunityAgingBucketDto> Buckets,
    int Total);

public record WorkflowSlaThresholdDto(
    int WarningDays,
    int TargetDays);

public record SlaStatusBandsDto(
    int OnTimeCount,
    int ApproachingCount,
    int OverdueCount);

public record OpportunityAgingSlaDto(
    int WarningDays,
    int TargetDays,
    int OnTimeCount,
    int ApproachingCount,
    int OverdueCount)
{
    public static OpportunityAgingSlaDto From(WorkflowSlaThresholdDto threshold, SlaStatusBandsDto bands) =>
        new(
            threshold.WarningDays,
            threshold.TargetDays,
            bands.OnTimeCount,
            bands.ApproachingCount,
            bands.OverdueCount);
}

public record OpportunityAgingBucketDto(
    string Key,
    string Label,
    int Count);
