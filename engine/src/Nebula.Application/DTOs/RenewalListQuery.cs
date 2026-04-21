namespace Nebula.Application.DTOs;

public record RenewalListQuery(
    Guid CallerUserId,
    IReadOnlyList<string> CallerRoles,
    IReadOnlyList<string> CallerRegions,
    string? DueWindow,
    string? Status,
    Guid? AssignedToUserId,
    string? LineOfBusiness,
    Guid? AccountId,
    Guid? BrokerId,
    bool IncludeTerminal,
    string? Urgency,
    string Sort,
    string SortDir,
    int Page,
    int PageSize);
