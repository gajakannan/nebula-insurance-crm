# F0025-S0004: Expected commission calculation review

**Story ID:** F0025-S0004
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Expected commission calculation review
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** finance operations user
**I want** to review expected commission using policy premium, schedule, and split context
**So that** I can validate the revenue attribution basis before management reporting

## Context & Background

This story turns schedule and split context into an expected commission review surface. It does not create payments, invoices, ledger entries, or reconciliation records.

## Acceptance Criteria

**Happy Path:**
- **Given** a commission record has policy premium context, an active commission schedule, and an active split assignment
- **When** I open the calculation review section
- **Then** I see expected gross commission, producer split allocation, source premium, rate basis, calculation status, and calculation timestamp
- **And** I can see which source fields contributed to the result
- **And** records with complete inputs show a review-ready status
- **And** read-only review does not create audit or timeline events in Phase A scope

**Alternative Flows / Edge Cases:**
- Missing schedule -> show missing schedule exception and no final expected amount.
- Missing split assignment -> show missing split exception and no producer allocation.
- Policy premium unavailable -> show missing premium exception and no final expected amount.
- Source policy version changes -> show stale calculation status until recalculation behavior is resolved by Phase B.

**Checklist:**
- [ ] Calculation review separates source inputs from derived values.
- [ ] Exception state names are visible to authorized users.
- [ ] The review section links back to schedule and split panels.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. Calculation review does not mutate CRM data in Phase A scope.

## Data Requirements

**Required Fields:**
- Policy premium source value: premium basis used for review.
- Commission schedule rate and basis: active schedule input.
- Split assignment: active producer allocation input.
- Expected gross commission: derived value or exception state.
- Producer allocation: derived value or exception state.

**Optional Fields:**
- Calculation timestamp, source policy version, stale source indicator, exception reason.

**Validation Rules:**
- Missing source values produce named exception states.
- Derived values must show currency and precision rules defined in Phase B.

## Role-Based Visibility

**Roles that can review calculation:**
- Distribution leader, finance operations user, producer manager, admin — may review expected commission within ABAC scope.

**Data Visibility:**
- InternalOnly content: source premium, schedule, split, expected amount, producer allocation, and exception states.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Security: unauthorized users cannot access expected amount or allocation details.
- Reliability: exception states are deterministic for the same source inputs.
- Auditability: review-only access does not create audit events unless Phase B defines telemetry.

## Dependencies

**Depends On:**
- F0025-S0002 — commission schedule maintenance.
- F0025-S0003 — producer split assignment.
- F0018 — policy premium and version context.

**Related Stories:**
- F0025-S0005 — adjustments address approved corrections.
- F0025-S0006 — rollups consume reviewed expected commission context.

## Business Rules

1. Source separation: Policy facts remain source data; commission outputs are derived review data.
2. Exception visibility: Missing inputs show named exceptions rather than silent zero amounts.

## Out of Scope

- Invoice generation.
- Cash receipt reconciliation.
- Producer payout calculation.
- General ledger posting.

## UI/UX Notes

- Screens involved: Commission Detail, Calculation Review Section.
- Key interactions: view source inputs, inspect exception state, navigate to schedule or split panels.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define the calculation persistence and recalculation model.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only review; no CRM mutation occurs)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
