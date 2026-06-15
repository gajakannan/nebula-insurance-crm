namespace Nebula.Application.DTOs;

/// <summary>
/// Body for POST /producer-ownership (per producer-ownership-assignment-request.schema.json).
/// The service derives period boundaries: a reassignment closes the current open period at
/// <see cref="EffectiveFrom"/> and opens the new one.
/// </summary>
public record ProducerOwnershipAssignmentRequestDto(
    string ScopeType,
    Guid ScopeId,
    Guid ProducerNodeId,
    DateOnly EffectiveFrom,
    string? AssignmentReason);
