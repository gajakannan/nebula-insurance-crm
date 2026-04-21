namespace Nebula.Application.DTOs;

/// <summary>
/// Timeline event DTO for BrokerUser audience (F0009 §8.1 / BROKER-VISIBILITY-MATRIX.md).
/// Returns only approved event types with BrokerDescription instead of EventDescription.
/// Excludes InternalOnly fields: eventDescription, actorUserId.
/// </summary>
public record TimelineBrokerUserEventDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string EventType,
    string? BrokerDescription,
    string? ActorDisplayName,
    DateTime OccurredAt);
