namespace Nebula.Domain.Entities;

/// <summary>
/// Read-model row backing F0023 global search. One row per indexed source object
/// (Account, Broker/MGA/Program, Policy, Submission, Renewal, Task). Source modules
/// remain authoritative for record detail and authorization; this projection only
/// supports permission-filtered discovery.
/// </summary>
public class SearchDocument : BaseEntity
{
    public required string ObjectType { get; set; }
    public Guid ObjectId { get; set; }
    public required string TargetUrl { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Status { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }

    // Source-visibility correlation keys (used to apply per-user authorization filters).
    public Guid? AccountId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? RenewalId { get; set; }
    public Guid? TaskId { get; set; }

    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? TerritoryId { get; set; }

    public required string SearchText { get; set; }
    public string MatchedFieldHintsJson { get; set; } = "[]";

    public DateTimeOffset SourceUpdatedAt { get; set; }
    public DateTimeOffset IndexedAt { get; set; }
    public string? LastProjectionError { get; set; }
}
