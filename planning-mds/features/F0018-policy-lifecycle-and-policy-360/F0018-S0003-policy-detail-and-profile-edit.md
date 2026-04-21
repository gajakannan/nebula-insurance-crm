---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0003: Policy Detail and Profile Edit

**Story ID:** F0018-S0003
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy detail page and profile edit with optimistic concurrency
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** authorized user (underwriter, distribution user, distribution manager, admin)
**I want** to open a dedicated policy detail page and edit mutable profile fields with optimistic concurrency
**So that** I can maintain accurate policy-level data (carrier linkage, LOB, dates on Pending policies) without corrupting history or clashing with concurrent edits

## Context & Background

Policy detail is the host page for Policy 360 tabs/rails (addressed in S0004). Profile edit is deliberately narrow in MVP: on `Pending` policies, most fields remain mutable; once a policy is `Issued`, material term changes MUST go through the endorsement path (S0006), not profile edit. This story owns the detail page shell, the profile edit affordance, and the rules about what is editable when.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `policy:read` in scope
- **When** they navigate to `/policies/{id}`
- **Then** the detail page renders with header (policy number, status badge, account link with fallback, LOB, carrier, effective/expiration dates, total premium, last activity date) and the Policy 360 tabs / rails (S0004)

- **Given** a `Pending` policy and a user with `policy:update`
- **When** they edit profile fields (carrier linkage, LOB, effective/expiration dates, total premium) and submit with the current `rowVersion`
- **Then** the fields update, `UpdatedAt` and `UpdatedByUserId` are set, `RowVersion` increments, and an `ActivityTimelineEvent` `policy.profile_updated` is appended

- **Given** an `Issued` policy and a user with `policy:update`
- **When** they edit non-material profile fields (e.g., `Notes`) via profile edit
- **Then** the update succeeds without producing a new `PolicyVersion`

- **Given** an `Issued` policy
- **When** a user attempts to edit material terms (coverage limits, premium, effective/expiration dates, LOB, carrier) via profile edit
- **Then** the API returns 409 `code=must_use_endorse` directing them to the endorsement path

**Alternative Flows / Edge Cases:**
- Stale `rowVersion` (concurrency conflict) → 412 `code=concurrency_conflict`
- `Cancelled` / `Expired` policy profile edit → 409 `code=readonly_state` (except for non-material denormalized fields that mirror account; those update via F0016 fallback flow, not a user action)
- Policy on merged account: the account link in the header renders the survivor (tombstone-forward); profile edit that would change `AccountId` is not allowed in MVP (policy → account relationship is immutable post-create)
- Unauthorized actor → 403
- Invalid field payload (e.g., `ExpirationDate ≤ EffectiveDate`) → 400 with field-level errors

**Checklist:**
- [ ] `GET /api/policies/{id}` returns full profile + fallback-aware account reference
- [ ] `PATCH /api/policies/{id}` with `If-Match`; only editable fields accepted
- [ ] Editable-on-`Pending` fields: `accountId` is NOT editable; `policyNumber` is NOT editable post-create; `lineOfBusiness`, `carrierRefId`, `carrierName`, `effectiveDate`, `expirationDate`, `totalPremium`, `premiumCurrency`, `predecessorPolicyId`, `notes` are editable
- [ ] Editable-on-`Issued` fields via profile edit (non-material): `notes` only; material changes require endorsement
- [ ] Editable-on-`Cancelled` / `Expired` fields via profile edit: none (except fallback-driven denormalization, which is not a user-driven action)
- [ ] Header renders: policy number, status badge, account reference with fallback, LOB, carrier, effective/expiration dates, total premium, last activity date, reinstatement deadline (when `Cancelled` and within window)
- [ ] Policy 360 tabs/rails hosted below the header (implementation in S0004)
- [ ] Timeline event `policy.profile_updated` with diff summary on every successful edit
- [ ] ABAC action `policy:update` enforced server-side

## Data Requirements

- See PRD "Data Requirements" section for the full Policy entity
- `ETag` = `RowVersion` for the detail endpoint
- Profile edit payload is a partial (PATCH) with field-level validation

**Validation Rules:**
- `ExpirationDate > EffectiveDate` (also on edit)
- `TotalPremium ≥ 0` when set
- Denormalized `AccountDisplayNameAtLink` is NOT user-editable (managed by F0016 fallback pipeline)
- `policyNumber` is frozen post-create; corrections require a fresh policy (documented)

## Role-Based Visibility

| Role | Read | Profile Edit |
|------|------|--------------|
| Distribution User | Yes (scoped) | Yes (Pending only) |
| Distribution Manager | Yes (scoped) | Yes (Pending only) |
| Underwriter | Yes (scoped) | Yes (Pending + non-material on Issued) |
| Relationship Manager | Yes, read-only | No |
| Program Manager | Yes, read-only | No |
| Admin | Yes | Yes (all states' allowed fields) |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: detail GET p95 ≤ 300 ms; PATCH p95 ≤ 400 ms
- Security: Casbin `policy:read` and `policy:update` gates
- Reliability: optimistic concurrency via `RowVersion`; no lost updates
- Correctness: material-change blocks MUST be enforced server-side (UI hints are advisory)

## Dependencies

**Depends On:**
- F0018-S0002 (policies must exist)
- F0016-S0009 (fallback contract for account reference in header)

**Related Stories:**
- F0018-S0004 (Policy 360 is hosted by this detail page)
- F0018-S0006 (endorsement is the material-change path)

## Out of Scope

- Mass edit across multiple policies
- Re-assigning a policy to a different account (policy → account relationship is immutable in MVP)
- Policy cloning
- Rich-text notes / formatted addenda (MVP accepts plain-text notes only)

## UI/UX Notes

- Edit affordance on fields is state-aware: on `Issued`, only Notes is editable inline; other fields are rendered read-only with a tooltip "Use Endorse to change this"
- Header surfaces the reinstatement deadline as a countdown when a `Cancelled` policy is still within its window
- Merged / deleted account reference in the header uses the F0016 tombstone-forward UI pattern (badge + survivor link)

## Questions & Assumptions

**Assumptions:**
- Notes is plain-text ≤ 4 000 chars
- `PolicyNumber` is frozen after create; re-issuing under a different number requires creating a new policy

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (409, 412, readonly states, material-change blocks)
- [ ] Permissions enforced (ABAC on read + update)
- [ ] Audit/timeline logged: Yes (`policy.profile_updated`)
- [ ] Tests pass (including concurrency and material-change-block cases)
- [ ] Documentation updated (OpenAPI + editable-field matrix)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
