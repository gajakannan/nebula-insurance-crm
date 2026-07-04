# F0008-S0004: Broker review snapshot

**Story ID:** F0008-S0004
**Feature:** F0008 - Broker Insights
**Title:** Broker review snapshot
**Priority:** Medium
**Phase:** MVP

## User Story

**As a** Broker Relationship Manager
**I want** a review-ready broker insight snapshot
**So that** I can prepare quarterly broker conversations from trusted CRM data

## Context & Background

Broker reviews need a concise summary that blends scorecard metrics, trend movement, recent activity, and source links. The MVP snapshot is read-only and does not replace document generation or outbound communication features.

## Acceptance Criteria

**Happy Path:**
- **Given** I am viewing a broker in Broker Insights
- **When** I open the review snapshot
- **Then** I see selected period, broker identity, key metric highlights, notable trend changes, recent activity summary, open opportunity summary, and source links
- **And** the snapshot shows last refreshed timestamp and data-source availability
- **And** the snapshot can be viewed without creating a saved document or outbound message

**Alternative Flows / Edge Cases:**
- No meaningful changes exist in the selected period -> show a neutral summary with available metrics.
- Activity source data is unavailable -> show a partial-data message and keep metric highlights visible.
- Source links are unauthorized -> omit them without showing hidden counts.
- Snapshot is opened on narrow screens -> sections stack in the order scorecard, trends, activity, source links.

**Checklist:**
- [ ] Snapshot includes scorecard highlights, trend movement, activity summary, open opportunity summary, and source links.
- [ ] Snapshot is read-only and does not create files, emails, or timeline events.
- [ ] Data provenance is visible.
- [ ] Snapshot respects the same filters used by the scorecard.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only snapshot story. The story does not save, export, email, or generate external documents.

## Data Requirements

**Required Fields:**
- Broker ID, broker name, selected period, metric highlights, trend deltas, recent activity summary, source route links, last refreshed timestamp.

**Optional Fields:**
- Producer, territory, program, renewal due context, premium summary, peer benchmark summary.

**Validation Rules:**
- Snapshot values must match the active scorecard filters.
- Source links must resolve only to authorized records.

## Role-Based Visibility

**Roles that can view snapshots:**
- Distribution Manager, Relationship Manager, Program Manager, Admin.

**Data Visibility:**
- Snapshot sections include only authorized metrics and source links.
- External broker/MGA users are out of scope.

## Non-Functional Expectations

- Performance: snapshot should reuse loaded scorecard data where possible.
- Security: snapshot content cannot expose hidden records through highlights, links, or counts.
- Accessibility: snapshot sections are navigable by keyboard and readable in narrow layouts.

## Dependencies

**Depends On:**
- F0008-S0001 - scorecard overview.
- F0008-S0002 - source record navigation.
- F0008-S0003 - benchmark comparison where benchmark highlights are included.

**Related Stories:**
- F0008-S0005 - permission-safe broker insight behavior.

## Out of Scope

- PDF export.
- Email send or draft creation.
- COI, ACORD, proposal, or document generation.
- Scheduled report distribution.

## UI/UX Notes

- Screens involved: Review Snapshot View.
- Key interactions: open snapshot, scan highlights, open source links, return to scorecard.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Document generation and outbound communication remain owned by F0027 and F0021 respectively.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only snapshot)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0008-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
