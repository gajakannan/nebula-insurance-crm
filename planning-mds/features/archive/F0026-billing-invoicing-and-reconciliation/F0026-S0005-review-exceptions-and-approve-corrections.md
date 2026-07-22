# F0026-S0005: Review exceptions and approve correction adjustments

**Story ID:** F0026-S0005
**Feature:** F0026 — Billing, Invoicing & Reconciliation
**Title:** Review exceptions and approve correction adjustments
**Priority:** Critical
**Phase:** Brokerage Platform Expansion

## User Story

**As a** Finance Operations Analyst or Finance Manager
**I want** to investigate reconciliation exceptions and control balance-affecting corrections through separate request and approval actions
**So that** operational billing errors can be corrected without automatic write-offs or untraceable edits

## Context & Background

Non-exact receipt/invoice pairs stay unapplied. Analysts may correct non-balance links/data directly where authorized or request an operational balance correction. A different Finance Manager must approve or reject any balance-affecting request. Corrections never post a ledger entry or mutate policy premium/expected commission.

## Acceptance Criteria

**Happy Path — Analyst Request:**
- **Given** I am an authorized Finance Operations Analyst viewing an open exception
- **When** I enter correction amount, reason, effective date, and supporting source note, then Submit for Approval
- **Then** one pending correction request is persisted with requester, source records, proposed before/after operational values, and audit event

**Happy Path — Manager Decision:**
- **Given** I am an authorized Finance Manager other than the requester
- **When** I approve or reject the pending request with a decision note
- **Then** one terminal decision is persisted
- **And** approval applies only the stated F0026 operational correction, while rejection changes no balance
- **And** the exception/detail/audit views show requester, decider, decision, reason, time, and before/after values after reload

**Alternative Flows / Edge Cases:**
- Requester attempts own decision -> deny and leave request pending.
- Missing correction fields or decision note -> show validation feedback and do not transition.
- Request is already decided or source row version changed -> return conflict and display current state.
- Proposed result is negative or represents a write-off/tolerance path -> reject as unsupported first-release behavior.
- Analyst corrects an eligible non-balance reference -> save correction with audit evidence; no manager approval is required because balance is unchanged.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|------------------------|-------------------------------|----------------------------|
| Exception Review -> Request Correction | Enter proposed correction fields and Submit for Approval | Enabled for Finance Operations Analyst on open authorized exception | Pending balance-correction request saved; no balance changed yet | Reload exception and observe pending request plus request audit event | Requester cannot decide same request |
| Exception Review -> Manager Decision | Approve or Reject with note | Enabled for Finance Manager other than requester while request pending/current | Approval applies stated operational correction; rejection preserves prior balance; request becomes terminal | Reload invoice/exception and observe decision, before/after values, actors, and audit events | Finance Manager only; stale/terminal/self decisions denied |
| Exception Review -> Correct Reference | Correct eligible non-balance link/source note and Save | Enabled for Finance Operations Analyst when no balance field changes | Reference correction saved and exception re-evaluated | Reload and observe corrected link/source plus audit event | No balance/tolerance/write-off change permitted |

- [x] Render-only behavior cannot satisfy the story.
- [x] Validation, stale, and self-approval errors are specified.
- [x] Every successful mutation has immutable audit evidence.
- [x] Reload proves pending and terminal states plus correction effects.

## Data Requirements

**Required Fields:** Exception id/type; request id; proposed correction amount; reason; effective date; supporting note; requester/time; decision/note; decider/time; before/after operational values; row version.

**Optional Fields:** Corrected receipt/invoice reference; related mock-import batch/row.

**Validation Rules:** Request fields complete; decision note required; requester differs from decider; one terminal decision; no negative operational result; unsupported tolerance/write-off path rejected; optimistic concurrency enforced.

## Role-Based Visibility

- Finance Operations Analyst: investigate, correct eligible non-balance data, request balance correction.
- Finance Manager: read all authorized context and approve/reject another user's pending request.
- Distribution/Relationship and external roles: no exception/request/decision detail.

## Non-Functional Expectations

- Security: separation of duties is server-enforced and cannot be bypassed by direct API use.
- Reliability: approval applies correction and audit evidence atomically.
- Auditability: source facts remain linked and unchanged; all before/after operational values are retained.

## Dependencies

**Depends On:** F0026-S0002 through F0026-S0004; F0018/F0025 source context remains read-only.

**Related Stories:** F0026-S0006 surfaces pending and aging exceptions.

## Business Rules

1. A correction requester cannot decide the same request.
2. Approval affects only the explicitly proposed F0026 operational value.
3. Corrections do not mutate policy premium/version or expected-commission records.
4. First release has no write-off or automatic tolerance classification.

## Out of Scope

- Credit memo, refund, chargeback, collection, or write-off workflow.
- General-ledger journal entry or bank settlement.
- Multi-step approval chain beyond requester plus one different manager.

## Questions & Assumptions

**Open Questions:** None blocking for Phase A approval.

**Assumptions:** Phase B defines exception taxonomy, correction state names, concurrency token, and exact policy action identifiers.

## Definition of Done

- [ ] Request, approve, reject, non-balance correction, self-denial, and stale cases pass
- [ ] Permissions and separation of duties enforced
- [ ] Approved correction and audit events persist atomically
- [ ] No write-off/tolerance/ledger mutation path exists
- [ ] Tests pass
- [ ] Story filename matches `F0026-S0005`
- [ ] Story index regenerated

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
