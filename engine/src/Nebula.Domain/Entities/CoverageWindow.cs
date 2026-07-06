namespace Nebula.Domain.Entities;

public class CoverageWindow : BaseEntity
{
    public Guid CoveredUserId { get; set; }
    public UserProfile CoveredUser { get; set; } = default!;
    public Guid BackupUserId { get; set; }
    public UserProfile BackupUser { get; set; } = default!;
    public Guid? WorkQueueId { get; set; }
    public WorkQueue? WorkQueue { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Reason { get; set; }
}
