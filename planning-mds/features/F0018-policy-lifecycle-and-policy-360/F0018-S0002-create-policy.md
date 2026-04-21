---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0002: Create Policy (Manual and Import-Lite)

**Story ID:** F0018-S0002
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Create policy (manual, import-lite, and F0019 bind-hook contract)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, distribution manager, underwriter, or admin
**I want** to create a new policy against an existing account either manually from the UI or via a CSV import, and know that F0019 will later call a `POST /api/policies/from-bind` hook to auto-create Pending policies at bind time
**So that** the Policy aggregate is reliably populated for new and existing book policies with a deterministic, auditable creation path

## Context & Background

Policy creation is the originating event for the aggregate. Three paths are in scope for MVP: manual (primary), import-lite (existing-book bulk load), and an F0019 bind-hook contract whose call site is deferred until F0019 lands. The feature must enforce globally-unique `PolicyNumber`, require an account, and seed the correct starting state (`Pending` from manual + bind-hook; directly `Issued` from import-lite when the CSV row reflects an already-bound policy).

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated distribution user with `policy:create` in scope and an Active account
- **When** they submit `POST /api/policies` with account id, LOB, carrier, effective/expiration dates, total premium, and at least one coverage line
- **Then** the system generates a `PolicyNumber` (`NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}`), creates the Policy in `Pending`, writes initial `PolicyCoverageLine` rows attached to a v0 draft version, appends an `ActivityTimelineEvent` `policy.created`, and returns 201 with the new policy and `Location` header

- **Given** an admin uploads a CSV of existing-book policies via `POST /api/policies/import`
- **When** each row carries a user-supplied `PolicyNumber`, account reference, coverage structure, and `importAsStatus=Issued`
- **Then** valid rows land directly in `Issued` with a `PolicyVersion` v1 reflecting the initial state and `ImportSource=csv-import`; invalid rows return a structured error report keyed by row number

- **Given** F0019 is live (future state) and calls `POST /api/policies/from-bind`
- **When** the payload carries the submission id, account id, bound terms, and predecessor policy id (if renewal)
- **Then** the system creates a `Pending` policy with `ImportSource=f0019-bind-hook` and the predecessor linkage is populated

**Alternative Flows / Edge Cases:**
- Duplicate `PolicyNumber` (user-supplied or import) → 409 `code=policy_number_conflict` with the existing policy id
- Account is `Merged` → 400 `code=account_merged_reject` (use survivor); `Deleted` → 400 `code=account_deleted_reject`
- `ExpirationDate ≤ EffectiveDate` → 400 `code=invalid_date_range`
- Zero coverage lines → manual create allowed (saved as `Pending`); `Pending → Issued` is the gate that enforces the coverage requirement
- Missing carrier → manual create allowed with `CarrierName` free-text; import-lite allowed with `CarrierName` only when `CarrierRefId` cannot be resolved
- Unauthorized actor → 403
- Idempotency: the same `Idempotency-Key` for `POST /api/policies` returns the same created resource (no duplicate)

**Checklist:**
- [ ] `POST /api/policies` — manual create; returns 201 with Location
- [ ] `POST /api/policies/import` — CSV import; returns 202 with per-row result report (async acceptable; synchronous also acceptable for MVP ≤ 1 000 rows)
- [ ] `POST /api/policies/from-bind` — specified contract; returns 201; implementation may return 501 `Not Implemented` until F0019 lands (acceptable)
- [ ] `PolicyNumber` generator produces deterministic human-readable codes; sequence is per-LOB-per-year; collision-safe under concurrency (row-lock or unique index + retry)
- [ ] `Account.Status` must be `Active` or `Inactive` to accept a create; `Merged` / `Deleted` rejected
- [ ] `PredecessorPolicyId` (if set) must reference a policy in `Expired` or `Cancelled` — validated at create
- [ ] `ImportSource` populated on every create path (`manual`, `csv-import`, `f0019-bind-hook`)
- [ ] `AccountDisplayNameAtLink` snapshotted at create per F0016 fallback contract
- [ ] Timeline event `policy.created` with actor and source written atomically with the row
- [ ] ABAC actions enforced in scope: `policy:create` for manual / bind-hook paths, `policy:import` for import-lite

