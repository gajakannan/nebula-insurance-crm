namespace Nebula.Domain.Entities;

/// <summary>
/// Self-referencing distribution hierarchy node (MGA, broker, sub-broker, or producer).
/// F0017-S0001/S0002 (ADR-026): arbitrary-depth tree with a materialized ancestry cache.
/// node_type ordering is advisory (not enforced) so any node may parent any node, subject to
/// the no-self-parent / no-cycle / no-orphan integrity rules enforced by DistributionNodeService.
/// </summary>
public class DistributionNode : BaseEntity
{
    public string NodeType { get; set; } = default!;      // MGA | Broker | SubBroker | Producer

    public string DisplayName { get; set; } = default!;

    public Guid? ParentId { get; set; }

    /// <summary>
    /// Materialized ancestry path, root-first, ancestors only (excludes self), '/'-delimited GUIDs:
    /// "/{root}/{...}/{parent}". Empty string for a root node. Recomputed transactionally for the
    /// node and all descendants on every reparent. Exposed to the API as ancestryPath[] (root→node, excluding self).
    /// </summary>
    public string AncestryPath { get; set; } = "";

    /// <summary>Ancestor count (= AncestryPath segment count). Root depth is 0.</summary>
    public int Depth { get; set; }

    /// <summary>Direct child count, maintained on reparent of any child.</summary>
    public int ChildCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DistributionNode? Parent { get; set; }
    public ICollection<DistributionNode> Children { get; set; } = [];
}
