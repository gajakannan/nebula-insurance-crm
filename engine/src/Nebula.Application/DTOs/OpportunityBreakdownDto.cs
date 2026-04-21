namespace Nebula.Application.DTOs;

public record OpportunityBreakdownDto(
    string EntityType,
    string Status,
    string GroupBy,
    int PeriodDays,
    IReadOnlyList<OpportunityBreakdownGroupDto> Groups,
    int Total);

public record OpportunityBreakdownGroupDto(
    string? Key,
    string Label,
    int Count);
