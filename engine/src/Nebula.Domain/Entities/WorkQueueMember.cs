namespace Nebula.Domain.Entities;

public class WorkQueueMember : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public Guid UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; } = default!;
    public string Role { get; set; } = "Member";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
