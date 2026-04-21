---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0007: Account Lifecycle — Deactivate, Reactivate, Delete

**Story ID:** F0016-S0007
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account lifecycle transitions (deactivate, reactivate, delete)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager or admin
**I want** to deactivate, reactivate, or delete an account through a role-gated state machine with required reason codes
**So that** the account book reflects reality, dependent views still render correctly, and every lifecycle change is auditable

## Context & Background

Lifecycle is distinct from profile edit because transitions require role gates, reason fields, denormalized column propagation, and timeline events. Merge is covered separately in F0016-S0008.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution manager with `account:deactivate` in scope and an `Active` account
- **When** they call `POST /api/accounts/{id}/deactivate` with the current `rowVersion`
- **Then** the status transitions to `Inactive`, a `WorkflowTransition` is appended, a timeline event `account.deactivated` is recorded, and downstream denormalized `AccountStatusAtRead` fields on linked submission / renewal list items reflect the new status on next read

- **Given** an `Inactive` account
- **When** a manager calls `POST /api/accounts/{id}/reactivate`
- **Then** the status returns to `Active` with a `WorkflowTransition` and timeline event `account.reactivated`

- **Given** a manager with `account:delete` in scope
- **When** they call `POST /api/accounts/{id}/delete` with `reasonCode` and `rowVersion`
- **Then** the status transitions to `Deleted`, `RemovedAt` is set, `StableDisplayName` is frozen, a timeline event `account.deleted` is recorded, and the fallback contract applies to all dependent views

**Alternative Flows / Edge Cases:**
- Invalid transition (e.g., `Merged → Active`) → 409 `invalid_transition`
- Delete without `reasonCode` → 400
- Delete with `reasonCode=Other` without `reasonDetail` → 400
- Reactivate on a Merged or Deleted account → 409 `invalid_transition`
- Unauthorized actor → 403
- Concurrent mutation (stale rowVersion) → 412 `concurrency_conflict`

**Checklist:**
- [ ] `POST /api/accounts/{id}/deactivate` with `If-Match`
- [ ] `POST /api/accounts/{id}/reactivate` with `If-Match`
- [ ] `POST /api/accounts/{id}/delete` with `reasonCode`, optional `reasonDetail`, `If-Match`
- [ ] Transitions recorded in `WorkflowTransition` (reusing ADR-011 pattern)
- [ ] Timeline events: `account.deactivated`, `account.reactivated`, `account.deleted`
- [ ] `StableDisplayName` frozen at delete
- [ ] Role gates: Distribution Manager + Admin only
- [ ] `Accounts(TaxId)` unique filtered index ignores non-Active rows so a duplicate tax id can be recreated after deletion if necessary

## Data Requirements

- See PRD `Data Requirements` section for Account state fields (`Status`, `RemovedAt`, `DeleteReasonCode`, `DeleteReasonDetail`, `StableDisplayName`)
- `WorkflowTransition` row per transition

**Validation Rules:**
- Reason codes validated at API layer (not DB enum): `DuplicateOfAnother`, `InvalidOrTestRecord`, `ReplacedByExternalSystem`, `RequestedByInsured`, `Other`
- `reasonDetail` required when `reasonCode=Other`

## Role-Based Visibility

- Mutations: Distribution Manager, Admin only
- Distribution User, Underwriter, Relationship Manager: 403

## Non-Functional Expectations

- Performance: each transition p95 ≤ 500 ms
- Security: dedicated Casbin actions `account:deactivate`, `account:reactivate`, `account:delete`
- Reliability: idempotent on retry with same `Idempotency-Key`; stale rowVersion returns 412 (never 500)

## Dependencies

**Depends On:**
- F0016-S0003 (profile exists)

**Related Stories:**
- F0016-S0008 (merge lifecycle is separate)
- F0016-S0009 (fallback contract on dependent views)
- F0016-S0010 (timeline)

## Out of Scope

- Undelete / unmerge (deferred)
- Time-delayed or scheduled lifecycle actions
- Bulk deactivation

## UI/UX Notes

- Lifecycle actions in a "Manage Account" menu on the detail header
- Delete confirmation requires reason code selection
- Deactivated badge visible everywhere the account is referenced

## Questions & Assumptions

**Assumptions:**
- Delete is soft (row persists with `Status=Deleted` and `RemovedAt` set); hard-delete is not offered in MVP

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (409, 412, reason code validation)
- [ ] Permissions enforced (manager+admin)
- [ ] Audit/timeline logged: Yes
- [ ] Tests pass (transition matrix)
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
