---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0004: Account 360 Composed Workspace

**Story ID:** F0016-S0004
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account 360 composed workspace (submissions, policies, renewals, contacts, activity)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution user, distribution manager, or relationship manager
**I want** a single composed workspace that shows submissions, policies, renewals, contacts, activity, and summary metrics for an insured
**So that** I can make underwriting and servicing decisions from one place without hopping between modules

## Context & Background

Account 360 is the primary business value of F0016. It composes records owned by other modules (submissions, renewals, policies) with records owned by F0016 (contacts, activity). Each rail loads independently to keep initial paint fast and to allow empty-state or partial-availability handling when a downstream module is degraded.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `account:read` in scope
- **When** they open Account 360 for an Active or Inactive account
- **Then** the overview section renders ≤ 500 ms with: display name, status, broker of record, primary producer, territory, region, active policy count, open submission count, renewal-due count, last activity date

- **Given** the overview has rendered
- **When** the user scrolls or selects a rail (Submissions, Policies, Renewals, Contacts, Activity)
- **Then** that rail calls its own paginated endpoint and renders independently of other rails

**Alternative Flows / Edge Cases:**
- Downstream list endpoint is slow or unavailable → rail shows a "Temporarily unavailable" state; other rails continue to render
- Merged account → 360 page redirects to the survivor account 360 on first render (tombstone-forward); user sees a toast "Viewing Acme Industrial Co (merged from <source>)"
- Deleted account → 360 page renders a tombstone view (stable name + `[Deleted]` badge + removedAt + reasonCode); no rails are loaded
- Empty rail (no records) → rail shows an empty state with a create-action when applicable (e.g., "Start a submission")
- F0020 Documents not yet live → Documents rail renders a placeholder

**Checklist:**
- [ ] Overview endpoint `GET /api/accounts/{id}/summary` returns overview metrics in one call
- [ ] Submissions rail: `GET /api/accounts/{id}/submissions?page=&pageSize=` (25/page)
- [ ] Policies rail: `GET /api/accounts/{id}/policies?page=&pageSize=`
- [ ] Renewals rail: `GET /api/accounts/{id}/renewals?page=&pageSize=`
- [ ] Contacts rail: `GET /api/accounts/{id}/contacts?page=&pageSize=`
- [ ] Activity rail: `GET /api/accounts/{id}/timeline?page=&pageSize=` (owned by F0016-S0010)
- [ ] Documents rail: placeholder or delegated call when F0020 is live
- [ ] Each rail is independently paginable and independently cache-safe
- [ ] Merged account: detect via `status=Merged`, redirect to `/accounts/{survivorAccountId}`
- [ ] Deleted account: 410 handling renders tombstone view
- [ ] Quick actions on overview: "Start Submission" (opens submission create with account pre-linked), "Start Renewal" (when active policies exist), "Add Contact"

## Data Requirements

**Summary payload fields:**
- `id`, `displayName`, `status`, `brokerOfRecordName`, `primaryProducerName`, `territoryCode`, `region`, `activePolicyCount`, `openSubmissionCount`, `renewalDueCount`, `lastActivityAt`, `rowVersion`

**Rail list-item fields:**
- Submissions: id, status, LOB, createdAt, assignedUser, expectedEffectiveDate
- Policies: id, policyNumber, status, LOB, effectiveDate, expirationDate, carrierName (stub today; extended in F0018)
- Renewals: id, status, LOB, policyExpirationDate, assignedUser, urgencyBadge
- Contacts: id, fullName, role, email, phone, isPrimary
- Activity: id, eventType, summary, actorName, occurredAt

**Validation Rules:**
- All rail queries enforce ABAC (the rail sees only what the user can see even within an account they have overview access to)

## Role-Based Visibility

- Overview: any role with `account:read` in scope
- Rails: scoped per rail (e.g., submissions rail hides submissions the user cannot see even on this account)

## Non-Functional Expectations

- Performance: overview p95 ≤ 500 ms; each rail page p95 ≤ 400 ms
- Reliability: rail isolation — one rail failure must not take down the page
- No N+1: all rails implemented as single paginated queries

## Dependencies

**Depends On:**
- F0016-S0003 (detail page host)
- F0006 submissions, F0007 renewals, F0007-landed Policy stub, F0016-S0010 timeline, F0016-S0011 summary projection

**Related Stories:**
- F0016-S0009 (fallback for merged / deleted)

## Out of Scope

- Inline editing of submissions / policies / renewals from the rails (users click through)
- Dashboard-grade visualizations inside Account 360 (out of MVP)
- Account-scoped reporting (deferred to F0023)

## UI/UX Notes

- Tabs or rails layout — design decision deferred to architecture + frontend
- Each rail has its own pagination and loading state
- Quick-action buttons pinned to the overview header

## Questions & Assumptions

**Assumptions:**
- Summary counts are query-time for MVP (materialized projection is a Future follow-up)
- Timeline rail reuses the existing ActivityTimelineEvent schema with `accountId` filter

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (merged redirect, deleted tombstone, rail isolation)
- [ ] Permissions enforced (ABAC per rail)
- [ ] Audit/timeline logged: No (read-only composition)
- [ ] Tests pass (including integration for merged / deleted paths)
- [ ] Documentation updated (OpenAPI for each rail)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