## Data Requirements

**Create payload (manual):**
- Required: `accountId`, `lineOfBusiness`, `effectiveDate`, `expirationDate`, `carrierRefId` OR `carrierName`
- Optional: `policyNumber` (auto-generated when omitted), `totalPremium`, `premiumCurrency` (default `USD`), `predecessorPolicyId`, initial `coverageLines[]`
- Returned: full Policy DTO + `PolicyNumber`, `status=Pending`, `currentVersionId=null`

**Import-Lite CSV columns:**
- `policyNumber` (required), `accountExternalId` OR `accountDisplayName` + `taxId` (for account resolution), `lineOfBusiness`, `effectiveDate`, `expirationDate`, `totalPremium`, `carrierName` or `carrierCode`, `importAsStatus` (`Pending` or `Issued`; defaults to `Issued`), one or more coverage-line columns per ADR TBD

**F0019 bind-hook payload (specified, deferred):**
- `submissionId`, `accountId`, `lineOfBusiness`, `effectiveDate`, `expirationDate`, `totalPremium`, `coverageLines[]`, optional `predecessorPolicyId`

**Validation Rules:**
- See PRD "Validation Rules" section
- Import-row errors collected in an `ImportResult` object (success_count, failure_count, row_errors[])

## Role-Based Visibility

| Role | Allowed |
|------|---------|
| Distribution User | Manual create in `Pending` only |
| Distribution Manager | Manual create in `Pending`; import-lite |
| Underwriter | Manual create in `Pending`; direct manual `Issued` is NOT allowed (must go via issue transition) |
| Relationship Manager | No |
| Program Manager | No |
| Admin | Manual, import-lite, bind-hook (when active) |

## Non-Functional Expectations

- Performance: single create p95 ≤ 500 ms; import-lite p95 ≤ 2 min for 1 000 rows
- Reliability: idempotent under `Idempotency-Key`; import-lite re-upload with the same batch id is a no-op for already-imported rows
- Security: Casbin `policy:create` for manual / bind-hook; `policy:import` for import-lite (Distribution Manager + Admin only in MVP)
- Concurrency: `PolicyNumber` generation collision-safe under concurrent load

## Dependencies

**Depends On:**
- F0016-S0002 (accounts must exist)
- F0016-S0009 (fallback contract for account status)

**Related Stories:**
- F0018-S0005 (version snapshots written at create time for import-lite `Issued` rows)
- F0018-S0009 (predecessor linkage)
- F0019 (future consumer of bind-hook contract)

## Out of Scope

- Full F0019 bind-hook *implementation* (contract only; F0019 owns the call site)
- Account auto-creation from policy import (account must exist; import row errors if account not resolved)
- Rating / pricing engines
- Policy cloning from an existing policy (not MVP; creators use predecessor linkage for renewals)

## UI/UX Notes

- Create entry points: `+ New Policy` button on Account 360 → policies rail (prefills `accountId`); standalone Policy List `+ New` (asks for account first)
- Import-lite: admin screen with drag-drop; validation report surfaces row-level errors; successful import redirects to Policy List filtered by `ImportSource=csv-import` and today's date
- Bind-hook is an internal contract only (no UI)

## Questions & Assumptions

**Assumptions:**
- `PolicyNumber` sequence generator is per-LOB-per-year; rollover at calendar year boundary
- Import-lite is synchronous up to 1 000 rows; larger batches go async (architect decision in Phase B)
- F0019 bind-hook may return 501 `Not Implemented` in MVP; the contract is specified so F0019 can implement against it

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (conflict, merged/deleted account, date range, idempotency)
- [ ] Permissions enforced (role-gated creation paths)
- [ ] Audit/timeline logged: Yes (`policy.created`)
- [ ] Tests pass (unit + integration for all three paths; contract test for F0019 hook)
- [ ] Documentation updated (OpenAPI for all three endpoints)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
