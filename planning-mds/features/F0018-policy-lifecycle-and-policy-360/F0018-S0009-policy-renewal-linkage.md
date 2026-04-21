---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0009: Policy Renewal Linkage (Predecessor / Successor)

**Story ID:** F0018-S0009
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy renewal linkage (predecessor / successor) and F0007 handoff
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution user, or distribution manager
**I want** each policy to know which policy it renewed from (predecessor) and, once a renewal in F0007 completes, which policy renewed it (successor)
**So that** Policy 360 surfaces renewal continuity end-to-end, the Account 360 policy history is complete, and no renewal ends up orphaned without a traceable policy chain

## Context & Background

Nebula's renewal domain (F0007) carries `PolicyId` (the expiring policy) and `BoundPolicyId` (the new policy created by binding the renewal). F0018 owns the Policy aggregate and must expose these linkages from the policy perspective: `PredecessorPolicyId` is persisted on the policy row and set at create time (manually or via F0019 bind-hook); `SuccessorPolicyId` is *computed* from F0007 `BoundPolicyId` via a read-side query. Cancelled and Expired policies retain their successor linkage for audit; reinstated policies do NOT invalidate an existing successor unless the successor is also later cancelled.

## Acceptance Criteria

**Happy Path:**
- **Given** a policy is created via manual create with `predecessorPolicyId` set (pointing at an Expired or Cancelled policy)
- **When** the create commits
- **Then** `Policy.PredecessorPolicyId` is persisted; the predecessor policy exposes a computed `SuccessorPolicyId` pointing at the new policy on next read

- **Given** an F0007 renewal completes its bind step and sets `Renewal.BoundPolicyId`
- **When** Policy 360 renders for the predecessor policy
- **Then** the Renewals rail shows the successor linkage and the summary exposes `SuccessorPolicyId` derived from the F0007 renewal row

- **Given** a policy's predecessor is itself a renewal of an earlier policy
- **When** the user opens Policy 360 and follows the predecessor chain
- **Then** they can navigate back through the chain one step at a time (MVP: one-hop navigation with "View predecessor policy"; full chain-traversal view is a follow-up)

**Alternative Flows / Edge Cases:**
- Predecessor policy does not exist or is `Pending` → 400 `code=invalid_predecessor` at policy create
- Predecessor policy is on a different account → 400 `code=predecessor_account_mismatch` (renewal continuity is scoped to the same account in MVP)
- `SuccessorPolicyId` query returns null when no F0007 renewal has completed → rail shows "No successor yet" empty state
- A policy has multiple F0007 renewals with `BoundPolicyId` pointing at different new policies → this is a data error; MVP returns the most recently bound renewal's `BoundPolicyId` as the canonical successor and logs a warning (single-successor assumption)
- Predecessor policy on a merged account → allowed; the predecessor reference resolves per F0016 tombstone-forward contract on display

**Checklist:**
- [ ] `Policy.PredecessorPolicyId` persisted on create when provided; nullable by default
- [ ] Validation at create: predecessor exists, predecessor `Status ∈ {Expired, Cancelled}`, predecessor `AccountId == Policy.AccountId`
- [ ] `SuccessorPolicyId` exposed as a computed field on Policy summary / detail responses (derived from `Renewal.BoundPolicyId` where `Renewal.PolicyId = this.Id`)
- [ ] Policy 360 Renewals rail surfaces: predecessor reference (when set), successor reference (when computed), any open renewals (F0007 data)
- [ ] Account 360 policy history (via F0016-S0011 summary projection extension) can surface predecessor-chain context when available
- [ ] Navigation: "View predecessor policy" affordance on Policy 360 header when `PredecessorPolicyId` is set
- [ ] Cancelled / Expired policies retain their successor linkage
- [ ] Index: `Policies(PredecessorPolicyId)` partial index for successor-lookup queries

## Data Requirements

- `Policy.PredecessorPolicyId` (uuid, FK → Policy, nullable; self-referential)
- Computed `SuccessorPolicyId` (uuid, nullable) — derived at read time from F0007 `Renewal` rows
- Policy 360 summary payload (S0004) carries both `PredecessorPolicyId` and `SuccessorPolicyId`
- Renewals rail list-item fields: id, status, policyExpirationDate, boundPolicyId (nullable), boundPolicyNumber (nullable)

**Validation Rules:**
- Predecessor must exist in state `Expired` or `Cancelled`
- Predecessor must share `AccountId` with the new policy
- `PredecessorPolicyId` is set only at create; not editable post-create in MVP (correcting a mistake requires recreating the policy — rare edge case, acknowledged)
- `SuccessorPolicyId` is a read-side derivation; never persisted on the Policy row

## Role-Based Visibility

- Any role that can read the policy can see its linkage
- Setting `PredecessorPolicyId` at create requires the same role that owns the create path (S0002)

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: successor lookup p95 ≤ 100 ms (single join from `Policy` to `Renewal`); index supports this
- Reliability: missing or stale F0007 data does not 500 the Policy 360 summary; successor-lookup failures return null with a warning log
- Correctness: circular predecessor chains prevented at create (policy cannot be its own predecessor transitively); detection is shallow in MVP — enforce `predecessor != self`; deep-cycle detection is a follow-up if it proves necessary

## Dependencies

**Depends On:**
- F0018-S0002 (policy create path sets PredecessorPolicyId)
- F0007 (source of `Renewal.BoundPolicyId`; archived / done)

**Related Stories:**
- F0018-S0004 (Renewals rail in Policy 360)
- F0016-S0011 (Account 360 summary may surface policy-chain metadata as an enhancement)

## Out of Scope

- Full chain-traversal UI (MVP: one-hop navigation)
- Multi-successor fan-out (MVP assumes single successor per policy; multi-binding splits are not supported)
- Automatic predecessor inference when user creates a policy without specifying one (MVP requires explicit `PredecessorPolicyId`)
- Cross-account renewal continuity (books moves between accounts are a follow-up)
- Editing `PredecessorPolicyId` post-create

## UI/UX Notes

- Policy detail header: "Renewal of →" chip linking to the predecessor when set; "Renewed by →" chip linking to the successor when computed
- Renewals rail: shows open renewals (in-flight), completed renewals with their `BoundPolicyId` reference, and any predecessor chain entries
- Cancelled / Expired policies show both chips when applicable

## Questions & Assumptions

**Assumptions:**
- Single successor per policy; multi-binding splits are a follow-up
- `SuccessorPolicyId` is a derived field (not persisted); source of truth is F0007 `Renewal.BoundPolicyId`
- Predecessor chains can be arbitrarily long in practice; MVP exposes one hop and the full chain is a navigation exercise

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (invalid predecessor, account mismatch, null successor, merged-account display)
- [ ] Permissions enforced (read through parent policy scope; write gated by create-path role)
- [ ] Audit/timeline logged: No (predecessor is a create-time attribute; successor is derived — both are side-effects of the create/renewal flows that own the events)
- [ ] Tests pass (including F0007 data coupling tests; successor-lookup correctness)
- [ ] Documentation updated (OpenAPI + successor computation reference)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
