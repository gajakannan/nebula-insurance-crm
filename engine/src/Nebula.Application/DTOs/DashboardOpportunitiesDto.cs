namespace Nebula.Application.DTOs;

public record DashboardOpportunitiesDto(
    IReadOnlyList<OpportunityStatusCountDto> Submissions,
    IReadOnlyList<OpportunityStatusCountDto> Renewals);

public record OpportunityStatusCountDto(
    string Status,
    int Count,
    string ColorGroup);

public record OpportunityFlowDto(
    string EntityType,
    int PeriodDays,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc,
    IReadOnlyList<OpportunityFlowNodeDto> Nodes,
    IReadOnlyList<OpportunityFlowLinkDto> Links);

public record OpportunityFlowNodeDto(
    string Status,
    string Label,
    bool IsTerminal,
    short DisplayOrder,
    string ColorGroup,
    int CurrentCount,
    int InflowCount,
    int OutflowCount,
    double? AvgDwellDays = null,
    string? Emphasis = null);

public record OpportunityFlowLinkDto(
    string SourceStatus,
    string TargetStatus,
    int Count);
