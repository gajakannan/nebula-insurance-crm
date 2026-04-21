---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0011: Account Summary Projection

**Story ID:** F0016-S0011
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account summary projection (policy / submission / renewal counts, last activity)
**Priority:** Medium
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution user, or distribution manager
**I want** summary counts (active policies, open submissions, renewal-due) and last-activity date on each account
**So that** Account List and Account 360 overview can prioritize the accounts that need attention without me clicking into each one

## Context & Background

This story provides the metrics that power the overview section of Account 360 (F0016-S0004) and the optional metrics columns on Account List (F0016-S0001). MVP uses query-time composition for correctness and operational simplicity; a materialized projection is tracked as a Future follow-up if the query becomes a hotspot.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `account:read` in scope
- **When** they call `GET /api/accounts/{id}/summary`
- **Then** the response includes `activePolicyCount`, `openSubmissionCount`, `renewalDueCount`, `lastActivityAt`, and the overview profile fields

- **Given** a list request with `include=summary`
- **When** the server responds
- **Then** each list item includes the same summary counts

**Alternative Flows / Edge Cases:**
- Policies module is the Policy stub today (F0007-seeded) → counts reflect the stub rows
- No linked records → all counts zero, `lastActivityAt` nullable
- Merged / Deleted account → summary returns zeros + the tombstone identifiers; the metrics fields are not computed

**Count Definitions:**
- `activePolicyCount` = count of policies where `accountId=<id>` AND policy status is non-terminal (today: not `Cancelled`, not `Expired`; final semantics follow F0018)
- `openSubmissionCount` = count of submissions where `accountId=<id>` AND status is non-terminal
- `renewalDueCount` = count of renewals where `accountId=<id>` AND status ∈ {`Identified`, `Outreach`, `InReview`, `Quoted`} AND policy expiration within 90 days
- `lastActivityAt` = max(`ActivityTimelineEvent.occurredAt` where `subjectType=Account` AND `subjectId=<id>`)

**Checklist:**
- [ ] `GET /api/accounts/{id}/summary` returns the documented payload
- [ ] List query with `include=summary` returns counts per row efficiently (single aggregated query, not N+1)
- [ ] Counts and last-activity values enforce ABAC (a user who cannot see a submission / renewal must not have it counted into the summary they see)
- [ ] Merged / Deleted accounts return zeros + tombstone fields

## Data Requirements

**Summary payload:**
- `activePolicyCount` (int), `openSubmissionCount` (int), `renewalDueCount` (int), `lastActivityAt` (timestamp, nullable)
- Echoes overview profile fields (`id`, `displayName`, `status`, etc.) for convenience

**Validation Rules:**
- Count semantics documented above; implementation must match and be covered by integration tests with mixed statuses

## Role-Based Visibility

- Any role with `account:read` in scope for the account

## Non-Functional Expectations

- Performance: summary GET p95 ≤ 500 ms; list-with-summary p95 ≤ 400 ms additional overhead
- Correctness: counts must enforce ABAC

## Dependencies

**Depends On:**
- F0006 submissions, F0007 renewals, F0007-seeded Policy stub, F0016-S0010 timeline

**Related Stories:**
- F0016-S0001 (list consumer), F0016-S0004 (360 consumer)

## Out of Scope

- Materialized projection table (Future follow-up if performance becomes a hotspot)
- Account-scoped KPI dashboards (deferred to F0023)

## UI/UX Notes

- Overview KPI row on Account 360 showing the four values
- Optional columns on Account List (shown when non-null)

## Questions & Assumptions

**Assumptions:**
- Query-time composition is performant enough for MVP at ≤ 10 000 accounts
- `renewalDueCount` uses the 90-day window as a simple default, matching F0007's most-inclusive window

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Counts correct against mixed-status fixtures
- [ ] ABAC enforced inside the aggregation query
- [ ] Tests pass (count semantics + ABAC + merged/deleted fallback)
- [ ] Documentation updated (OpenAPI)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
