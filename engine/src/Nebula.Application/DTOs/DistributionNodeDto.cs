namespace Nebula.Application.DTOs;

/// <summary>
/// Read model for a distribution hierarchy node. `AncestryPath` is root→node (excluding self);
/// `RowVersion` is the xmin concurrency token serialized as a string (per distribution-node.schema.json).
/// </summary>
public record DistributionNodeDto(
    Guid Id,
    string NodeType,
    string DisplayName,
    Guid? ParentId,
    IReadOnlyList<Guid> AncestryPath,
    int Depth,
    int ChildCount,
    bool IsActive,
    string RowVersion);
