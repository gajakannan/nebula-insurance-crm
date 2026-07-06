# F0008-S0002: Trend drilldown and source record navigation

**Story ID:** F0008-S0002
**Feature:** F0008 - Broker Insights
**Title:** Trend drilldown and source record navigation
**Priority:** High
**Phase:** MVP

## User Story

**As a** Broker Relationship Manager
**I want** to drill from a broker metric into its trend and source records
**So that** I can understand what changed before contacting the broker

## Context & Background

Relationship managers need the evidence behind a scorecard number. This story turns summary metrics into explainable trend views and source-record navigation while keeping the source systems authoritative.

## Acceptance Criteria

**Happy Path:**
- **Given** I am viewing a broker scorecard metric
- **When** I open the metric drilldown
- **Then** I see a period-over-period trend for the selected metric
- **And** I see the authorized source records that contributed to the selected point or period
- **And** selecting a source row opens the authoritative broker, submission, renewal, policy, or activity screen
- **And** the drilldown preserves the broker, time window, metric, and filter context

**Alternative Flows / Edge Cases:**
- No source records contribute to the selected metric -> show an empty source list with the metric definition still visible.
- A source record becomes unauthorized after the trend loads -> remove it from the list or return forbidden on open without exposing details.
- A source record was archived or inactive -> open the source screen's existing archived/inactive state.
- Trend data is partially unavailable -> show the available periods and a partial-data message.

**Checklist:**
- [ ] Trend drilldown supports quote, bind, retention, pipeline, activity, and production metric families.
- [ ] Source rows include object type, source ID, display label, date, status, and route target.
- [ ] Drilldowns reuse existing source record permission behavior.
- [ ] No source data is edited from the drilldown.
- [ ] Audit/timeline logging is N/A because the drilldown is read-only.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only drilldown story. The story only navigates to existing source screens and does not mutate source records.

## Data Requirements

**Required Fields:**
- Metric ID, broker ID, period bucket, trend value, denominator, source object type, source record ID, status, source route.

**Optional Fields:**
- Account, program, producer, territory, premium amount, line of business.

**Validation Rules:**
- Drilldown rows must reconcile to the displayed metric denominator after authorization filtering.
- Source navigation must use stable deep-link parameters for existing source screens.

## Role-Based Visibility

**Roles that can use drilldowns:**
- Distribution Manager, Relationship Manager, Program Manager, Admin.

**Data Visibility:**
- Drilldown rows include only authorized records.
- Unauthorized source records must not appear as hidden counts or placeholder rows.

## Non-Functional Expectations

- Performance: trend and source rows should use paginated or bounded result sets.
- Security: source-record access is checked when the drilldown opens and again when a source row is opened.
- Reliability: source navigation failures show user-safe errors and preserve the drilldown state.

## Dependencies

**Depends On:**
- F0008-S0001 - broker scorecard overview.
- F0023 - reporting/search source navigation behavior.
- Existing source screens for broker, submission, renewal, policy, and activity records.

**Related Stories:**
- F0008-S0005 - permission-safe broker insight behavior.

## Out of Scope

- Editing records from the drilldown.
- Building new source record screens.
- Exporting drilldown rows.

## UI/UX Notes

- Screens involved: Broker Scorecard Panel, Trend Drilldown Drawer, existing source detail screens.
- Key interactions: open drilldown, select trend point, page source list, open source record.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Source screens already expose stable routes for broker, submission, renewal, policy, and activity context.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only drilldown)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0008-S0002-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
