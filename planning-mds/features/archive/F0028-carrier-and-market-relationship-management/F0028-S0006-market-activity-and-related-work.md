# F0028-S0006: Market activity and related work visibility

**Story ID:** F0028-S0006
**Feature:** F0028 — Carrier & Market Relationship Management
**Title:** Market activity and related work visibility
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** underwriter
**I want** market activity and related submission/policy links visible from the market workspace
**So that** I can understand relationship history and current placement work in context

## Context & Background

F0028 needs a market-side activity rail that records changes from this feature and shows related submissions and policies through stable links. This story avoids duplicating submission or policy workflow data.

## Acceptance Criteria

**Happy Path:**
- **Given** a market profile with contacts, appetite notes, appointments, or related placement work
- **When** I open its workspace
- **Then** I see recent market activity events with actor, timestamp, event type, and summary
- **And** I see linked submissions and policies where a stable carrier/market reference exists
- **And** selecting a related work item opens its authoritative source screen

**Alternative Flows / Edge Cases:**
- No activity -> show empty state explaining that future changes will appear here.
- No related work -> show empty related work state.
- Related submission or policy unauthorized for viewer -> omit row and count.
- Source record deleted/archived -> show only if the user is authorized and source screen supports the state.

**Checklist:**
- [ ] Activity includes profile, contact, appetite, and appointment mutations from F0028 as audit/timeline events.
- [ ] Related work links do not duplicate submission or policy workflow state.
- [ ] Authorization filtering occurs before related work rows and counts render.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. This story displays activity and related work produced by other mutations.

## Data Requirements

**Required Fields:**
- Activity event type.
- Actor.
- Timestamp.
- Summary.
- Source record reference.
- Related work object type and route when present.

**Optional Fields:**
- Before/after summary, source contact, source appetite note, source appointment record.

**Validation Rules:**
- Activity rows require a source record reference.
- Related work rows require an authorized source route.

## Role-Based Visibility

**Roles that can view activity and related work:**
- Distribution leader, distribution manager, relationship manager, underwriter, program manager, admin within ABAC scope.

**Data Visibility:**
- InternalOnly content: market activity, related work rows, and summaries.
- ExternalVisible content: none in F0028.

## Non-Functional Expectations

- Security: unauthorized related work rows and counts are not returned.
- Reliability: source links open authoritative screens rather than copied detail views.

## Dependencies

**Depends On:**
- F0028-S0002 — profile activity source.
- F0028-S0003 — contact activity source.
- F0028-S0004 — appetite activity source.
- F0028-S0005 — appointment activity source.
- F0019 and F0018 — source submissions and policies.

**Related Stories:**
- F0028-S0001 — directory preview may show recent activity or related work summary.

## Business Rules

1. **No duplicated workflow state:** related work shows links and summaries only; submissions and policies remain authoritative in their own screens.
2. **Permission-safe counts:** omitted unauthorized rows do not produce hidden-record hints.

## Out of Scope

- New submission or policy workflow states.
- Market recommendation scoring.
- External sharing of market activity.

## UI/UX Notes

- Screens involved: Market Relationship Workspace activity tab and related work panel.
- Key interactions: review activity, open source submission/policy, filter activity type.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Related work links can initially use existing carrier/market reference fields from submission quote packets and policy carrier references where available.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only view of existing events)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0028-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
