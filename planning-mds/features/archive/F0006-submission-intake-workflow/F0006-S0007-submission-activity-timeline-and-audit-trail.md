---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0007: Submission Activity Timeline and Audit Trail

**Story ID:** F0006-S0007
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission activity timeline and audit trail
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, or distribution manager
**I want** to see a complete, chronological timeline of all activity on a submission — creation, transitions, assignments, and follow-up notes
**So that** I can understand the full intake history, explain delays, and maintain audit compliance

## Context & Background

The activity timeline is the audit backbone of the submission intake workflow. Every mutation — creation, status transition, assignment change — produces an immutable ActivityTimelineEvent. The timeline is displayed on the submission detail view and is the primary tool for understanding what has happened to a submission and why. This follows the established append-only pattern used across Nebula (ADR-011, existing broker and renewal timelines).

## Acceptance Criteria

**Happy Path — View Timeline:**
- **Given** a submission with multiple timeline events (created, transitioned, assigned)
- **When** the user views the submission detail timeline section
- **Then** events are displayed in reverse chronological order (most recent first) with: event type, description, actor display name, and timestamp

**Event Types Produced by F0006:**
- `SubmissionCreated` — Submission created; includes key fields (account, broker, LOB)
- `SubmissionTransitioned` — Status changed; includes fromState, toState, and optional reason
- `SubmissionAssigned` — Ownership changed; includes old assignee and new assignee
- `SubmissionUpdated` — Submission fields edited; includes changed fields summary

**Pagination:**
- **Given** a submission with more than 20 timeline events
- **When** the user scrolls to the bottom of the timeline
- **Then** a "Load more" action fetches the next page of events

**Alternative Flows / Edge Cases:**
- No events (should not happen for F0006-created records because creation always produces one; legacy or malformed seeded data may still render empty) → empty timeline with message
- Actor user was soft-deleted → show "[Deleted User]" with userId as fallback
- Large event payload (long reason text) → truncate in list view; full text on expand/click
- Caller lacks route/action-level `submission:read` permission for timeline access → HTTP 403
- Submission is not found or hidden by entity-scope visibility rules → HTTP 404
- Timeline events from downstream features (F0019 transitions, F0020 document uploads) appear in the same timeline once those features are active

**Checklist:**
- [ ] All F0006 mutations produce an ActivityTimelineEvent: creation, transition, assignment, field update
- [ ] All F0006 transitions produce a WorkflowTransition record in addition to the timeline event
- [ ] Timeline events are immutable (append-only, no update or delete)
- [ ] Timeline events include: Id, EntityType=Submission, EntityId, EventType, EventPayloadJson, ActorUserId, OccurredAt
- [ ] WorkflowTransition records include: Id, WorkflowType=Submission, EntityId, FromState, ToState, Reason, ActorUserId, OccurredAt
- [ ] Both records created atomically with the mutation (single DB transaction)
- [ ] Timeline displayed on submission detail view in reverse chronological order
- [ ] Pagination: default 20 events per page, load-more pattern
- [ ] Actor display name resolved from UserProfile; graceful fallback for deleted users
- [ ] Event descriptions are pre-rendered and human-readable (e.g., "Lisa Wong transitioned from Triaging to ReadyForUWReview")

## Data Requirements

**Required Fields (ActivityTimelineEvent):**
- `id` (uuid): Event identifier
- `entityType` (string): "Submission"
- `entityId` (uuid): Submission ID
- `eventType` (string): One of SubmissionCreated, SubmissionTransitioned, SubmissionAssigned, SubmissionUpdated
- `eventPayloadJson` (json): Structured payload with event-specific data
- `eventDescription` (string): Pre-rendered human-readable description
- `actorUserId` (uuid): User who performed the action
- `occurredAt` (datetime): Timestamp of the event

**Required Fields (WorkflowTransition — for transitions only):**
- `id` (uuid): Transition identifier
- `workflowType` (string): "Submission"
- `entityId` (uuid): Submission ID
- `fromState` (string): Previous state (null for creation)
- `toState` (string): New state
- `reason` (string, nullable): Transition reason or comment
- `actorUserId` (uuid): User who performed the transition
- `occurredAt` (datetime): Timestamp

**Validation Rules:**
- Timeline events and workflow transitions are immutable — no update or delete operations
- EventPayloadJson must be valid JSON
- ActorUserId must reference the authenticated user performing the action

## Role-Based Visibility

**Roles that can view timeline:**
- All roles that can read the submission (timeline inherits submission read scope)

**Data Visibility:**
- All timeline data is internal-only in MVP
- No filtering of timeline events by role (all events visible to all readers)

## Non-Functional Expectations

- Performance: Timeline query returns first page (20 events) in < 300ms
- Security: Timeline read access inherits submission ABAC scope
- Reliability: Timeline event creation is atomic with the triggering mutation; if the mutation succeeds, the timeline event must exist

## Dependencies

**Depends On:**
- F0006-S0002 — SubmissionCreated events
- F0006-S0003 — SubmissionUpdated events
- F0006-S0004 — SubmissionTransitioned events
- F0006-S0006 — SubmissionAssigned events

**Related Stories:**
- F0006-S0003 — Timeline section on detail view

## Business Rules

1. **Append-Only Immutability (ADR-011):** ActivityTimelineEvent and WorkflowTransition records are immutable. No update or delete operations exist on these tables. This is a non-negotiable audit requirement — the timeline is the compliance backbone for submission intake operations.
2. **Transition Atomicity (ADR-011):** Timeline event and workflow transition records are created in the same DB transaction as the triggering mutation. If the mutation succeeds, the audit records must exist. If either audit append fails, the entire mutation rolls back.
3. **Pre-Rendered Event Descriptions:** Event descriptions are computed and stored at write time, not read time. This ensures immutability (the description captures the exact state at the moment of the event) and avoids runtime computation cost on timeline reads.
4. **Actor Identity from Authenticated Principal:** The `ActorUserId` on every timeline event and workflow transition is set from the authenticated user's resolved internal UserId (per ADR-006 principal key pattern), not from a client-supplied value. This prevents actor spoofing.
5. **Timeline Read Authorization:** Timeline access inherits submission read semantics. Route/action-level deny of `submission:read` returns HTTP 403. If the caller has general read capability but the submission is outside resolved visibility scope, the API may cloak that outcome as HTTP 404.
6. **Cross-Feature Timeline Extensibility:** The timeline uses EntityType/EntityId indexing. Events from downstream features (F0019 transitions, F0020 document uploads) will appear in the same submission timeline once those features are active, using the same append-only pattern.

## Out of Scope

- Timeline event editing or deletion (timeline is append-only)
- Timeline event filtering by type (future enhancement)
- Timeline events from non-F0006 features (those features produce their own events using the same pattern)
- Timeline export or reporting

## UI/UX Notes

- Screens involved: Submission Detail — Activity Timeline section
- Key interactions: Scrollable timeline feed; each event shows icon (by type), actor name, description, and relative timestamp; expand for full payload details
- Event type icons: create (plus), transition (arrow), assignment (person), update (pencil)
- Timestamps shown as relative ("2 hours ago") with full datetime on hover

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Event descriptions are pre-rendered at write time (not computed at read time) for performance and immutability
- Timeline pagination uses cursor-based pagination on OccurredAt for stable ordering
- SubmissionUpdated events include a summary of changed fields (not a full diff)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (inherits submission:read scope)
- [ ] Audit/timeline logged (this story IS the timeline implementation)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0007-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
