---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0009: Deleted / Merged Account Fallback Contract for Dependent Views

**Story ID:** F0016-S0009
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Deleted / merged account fallback contract for dependent submission, policy, renewal, timeline, and search views
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or distribution user
**I want** linked submission, policy, renewal, timeline, and search views to render predictably even when the underlying account is deactivated, merged, or deleted
**So that** I never see broken pages, blank headers, or 500 errors because of account lifecycle changes

## Context & Background

This is the owning contract descoped from F0006 at its closeout. Every dependent module must adopt it. The contract prevents the class of failure where a join to a removed Account row makes dependent views unrenderable.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission (or renewal, or policy) linked to an account
- **When** the account transitions to `Merged` or `Deleted`
- **Then** the dependent list endpoint still returns the item with `accountDisplayName` (stable) and `accountStatus` denormalized on the list-item payload

- **Given** a user opens a submission detail whose linked account is `Deleted`
- **When** the page renders
- **Then** the header shows `"<stable account name> [Deleted]"` with no clickthrough, the submission itself remains readable, and any account-link buttons are disabled with a tooltip

- **Given** a user opens a submission / renewal / policy whose linked account is `Merged`
- **When** the page renders
- **Then** the header shows `"<stable source name> → <survivor name>"` with a clickthrough to the survivor's Account 360

- **Given** a user calls `GET /api/accounts/{id}` for a Deleted account
- **When** the API responds
- **Then** the response is `410 Gone` with a ProblemDetails body including `stableDisplayName`, `removedAt`, `reasonCode`

- **Given** a user calls `GET /api/accounts/{id}` for a Merged account
- **When** the API responds
- **Then** the response is `200 OK` with `status=Merged` and `survivorAccountId` populated

**Alternative Flows / Edge Cases:**
- Global search (F0023 scope later; MVP list search here): Deleted accounts are NOT returned by default; admins may opt-in with `includeRemoved=true`
- Create-from-account actions on a Merged or Deleted account → disabled with tooltip
- Merged-forward navigation: the frontend redirects once; if the survivor is itself Merged (chain), the frontend follows one hop and surfaces a warning (chained merges are operationally discouraged)
- Deleted account's `stableDisplayName` is frozen at delete time — no further mutations

**Checklist:**
- [ ] Denormalized columns `AccountDisplayNameAtLink` and `AccountStatusAtRead` materialized on Submission / Renewal / Policy list queries
- [ ] `GET /api/accounts/{id}` returns 410 Gone for Deleted accounts with the documented payload
- [ ] `GET /api/accounts/{id}` returns 200 with `survivorAccountId` for Merged accounts
- [ ] Dependent list endpoints (submissions, renewals, policies) return stable fallback fields on every row
- [ ] Dependent detail endpoints include stable account display and status in the payload
- [ ] Regression tests: at least one integration test per dependent feature (F0006 submissions, F0007 renewals, F0018 when live) exercises deleted- and merged-account paths
- [ ] Frontend renders deleted / merged labels and badges consistently across submission, renewal, policy, and timeline views
- [ ] Migration: backfill `AccountDisplayNameAtLink` for existing submissions and renewals from current account rows

## Data Requirements

- Denormalized fields added (migration-additive) to Submission, Renewal, Policy list / detail DTOs:
  - `accountId`, `accountDisplayName` (from `Account.StableDisplayName` at link time; updated opportunistically while account is Active/Inactive), `accountStatus` (current status), `accountSurvivorId` (nullable; populated when `accountStatus=Merged`)
- No direct schema change is required on the dependent DTO contracts beyond additive fields

**Validation Rules:**
- `accountDisplayName` must always be populated on list-item payloads (even when the live account is `Merged` or `Deleted`)
- Backfill migration required for existing data

## Role-Based Visibility

- The contract applies to every role that can see the dependent record; it does not grant new access to the account itself
- An underwriter who could see a submission before account deletion continues to see the submission with a deleted-account label

## Non-Functional Expectations

- Performance: contract must not add N+1 joins; denormalized columns are used
- Reliability: dependent views MUST NOT 500 because an account is Merged or Deleted; at-least-one integration test per dependent feature enforces this

## Dependencies

**Depends On:**
- F0016-S0007 (delete path sets `RemovedAt`, freezes StableDisplayName)
- F0016-S0008 (merge path sets `MergedIntoAccountId`, fires survivor timeline)

**Related Stories:**
- All dependent features consuming the contract (F0006 archived, F0007 archived, F0018 future)

## Out of Scope

- Re-pointing historical FKs to the survivor account (deferred)
- Admin-only unmerge / undelete flows
- Global search integration beyond the `includeRemoved=true` flag (full search UX lives in F0023)

## UI/UX Notes

- Deleted label: red pill with `[Deleted]` suffix; no clickthrough
- Merged label: amber pill with `[Merged → survivor]` and clickthrough icon
- Toast on Account 360 when arriving via tombstone-forward from a merged source

## Questions & Assumptions

**Assumptions:**
- Chained merges (source merged to A, A then merged to B) are operationally rare in MVP; one-hop-forward in the UI is sufficient

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (chained merges, includeRemoved)
- [ ] Integration tests on every active dependent feature (F0006 submissions, F0007 renewals) with deleted- and merged-account cases
- [ ] Denormalization migration deployed with rollback path
- [ ] Permissions unchanged (contract does not widen or narrow access)
- [ ] Audit/timeline logged: N/A (contract is read behavior)
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
