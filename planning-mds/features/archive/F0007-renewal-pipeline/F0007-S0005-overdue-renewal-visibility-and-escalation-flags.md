---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0007-S0005: Overdue Renewal Visibility and Escalation Flags

**Story ID:** F0007-S0005
**Feature:** F0007 — Renewal Pipeline
**Title:** Overdue renewal visibility and escalation flags
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager
**I want** overdue renewals to be visually flagged across the pipeline and surfaced on the dashboard
**So that** I can quickly identify renewals that are at risk of lapsing and take corrective action

## Context & Background

A renewal is overdue when the current date is past the LOB-specific target outreach date and the renewal has not transitioned beyond Identified. Overdue renewals represent the highest retention risk — they are expiring policies where no outreach has been initiated. Managers need these flagged prominently in both the pipeline list and the dashboard so they can reassign, escalate, or intervene before the policy lapses.

## Acceptance Criteria

**Happy Path — Pipeline Overdue Flag:**
- **Given** a renewal in Identified state where `current_date > (PolicyExpirationDate - LOB outreach target days)`
- **When** the pipeline list renders
- **Then** the renewal row displays a red "Overdue" badge next to the status

**Happy Path — Approaching Flag:**
- **Given** a renewal in Identified state where `current_date > (PolicyExpirationDate - LOB outreach target days - warning buffer)` but not yet overdue
- **When** the pipeline list renders
- **Then** the renewal row displays an amber "Approaching" badge next to the status

**Happy Path — Dashboard Nudge Card:**
- **Given** overdue or approaching renewals exist for the authenticated user's scope
- **When** the user views the dashboard
- **Then** a renewal nudge card appears showing: count of overdue renewals, count of approaching renewals, and a link to the pipeline list filtered to overdue

**Happy Path — No Overdue:**
- **Given** all renewals in the user's scope are on track (past Identified or within timing)
- **When** the pipeline list renders
- **Then** no overdue or approaching badges are shown; dashboard renewal nudge card is suppressed

**Alternative Flows / Edge Cases:**
- Renewal with null LOB → Uses default timing thresholds for overdue calculation
- Renewal already past Identified (in Outreach or later) → Never flagged as overdue regardless of date (overdue only applies to Identified state)
- LOB threshold seed data missing → Falls back to default (90-day outreach target)
- Multiple LOBs have different thresholds → Each renewal's overdue is computed from its own LOB threshold

**Checklist:**
- [ ] Overdue computation: `current_date > (PolicyExpirationDate - outreachTargetDays)` AND `CurrentStatus = Identified`
- [ ] Approaching computation: `current_date > (PolicyExpirationDate - outreachTargetDays - warningDays)` AND not overdue AND `CurrentStatus = Identified`
- [ ] Red "Overdue" badge on pipeline list rows for overdue renewals
- [ ] Amber "Approaching" badge on pipeline list rows for approaching renewals
- [ ] Pipeline list "Overdue" filter returns only overdue renewals
- [ ] Dashboard renewal nudge card shows overdue count + approaching count
- [ ] Nudge card links to pipeline list filtered to overdue
- [ ] Nudge card is suppressed when no overdue/approaching renewals exist
- [ ] Overdue/approaching computations use LOB-specific thresholds from WorkflowSlaThreshold
- [ ] Default thresholds used when LOB is null or threshold entry is missing

## Data Requirements

**Required Fields:**
- Renewal: PolicyExpirationDate, CurrentStatus, LineOfBusiness
- WorkflowSlaThreshold: EntityType="renewal", Status="Identified", WarningDays, TargetDays (per LOB or default)

**Validation Rules:**
- Overdue flag is computed server-side and returned as a field on the renewal list API response
- Dashboard nudge card aggregation is a server-side count query

## Role-Based Visibility

**Roles that see overdue flags:**
- Distribution User — On own assigned renewals
- Distribution Manager — On all team renewals (primary consumer for escalation)
- Underwriter — On renewals in InReview/Quoted (not typically overdue, but visible if data is shown)
- Admin — On all renewals

**Data Visibility:**
- Overdue/approaching indicators are internal-only metadata derived from renewal state

## Non-Functional Expectations

- Performance: Overdue flag computation must not degrade pipeline list performance (< 2s for 500 renewals); consider pre-computed flag or indexed query
- Security: Overdue counts on dashboard respect ABAC scope (manager sees team totals, user sees own)
- Reliability: Thresholds default gracefully when seed data is incomplete

## Dependencies

**Depends On:**
- F0007-S0001 — Pipeline list displays the overdue/approaching badges
- F0007-S0006 — Renewals must exist with PolicyExpirationDate
- ADR-009 — WorkflowSlaThreshold pattern and seed data

**Related Stories:**
- F0007-S0004 — Manager reassigns overdue renewals after identifying them
- F0001-S0005 — Dashboard nudge card pattern (existing implementation to extend)

## Out of Scope

- Email or push notification for overdue renewals (future Temporal workflow)
- Automated escalation (auto-reassign overdue renewals to manager)
- Overdue renewal reporting/analytics (deferred to F0023)
- Configurable overdue thresholds via admin UI (thresholds are seed data in MVP; admin UI deferred to F0032)

## UI/UX Notes

- Screens involved: Renewal Pipeline List, Dashboard
- Pipeline list: Overdue/approaching badge appears inline on each renewal row, always visible regardless of filter
- Dashboard: Renewal nudge card follows existing nudge card pattern (F0001-S0005) — dismissible, action-oriented, with count and navigation link
- Color coding: Red for overdue, amber for approaching, consistent with existing SLA gauge patterns

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- Overdue applies only to Identified state (once outreach has started, the renewal is no longer "overdue" even if the date has passed)
- Dashboard nudge card uses the same nudge framework as existing nudge cards (F0001-S0005)
- Approaching window = outreach target - warning buffer (e.g., if outreach target is 90 days and warning is 60 days, approaching = 60-90 day window)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC scoping on overdue counts and flags)
- [ ] Audit/timeline logged: No (computed view, not a mutation)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0007-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
