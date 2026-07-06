namespace Nebula.Domain.Entities;

public class RoutingDecisionLog : BaseEntity
{
    public Guid? QueueWorkItemId { get; set; }
    public QueueWorkItem? QueueWorkItem { get; set; }
    public string SourceType { get; set; } = default!;
    public Guid SourceId { get; set; }
    public string Outcome { get; set; } = default!;
    public string ReasonCode { get; set; } = default!;
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string DecisionPayloadJson { get; set; } = "{}";
}
