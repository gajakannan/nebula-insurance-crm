namespace Nebula.Domain.Entities;

public class WorkflowTransition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkflowType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string? FromState { get; set; }
    public string ToState { get; set; } = default!;
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
}
