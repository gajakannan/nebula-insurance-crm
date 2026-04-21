---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0006: Policy Endorsements

**Story ID:** F0018-S0006
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy endorsement events and material term changes
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or admin
**I want** to endorse an `Issued` policy by changing profile fields, coverage lines, or premium with a required reason code and effective date
**So that** every material change is captured as an explicit event, produces an immutable version snapshot, and is traceable in the audit timeline

## Context & Background

Endorsement is the mid-term change mechanism: it does not change the policy's lifecycle state (`Issued → Issued`) but it produces a new `PolicyEndorsement` row, a new `PolicyVersion` with complete snapshots, a fresh `PolicyCoverageLine` set on the new version, and an `ActivityTimelineEvent`. This keeps Policy 360 auditable without conflating edits with state transitions. `Pending` policies take edits through profile edit (S0003); only `Issued` policies accept endorsements.

## Acceptance Criteria

**Happy Path:**
- **Given** an `Issued` policy with v1 and a user with `policy:endorse` in scope
- **When** they submit `POST /api/policies/{id}/endorsements` with `endorsementReasonCode=LimitChange`, `effectiveDate=2026-05-15`, changed coverage lines (e.g., raise `GL-PREMISES.LimitPerOccurrence`), and the current `rowVersion`
- **Then** the system creates a `PolicyEndorsement` row (endorsement #1), a new `PolicyVersion` v2 with `VersionReason=Endorsement`, new `PolicyCoverageLine` rows under v2 reflecting the changes, updates `Policy.CurrentVersionId` to v2, increments `Policy.RowVersion`, and appends an `ActivityTimelineEvent` `policy.endorsed`

- **Given** an endorsement that changes `TotalPremium`
- **When** the endorsement is committed
- **Then** the new `PolicyVersion.PremiumSnapshot` reflects the new value; `Policy.TotalPremium` is updated to match

- **Given** the endorsement succeeds
- **When** the user returns to Policy 360
- **Then** the Versions rail shows v2 on top, the Endorsements rail shows endorsement #1, the Coverages rail shows the new current coverages, the timeline shows `policy.endorsed`

**Alternative Flows / Edge Cases:**
- Policy not `Issued` → 409 `code=invalid_transition` (cannot endorse Pending / Cancelled / Expired)
- Missing `endorsementReasonCode` → 400 `code=missing_field`
- No actual field changes → 400 `code=empty_endorsement` (no-op endorsements not allowed)
- `Other` reason code without `endorsementReasonDetail` → 400 `code=reason_detail_required`
- `effectiveDate` outside policy term (`< EffectiveDate` or `> ExpirationDate`) → 400 `code=effective_date_out_of_term`
- Stale `rowVersion` → 412 `code=concurrency_conflict`
- Unauthorized actor → 403
- Idempotent retry: same `Idempotency-Key` returns the same endorsement (no duplicate version / endorsement / timeline)

**Checklist:**
- [ ] `POST /api/policies/{id}/endorsements` with `If-Match` + `Idempotency-Key`
- [ ] Endorsement payload accepts: `endorsementReasonCode`, `endorsementReasonDetail` (optional), `effectiveDate`, partial `profileChanges` (e.g., coverage edits, premium change, carrier change), new `coverageLines[]` (replacement set for the new version)
- [ ] Reason codes validated at API layer: `CoverageChange`, `LimitChange`, `DeductibleChange`, `AdditionalInsured`, `PremiumAdjustment`, `AddressChange`, `NamedInsuredChange`, `Other`
- [ ] `EndorsementNumber` monotonic within a policy (enforced via ordered insert under row lock or unique constraint + retry)
- [ ] New `PolicyVersion` (`VersionReason=Endorsement`) with full snapshots
- [ ] New `PolicyCoverageLine` rows materialized under the new version
- [ ] `Policy.CurrentVersionId` updated atomically; `Policy.TotalPremium` synchronized when premium is endorsed
- [ ] Timeline event `policy.endorsed` with endorsement id and reason
- [ ] ABAC `policy:endorse` enforced
- [ ] All writes in a single transaction (endorsement, version, coverages, policy update, timeline, workflow transition)

## Data Requirements

- See PRD "Entity: PolicyEndorsement", "Entity: PolicyVersion", "Entity: PolicyCoverageLine"
- Request payload carries full replacement coverage set for the new version (additive/delta authoring is a follow-up)
- Response returns: new endorsement id, new version id, new version number, new current coverages, updated `Policy.TotalPremium` + `RowVersion`

**Validation Rules:**
- Cannot change `PolicyNumber` via endorsement (frozen)
- Cannot change `AccountId` via endorsement (frozen)
- Cannot change `EffectiveDate` via endorsement (anchors the policy term)
- CAN change `ExpirationDate` via endorsement (extension / shortening); `NewExpirationDate > EffectiveDate` required
- CAN change `LineOfBusiness` only when zero `PolicyCoverageLine` changes accompany it (LOB-coherent coverage validation is a follow-up)
- CAN change `CarrierRefId` / `CarrierName` (carrier replacement mid-term is permitted for servicing flexibility)
- Every endorsement must result in at least one field or coverage difference from the prior version

## Role-Based Visibility

| Role | Endorse |
|------|---------|
| Distribution User | No |
| Distribution Manager | No |
| Underwriter | Yes (scoped) |
| Relationship Manager | No |
| Program Manager | No |
| Admin | Yes |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: endorsement commit p95 ≤ 1 s for policies with ≤ 50 coverage lines
- Reliability: all writes transactional; rollback on any failure leaves the policy unchanged
- Idempotency: same `Idempotency-Key` returns the prior result (no duplicate endorsement / version)
- Security: Casbin `policy:endorse` gate; every endorsement audited

## Dependencies

**Depends On:**
- F0018-S0002 (policy exists), F0018-S0003 (detail page as entry point), F0018-S0005 (version creation machinery)

**Related Stories:**
- F0018-S0004 (Endorsements rail), F0018-S0010 (timeline)

## Out of Scope

- Additive / delta coverage authoring (MVP requires full replacement set on the new version)
- Mid-term rating / re-pricing engine (MVP accepts user-supplied premium)
- Endorsement approval workflow (single-actor commit in MVP)
- Carrier-facing endorsement notices / paper trail export (deferred to F0027)
- Endorsement templates / saved endorsement types

## UI/UX Notes

- Entry point: "Endorse" button on Policy detail header (enabled only when `Status=Issued` and actor is authorized)
- Wizard-style flow: pick reason → pick effective date → edit coverages → review diff → commit
- Review step shows the computed diff against current version (leverages the Compare UI from S0005)
- Post-commit redirect lands on the new version in Policy 360

## Questions & Assumptions

**Assumptions:**
- Endorsements are committed by a single actor (Underwriter / Admin); approval workflow is a follow-up
- Premium edits on endorsement do not recompute taxes/fees (MVP stores premium as a single `TotalPremium`)
- Effective date of the endorsement anchors the new version; prior version remains the source of truth before that date

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (invalid state, empty endorsement, bad effective date, concurrency, idempotency)
- [ ] Permissions enforced (Underwriter + Admin only)
- [ ] Audit/timeline logged: Yes (`policy.endorsed`)
- [ ] Tests pass (including transactional rollback, monotonic endorsement number under concurrency, idempotency)
- [ ] Documentation updated (OpenAPI + reason-code list)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
