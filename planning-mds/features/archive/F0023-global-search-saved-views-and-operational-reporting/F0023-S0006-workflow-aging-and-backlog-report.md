# F0023-S0006: Workflow aging and backlog drilldowns

**Story ID:** F0023-S0006
**Feature:** F0023 - Global Search, Saved Views & Operational Reporting
**Title:** Workflow aging and backlog drilldowns
**Priority:** High
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** workflow aging and backlog drilldowns
**So that** I can identify records stuck in a stage and focus follow-up on the source workflow item

## Context & Background

Submission and renewal workflows already produce status and transition history. Managers need aging and backlog reports over that history without turning F0023 into an analytics platform.

## Acceptance Criteria

**Happy Path:**
- **Given** I open the workflow aging report
- **When** I select submission, renewal, or all workflow-backed records
- **Then** I see backlog counts by workflow stage and age bucket
- **And** selecting a count opens a drilldown list of authorized source records
- **And** each drilldown row includes current stage, age in stage, owner, and source route
- **And** no audit/timeline event is created because the report reads existing transition history and does not mutate workflow state

**Alternative Flows / Edge Cases:**
- A record lacks transition history needed for age-in-stage -> place it in an "age unavailable" bucket.
- A workflow has no matching records -> show zero counts and an empty drilldown.
- A user lacks access to a workflow record -> exclude it from counts and drilldowns.
- F0017 hierarchy/territory data is not available -> hide those facets and show a non-blocking unavailable-dimension note.

**Checklist:**
- [ ] Report uses workflow stage and transition history from source workflow features.
- [ ] Age buckets are visible and consistently labeled.
- [ ] Drilldown criteria are deep-linkable and saved-view compatible.
- [ ] F0037-owned hierarchy-aware rollup authorization is not included in this story.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - read-only report story. Workflow transition and assignment mutations remain owned by the source workflow features.

## Data Requirements

**Required Fields:**
- Workflow type, current stage, stage-entered timestamp or transition-derived equivalent, owner, source record ID, source route.

**Optional Fields:**
- Broker, account, line of business, region, territory, producer owner.

**Validation Rules:**
- Age bucket labels and thresholds are defined by Phase B architecture or existing workflow configuration.
- Records without age inputs are not silently included in numeric age buckets.

## Role-Based Visibility

**Roles that can view aging reports:**
- Distribution manager, Program manager, Admin - manager view within authorization scope.
- Underwriter, Relationship manager, Distribution user - own or permitted team slices when authorized.

**Data Visibility:**
- Counts, age buckets, and drilldowns include only authorized records.
- Hidden records cannot be inferred through aggregate count differences exposed to unauthorized users.

## Non-Functional Expectations

- Performance: report summaries remain bounded and use indexed/projection-backed reads as designed in Phase B.
- Security: authorization filtering happens before count and drilldown display.
- Reliability: unavailable age inputs are visible as a separate bucket.

## Dependencies

**Depends On:**
- F0006 - submission workflow status history.
- F0007 - renewal workflow status history.
- F0019 - downstream submission quote/bind workflow context.
- F0017 - optional hierarchy/territory dimensions when available.

**Related Stories:**
- F0023-S0005 - daily workload report.
- F0023-S0007 - permission-safe reporting.

## Out of Scope

- Changing workflow state machines.
- Defining SLA breach policy beyond visible aging buckets.
- Hierarchy-aware distribution rollups owned by F0037.

## UI/UX Notes

- Screens involved: Operational Reports Workspace.
- Key interactions: choose workflow scope, review stage/age buckets, drill into source records.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- Exact age-bucket thresholds are a Phase B architecture/configuration detail; Phase A requires visible buckets and drilldowns, not a specific threshold policy.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - read-only report)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F0023-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
