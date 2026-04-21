---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0002: Create Account (Manual and From Submission / Policy)

**Story ID:** F0016-S0002
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Create account with duplicate detection hint
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user or distribution manager
**I want** to create an insured account manually or in-flow during submission / policy creation, with a duplicate hint before I commit
**So that** the account book stays accurate and I avoid creating duplicates that later require merging

## Context & Background

Account creation happens both from a dedicated "Create Account" action and as an inline step during new-business submission intake and new-policy entry. The duplicate-detection hint is the primary lever for reducing duplicates at creation time; fuzzy / auto-resolution is deferred.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution user is authenticated and in-scope to create accounts
- **When** they submit a valid create request with display name, legal name, tax id, industry, territory, and broker of record
- **Then** a new `Active` account is persisted with a generated id, audit fields set, and a `StableDisplayName` snapshot equal to `DisplayName`

- **Given** the user enters a display name or tax id that matches an existing Active account
- **When** the form is being filled or on submit pre-check
- **Then** the UI shows a duplicate hint listing the top matches (name-match OR tax-id-match, ≤ 5 results); the user may proceed or pick an existing account

- **Given** a submission intake flow is creating an account inline
- **When** the account is created successfully
- **Then** the new account id is returned to the intake flow and the submission is linked to it

**Alternative Flows / Edge Cases:**
- Duplicate tax id on `Active` → 409 Conflict (code=`duplicate_tax_id`) unless the request explicitly asserts `allowDuplicateTaxId=false` is the default — there is no override
- Invalid territory code → 400
- BrokerOfRecordId references an inactive or non-existent broker → 400
- Inline create from submission / policy succeeds but the caller fails downstream → the account persists; cleanup is the caller's responsibility
- Retries with same `Idempotency-Key` return the originally created account (idempotent POST)

**Checklist:**
- [ ] Required fields validated: `DisplayName`
- [ ] Optional fields accepted and persisted: legal name, tax id, industry, primary LOB, territory, region, broker of record, primary producer, address fields
- [ ] Duplicate hint endpoint: `GET /api/accounts/duplicate-hints?displayName=...&taxId=...` returns name-match OR tax-id-match candidates (max 5)
- [ ] Active-row tax-id uniqueness enforced at DB (filtered unique index) and at API (409 on conflict)
- [ ] `Idempotency-Key` honored on create
- [ ] `StableDisplayName` initialized from `DisplayName`
- [ ] Timeline event `account.created` appended
- [ ] RowVersion returned in response

## Data Requirements

**Request Body:**
- `displayName` (required), `legalName`, `taxId`, `industry`, `primaryLineOfBusiness`, `brokerOfRecordId`, `primaryProducerUserId`, `territoryCode`, `region`, address fields

**Response Body:**
- Full account detail payload including `id`, `status=Active`, `rowVersion`, `stableDisplayName`, audit fields

**Validation Rules:**
- `DisplayName` required, ≤ 200 chars
- `TaxId` must be unique among Active accounts (409 on conflict)
- `BrokerOfRecordId` must reference an active broker
- `PrimaryLineOfBusiness` validated against known LOB set when provided

## Role-Based Visibility

- Create allowed for Distribution User, Distribution Manager, Admin
- Underwriters and Relationship Managers cannot create accounts

## Non-Functional Expectations

- Performance: create p95 ≤ 500 ms
- Security: ABAC `account:create`; region / territory scope enforced
- Reliability: Idempotency-Key deduplication; duplicate-hint endpoint cache-safe (short TTL acceptable)

## Dependencies

**Depends On:**
- F0002 Broker (active; for broker-of-record reference)

**Related Stories:**
- F0016-S0003 (detail / edit), F0016-S0008 (merge for when a duplicate slips through)

## Out of Scope

- Fuzzy / phonetic match on display name (MVP uses exact + normalized match only)
- Bulk create / import (deferred to F0031)
- Auto-merge on duplicate detection

## UI/UX Notes

- Create form accessible from Account List, from Submission Intake, and from Policy create flow
- Duplicate hint appears inline as the user types (debounced) and again at submit time
- Address fields collapsed into an optional section

## Questions & Assumptions

**Assumptions:**
- Duplicate-hint exact-match algorithm is sufficient for MVP
- Inline create from downstream flows always uses the same API as manual create

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (409 on duplicate tax id, Idempotency-Key)
- [ ] Permissions enforced (`account:create`)
- [ ] Audit/timeline logged: Yes (`account.created`)
- [ ] Tests pass
- [ ] Documentation updated (OpenAPI)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
