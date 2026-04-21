namespace Nebula.Domain.Entities;

public class ActivityTimelineEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string EventType { get; set; } = default!;
    public string? EventPayloadJson { get; set; }
    public string EventDescription { get; set; } = default!;
    /// <summary>
    /// Broker-safe public description for BrokerUser-visible event types (F0009-S0004 §8.1).
    /// Populated at event creation time by the domain service using predefined templates.
    /// NULL for InternalOnly event types — those events are excluded from BrokerUser responses entirely.
    /// </summary>
    public string? BrokerDescription { get; set; }
    public Guid ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public DateTime OccurredAt { get; set; }
}
