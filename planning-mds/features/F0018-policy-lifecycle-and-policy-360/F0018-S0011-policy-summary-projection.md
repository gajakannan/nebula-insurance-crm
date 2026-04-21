---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0011: Policy Summary Projection for Account 360

**Story ID:** F0018-S0011
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy summary projection for Account 360 and Policy List
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution user, distribution manager, or relationship manager
**I want** an account-scoped policy summary (active count, expired count, cancelled count, next-expiring date, total current premium) available without per-policy fetches
**So that** Account 360 renders portfolio-level policy metrics fast, and Policy List rows can show a per-policy summary chip without extra round-trips

## Context & Background

F0016 introduced an account summary projection shape (active policy count, open submission count, renewal-due count, last activity). F0018 now owns the policy-side numbers: active policies, expired policies, cancelled policies, next-expiring date, total current premium. MVP composes this at query time (no materialized projection) because Policy is a moderate-cardinality entity per account. A dedicated materialized projection is a follow-up if the query becomes a hotspot.

## Acceptance Criteria

**Happy Path:**
- **Given** an account with a mix of Issued, Expired, and Cancelled policies
- **When** Account 360 calls `GET /api/accounts/{accountId}/policies/summary`
- **Then** the response returns: `activePolicyCount`, `expiredPolicyCount`, `cancelledPolicyCount`, `pendingPolicyCount`, `nextExpiringDate`, `nextExpiringPolicyId`, `totalCurrentPremium`, `premiumCurrency` — all scoped to policies that are readable by the caller (ABAC scope)

- **Given** an account with zero policies
- **When** the endpoint is called
- **Then** counts return 0, `nextExpiringDate` returns null, `nextExpiringPolicyId` returns null, `totalCurrentPremium` returns 0 with default currency

- **Given** Account 360 summary endpoint (F0016-S0011 / S0004)
- **When** it is called
- **Then** the F0018 policy counts can be composed into the account summary payload via a single join (no N+1)

**Alternative Flows / Edge Cases:**
- Account is `Merged` or `Deleted` → counts reflect the tombstone/survivor per F0016 fallback contract; policies on merged accounts continue to roll up to the survivor for the policy summary
- ABAC: counts only include policies the caller can read; cross-scope policies are excluded from counts (not a 403; counts shrink silently per list-scope semantics)
- Mixed currencies on premiums → MVP assumes `USD` across an account; if mixed, `totalCurrentPremium` returns the sum in base currency units with a `premiumCurrency=MIXED` marker flagged for future multi-currency work
- Policy whose status has just transitioned (e.g., sweep job runs mid-read) → counts are eventually consistent but must never double-count within a single query

**Checklist:**
- [ ] `GET /api/accounts/{accountId}/policies/summary` returns the summary shape above
- [ ] Account 360 summary endpoint (F0016) is extended to include the policy counts by composing this endpoint (or via an in-service aggregate — implementation decision by Architect)
- [ ] ABAC scope applied: only policies readable by the caller contribute to counts
- [ ] `nextExpiringDate` = min(`ExpirationDate`) across `Status=Issued` policies on the account readable by the caller
- [ ] `totalCurrentPremium` = sum(`TotalPremium`) across `Status=Issued` policies on the account readable by the caller
- [ ] Merged accounts: summary on the survivor includes policies from the merged source (per F0016 fallback)
- [ ] Deleted accounts: summary returns a tombstone-friendly shape (counts may be 0 or reflect pre-delete depending on fallback semantics — covered by tests)
- [ ] No N+1: summary computed with a single grouped query per account

## Data Requirements

**Summary payload fields:**
- `accountId`, `activePolicyCount`, `expiredPolicyCount`, `cancelledPolicyCount`, `pendingPolicyCount`, `nextExpiringDate`, `nextExpiringPolicyId`, `nextExpiringPolicyNumber`, `totalCurrentPremium`, `premiumCurrency`, `computedAt`

**Per-policy summary fields (optional; for Policy List row chip):**
- `id`, `versionCount`, `endorsementCount`, `hasOpenRenewal` (boolean derived from F0007)

**Validation Rules:**
- `accountId` must exist and be visible to caller; non-existent / non-visible returns 404
- Currency defaults to `USD`; `MIXED` marker set only when the account carries policies in multiple currencies (not expected in MVP)

## Role-Based Visibility

- Any role with `account:read` + `policy:read` in scope sees the counts for the policies they can read
- Counts are ABAC-gated per-policy: cross-scope policies don't leak into counts

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: summary p95 ≤ 300 ms for accounts with ≤ 100 policies; ≤ 500 ms for 500 policies
- Reliability: single grouped query; no per-policy fetches
- Correctness: counts are a snapshot of read-time state; eventual consistency with in-flight transitions is acceptable as long as no double-count occurs within a single read

## Dependencies

**Depends On:**
- F0018-S0002 (policies must exist), F0018-S0007 (cancellation state), F0018-S0003 (status field source)
- F0016-S0011 (Account summary projection host)

**Related Stories:**
- F0018-S0001 (Policy List may consume per-policy summary chips)
- F0018-S0004 (Policy 360 summary endpoint — separate contract, this story is account-scoped)

## Out of Scope

- Materialized projection table with trigger-based maintenance (MVP uses query-time composition)
- Multi-currency premium handling beyond the `MIXED` marker (follow-up)
- Policy-type breakdown by LOB within the summary (follow-up; API consumers can filter by LOB via list endpoints today)
- Historical trend data (active policies over time) — deferred
- Summary for the entire book (cross-account aggregation) — deferred to reporting

## UI/UX Notes

- Account 360 overview renders the counts as chips: "5 Active", "2 Expired", "1 Cancelled", with a "Next expires 2026-06-15" tagline
- Policy List row chip (optional) renders "v3 · 2 endorsements" when summary data available; null-safe when absent

## Questions & Assumptions

**Assumptions:**
- Query-time composition is performant enough for CRM Release MVP cardinalities
- Multi-currency policies on a single account are rare / absent in MVP
- ABAC scope applied identically to both reads and count calculations; silent shrinkage is intentional

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (merged / deleted accounts, zero policies, mixed currency, ABAC shrinkage)
- [ ] Permissions enforced (account-scope + per-policy ABAC)
- [ ] Audit/timeline logged: No (read-only)
- [ ] Tests pass (including N+1 regression test and ABAC shrinkage correctness)
- [ ] Documentation updated (OpenAPI + account-summary composition reference)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
