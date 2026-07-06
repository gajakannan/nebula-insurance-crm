namespace Nebula.Domain.Entities;

public class WorkQueue : BaseEntity
{
    public string Name { get; set; } = default!;
    public string WorkType { get; set; } = default!;
    public string Status { get; set; } = "Active";
    public bool IsFallback { get; set; }
    public string? Description { get; set; }

    public ICollection<WorkQueueMember> Members { get; set; } = new List<WorkQueueMember>();
    public ICollection<AssignmentRule> AssignmentRules { get; set; } = new List<AssignmentRule>();
    public ICollection<CoverageWindow> CoverageWindows { get; set; } = new List<CoverageWindow>();
    public ICollection<QueueWorkItem> QueueWorkItems { get; set; } = new List<QueueWorkItem>();
}
