namespace Nebula.Domain.Entities;

/// <summary>
/// Immutable, append-only audit record for every saved-view mutation. Written in the
/// same transaction as the <see cref="SavedView"/> change. Not a <see cref="BaseEntity"/>
/// (no soft-delete / row-version): audit rows are never updated or deleted.
/// </summary>
public class SavedViewAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SavedViewId { get; set; }

    /// <summary>Created | Updated | DefaultChanged | Archived</summary>
    public required string EventType { get; set; }

    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }

    public SavedView SavedView { get; set; } = default!;
}
