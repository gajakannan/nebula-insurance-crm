namespace Nebula.Application.DTOs;

public record AccountListQuery(
    string? Query,
    string? Status,
    string? TerritoryCode,
    string? Region,
    Guid? BrokerOfRecordId,
    string? PrimaryLineOfBusiness,
    bool IncludeSummary,
    bool IncludeRemoved,
    string Sort,
    string SortDir,
    int Page,
    int PageSize);
