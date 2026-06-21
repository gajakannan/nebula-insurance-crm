namespace Nebula.Domain.Entities;

/// <summary>
/// Persisted search/report criteria a user (Personal) or an administered team scope
/// (Team) can apply. The only F0023 user-mutation surface; every mutation appends a
/// <see cref="SavedViewAuditEvent"/>. Concurrency uses the inherited <see cref="BaseEntity.RowVersion"/>.
/// </summary>
public class SavedView : BaseEntity
{
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Description { get; set; }

    /// <summary>Search | WorkloadReport | WorkflowAgingReport</summary>
    public required string ViewType { get; set; }

    /// <summary>Personal | Team</summary>
    public required string Visibility { get; set; }

    public Guid OwnerUserId { get; set; }

    /// <summary>Role | Region | Program | Territory (Team visibility only).</summary>
    public string? TeamScopeType { get; set; }
    public string? TeamScopeKey { get; set; }

    /// <summary>Structured, server-validated criteria JSON (not opaque display text).</summary>
    public required string CriteriaJson { get; set; }
    public string SortJson { get; set; } = "{}";

    public bool IsDefault { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? LastEditedByUserId { get; set; }
}
