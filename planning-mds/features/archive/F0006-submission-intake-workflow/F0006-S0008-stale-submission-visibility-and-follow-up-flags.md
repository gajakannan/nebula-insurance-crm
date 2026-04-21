---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0008: Stale Submission Visibility and Follow-Up Flags

**Story ID:** F0006-S0008
**Feature:** F0006 — Submission Intake Workflow
**Title:** Stale submission visibility and follow-up flags
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager
**I want** submissions that are stuck in early intake states to be visually flagged and surfaced on the dashboard
**So that** I can identify bottlenecks, reassign stalled work, and ensure broker response times are met

## Context & Background

Stale submissions are new business opportunities that are not progressing through intake. A submission becomes stale when it has remained in Received or Triaging state beyond a configurable threshold without advancing. WaitingOnBroker submissions that exceed a longer threshold are also flagged. The stale flag appears as a visual indicator on the pipeline list (F0006-S0001) and as a nudge card on the dashboard. This mirrors the overdue renewal pattern (F0007-S0005) but for the intake workflow.

## Acceptance Criteria

**Happy Path — Stale Detection:**
- **Given** a submission created 3 days ago still in Received state with a stale threshold of 48 hours
- **When** the pipeline list is rendered or the staleness query runs
- **Then** the submission is flagged as stale with `isStale=true`

**Happy Path — Not Stale:**
- **Given** a submission created 1 hour ago in Received state
- **When** the pipeline list is rendered
- **Then** `isStale=false`; no stale indicator shown

**Happy Path — WaitingOnBroker Stale:**
- **Given** a submission that transitioned to WaitingOnBroker 5 days ago with a WaitingOnBroker threshold of 72 hours
- **When** the pipeline list is rendered
- **Then** the submission is flagged as stale

**Happy Path — Dashboard Nudge Card:**
- **Given** 3 stale submissions in the user's ABAC scope
- **When** the dashboard renders
- **Then** a "Stale Submissions" nudge card shows the count (3) with a link to the pipeline list filtered by stale=true

**Happy Path — Stale Resolved:**
- **Given** a previously stale submission in Received state
- **When** a distribution user transitions it to Triaging
- **Then** the stale flag is cleared (submission is no longer stale because the state changed and the threshold clock resets)

**Alternative Flows / Edge Cases:**
- Submission in ReadyForUWReview → never stale (intake is complete)
- Submission in terminal states (Bound, Declined, Withdrawn) → never stale
- No stale submissions in scope → nudge card not shown on dashboard
- Stale threshold configured as seed data (not hardcoded)
- Stale threshold clock is based on the most recent WorkflowTransition `OccurredAt`, not `CreatedAt` (so transitioning resets the clock)

**Checklist:**
- [ ] Staleness evaluated based on time since last workflow transition (not creation time)
- [ ] Default stale thresholds: Received = 48 hours, Triaging = 48 hours, WaitingOnBroker = 72 hours
- [ ] Thresholds stored as configurable seed data (not hardcoded)
- [ ] ReadyForUWReview and downstream/terminal states are never stale
- [ ] `isStale` flag computed on pipeline list API response
- [ ] Stale indicator displayed on pipeline list rows (F0006-S0001)
- [ ] Pipeline list supports filter by stale=true
- [ ] Dashboard nudge card shows count of stale submissions in user's scope
- [ ] Nudge card click navigates to pipeline list filtered by stale=true
- [ ] Stale flag clears when submission transitions to a new state (threshold clock resets)
- [ ] ABAC scope applied to stale counts (users only see stale counts for their submissions)

## Data Requirements

**Required Fields (staleness computation):**
- `currentStatus` (string): Submission state
- `lastTransitionAt` (datetime): OccurredAt of the most recent WorkflowTransition for this submission
- `staleThresholdHours` (integer): Configurable threshold per state

**Stale Threshold Configuration (seed data):**

| State | Default Threshold (hours) | Description |
|-------|--------------------------|-------------|
| Received | 48 | Submission not yet triaged |
| Triaging | 48 | Triage in progress but not advancing |
| WaitingOnBroker | 72 | Broker follow-up not resolved |

