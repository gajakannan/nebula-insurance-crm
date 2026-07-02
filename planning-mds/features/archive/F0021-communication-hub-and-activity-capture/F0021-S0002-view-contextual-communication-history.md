# F0021-S0002: View contextual communication history

**Story ID:** F0021-S0002
**Feature:** F0021 — Communication Hub & Activity Capture
**Title:** View contextual communication history
**Priority:** High
**Phase:** MVP

## User Story

**As a** distribution user, coordinator, underwriter, or relationship manager
**I want** to see communication history in the context of the record I am working on
**So that** I can understand recent conversations without searching email or asking teammates

## Context & Background

Communication history must be visible from the records where users work: broker, account, submission, and policy. This story is read-only and depends on captured or linked communication events.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated internal user with access to a broker, account, submission, or policy
- **When** they open the Communications panel for that record
- **Then** they see a paginated reverse-chronological list of relevant communication events
- **And** each item shows type, subject, occurred-at, actor/owner, participants summary, primary/related context, and follow-up status when present

**Alternative Flows / Edge Cases:**
- Empty history shows an empty state with an Add Communication action when the user can capture events.
- Unauthorized users cannot read communication events through primary or related record links.
- Redacted communication content shows metadata plus a redacted-content indicator.
- Timeline loading failure does not block the rest of the record page.

## Interaction Contract

N/A — read-only story.

## Data Requirements

**Required Fields:**
- Event id, type, subject, occurred-at, primary entity, created by, redaction state.

**Optional Fields:**
- Body preview, participants summary, related entity labels, follow-up task state.

**Validation Rules:**
- Page size is bounded.
- Sorting is newest first.
- Related-record visibility cannot bypass primary-record authorization.

## Role-Based Visibility

**Roles that can view:**
- Internal roles with access to the relevant primary record.

**Data Visibility:**
- InternalOnly content: body/summary, participants, linked follow-up details.
- Redacted content: hidden body/summary with visible audit metadata.

## Non-Functional Expectations

- Performance: paginated contextual list p95 target is 300 ms for normal page sizes.
- Reliability: communication panel failure is isolated from the host detail page.

## Dependencies

**Depends On:**
- F0021-S0001 — captured communication events.

**Related Stories:**
- F0021-S0003 — related record links.
- F0021-S0005 — redaction state.

## Out of Scope

- Full-text search across all communications.
- Analytics or scoring.

## UI/UX Notes

- Screens involved: Communications panel on broker, account, submission, and policy detail pages.
- Key interactions: paginate, filter by type/date if Phase B confirms filter contract, open event detail.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Communication history is scoped by primary record and approved related links.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only story)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0021-S0002-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
