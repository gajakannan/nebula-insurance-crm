namespace Nebula.Application.DTOs;

/// <summary>
/// Point-in-time territory assignment lookup for GET /territory-assignments
/// (per territory-assignment-lookup-response.schema.json). `Assignment` is null when no period covers `AsOf`.
/// </summary>
public record TerritoryAssignmentLookupResponseDto(
    string MemberType,
    Guid MemberId,
    DateOnly AsOf,
    TerritoryAssignmentDto? Assignment);
