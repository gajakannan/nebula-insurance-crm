# F0021 API And Schema Deltas

Plan run: `2026-07-01-c1726908`
Owner: Architect
Status: Drafted for G4/G5 approval

## Endpoints

### `POST /communications`

Creates a structured communication source record.

- Request schema: `CommunicationEventCreateRequest`
- Response schema: `CommunicationEvent`
- Authorization: `communication_event:create` and read access to every linked entity.
- Validation failures: missing required fields, unsupported type/entity, inaccessible linked entity, invalid participant, invalid email reference.
- Persistence evidence: communication record, links, participants, audit fields, and timeline projections are committed atomically.

### `GET /communications`

Lists communication source records for a contextual entity.

- Query: `entityType`, `entityId`, `page`, `pageSize`
- Response: paginated `CommunicationEvent` summaries.
- Authorization: `communication_event:read` and read access to the requested entity.
- Sort: `occurredAt` descending, then `createdAt` descending.

### `GET /communications/{communicationId}`

Returns a single communication source record.

- Authorization: `communication_event:read` and read access to at least one linked entity.
- Redacted records return metadata plus masked `summary`/`body` fields unless the caller has explicit redaction audit authority.

### `POST /communications/{communicationId}/follow-up-task`

Creates a task linked to the communication and selected business record.

- Request schema: `CommunicationEventFollowUpRequest`
- Response schema: `Task`
- Authorization: `communication_event:read`, `communication_event:create_follow_up`, linked entity read access, and `task:create`.
- Persistence evidence: created task, task timeline event, and communication follow-up reference.

### `POST /communications/{communicationId}/corrections`

Records an append-only correction or redaction.

- Request schema: `CommunicationEventCorrectionRequest`
- Response schema: `CommunicationEvent`
- Authorization: `communication_event:correct` for corrections and `communication_event:redact` for redactions.
- Persistence evidence: correction audit entry, updated source projection state, and timeline/audit event.

## Timeline Projection

F0021 extends timeline payload definitions with:

- `CommunicationCaptured`
- `CommunicationCorrected`
- `CommunicationRedacted`
- `CommunicationFollowUpTaskCreated`

The timeline payload references the source `communicationId`, type, summary display text, primary linked entity, and follow-up task id when relevant. Sensitive body content is never embedded in `ActivityTimelineEvent`.

## Schema Files

- `planning-mds/schemas/communication-event.schema.json`
- `planning-mds/schemas/communication-event-create-request.schema.json`
- `planning-mds/schemas/communication-event-correction-request.schema.json`
- `planning-mds/schemas/communication-event-follow-up-request.schema.json`
