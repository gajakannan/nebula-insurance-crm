namespace Nebula.Domain.Entities;

public class QueueWorkItem : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public string SourceType { get; set; } = default!;
    public Guid SourceId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public UserProfile? AssignedToUser { get; set; }
    public string QueueStatus { get; set; } = "Open";
    public DateTime RoutedAt { get; set; }
    public string? RuleVersion { get; set; }
    public string? MatchReason { get; set; }
    public string IdempotencyKey { get; set; } = default!;
}