**Validation Rules:**
- Staleness is computed at query time (not stored as a field on the submission)
- Threshold configuration is read from reference/seed data

## Role-Based Visibility

**Roles that see stale indicators:**
- Distribution User — Stale flag on own-scope submissions; nudge card for own stale submissions
- Distribution Manager — Stale flag on region-scoped submissions; nudge card for team stale submissions
- Admin — Stale flag on all submissions; nudge card for all stale submissions

**Roles excluded from stale visibility:**
- Underwriter, Relationship Manager, Program Manager — They see submissions but stale flag is primarily an intake operations concern. They can see the stale indicator on the list but do not receive a nudge card.

**Data Visibility:**
- Stale data is internal-only in MVP

## Non-Functional Expectations

- Performance: Stale computation adds < 100ms to pipeline list query; nudge card query completes in < 500ms
- Security: ABAC-scoped stale counts
- Reliability: Stale threshold changes in seed data take effect on next query (no cache invalidation required)

## Dependencies

**Depends On:**
- F0006-S0001 — Pipeline list displays stale indicator
- F0006-S0004 — Transitions produce WorkflowTransition records used for staleness clock
- F0006-S0007 — Timeline records used for `lastTransitionAt`

**Related Stories:**
- F0006-S0003 — Stale indicator on detail view header

## Business Rules

1. **Stale Threshold Clock Uses Last Transition:** Staleness is measured from the most recent WorkflowTransition.OccurredAt for the submission, not from Submission.CreatedAt or UpdatedAt. Field edits (description changes, premium updates) do not reset the stale clock — only workflow state transitions reset it. This ensures that cosmetic edits cannot mask a genuinely stuck submission.
2. **Configurable Thresholds via Seed Data:** Stale thresholds are stored as configurable seed data (WorkflowSlaThresholds per ADR-009, EntityType="submission"), not hardcoded. Default values: Received=48h, Triaging=48h, WaitingOnBroker=72h. Changes take effect on the next query without cache invalidation.
3. **ReadyForUWReview and Terminal States Are Never Stale:** Only early intake states (Received, Triaging, WaitingOnBroker) can trigger the stale flag. Once a submission reaches ReadyForUWReview or any downstream/terminal state, it is no longer subject to stale detection.
4. **Uniform Thresholds Across LOBs in MVP:** All lines of business share the same stale thresholds. Per-LOB threshold customization is Future scope.
5. **ABAC-Scoped Stale Counts:** Dashboard nudge card stale counts are scoped by the same ABAC rules as the pipeline list (policy.csv §2.3). Users only see stale counts for submissions within their authorized scope.
6. **Staleness Is Query-Time Computation:** The `isStale` flag is computed at query time, not stored as a persistent field on the Submission entity. This eliminates the need for background jobs or scheduled recalculation.

## Out of Scope

- Automated escalation actions (auto-reassign, auto-notify) — manual follow-up only in MVP
- Stale submission notifications (email or push — deferred to F0021)
- SLA reporting or metrics dashboard for intake staleness (deferred to F0023)
- Per-LOB stale thresholds (future — all LOBs share the same thresholds in MVP)

## UI/UX Notes

- Screens involved: Submission Pipeline List (stale badge), Dashboard (nudge card)
- Key interactions: Warning icon or badge on stale rows in pipeline list; nudge card on dashboard with count and link
- Nudge card text: "{count} submission(s) need attention — intake stalled" with click to filtered list
- Stale badge color: amber/warning (not red/error — stale is a prompt, not a failure)

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Stale thresholds are the same for all LOBs in MVP; per-LOB thresholds are Future scope
- Staleness clock uses the last WorkflowTransition OccurredAt, not the submission's UpdatedAt (field edits do not reset the stale clock; only status transitions reset it)
- Dashboard nudge card follows the same pattern as existing nudge cards (F0001-S0005, F0007 renewal nudge)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC-scoped stale counts)
- [ ] Audit/timeline logged (not applicable — read-only computation)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0008-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
