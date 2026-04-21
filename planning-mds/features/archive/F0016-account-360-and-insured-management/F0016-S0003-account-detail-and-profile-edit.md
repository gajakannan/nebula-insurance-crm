---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0003: Account Detail and Profile Edit

**Story ID:** F0016-S0003
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account detail view with inline profile edit
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** an account detail view where I can see the full profile and edit it with concurrency-safe updates
**So that** the account record stays accurate and my edits do not silently overwrite someone else's changes

## Context & Background

The detail view is the canonical profile surface. Account 360 (F0016-S0004) is a tab or section within this page, but the profile surface itself is its own story because edit semantics, audit, and concurrency apply only here.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `account:read` in scope for the account
- **When** they open `/accounts/{id}`
- **Then** the profile renders with all persisted fields, current status badge, and audit metadata (createdAt/By, updatedAt/By)

- **Given** a user with `account:update` in scope
- **When** they save an inline edit with the correct `If-Match` rowVersion
- **Then** the account is updated, `StableDisplayName` is refreshed if `Status ∈ {Active, Inactive}`, and a timeline event `account.profile_updated` is appended with a diff payload

- **Given** another user updated the same account first
- **When** the first user's `If-Match` is stale
- **Then** the API returns 412 Precondition Failed with `code=concurrency_conflict` and the client refreshes

**Alternative Flows / Edge Cases:**
- Tax id change to a value already used by another Active account → 409 `duplicate_tax_id`
- Profile edit on a Merged account → 409 `invalid_state` (read-only)
- Profile edit on a Deleted account → 410 Gone per fallback contract
- Edits to `status` itself are rejected here — use the lifecycle endpoints
- Edits to `mergedIntoAccountId`, `removedAt`, `stableDisplayName` directly are rejected (server-managed)

**Checklist:**
- [ ] `GET /api/accounts/{id}` returns full profile + rowVersion
- [ ] `PATCH /api/accounts/{id}` with `If-Match: "<rowVersion>"` updates profile fields
- [ ] 412 on rowVersion mismatch
- [ ] 409 on tax id duplicate
- [ ] Timeline event `account.profile_updated` with field-diff payload
- [ ] Editable fields: displayName, legalName, taxId, industry, primaryLineOfBusiness, territoryCode, region, address fields, primaryProducerUserId, brokerOfRecordId
- [ ] Non-editable here: status, mergedIntoAccountId, removedAt, stableDisplayName, audit fields
- [ ] Read-only rendering when status ∈ {Merged, Deleted}

## Data Requirements

See PRD `Data Requirements` section for the Account entity shape.

**Validation Rules:**
- `displayName` required, ≤ 200
- `taxId` unique among Active accounts
- `primaryLineOfBusiness` validated against known LOB when provided
- `brokerOfRecordId` must reference an active broker
- Tracks the relationship-history log when `brokerOfRecordId`, `primaryProducerUserId`, or `territoryCode` change (owned by F0016-S0006)

## Role-Based Visibility

- Read: Distribution User (scope), Distribution Manager (territory), Underwriter (read-only, own book), Relationship Manager (own brokers), Admin
- Update: Distribution User (scope), Distribution Manager (territory), Admin

## Non-Functional Expectations

- Performance: detail GET p95 ≤ 250 ms; PATCH p95 ≤ 500 ms
- Security: ABAC `account:read` / `account:update`
- Reliability: `RowVersion` optimistic concurrency on every mutation

## Dependencies

**Depends On:**
- F0016-S0002 (account must exist)

**Related Stories:**
- F0016-S0004 (Account 360 renders as a tab beside profile)
- F0016-S0006 (relationship changes trigger history rows)
- F0016-S0007 (lifecycle transitions live on a separate endpoint)

## Out of Scope

- Full-screen edit mode (inline per-section edit only in MVP)
- Field-level audit drill-down UI (timeline shows the diff payload; richer drill-down is Future)

## UI/UX Notes

- Detail page is a two-column layout: profile card on the left, Account 360 tabs on the right (see F0016-S0004)
- Inline edit uses per-section "Edit" buttons with Save / Cancel
- Concurrency-conflict dialog prompts user to reload

## Questions & Assumptions

**Assumptions:**
- Editing displayName updates StableDisplayName while Active/Inactive
- Inline edits produce a single timeline event per save (batched diff)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (412, 409, state checks)
- [ ] Permissions enforced
- [ ] Audit/timeline logged: Yes (`account.profile_updated`)
- [ ] Tests pass
- [ ] Documentation updated (OpenAPI)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
