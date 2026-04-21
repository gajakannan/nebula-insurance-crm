---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0001: Policy List with Search and Filtering

**Story ID:** F0018-S0001
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy list with search and filtering
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, distribution manager, underwriter, or relationship manager
**I want** a searchable, filterable, paginated policy list
**So that** I can find any policy quickly by policy number, account, carrier, LOB, or expiration window and reach its Policy 360 in one click

## Context & Background

The policy list is the primary entry point into Policy 360 and the main operating surface for tracking expiring, cancelled, and in-force policies across the book. Without a reliable list, users cannot audit the book at the territory level and cannot find historical policies by anything other than account navigation. The list is also the staging surface for bulk expiration review.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `policy:read` in scope
- **When** they navigate to the Policy List
- **Then** they see a paginated list of policies in their scope showing: policy number, account display name, status, LOB, carrier, effective date, expiration date, total premium, last activity date

- **Given** policies exist across multiple LOBs and carriers
- **When** the user filters by LOB = `Property` and expiration window = `next 60 days`
- **Then** only matching policies are returned, sorted by expiration date ascending by default

- **Given** a user searches by policy number fragment
- **When** the server query runs
- **Then** policies with `PolicyNumber` containing the fragment (case-insensitive) are returned; exact-match surfaces first

**Alternative Flows / Edge Cases:**
- No results → Empty state "No policies match your filters"
- `Cancelled` and `Expired` policies are included by default but visually muted; `status` filter narrows as requested
- Merged / Deleted accounts continue to render policies via the F0016 tombstone-forward contract (stable name + status badge on the account column)
- Multiple filters combine with AND semantics; expiration window and explicit date range are mutually exclusive in the UI
- Sorting an empty list is a no-op

**Checklist:**
- [ ] Columns: policy number, account display name, status badge, LOB, carrier, effective date, expiration date, total premium, last activity date
- [ ] Filters: status (default all), LOB, carrier (CarrierRef), account (typeahead), broker (via account), expiration window (`30`/`60`/`90`/`expired`/`custom`)
- [ ] Search by policy number, account display name, account legal name
- [ ] Sort options: expiration date (default asc), policy number, total premium, last activity date
- [ ] Pagination: 25 per page with page navigation
- [ ] ABAC scoping enforced server-side on the list query (broker / territory / region / own-book predicates)
- [ ] Row click navigates to `/policies/{id}` (detail); status badge click scopes filter to that status
- [ ] Account column honors F0016 fallback contract (displays `AccountDisplayNameAtLink` with status badge when account is merged or deleted)

## Data Requirements

**Required list-item fields:**
- `id`, `policyNumber`, `accountId`, `accountDisplayName`, `accountStatusAtRead`, `status`, `lineOfBusiness`, `carrierName`, `carrierRefId`, `effectiveDate`, `expirationDate`, `totalPremium`, `premiumCurrency`, `lastActivityAt`

**Optional fields:**
- `versionCount`, `endorsementCount` (served from the summary projection when available; null-safe when absent)

**Validation Rules:**
- Query must enforce ABAC scope; cross-scope requests return an empty page, not 403
- Search input length ≤ 200; server trims
- Expiration window values validated at API layer: `30`, `60`, `90`, `expired`, `custom`; `custom` requires `expirationFrom` and `expirationTo`

## Role-Based Visibility

| Role | Scope |
|------|-------|
| Distribution User | Own region + assigned broker(s) via account |
| Distribution Manager | Own territory via account |
| Underwriter | Own assigned book (policies on accounts with assigned submissions / renewals to the user, plus policies the user has endorsed) |
| Relationship Manager | Read-only, own managed broker(s) via account |
| Program Manager | Read-only, own program scope |
| Admin | All |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: p95 ≤ 300 ms for up to 50 000 policies with filters + pagination
- Security: ABAC enforced in the query; no client-side filtering of privileged data
- Reliability: Cancelled / Expired policies never cause a 500; merged / deleted accounts never cause a 500

## Dependencies

**Depends On:**
- F0018-S0002 (policies must exist to populate the list)
- F0016-S0011 (account summary projection for account-scoped filter typeahead)

**Related Stories:**
- F0018-S0003 (navigation target), F0018-S0011 (summary counts in list)

## Out of Scope

- Saved / named views (deferred)
- Kanban / board view
- CSV export
- URL-synced filter state (follow-up)

## UI/UX Notes

- Screens: Policy List
- Filter bar at top with expiration-window chips pinned left; table below with sortable headers
- Status badges: Pending (neutral), Issued (positive), Expired (muted), Cancelled (amber with reinstatement-window countdown when still eligible)
- Clicking a policy on a merged account renders a toast "Viewing policy on merged account — forwarded to survivor"

## Questions & Assumptions

**Assumptions:**
- Default sort is expiration date ascending; default status filter is "all"
- Expiration window "expired" includes only policies in `Expired` status; policies that passed their date but are still `Issued` (not yet swept) are included in "next 30 days = 0 days" bucket until the nightly job runs

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (ABAC on list query)
- [ ] Audit/timeline logged: No (read-only)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
