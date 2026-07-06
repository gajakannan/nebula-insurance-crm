namespace Nebula.Domain.Entities;

public class AssignmentRule : BaseEntity
{
    public Guid WorkQueueId { get; set; }
    public WorkQueue WorkQueue { get; set; } = default!;
    public string RuleType { get; set; } = default!;
    public int Precedence { get; set; }
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Draft";
    public string ConditionsJson { get; set; } = "{}";
    public string OutcomeJson { get; set; } = "{}";
    public DateTime? ActivatedAt { get; set; }
    public Guid? ActivatedByUserId { get; set; }
}
