# Downstream Submission Pipeline List and Workflow Visibility

## Story Header

**Story ID:** F0019-S0007
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Downstream submission pipeline list and workflow visibility
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or underwriter
**I want** to see submissions across the downstream workflow with approval and aging visibility
**So that** I can find work that needs attention and see where approvals are stuck

## Context & Background

F0006 shipped the submission pipeline list with intake-status filtering. This story extends that
list (read-only) for the downstream workflow: filter by `InReview`/`Quoted`/`BindRequested`/`Bound`/
`Declined`/`Withdrawn`, surface approval-pending and stuck-in-state (aging beyond SLA) signals so
approval bottlenecks become visible (a feature success criterion), and make archived submissions
discoverable via an explicit toggle. This is a read/visibility slice; the mutations it surfaces are
owned by S0001–S0006.

## Acceptance Criteria

**Happy Path:**
- **Given** submissions across downstream states
- **When** a user opens the submission list
- **Then** each row shows ref, insured, downstream status pill, owner, age (days in current state), and an approval chip (`Pending`/`Approved`/`—`), and the list defaults to **active** (non-archived) submissions.

**Filtering & visibility:**
- Filter by one or more downstream statuses.
- "Approval pending" filter shows submissions awaiting an approval decision.
- "Stuck > SLA" filter/flag highlights submissions whose time-in-state exceeds the configured threshold.
- "Include archived" toggle adds archived submissions, clearly flagged.

**Alternative Flows / Edge Cases:**
- Empty result for a filter combination → explicit empty state, not an error.
- Default page size 25; max page size 100; last page with < full page renders the remaining rows without error.
- BrokerUser / unauthorized access to the internal list → `403 Forbidden` (no internal rows leak).
- A submission the user is not authorized to see is excluded (ABAC scope), not shown as a partial row.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story. Filters and the "Include archived" toggle change the **query/view** only and
do not mutate submissions. All state changes are owned by S0001–S0006.

## Data Requirements

**Displayed Fields (list item):**
- `submissionRef`, `insuredName`, `downstreamStatus`, `ownerUserId`, `ageDaysInState`, `approvalStatus`, `isArchived`, `stuckFlag`

**Query Inputs:**
- `statusFilter[]`, `approvalPending` (bool), `stuckOnly` (bool), `includeArchived` (bool), `page`, `pageSize`

**Validation Rules:**
- `stuckFlag` derived from time-in-state vs configured SLA threshold (a duration comparison — not a pricing/rating computation).
- Default excludes archived; archived rows visibly flagged when included.

## Role-Based Visibility

**Roles that can view the downstream list:**
- Underwriter, Distribution user, Admin — permitted, scoped by ABAC (assignment/ownership where applicable)
- BrokerUser — no access to the internal pipeline list

**Data Visibility:**
- InternalOnly content: owner, approval status, aging/stuck flags.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: list query < 1s p95 at expected MVP volume; pagination server-side.
- Security: ABAC scoping enforced on every row; unauthorized list access → `403`.
- Reliability: degrades gracefully if an upstream signal (e.g., SLA config) is unavailable — list still renders without the stuck flag.

## Dependencies

**Depends On:**
- F0019-S0001 — downstream states exist to filter on.
- F0019-S0003 — approval status to surface.
- F0019-S0006 — archived flag for the "Include archived" toggle.
- F0006 — base submission list this story extends.

**Related Stories:**
- F0019-S0008 — timeline (per-submission audit detail) complements list-level visibility.

## Business Rules

1. **Active-by-default:** archived submissions are excluded unless explicitly included.
2. **Bottleneck visibility:** approval-pending and stuck-in-state signals are first-class so bottlenecks are visible (success criterion).
3. **Scope safety:** authorization scoping filters rows; unauthorized submissions never appear.

## Out of Scope

- Saved views / cross-object global search (F0023).
- Operational reporting dashboards (F0023).
- Editing or transitioning submissions from the list (owned by S0001–S0006).

## UI/UX Notes

- Screens involved: Submission Pipeline List (downstream filters, approval chip, aging/stuck flag, include-archived toggle).
- Key interactions: multi-select status filter, toggles, status pills, stuck indicator. Desktop + narrow layouts in the PRD `Screen Layouts (ASCII)`.

## Questions & Assumptions

**Open Questions:**
- [ ] Source of the SLA/stuck threshold per downstream state — reuse the F0007 SLA-threshold config pattern, or define F0019-specific thresholds? (Default assumption: reuse the existing workflow SLA-threshold config pattern.)

**Assumptions (to be validated):**
- The F0006 submission list component can be extended for downstream states without a separate screen.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (empty state, pagination bounds, `403` scope)
- [ ] Permissions enforced (ABAC row scoping)
- [ ] Audit/timeline logged — N/A (read-only list; no mutations)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
