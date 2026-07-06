# F0021-S0001: Capture a structured communication event

**Story ID:** F0021-S0001
**Feature:** F0021 — Communication Hub & Activity Capture
**Title:** Capture a structured communication event
**Priority:** High
**Phase:** MVP

## User Story

**As a** distribution user, coordinator, underwriter, or relationship manager
**I want** to capture a note, call, meeting, or email-linked reference from a CRM record
**So that** important communication becomes part of the operating record

## Context & Background

Nebula already has contextual records and timeline patterns, but user-authored communication history is not captured as its own CRM business record. This story creates the first structured capture path.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user with read access to a broker, account, submission, or policy
- **When** they open that record and select "Add Communication"
- **Then** a capture drawer opens with type, direction, occurred-at, subject, summary/body, primary record, participants, and optional related records
- **And** saving persists the communication event
- **And** reloading the record shows the event in communication history
- **And** a timeline/audit event records the capture

**Alternative Flows / Edge Cases:**
- Missing subject or occurred-at returns inline validation and does not save.
- Missing primary record is not possible from contextual entry points; direct API attempts without a primary record return validation error.
- Email-linked reference stores reference metadata only; no external email is sent, fetched, or synced.
- Unauthorized create attempts return forbidden and do not create timeline events.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Broker/account/submission/policy detail → Communications panel | Click Add Communication, complete form, Save | Enabled for authenticated internal roles with access to the primary record; read-only for unauthorized users | Creates one communication event with primary entity, type, occurred-at, subject, body/summary, participants, optional related records | Communication appears after reload/query invalidation; timeline/audit entry exists | Must have access to primary record; archived/deleted records are read-only unless Phase B explicitly allows capture |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Communication type: note, call, meeting, or email reference.
- Direction: internal, inbound, outbound, or meeting.
- Occurred at: timestamp of communication.
- Subject: short user-facing summary.
- Primary entity type/id: ownership and authorization anchor.

**Optional Fields:**
- Body/summary text.
- Participants.
- Related entity links.
- External reference text for email-linked references.

**Validation Rules:**
- Subject is required and length-limited.
- Occurred-at is required.
- Primary entity type must be one of broker, account, submission, or policy.
- Email-linked reference must not require raw email body ingestion.

## Role-Based Visibility

**Roles that can capture:**
- Internal roles with access to the primary record.

**Data Visibility:**
- InternalOnly content: communication body/summary and participant details.
- ExternalVisible content: none in MVP unless a later feature explicitly exposes it.

## Non-Functional Expectations

- Performance: save completes within normal CRM mutation expectations.
- Security: primary-record authorization is enforced before save.
- Reliability: communication event and timeline/audit event are committed consistently.

## Dependencies

**Depends On:**
- F0002 — broker detail foundation.
- F0016 — account detail foundation.
- F0006 — submission detail foundation.
- F0018 — policy detail foundation.

**Related Stories:**
- F0021-S0002 — history display.
- F0021-S0003 — related records and participants.

## Out of Scope

- Real outbound send.
- External mailbox synchronization.
- AI-generated summaries.

## UI/UX Notes

- Screens involved: contextual detail page Communications panel and Add Communication drawer.
- Key interactions: open drawer, validate, save, reload, view created event.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Email reference capture is metadata/reference-only in MVP.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0021-S0001-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
