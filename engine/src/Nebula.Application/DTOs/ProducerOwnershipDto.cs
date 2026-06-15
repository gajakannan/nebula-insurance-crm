namespace Nebula.Application.DTOs;

/// <summary>
/// Read model for an effective-dated producer ownership period (per producer-ownership.schema.json).
/// `RowVersion` is the xmin concurrency token as a string. `ChangedBy`/`ChangedAt` reflect who opened
/// the period and when. Dates serialize as `YYYY-MM-DD`.
/// </summary>
public record ProducerOwnershipDto(
    Guid Id,
    string ScopeType,
    Guid ScopeId,
    Guid ProducerNodeId,
    string? ProducerDisplayName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? AssignmentReason,
    string RowVersion,
    Guid? ChangedBy,
    DateTime? ChangedAt);
