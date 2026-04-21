---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0010: Account Activity Timeline and Audit Trail

**Story ID:** F0016-S0010
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account-level activity timeline and audit trail (append-only)
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, distribution manager, underwriter, or relationship manager
**I want** a paginated, append-only timeline of everything that has happened on the account
**So that** I can explain past changes (profile edits, relationship changes, lifecycle transitions, merges) without hunting across modules

## Context & Background

Timeline reuses the existing `ActivityTimelineEvent` schema (ADR-011 / already shared with F0006, F0007). F0016 adds a filter by `subjectType=Account` and `subjectId=<accountId>` and emits the documented event types.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `account:read` in scope
- **When** they load the Activity rail in Account 360
- **Then** they receive a paginated list of `ActivityTimelineEvent` rows filtered to this account, sorted by `occurredAt` descending

- **Given** events produced by other F0016 stories (create, update, relationship change, lifecycle transition, merge source + merge destination, contact events)
- **When** any of those events fire
- **Then** a single timeline row is appended with `subjectType=Account`, `subjectId=<accountId>`, `eventType`, `actorUserId`, `summary`, `payload` (JSON), `occurredAt`

**Alternative Flows / Edge Cases:**
- Merged account: timeline rail in Account 360 renders the survivor's timeline (with tombstone-forward); the source's timeline rows remain queryable via its tombstone view
- Deleted account: timeline rail remains queryable via the tombstone view (read-only)
- Attempted direct write / delete on a timeline row → 405 (append-only)

**Checklist:**
- [ ] `GET /api/accounts/{id}/timeline?page=&pageSize=` paginated
- [ ] Event types emitted by F0016 stories:
  - `account.created`, `account.profile_updated`, `account.broker_of_record_changed`, `account.primary_producer_changed`, `account.territory_changed`, `account.deactivated`, `account.reactivated`, `account.deleted`, `account.merged_into`, `account.merged_from`, `account.contact_added`, `account.contact_updated`, `account.contact_removed`, `account.primary_contact_set`
- [ ] Append-only; no mutation / delete endpoints
- [ ] ABAC scoping identical to `account:read`
- [ ] Rail isolation: timeline failure must not prevent the rest of Account 360 from rendering

## Data Requirements

- Reuses `ActivityTimelineEvent` schema (ADR-011 / shared)
- Event `payload` schemas documented per event type (e.g., `account.profile_updated.payload = { changedFields: [...], previousValues: {...}, newValues: {...} }`)

**Validation Rules:**
- Each producer story must emit exactly one timeline event per user-visible change
- No retroactive editing of timeline rows

## Role-Based Visibility

- Read: any role with `account:read` in scope for this account

## Non-Functional Expectations

- Performance: timeline page p95 ≤ 300 ms (paginated; 50 events per page)
- Reliability: never loses an event; writes always co-commit with the mutation that emitted them

## Dependencies

**Depends On:**
- F0016-S0002, F0016-S0003, F0016-S0005, F0016-S0006, F0016-S0007, F0016-S0008 (emitters)

**Related Stories:**
- F0016-S0004 (Account 360 rail host)

## Out of Scope

- Rich filter / search within the timeline (follow-up)
- CSV export of timeline (follow-up)

## UI/UX Notes

- Timeline rail groups events by day
- Event rows show actor avatar, event type chip, summary, timestamp, and an expandable payload JSON for advanced users

## Questions & Assumptions

**Assumptions:**
- Timeline reuses the same table as F0006 / F0007 timeline with `subjectType` discriminator — no new table

## Definition of Done

- [ ] Acceptance criteria met
- [ ] All F0016 emitter stories produce correct events
- [ ] Permissions enforced (`account:read`)
- [ ] Audit/timeline logged: This story IS the timeline
- [ ] Tests pass
- [ ] Documentation updated (event type catalog)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
