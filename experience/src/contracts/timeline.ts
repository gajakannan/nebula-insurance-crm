export interface TimelineEventDto {
  id: string;
  entityType: string;
  entityId: string;
  eventType: string;
  eventDescription: string | null;
  entityName: string | null;
  actorDisplayName: string | null;
  occurredAt: string;
}
