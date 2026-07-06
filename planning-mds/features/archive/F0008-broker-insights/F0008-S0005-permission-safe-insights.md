# F0008-S0005: Permission-safe broker insight behavior

**Story ID:** F0008-S0005
**Feature:** F0008 - Broker Insights
**Title:** Permission-safe broker insight behavior
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** CRM user
**I want** broker insights to respect my record permissions
**So that** scorecards, trends, benchmarks, and snapshots never reveal hidden broker data

## Context & Background

Broker insights aggregate across broker, submission, renewal, policy, activity, hierarchy, producer, and territory dimensions. Because aggregated values can leak hidden records, authorization is a product requirement for every F0008 surface.

## Acceptance Criteria

**Happy Path:**
- **Given** I open a scorecard, trend drilldown, benchmark, or review snapshot
- **When** matching source records include both authorized and unauthorized records
- **Then** I see only metrics, counts, trend points, peer values, source rows, and links computed from authorized records
- **And** another user applying the same filters sees results based on that user's authorization

**Alternative Flows / Edge Cases:**
- All matches are unauthorized -> show the same empty state used when no authorized records match.
- A broker, producer, territory, or peer set becomes unauthorized -> hide or disable that filter value without exposing hidden counts.
- A source record becomes unauthorized after a result loads -> opening it returns no-access behavior without extra details.
- A benchmark peer set contains too few visible brokers -> suppress rank and percentile values.

**Checklist:**
- [ ] Authorization is enforced before broker insight data leaves the server.
- [ ] Counts, percentages, ranks, medians, trend points, peer sets, source rows, and snapshots use authorized records only.
- [ ] Saved criteria or filter URLs do not grant elevated access.
- [ ] Security Reviewer is required for feature closeout because broker insights cross visibility boundaries.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only/security behavior story. The story validates authorization behavior across F0008 surfaces.

## Data Requirements

**Required Fields:**
- Current user identity and roles, source object type, source record access attributes, broker filter criteria, selected time window, metric criteria.

**Optional Fields:**
- Producer, territory, program, team scope, line of business, premium amount.

**Validation Rules:**
- Requests without authenticated identity are rejected.
- Criteria referencing unauthorized brokers, producers, territories, or source records return no-access or empty-state behavior without hidden data.
- Aggregate values are computed after authorization filtering.

## Role-Based Visibility

**Roles that can use permission-safe broker insights:**
- Distribution Manager, Relationship Manager, Program Manager, Admin.

**Data Visibility:**
- InternalOnly content never appears to unauthorized users.
- External broker/MGA users are out of scope for F0008 MVP.
- Authorization applies to filters, rows, counts, percentages, trends, peer sets, ranks, snapshots, and source links.

## Non-Functional Expectations

- Performance: permission filtering must scale with the bounded query patterns defined in Phase B.
- Security: no hidden-record existence leakage through counts, peer sets, timing-specific messaging, or disabled filters.
- Reliability: permission changes take effect on the next F0008 query or source-record open.

## Dependencies

**Depends On:**
- F0009 - authenticated identity and role-based login.
- F0017 - broker/MGA hierarchy, producer ownership, and territory dimensions.
- F0023 - permission-safe reporting substrate.
- Existing source-object authorization policies.

**Related Stories:**
- F0008-S0001 through F0008-S0004.

## Business Rules

1. Broker insight filters never grant access. They only constrain data under current permissions.
2. Unauthorized matches are indistinguishable from non-matches in user-facing result states.
3. Aggregate counts, percentages, ranks, and peer statistics are computed after authorization filtering.
4. F0037 remains responsible for new hierarchy-aware access enforcement and distribution rollups.

## Out of Scope

- Introducing hierarchy-aware access-control enforcement; F0037 owns that scope.
- External broker/MGA insight access.
- Replacing source feature authorization policies.
- Saving shared broker insight views.

## UI/UX Notes

- Screens involved: Broker Insights Workspace, Scorecard Panel, Trend Drilldown Drawer, Benchmark Comparison View, Review Snapshot View.
- Key interactions: apply filters, inspect metrics, open drilldowns, open source records after permission changes.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will decide whether F0008 needs a dedicated broker-insight read policy or can compose existing source-object policies.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only/security behavior)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0008-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
