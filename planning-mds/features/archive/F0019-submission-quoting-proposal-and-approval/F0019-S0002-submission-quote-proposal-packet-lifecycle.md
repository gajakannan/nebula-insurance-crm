# Submission Quote/Proposal Packet Lifecycle

## Story Header

**Story ID:** F0019-S0002
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Submission quote/proposal packet lifecycle
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter
**I want** to assemble and track a submission's quote/proposal packet and mark the submission quoted
**So that** approval and bind decisions have a coherent, auditable working set of documents and recorded terms

## Context & Background

The submission-bound quote/proposal packet is the **mandated packet story**. It is a **CRM
coordination record**, not a quoting calculator: it holds packet status, links submission-parented
documents (F0020 / ADR-012), and **records** reference terms (premium, limits, deductibles,
effective dates, carrier/market) that a user enters or captures from a document. Nebula never
computes these values. Reusable rendering, templates, COI, and ACORD generation are explicitly
deferred to F0027. Bringing the packet to a ready state drives `InReview -> Quoted`.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission in `InReview`
- **When** an authorized underwriter creates/updates its quote packet (links documents, records terms) and marks it ready
- **Then** the packet `status` advances (`Draft -> ReadyForApproval`), the submission transitions `InReview -> Quoted`, and an `ActivityTimelineEvent` ("quote ready") plus the `WorkflowTransition` are appended atomically.

**Packet contents (recorded, never computed):**
- Linked documents: references to submission-parented documents via F0020; the readiness signal reflects document completeness, not pricing.
- Recorded reference facts: premium amount, limits, deductibles, effective dates, carrier/market — validated for **presence/format only**.
- Structured coverage/product attributes are sourced from F0034 product-schema attributes, not re-modeled here.

**Alternative Flows / Edge Cases:**
- Mark-ready attempted with required documents missing → rejected with `409` / validation error listing missing items; submission stays `InReview`.
- Recorded premium in a non-numeric/!currency format → `400` validation error ("premium must be a recorded amount"); no calculation is attempted.
- Unauthorized actor edits the packet → `403 Forbidden`.
- Packet edit on a submission past `Quoted` (e.g., `Bound`) → rejected (read-only after bind); `409`.
- Submission not found → `404`.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → Packet → "Edit packet" | Link documents, record terms, Save | Editable while submission ∈ {`InReview`,`Quoted`} and actor authorized | Packet fields persisted; readiness recomputed (status only) | Reload shows recorded terms + linked docs; timeline shows "packet updated" | Underwriter/Admin; read-only once `Bound` |
| Submission Detail → Packet → "Mark ready / Move to Quoted" | Click | Enabled when packet readiness = docs complete | Packet `ReadyForApproval`; submission `InReview -> Quoted` + audit | Reload shows `Quoted`; timeline shows transition | Underwriter/Admin |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — recorded terms and links persist and survive reload/query invalidation.
- [ ] Save path validates presence/format only (no computation); failures return `400`/`409`.
- [ ] A successful save/transition appends append-only timeline/audit records.
- [ ] Tests prove an authorized user records terms, links a document, marks ready, and sees `Quoted` after reload.

## Data Requirements

**Required Fields (packet):**
- `submissionId`: parent submission (1:1 packet per submission in MVP)
- `status`: `Draft | ReadyForApproval | Approved | BindRequested` (packet-level coordination status)
- `linkedDocumentRefs[]`: references to F0020 submission-parented documents

**Optional / Recorded Fields (reference values, not computed):**
- `recordedPremiumAmount`, `recordedLimits`, `recordedDeductibles`, `effectiveDate`, `carrierMarket`

**Validation Rules:**
- Value-bearing fields validated for presence/format only; **no rating/calculation**.
- Mark-ready requires document completeness per F0020 completeness signal.
- Packet becomes read-only once the submission is `Bound`.

## Role-Based Visibility

**Roles that can edit the packet / mark quoted:**
- Underwriter — permitted (ABAC `submission:transition` for the `Quoted` move + packet edit permission)
- Admin — permitted
- Distribution user — view packet status; cannot record terms or mark quoted
- BrokerUser — no access

**Data Visibility:**
- InternalOnly content: recorded terms, packet status, linked-document metadata.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: packet save < 700ms p95; readiness recompute is a status check (no heavy computation).
- Security: authorized roles only; unauthorized edits → `403`.
- Reliability: packet save + any state transition + audit records are atomic.

## Dependencies

**Depends On:**
- F0019-S0001 — downstream states must exist (`InReview`, `Quoted`).
- F0020 Document Management — linked submission-parented documents + completeness signal.
- F0034 Product Schema Registry — structured coverage/product attributes (handoff F0034-S0007).
- ADR-012 — shared document storage/linkage contract.

**Related Stories:**
- F0019-S0003 — approval consumes the ready packet.
- F0027 (future) — reusable rendering/templates for packet output.

## Business Rules

1. **Recorded, never computed:** the packet stores reference terms entered or captured; Nebula performs no rating, pricing, or scoring (CRM boundary guardrail).
2. **Submission-scoped packet:** the packet is owned by F0019 and remains compatible with later F0027 rendering; it does not generate documents itself.
3. **Quoted requires a coordination-ready packet:** docs complete + terms recorded, not a priced/eligibility computation.

## Out of Scope

- Reusable rendering, templates, COI, generic ACORD generation (F0027).
- Any premium calculation, rating, eligibility scoring, or quote comparison (boundary guardrail).
- Approval decision (F0019-S0003) and bind (F0019-S0004).

## UI/UX Notes

- Screens involved: Submission Detail (Quote/Proposal Packet panel; "Edit packet", "Mark ready").
- Key interactions: link documents, record terms, completeness/readiness indicator, mark-quoted control.

## Questions & Assumptions

**Open Questions:**
- [ ] Is the packet strictly 1:1 with a submission in MVP, or can multiple proposal options coexist? (Default assumption: single packet per submission in MVP; multiple options is Future.)

**Assumptions (to be validated):**
- F0020 completeness signal is sufficient to gate "ready"; no new completeness logic is added by F0019.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`400`/`409`/`403`/`404`)
- [ ] Permissions enforced
- [ ] Audit/timeline logged (packet updates + `Quoted` transition)
- [ ] Recorded-not-computed boundary proven (no calculation path; presence/format validation only)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
