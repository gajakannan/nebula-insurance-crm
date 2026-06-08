namespace Nebula.Application.DTOs;

/// <summary>
/// Body for PUT /distribution-nodes/{nodeId}/parent. `ParentId` null clears the parent (makes the
/// node a root). `Note` is an optional free-text reason recorded on the timeline event.
/// </summary>
public record DistributionNodeParentRequestDto(
    Guid? ParentId,
    string? Note);
