# F0008-S0001: Broker scorecard overview

**Story ID:** F0008-S0001
**Feature:** F0008 - Broker Insights
**Title:** Broker scorecard overview
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution Manager
**I want** a broker scorecard with quote, bind, retention, pipeline, activity, and production metrics
**So that** I can prioritize broker relationship reviews without building a spreadsheet

## Context & Background

Distribution leaders need a single scorecard over existing broker, submission, renewal, policy, and activity data. The first slice is read-only and focuses on trusted, explainable metrics with denominator counts and refresh context.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to view Broker Insights
- **When** I open the Broker Insights workspace for a broker and selected time window
- **Then** I see quote count, bind count, quote-to-bind rate, renewal retention indicator, open pipeline count, activity count, and production summary where source data exists
- **And** each metric shows denominator count, selected period, comparison period, and last refreshed timestamp
- **And** metrics with available detail expose a drilldown entry point handled by F0008-S0002

**Alternative Flows / Edge Cases:**
- No source records match the selected period -> show zero-value cards with a clear empty state.
- A metric source is unavailable -> show a partial-data state for that metric and keep the remaining cards visible.
- A broker is no longer visible to the current user -> show the standard no-access state without metric values.
- A denominator is zero -> show "N/A" for percentage metrics and preserve the raw count.

**Checklist:**
- [ ] Scorecard covers quote, bind, quote-to-bind, retention, pipeline, activity, and production summary metrics.
- [ ] Every card displays denominator and refresh provenance.
- [ ] Time-window filters include 30 days, 90 days, quarter-to-date, and year-to-date.
- [ ] Scorecard is read-only and does not mutate broker, submission, renewal, policy, or activity records.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only scorecard story. The story does not create, edit, save, assign, transition, or approve records.

## Data Requirements

**Required Fields:**
- Broker ID, broker name, selected time window, metric label, metric value, denominator count, comparison period value, last refreshed timestamp, source availability state.

**Optional Fields:**
- Producer, territory, program, line of business, account segment, premium amount where available from existing source records.

**Validation Rules:**
- Percentage metrics require a non-zero denominator.
- Time windows use inclusive calendar ranges in the user's locale.
- Metric values must be traceable to source object types.

## Role-Based Visibility

**Roles that can view broker scorecards:**
- Distribution Manager, Relationship Manager, Program Manager, Admin.

**Data Visibility:**
- Scorecard metrics include only records authorized for the current user.
- InternalOnly content remains hidden from unauthorized users.
- External broker/MGA users are out of scope for this MVP.

## Non-Functional Expectations

- Performance: initial scorecard summary should load within the Phase B target for bounded report queries.
- Security: counts cannot reveal hidden records.
- Reliability: partial-source failures do not block unrelated scorecard metrics.

## Dependencies

**Depends On:**
- F0006 Submission Intake Workflow.
- F0007 Renewal Pipeline.
- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management.
- F0019 Submission Quoting, Proposal & Approval Workflow.
- F0023 Global Search, Saved Views & Operational Reporting.

**Related Stories:**
- F0008-S0002 - trend drilldown and source record navigation.
- F0008-S0005 - permission-safe broker insight behavior.

## Out of Scope

- Predictive broker scoring.
- Carrier appetite recommendations.
- Commission or producer split calculations.
- Hierarchy-aware access enforcement and distribution rollups owned by F0037.

## UI/UX Notes

- Screens involved: Broker Insights Workspace, Broker Scorecard Panel.
- Key interactions: select broker, select time window, scan metric cards, open drilldown.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Phase B will define the exact read model or projection shape for scorecard metrics.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only scorecard)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0008-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
