# F0025-S0006: Revenue attribution rollups

**Story ID:** F0025-S0006
**Feature:** F0025 — Commission, Producer Splits & Revenue Tracking
**Title:** Revenue attribution rollups
**Priority:** High
**Phase:** Brokerage Platform Expansion

## User Story

**As a** distribution leader
**I want** rollups of expected revenue and attribution by producer, broker, territory, carrier, and policy period
**So that** I can review production economics and unresolved exceptions by management dimension

## Context & Background

After commission records, schedules, splits, and adjustments exist, leaders need aggregated visibility. This story provides management reporting without exporting statements, posting ledgers, or running reconciliation.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an authorized distribution leader
- **When** I open the revenue rollup view for a selected date range
- **Then** I see expected commission totals, adjusted totals, producer allocation totals, and exception counts
- **And** I can group by producer, broker, territory, carrier or market, and policy period
- **And** selecting a rollup row opens the filtered commission workspace records that contribute to the row

**Alternative Flows / Edge Cases:**
- No authorized records in range -> show an empty state with selected filters.
- Rollup includes unresolved exceptions -> show exception count and link to contributing records.
- Unauthorized records -> exclude amounts and counts from rollups.
- Date range exceeds Phase B limit -> show validation feedback and do not run the rollup.

**Checklist:**
- [ ] Rollup rows show grouping label, expected commission total, adjusted total, producer allocation total, record count, and exception count.
- [ ] Filters include date range, producer, broker, territory, carrier or market, status, and exception state.
- [ ] Drilldown preserves the selected rollup filters.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. Rollup filtering and drilldown do not mutate CRM data.

## Data Requirements

**Required Fields:**
- Date range: start and end date.
- Grouping dimension: producer, broker, territory, carrier or market, or policy period.
- Expected commission total: sum of authorized expected commission values.
- Exception count: count of unresolved exceptions in the authorized result set.

**Optional Fields:**
- Adjusted total, producer allocation total, record count, drilldown filter payload.

**Validation Rules:**
- Start date and end date are required.
- End date must be on or after start date.
- Date range limits are defined in Phase B.

## Role-Based Visibility

**Roles that can view rollups:**
- Distribution leader, finance operations user, producer manager, admin — may view rollups within ABAC scope.

**Data Visibility:**
- InternalOnly content: expected totals, adjusted totals, split allocation totals, record counts, and exception counts.
- ExternalVisible content: none in F0025.

## Non-Functional Expectations

- Performance: rollup response target and date-range limits are defined by Phase B.
- Security: unauthorized amounts and counts are excluded before aggregation.
- Reliability: drilldown results match the rollup row filters.

## Dependencies

**Depends On:**
- F0025-S0001 — commission workspace and detail context.
- F0025-S0004 — expected commission calculation review.
- F0025-S0005 — approved adjustments.

**Related Stories:**
- F0025-S0002 — schedule values contribute to expected commission.
- F0025-S0003 — split assignments contribute to producer allocation.

## Business Rules

1. Authorized aggregation: Rollups include only records the current user may view.
2. Exception transparency: Rollups show unresolved exception counts alongside amounts.

## Out of Scope

- Statement export.
- Ledger export.
- Billing reconciliation.
- Producer payout approval.

## UI/UX Notes

- Screens involved: Revenue Rollup View, Commission Workspace.
- Key interactions: filter, group, drill into contributing records.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define rollup performance targets and date-range limits.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only reporting; no CRM mutation occurs)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0025-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
