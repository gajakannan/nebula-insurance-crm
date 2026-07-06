# F0008-S0003: Authorized benchmark comparison

**Story ID:** F0008-S0003
**Feature:** F0008 - Broker Insights
**Title:** Authorized benchmark comparison
**Priority:** High
**Phase:** MVP

## User Story

**As a** Program Manager
**I want** to compare a broker against an authorized peer set
**So that** I can understand performance outliers without seeing restricted broker data

## Context & Background

Benchmarking is useful only if it is both explainable and permission-safe. This story compares brokers using visible peer sets derived from current filters and authorization rather than creating a new hierarchy rollup model.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to view Broker Insights
- **When** I select a broker, time window, and peer set
- **Then** I see the selected broker's metric values beside authorized peer median, rank, and variance where enough peer data exists
- **And** the peer set definition is visible
- **And** benchmark rows can open the same drilldown behavior defined in F0008-S0002

**Alternative Flows / Edge Cases:**
- Fewer than the minimum visible peer count exists -> suppress rank/percentile and show an insufficient-peer-data state.
- A filter removes all visible peers -> show an empty benchmark state.
- Peer records become unauthorized -> recompute the peer set without hidden counts.
- A broker is outside the selected peer set -> show a no-comparison state without changing the selected broker.

**Checklist:**
- [ ] Benchmarks support quote-to-bind, retention, activity intensity, pipeline volume, and production summary metrics.
- [ ] Peer set filters include visible broker/MGA group, producer, territory, program, and time window where available.
- [ ] Benchmark counts, ranks, and percentiles use authorized records only.
- [ ] The view does not create distribution rollups owned by F0037.
- [ ] Audit/timeline logging is N/A because the benchmark view is read-only.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only benchmark story. The story does not save benchmark definitions or mutate source records.

## Data Requirements

**Required Fields:**
- Selected broker, selected time window, peer set criteria, visible peer count, metric value, median, rank, variance, denominator.

**Optional Fields:**
- Producer, territory, program, line of business, premium amount, broker group.

**Validation Rules:**
- Benchmark percentile/rank must be suppressed when the visible peer count is below the Phase B threshold.
- Peer membership is derived from authorized records and selected criteria.

## Role-Based Visibility

**Roles that can view benchmarks:**
- Distribution Manager, Program Manager, Admin.
- Relationship Manager may view benchmarks only where authorized by existing source permissions.

**Data Visibility:**
- Hidden brokers do not contribute to peer counts, ranks, medians, or variance.
- Peer set labels must not disclose hidden broker names or counts.

## Non-Functional Expectations

- Performance: benchmark queries use bounded peer sets and asynchronous loading states.
- Security: aggregate comparison values cannot leak inaccessible broker performance.
- Reliability: insufficient data states are explicit and do not imply system failure.

## Dependencies

**Depends On:**
- F0008-S0001 - scorecard metric definitions.
- F0008-S0002 - drilldown navigation.
- F0017 - broker/MGA hierarchy, producer, and territory dimensions.
- F0023 - reporting substrate and permission-safe aggregation pattern.

**Related Stories:**
- F0008-S0005 - permission-safe broker insight behavior.

## Out of Scope

- Custom report builder.
- Predictive scoring.
- Hierarchy-aware rollup enforcement owned by F0037.
- Saving shared benchmark templates.

## UI/UX Notes

- Screens involved: Benchmark Comparison View, Trend Drilldown Drawer.
- Key interactions: choose peer set, compare broker, open metric drilldown.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will set the minimum visible peer count for showing rank and percentile.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only benchmark)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0008-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
