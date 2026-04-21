namespace Nebula.Application.DTOs;

public record OpportunityOutcomesDto(
    int PeriodDays,
    int TotalExits,
    IReadOnlyList<OpportunityOutcomeDto> Outcomes);

public record OpportunityOutcomeDto(
    string Key,
    string Label,
    string BranchStyle,
    int Count,
    double PercentOfTotal,
    double? AverageDaysToExit);

