namespace Nebula.Domain.Entities;

/// <summary>
/// Effective-dated territory membership for a broker or producer (F0017-S0004, ADR-026).
/// Reassignment closes the prior open period and opens a new one in one transaction. A member may
/// not hold two conflicting active assignments for the same period (overlap → 409). At most one open
/// assignment per (TerritoryId, MemberType, MemberId) is enforced by a filtered unique index.
/// </summary>
public class TerritoryAssignment : BaseEntity
{
    public Guid TerritoryId { get; set; }

    public string MemberType { get; set; } = default!;    // Broker | Producer

    public Guid MemberId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Null = open/current period.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public string? AssignmentReason { get; set; }

    public Territory? Territory { get; set; }
}
