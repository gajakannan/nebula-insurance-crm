namespace Nebula.Domain.Entities;

/// <summary>
/// Read-model fact row backing F0023 operational reports (daily workload + workflow
/// aging/backlog). One row per in-flight workflow-bearing source object (Submission,
/// Renewal, Policy, Task). Source modules remain authoritative; reports filter these
/// rows by source visibility before any count/aggregation.
/// </summary>
public class OperationalReportProjection : BaseEntity
{
    /// <summary>Submission | Renewal | Policy | Task</summary>
    public required string SourceObjectType { get; set; }
    public Guid SourceObjectId { get; set; }
    public required string TargetUrl { get; set; }

    public string? WorkflowType { get; set; }
    public string? CurrentStatus { get; set; }
    public DateTimeOffset? StatusEnteredAt { get; set; }
    public int? DaysInStatus { get; set; }

    public Guid? OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }

    public DateOnly? DueDate { get; set; }
    public bool IsDueToday { get; set; }
    public bool IsOverdue { get; set; }

    /// <summary>OnTrack | ApproachingSla | Overdue</summary>
    public string? AgeBand { get; set; }

    // Source-visibility correlation keys.
    public Guid? AccountId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? PolicyId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? TerritoryId { get; set; }

    public DateTimeOffset LastSourceUpdatedAt { get; set; }
    public DateTimeOffset ProjectedAt { get; set; }
}
