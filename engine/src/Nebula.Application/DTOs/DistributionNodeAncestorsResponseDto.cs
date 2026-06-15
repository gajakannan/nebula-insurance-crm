namespace Nebula.Application.DTOs;

/// <summary>
/// Response for GET /distribution-nodes/{nodeId}/ancestors: the node plus its ordered
/// root→parent ancestors (from the cached ancestry path).
/// </summary>
public record DistributionNodeAncestorsResponseDto(
    DistributionNodeDto Node,
    IReadOnlyList<DistributionNodeDto> Ancestors);
