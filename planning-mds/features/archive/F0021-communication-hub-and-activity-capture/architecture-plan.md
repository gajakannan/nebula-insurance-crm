# F0021 Phase B Architecture Plan

Plan run: `2026-07-01-c1726908`
Owner: Architect
Status: Drafted for G4/G5 approval

## Decision Summary

F0021 introduces `CommunicationEvent` as the durable source record for structured notes, calls, meetings, and email-linked references. `ActivityTimelineEvent` remains the append-only feed and audit projection used by dashboards and record timelines. A communication save emits timeline projections for the primary linked record and allowed related records, but the timeline projection is not the editable source of communication content.

The MVP uses manual capture only. It does not send email, ingest external messages, create marketing automation, or run connector/background synchronization.

## Service Boundaries

- Communication capture lives in the CRM API/application layer beside task and timeline services.
- Timeline services read projected `ActivityTimelineEvent` records and do not own communication mutation behavior.
- Task services remain the owner of task persistence. F0021 follow-up creation calls the existing task creation behavior with a communication back-reference.
- Authorization combines `communication_event:*` decisions with the existing linked entity read rules for broker, account, submission, policy, renewal, and task context.

## Data Model Deltas

`CommunicationEvent`
- Source record for a note, call, meeting, or email reference.
- Required fields: `id`, `type`, `summary`, `occurredAt`, `createdByUserId`, `createdAt`, `visibility`, and at least one linked entity.
- Optional fields: `body`, `direction`, `emailReference`, `participants`, `links`, `followUpTaskIds`, `redactedAt`, `redactedByUserId`, `redactionReason`, `rowVersion`.

`CommunicationLink`
- Associates a communication with one or more business records.
- Allowed entity types for MVP: `Broker`, `Account`, `Submission`, `Policy`, `Renewal`, `Task`.
- Exactly one primary link is required.

`CommunicationParticipant`
- Captures display name, optional email, optional role, participant type, and optional linked contact/broker/user reference.
- Participants are structured metadata, not external delivery recipients.

`CommunicationCorrection`
- Append-only audit entry for correction or redaction reason.
- Original communication remains auditable. Redaction masks sensitive body/summary content in normal reads while retaining non-sensitive metadata.

## Workflow Rules

Create communication:
- Entry point: contextual capture panel on approved broker, account, submission, policy, renewal, and task surfaces.
- Validation: reject missing type, summary, occurredAt, primary link, or inaccessible linked records with problem details.
- Persistence evidence: create source record, participant rows, link rows, and timeline projection rows in one transaction.
- Reload behavior: after save/reload, the communication appears in contextual communication history and the associated timeline.

List communication history:
- Uses communication source records, filtered by linked entity access.
- Timeline surfaces can show communication projections via `ActivityTimelineEvent`.
- BrokerUser and ExternalUser are denied for MVP.

Link related records:
- Links can be supplied on create and corrected through audit-preserving correction flow after create.
- Linked entities must pass the caller's read scope and be from the approved entity type set.

Create follow-up task:
- Requires `communication_event:read`, linked entity read access, and `task:create`.
- Creates a normal task with the selected linked entity and stores the originating communication reference.
- Emits task-created timeline evidence and communication-linked follow-up evidence.

Correct or redact:
- Corrections are append-only and preserve prior values in audit evidence.
- Redaction masks sensitive content from standard read/timeline surfaces and emits a redaction timeline/audit event.
- Redaction requires Security Reviewer signoff during feature action and is limited to Admin in MVP policy.

## API And Schema Deltas

Canonical endpoint and schema deltas are captured in `api-schema-deltas.md` and the new `communication-event*.schema.json` files. The OpenAPI file should be updated during the feature action from those approved deltas.

## Authorization

Canonical authorization decisions are captured in `authorization-deltas.md`. F0021 adds `communication_event:create/read/correct/redact/link/create_follow_up` policy semantics and reuses linked entity read checks. BrokerUser and ExternalUser are denied for MVP communication capture/history.

## Frontend Surfaces

- Contextual communication capture on broker, account, submission, policy, renewal, and task record pages approved by the feature action assembly plan.
- Contextual communication history list with read-only timeline projection.
- Follow-up task creation affordance from a saved communication.
- Correction/redaction entry points gated by role and record state.

## Required Signoff Roles

- Architect: required.
- Quality Engineer: required for story acceptance, reload/persistence, timeline, and follow-up evidence.
- Code Reviewer: required for source/timeline/task integration.
- Security Reviewer: required because notes, email references, visibility, and redaction are sensitive scope.
- DevOps: not required unless implementation adds a new runtime service, connector, or background processor.
- AI Engineer: not required; F0021 has no AI scope.

## Feature Action Handoff

The separate feature action must begin with G0 and create `feature-assembly-plan.md`. This Phase B plan is not an implementation authorization by itself.
