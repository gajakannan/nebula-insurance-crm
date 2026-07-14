# F0025-S0002: Commission schedule maintenance

**Story ID:** F0025-S0002
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Commission schedule maintenance
**Priority:** Critical
**Phase:** Brokerage Platform Expansion

## User Story

**As a** finance operations user
**I want** to maintain effective-dated commission schedule values for a policy and carrier or market context
**So that** expected commission can be reviewed from a controlled source basis

## Context & Background

Commission schedules define the rate basis used for expected commission. This story keeps schedule setup bounded to operational visibility and avoids payment or accounting settlement.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authorized finance operations user on a commission detail record
- **When** I enter a commission rate, basis, effective period, source note, and save
- **Then** the schedule is available on the commission detail record
- **And** the record shows who saved the schedule and when
- **And** the prior schedule remains available in history when a new effective period is added

**Alternative Flows / Edge Cases:**
- Missing rate, basis, or effective start date -> show validation feedback and do not save.
- Effective period overlaps an existing schedule for the same policy and carrier or market -> show conflict feedback and do not save.
- User lacks schedule maintenance permission -> show read-only schedule values and no save controls.
- Source policy is cancelled or archived -> schedule is read-only unless Phase B defines an exception path.

**Checklist:**
- [ ] Schedule values are effective-dated.
- [ ] Saved schedule values show source note and audit summary.
- [ ] Users can distinguish current, future, and expired schedule rows.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Commission Detail -> Schedule Maintenance Panel | Add or edit schedule row, then save | Enabled only for finance operations user or admin with schedule maintenance permission | Effective-dated schedule row is saved and prior rows remain historical | Reopen detail or reload page and see schedule row plus audit summary | Internal users only; policy status constraints confirmed in Phase B |

Required checks for mutation stories:
- [x] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [x] The save path has validation and error behavior specified.
- [x] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [x] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Commission rate: percentage or amount basis as defined in Phase B.
- Rate basis: policy premium, fee amount, or other approved basis from Phase B.
- Effective start date: first date the schedule applies.
- Source note: human-readable reason or source for the rate.

**Optional Fields:**
- Effective end date, carrier or market reference, line of business, schedule label.

**Validation Rules:**
- Commission rate is required and must be non-negative.
- Effective start date is required.
- Overlapping active periods for the same schedule scope are rejected.
- Source note is required for create and edit actions.

## Role-Based Visibility

**Roles that can maintain schedules:**
- Finance operations user and admin — may create or edit schedule values within ABAC scope.

**Data Visibility:**
- InternalOnly content: all schedule values, source notes, and audit details.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Security: schedule maintenance is denied for unauthorized users.
- Reliability: failed saves preserve entered values so users can correct validation errors.
- Auditability: create and edit actions record actor, timestamp, changed fields, and reason.

## Dependencies

**Depends On:**
- F0025-S0001 — users open a commission detail record before maintaining schedules.
- F0018 — policy context.
- F0028 — carrier and market context.

**Related Stories:**
- F0025-S0004 — expected commission calculation uses schedule values.
- F0025-S0005 — adjustments can address exceptions after schedule review.

## Business Rules

1. Schedule history: New effective periods do not overwrite historical schedule context.
2. Source note: Every schedule create or edit action needs a source note for later review.

## Out of Scope

- Carrier billing feed ingestion.
- General ledger configuration.
- Producer payout statements.

## UI/UX Notes

- Screens involved: Commission Detail, Schedule Maintenance Panel.
- Key interactions: add schedule row, edit pending schedule row, save, cancel, view history.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define the allowed first-release rate basis values.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
