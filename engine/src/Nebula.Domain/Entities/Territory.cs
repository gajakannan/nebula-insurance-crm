namespace Nebula.Domain.Entities;

/// <summary>
/// Territory definition used for distribution accountability (F0017-S0004, ADR-026).
/// Active territory names are unique (case-insensitive) via a filtered unique index.
/// Nested territories are out of scope.
/// </summary>
public class Territory : BaseEntity
{
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>Region/segment criteria (object of string→string) stored as JSON text.</summary>
    public string CriteriaJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public ICollection<TerritoryAssignment> Assignments { get; set; } = [];
}
