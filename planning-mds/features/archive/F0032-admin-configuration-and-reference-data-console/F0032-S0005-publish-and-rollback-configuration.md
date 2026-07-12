---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0005: Publish and roll back configuration sets

**Story ID:** F0032-S0005
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Publish and roll back configuration sets
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** platform Admin
**I want** to publish validated configuration sets and roll back to eligible prior versions
**So that** operational changes take effect deliberately and can be reversed with traceability

## Context & Background

ADR-016 defines published configuration sets with versioning, validation, downstream refresh expectations, and rollback behavior. This story makes publish and rollback explicit admin actions.

## Acceptance Criteria

**Happy Path:**
- **Given** a draft configuration set has a passing validation result for its current version
- **When** I publish it with a reason
- **Then** the draft becomes the current published version
- **And** the prior published version remains available in history
- **And** the domain shows downstream refresh status as refreshed, pending, failed, or not applicable

**Alternative Flows / Edge Cases:**
- If validation is missing, failed, or stale, publish is disabled.
- If another Admin published a newer version after the draft was validated, publish is blocked with a stale-baseline message.
- If downstream refresh fails, the published version remains visible with a refresh-failed status and audit evidence.
- If I roll back, I must select an eligible prior version, review the rollback summary, enter a reason, and confirm before the rollback publishes.
- If rollback target is ineligible, the action is disabled with the reason.

**Checklist:**
- [ ] Publish requires Admin role.
- [ ] Publish requires reason text.
- [ ] Rollback requires Admin role and reason text.
- [ ] Prior versions remain visible for audit.
- [ ] Failed publish does not silently change the authoritative version.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Configuration Domain Detail -> Publish Confirmation | Publish Validated Draft | Enabled only when latest validation passes for current draft version | Draft becomes current published configuration version; publish event is recorded | Reloading domain shows new published version, publisher, publish time, and refresh state | Admin only; draft must be validated and baseline must not be stale |
| Configuration Domain Detail -> Rollback Confirmation | Roll Back to Prior Version | Enabled only for eligible prior versions | Selected prior version is restored through an explicit rollback publication event | Reloading domain shows rollback version as published and audit history includes rollback reason | Admin only; target version must be rollback-eligible |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [ ] The save path has validation and error behavior specified.
- [ ] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [ ] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Draft version and published version.
- Publish actor, timestamp, and reason.
- Rollback target version and rollback reason when applicable.
- Downstream refresh status.

**Optional Fields:**
- Refresh failure summary.
- Downstream consumer list.

**Validation Rules:**
- Publish requires exact-version passing validation.
- Publish requires current published baseline to match validation baseline.
- Rollback target must be an eligible prior published version for the same domain.

## Role-Based Visibility

**Roles that can publish or roll back:**
- Admin — publish and rollback for all supported domains.
- Authorization checks must reject non-Admin publish and rollback attempts before any runtime configuration state changes.

**Data Visibility:**
- InternalOnly content: publish state, version history, rollback options, refresh state.
- ExternalVisible content: none.

## Non-Functional Expectations

- Reliability: failed publish or rollback must preserve clear authoritative state.
- Security: only Admin can trigger runtime-affecting publish or rollback.
- Auditability: publish and rollback actions are append-only.

## Audit & Timeline Requirements

- Log publish events with actor, timestamp, domain, new published version, prior published version, reason, and downstream refresh state.
- Log rollback events with actor, timestamp, domain, rollback target version, replaced version, and reason.
- Log failed publish and rollback attempts with failure reason and without changing the authoritative version.

## Dependencies

**Depends On:**
- F0032-S0004 — validation and compare.
- ADR-016 Published Operational Configuration Governance.

**Related Stories:**
- F0032-S0006 — audit history for publish and rollback.

## Business Rules

1. Only a validated draft can be published.
2. Rollback is a new governed action, not deletion of history.
3. Downstream refresh state must be visible after publish when applicable.

## Out of Scope

- Automatic rollback without Admin confirmation.
- Bulk publish across unrelated domains.
- Infrastructure deployment rollback.

## UI/UX Notes

- Screens involved: Configuration Domain Detail, Publish Confirmation, Rollback Confirmation.
- Key interactions: publish, review refresh state, select rollback target, confirm rollback.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- First-release rollback eligibility is limited to prior published versions for the same configuration domain.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
