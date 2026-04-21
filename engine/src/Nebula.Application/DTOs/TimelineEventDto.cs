namespace Nebula.Application.DTOs;

public record TimelineEventDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string EventType,
    string? EventDescription,
    string? EntityName,
    string? ActorDisplayName,
    DateTime OccurredAt);
