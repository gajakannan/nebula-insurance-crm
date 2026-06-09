namespace Nebula.Application.DTOs;

/// <summary>
/// Read model for an effective-dated territory membership (per territory-assignment.schema.json).
/// `MemberDisplayName` is not resolved in F0017 (schema-nullable). Dates serialize as `YYYY-MM-DD`.
/// </summary>
public record TerritoryAssignmentDto(
    Guid Id,
    Guid TerritoryId,
    string? TerritoryName,
    string MemberType,
    Guid MemberId,
    string? MemberDisplayName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? AssignmentReason,
    string RowVersion,
    Guid? ChangedBy,
    DateTime? ChangedAt);
