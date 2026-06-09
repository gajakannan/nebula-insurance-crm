namespace Nebula.Application.DTOs;

/// <summary>
/// Body for POST /territories/{territoryId}/members (per territory-member-assignment-request.schema.json).
/// A reassignment closes the member's current open period at <see cref="EffectiveFrom"/> and opens the new one.
/// </summary>
public record TerritoryMemberAssignmentRequestDto(
    string MemberType,
    Guid MemberId,
    DateOnly EffectiveFrom,
    string? AssignmentReason);
