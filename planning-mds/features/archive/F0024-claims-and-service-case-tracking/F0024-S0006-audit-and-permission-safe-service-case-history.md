# F0024-S0006: Audit and permission-safe service case history

**Story ID:** F0024-S0006
**Feature:** F0024 — Claims & Service Case Tracking
**Title:** Audit and permission-safe service case history
**Priority:** High
**Phase:** MVP+

## User Story

**As a** manager, underwriter, or authorized service user
**I want** service case history to be auditable and permission-safe
**So that** sensitive claim-adjacent activity is visible only to authorized users and every change is defensible

## Context & Background

F0024 creates sensitive servicing records linked to accounts and policies. The first release must make history useful without leaking service or claim-adjacent context through lists, rails, or related records.

## Acceptance Criteria

**Happy Path:**
- **Given** an authorized internal user opens a service case
- **When** they view the timeline/history section
- **Then** they see creation, field updates, ownership changes, claim-reference updates, communication links, follow-up creation, and status transitions
- **And** each history item includes actor, timestamp, event type, and safe summary
- **And** authorization-filtered users see only cases and history they are allowed to access

**Alternative Flows / Edge Cases:**
- Unauthorized users do not see hidden counts, snippets, case IDs, or filter options for inaccessible cases.
- Redacted or corrected communication events linked from F0021 follow F0021 display semantics.
- Deleted/archived account or policy context follows existing fallback contracts.
- History is append-only; Phase B must define any correction mechanism without hard-deleting audit evidence.

## Interaction Contract

N/A — read-only story.

## Data Requirements

**Required Fields:**
- Event type, actor, timestamp, service case ID, safe summary.

**Optional Fields:**
- Previous value summary, new value summary, linked communication event, linked task.

**Validation Rules:**
- History reads must be authorization-filtered through the same account/policy scope as the case.
- Read models must not expose inaccessible cases through counts or snippets.

## Role-Based Visibility

**Roles that can view history:**
- Internal roles with service-case read access.

**Data Visibility:**
- InternalOnly content: service case history and claim-adjacent summaries.
- ExternalVisible content: none in MVP+.

## Non-Functional Expectations

- Performance: bounded history loads with predictable pagination or limits.
- Security: all history and related snippets are authorization-filtered.
- Reliability: history remains available after service case closure.

## Dependencies

**Depends On:**
- F0024-S0001 — service case exists.
- F0021 — communication correction/redaction behavior.

**Related Stories:**
- F0024-S0003 — work-management updates.
- F0024-S0004 — status transitions.
- F0024-S0005 — claim-reference updates.

## Out of Scope

- External/broker-visible service history.
- Legal hold workflows.
- Audit export/reporting beyond case detail history.

## UI/UX Notes

- Screens involved: Service Case Detail timeline/history section, service rails with safe summaries.
- Key interactions: view history, open linked communication or follow-up task when authorized.

## Questions & Assumptions

**Open Questions:**
- [ ] None blocking for Phase A approval.

**Assumptions (to be validated):**
- Existing timeline patterns can carry service-case audit entries after Phase B defines exact event payloads.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0024-S0006-...`)
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
