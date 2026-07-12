---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0002: Draft reference data and workflow SLA configuration

**Story ID:** F0032-S0002
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Draft reference data and workflow SLA configuration
**Priority:** High
**Phase:** MVP

## User Story

**As a** Configuration Steward
**I want** to draft supported reference-data and workflow SLA threshold changes
**So that** routine operational timing and reference settings can be prepared without changing the current published behavior

## Context & Background

Workflow SLA thresholds already model entity type, status, optional line of business, warning days, and target days. This story makes draft edits governable without changing the published thresholds until validation and publish occur later.

## Acceptance Criteria

**Happy Path:**
- **Given** I have delegated configuration rights for Workflow SLA
- **When** I create or edit a draft threshold for a supported submission or renewal status
- **Then** the draft records entity type, status, optional line of business, warning days, target days, change reason, and draft version
- **And** the current published threshold remains unchanged

**Alternative Flows / Edge Cases:**
- If warning days are greater than or equal to target days for a threshold where that relationship is invalid, the draft remains editable and validation marks the row as blocking before publish.
- If a duplicate default or line-of-business-specific threshold is entered for the same entity type/status/LOB combination, validation reports a blocking conflict.
- If the user lacks delegated rights, edit controls are unavailable and direct mutation attempts are denied.
- If a draft exists, opening the domain resumes that draft instead of creating a second active draft for the same domain.

**Checklist:**
- [ ] Draft changes are saved separately from the published set.
- [ ] Draft rows capture a change reason.
- [ ] Draft rows can be cancelled without changing the published version.
- [ ] The screen explains renewal and submission timing semantics without changing ADR-014 rules.
- [ ] Draft saves create audit history and do not write source-record timeline events.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Admin Configuration Catalog -> Workflow SLA domain | Create Draft / Edit Draft / Save Draft | Enabled for Admin or delegated Configuration Steward; read-only for review-only roles | Draft configuration version is created or updated; published version is unchanged | Reloading the domain shows the same draft version, changed values, actor, and reason | Admin or delegated Configuration Steward only; draft state must not be locked for publish |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [ ] The save path has validation and error behavior specified.
- [ ] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [ ] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Entity type: submission or renewal.
- Status: supported workflow status.
- Line of business: optional value, null means default.
- Warning days: integer threshold.
- Target days: integer threshold.
- Change reason: required text for draft save.

**Optional Fields:**
- Effective label or description.
- Prior published value summary.

**Validation Rules:**
- Entity type/status combinations must be supported by the existing workflow status catalog.
- One default threshold and one line-of-business-specific threshold can exist per entity type/status/LOB in a draft set.
- Invalid numeric relationships are allowed to be saved as draft only when marked as validation-blocking before publish.

## Role-Based Visibility

**Roles that can draft:**
- Admin — all supported first-release domains.
- Configuration Steward — delegated SLA/reference domains.

**Data Visibility:**
- InternalOnly content: all draft and published threshold values.
- ExternalVisible content: none.

## Non-Functional Expectations

- Security: unauthorized direct mutation attempts return a permission-safe denial.
- Reliability: save failure preserves entered draft data on screen when possible and does not alter published values.

## Audit & Timeline Requirements

- Log a configuration draft event with domain, draft version, actor, timestamp, changed-field summary, and change reason.
- Do not write source-record timelines for SLA draft edits because this is admin configuration history, not account/submission/renewal activity.

## Dependencies

**Depends On:**
- ADR-014 Workflow SLA Threshold Per-LOB Extension.
- WorkflowSlaThreshold model.

**Related Stories:**
- F0032-S0004 — validates and compares the draft.
- F0032-S0005 — publishes or rolls back validated sets.
- F0032-S0006 — audits draft updates.

## Business Rules

1. Draft SLA changes have no runtime effect until published.
2. Existing workflow SLA semantics from ADR-014 remain authoritative.
3. Every draft save requires a reason for audit review.

## Out of Scope

- Creating new workflow statuses.
- Changing submission or renewal state machines.
- Changing hardcoded fallback behavior when no threshold exists.

## UI/UX Notes

- Screens involved: Configuration Domain Detail, Configuration Draft Editor.
- Key interactions: create draft, edit thresholds, save draft, cancel draft.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- First-release reference-data drafting is limited to workflow SLA thresholds unless the operator approves additional reference tables.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
