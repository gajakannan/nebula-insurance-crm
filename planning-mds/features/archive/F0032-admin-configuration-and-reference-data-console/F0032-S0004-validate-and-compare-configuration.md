---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0004: Validate and compare configuration before publish

**Story ID:** F0032-S0004
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Validate and compare configuration before publish
**Priority:** High
**Phase:** MVP

## User Story

**As a** platform Admin
**I want** to validate and compare draft configuration against the current published version
**So that** I can catch unsafe changes before they affect live routing, reporting, templates, or SLA behavior

## Context & Background

ADR-016 requires explicit validation before publish. F0032 must make validation and comparison visible to admins instead of letting changes move directly from edit to runtime behavior.

## Acceptance Criteria

**Happy Path:**
- **Given** a draft configuration set exists
- **When** I run validation
- **Then** Nebula shows pass/fail status, validation timestamp, draft version, actor, blocking errors, warnings, and changed-field summary
- **And** I can compare the draft against the current published version before publishing

**Alternative Flows / Edge Cases:**
- If the draft changed after validation, publish remains disabled until validation is rerun.
- If validation fails, publish is disabled and blocking issues are grouped by affected domain and field.
- If comparison cannot load the published baseline, publish is disabled and the user sees a retryable system-error state.
- If a draft has only warnings and no blocking errors, the user can proceed to publish only after reviewing the warning summary.

**Checklist:**
- [ ] Validation results are version-bound to the draft being validated.
- [ ] Compare shows added, changed, removed, and unchanged values.
- [ ] Blocking errors prevent publish.
- [ ] Warnings are visible before publish.
- [ ] Validation attempts create audit history and do not write source-record timeline events.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Configuration Domain Detail -> Validation and Compare Drawer | Validate Draft | Enabled for Admin and delegated Configuration Steward where domain allows validation | Validation result is recorded against the draft version | Reloading the draft shows the latest validation result and timestamp | Draft must exist and not be publish-locked |
| Validation and Compare Drawer | Open Compare | Read-only comparison; no direct mutation | No configuration change; comparison result is generated from draft and published versions | Reopening compare shows same version pair unless draft/published version changed | User must be authorized to view both draft and published configuration |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [ ] The save path has validation and error behavior specified.
- [ ] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [ ] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Draft version.
- Published baseline version.
- Validation result: pass or fail.
- Blocking errors and warnings.
- Changed-field summary.
- Validation actor and timestamp.

**Optional Fields:**
- Downstream impact summary.
- Domain-specific warning categories.

**Validation Rules:**
- Validation result is invalidated when draft content changes.
- Publish requires the latest validation result for the exact draft version.
- Blocking errors must include field or domain identifiers.

## Role-Based Visibility

**Roles that can validate:**
- Admin — all supported domains.
- Configuration Steward — delegated domains.

**Data Visibility:**
- InternalOnly content: validation results, compare output, warning/error details.
- ExternalVisible content: none.

## Non-Functional Expectations

- Reliability: validation failure must not modify the published version.
- Security: compare output must not leak configuration details across authorization boundaries.

## Audit & Timeline Requirements

- Log each validation attempt with actor, timestamp, draft version, result, blocking error count, warning count, and domain.
- Log compare access only when the comparison is generated for a mutable draft; read-only compare views do not write source-record timelines.

## Dependencies

**Depends On:**
- F0032-S0002 or F0032-S0003 draft configuration.
- ADR-016 Published Operational Configuration Governance.

**Related Stories:**
- F0032-S0005 — publish requires a passing validation result.
- F0032-S0006 — audit includes validation attempts and failures.

## Business Rules

1. Publish is blocked unless validation passes for the exact draft version.
2. Validation and compare results are review evidence, not runtime configuration.
3. Blocking errors must be actionable enough for an Admin or Steward to correct the draft.

## Out of Scope

- Auto-fixing configuration.
- Publishing changes.
- Defining new domain validation vocabularies outside first-release domains.

## UI/UX Notes

- Screens involved: Configuration Domain Detail, Validation and Compare Drawer.
- Key interactions: validate draft, review errors, compare versions.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- Domain-specific validation messages can be represented as blocking errors and warnings without inventing a free-form scripting language.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
