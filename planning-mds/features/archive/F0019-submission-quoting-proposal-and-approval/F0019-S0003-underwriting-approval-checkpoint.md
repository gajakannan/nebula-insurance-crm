# Underwriting Approval Checkpoint

## Story Header

**Story ID:** F0019-S0003
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Underwriting approval checkpoint
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriting approval authority
**I want** to grant or decline approval on a quoted submission with a recorded reason
**So that** bind decisions are gated by an accountable, auditable approval

## Context & Background

The MVP approval model is a **single authorized approver** (G1 decision Q2). One authorized approver
records an explicit grant or decline with a reason and authority metadata; the decision is stored as
first-class, append-only history — not a free-form comment. Approval is a prerequisite for bind
(F0019-S0004). The data is shaped so maker-checker and authority-limit logic can be added later
without a workflow rewrite, but neither is in this MVP.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission in `Quoted` with a `ReadyForApproval` packet
- **When** an authorized approver grants approval with a reason
- **Then** an append-only `ApprovalDecision` record is created (approver, decision=`Granted`, reason, authority metadata, timestamp), the packet status becomes `Approved`, and an `ActivityTimelineEvent` ("approval granted") is appended.

**Decline path:**
- **Given** the same precondition
- **When** the approver declines with a required reason
- **Then** an append-only `ApprovalDecision` (`Declined`, reason) is recorded, an "approval declined" timeline event is appended, and bind remains blocked. (Whether the submission moves to `Declined` is owned by S0005; this story records the approval outcome.)

**Alternative Flows / Edge Cases:**
- Approval attempted before `Quoted` / packet not ready → `409` (nothing to approve).
- Unauthorized actor attempts grant/decline → `403 Forbidden`; no record.
- Grant without a packet that meets readiness → `409`.
- Decline without a reason → `400` (reason required).
- Duplicate grant on an already-`Approved` packet → `409` (idempotent; no second decision record).

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → Approval → "Grant" / "Decline" | Click, enter reason | Enabled when submission = `Quoted`, packet `ReadyForApproval`, actor is approval authority | `ApprovalDecision` appended; packet `Approved` on grant | Reload shows approval state + approver; timeline shows decision; bind action unlocks on grant | Approval authority only; not the same plain underwriter unless they hold the authority role |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — the decision persists and gates bind after reload.
- [ ] Decline requires a reason; grant requires packet readiness; failures return `400`/`409`.
- [ ] A decision appends append-only audit/timeline records (approver, reason, authority metadata).
- [ ] Tests prove an authorized approver grants approval and an unauthorized user is denied (`403`).

## Data Requirements

**Required Fields (ApprovalDecision):**
- `submissionId`, `decision` (`Granted | Declined`), `approverUserId`, `reason`, `decidedAt`
- `authorityContext`: metadata describing the basis of authority (role/assignment); shaped to extend to limits later

**Optional Fields:**
- `blockingConditions[]`: recorded conditions that must clear before bind (informational in MVP)

**Validation Rules:**
- One effective decision per approval cycle; records are append-only (re-approval after change is a new cycle, Future).
- Reason required for decline; authority context required for grant.

## Role-Based Visibility

**Roles that can grant/decline approval:**
- Approval authority (senior underwriter / underwriting manager role) — permitted via ABAC `submission:approve`
- Admin — permitted
- Underwriter (without approval authority) — may request approval; cannot grant own work
- Distribution user / BrokerUser — no access

**Data Visibility:**
- InternalOnly content: approval decisions, approver identity, reasons, authority metadata.
- ExternalVisible content: none.

## Non-Functional Expectations

- Performance: decision recorded < 500ms p95.
- Security: separate `submission:approve` authorization, distinct from ordinary `submission:transition`; unauthorized → `403`.
- Reliability: decision record + packet status + timeline event are atomic; retries are idempotent.

## Dependencies

**Depends On:**
- F0019-S0002 — a `ReadyForApproval` packet to approve.
- ADR-011 — append-only history pattern.

**Related Stories:**
- F0019-S0004 — bind requires a granted approval.
- F0019-S0005 — a decline may precede a terminal `Declined` decision.

## Business Rules

1. **Single authorized approver (MVP):** one approver records the decision; maker-checker/authority-limits are Future and must not be implemented now.
2. **Separation of action types:** approval is a distinct authorization from ordinary workflow edits and carries its own audit expectations.
3. **First-class decisions:** approver, reason, authority metadata, and timestamp are structured records, not comments.
4. **No computation:** approval records a human decision; it performs no eligibility/rating scoring.

## Out of Scope

- Maker-checker / separation-of-duties enforcement and authority-limit thresholds (Future).
- The bind action itself (F0019-S0004) and terminal `Declined` transition (F0019-S0005).
- Any automated approval, eligibility scoring, or rating (boundary guardrail).

## UI/UX Notes

- Screens involved: Submission Detail (Approval panel with Grant/Decline + reason).
- Key interactions: approval state chip, grant/decline with required reason, bind unlocks on grant.

## Questions & Assumptions

**Open Questions:**
- [ ] Should an underwriter be allowed to approve their own quoted submission in MVP if they hold the authority role? (Default assumption: allowed in MVP single-approver model; revisit when maker-checker lands.)

**Assumptions (to be validated):**
- A distinct approval-authority role/permission exists or is added in Phase B authorization deltas.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`400`/`409`/`403`)
- [ ] Permissions enforced (distinct `submission:approve`)
- [ ] Audit/timeline logged (ApprovalDecision + timeline event)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
