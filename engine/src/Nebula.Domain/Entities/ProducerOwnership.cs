namespace Nebula.Domain.Entities;

/// <summary>
/// Effective-dated producer ownership period for an account or broker-relationship scope.
/// F0017-S0003 (ADR-026): reassignment closes the prior open period and opens a new one in one
/// transaction; rows are never destructively overwritten. At most one open (EffectiveTo == null)
/// period per (ScopeType, ScopeId) is enforced by a filtered unique index and by the service.
/// </summary>
public class ProducerOwnership : BaseEntity
{
    public string ScopeType { get; set; } = default!;     // Account | BrokerRelationship

    public Guid ScopeId { get; set; }

    public Guid ProducerNodeId { get; set; }              // FK -> DistributionNode (NodeType == Producer)

    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Null = open/current period.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public string? AssignmentReason { get; set; }

    public DistributionNode? ProducerNode { get; set; }
}
