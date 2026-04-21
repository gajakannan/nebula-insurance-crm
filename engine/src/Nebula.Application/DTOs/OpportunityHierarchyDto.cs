namespace Nebula.Application.DTOs;

public record OpportunityHierarchyDto(
    int PeriodDays,
    OpportunityHierarchyNodeDto Root);

public record OpportunityHierarchyNodeDto(
    string Id,
    string Label,
    int Count,
    string? LevelType = null,
    string? ColorGroup = null,
    IReadOnlyList<OpportunityHierarchyNodeDto>? Children = null);
