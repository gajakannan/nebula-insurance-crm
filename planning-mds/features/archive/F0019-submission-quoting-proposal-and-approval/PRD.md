---
template: feature
version: 1.1
applies_to: product-manager
---

# F0019: Submission Quoting, Proposal & Approval Workflow

**Feature ID:** F0019
**Feature Name:** Submission Quoting, Proposal & Approval Workflow
**Priority:** Critical
**Phase:** CRM Release MVP

## Feature Statement

**As an** underwriter or distribution user
**I want** to move submissions through quoting, proposal, approval, and bind decisions
**So that** Nebula supports the full commercial P&C submission journey instead of only intake

## Planning Decisions (Locked 2026-06-01)

Resolved at the plan clarification gate (G1) before story breakdown:

1. **Lifecycle scope — full through Bind.** This MVP covers the complete downstream path:
   `ReadyForUWReview → InReview → Quoted → BindRequested → Bound`, plus the `Declined` and
   `Withdrawn` terminal outcomes.
2. **Approval model — single authorized approver.** One authorized approver records an explicit
   grant/decline with a reason and authority metadata, captured as append-only history. The data
   model is shaped so maker-checker and authority-limit logic can be added later **without** a
   workflow rewrite, but neither is in this MVP.
3. **Archive/deactivate — in scope now.** F0019 owns the submission end-of-life contract that
   replaces F0006's descoped soft-delete, delivered as an explicit, audit-preserving
   archive/deactivate lifecycle action (not a generic delete).
4. **Packet — thin CRM coordination record (recorded, never computed).** See
   [Quote/Proposal Packet Contract](#quoteproposal-packet-contract-recorded-never-computed).
   F0019 is a CRM status/workflow layer, **not an underwriting workbench**.

## Personas

Primary actors (defined in `planning-mds/examples/personas/nebula-personas.md` and BLUEPRINT §3.2):

- **Underwriter** — reviews triaged submissions, prepares the quote/proposal packet, and records
  quote and bind decisions with traceability.
- **Underwriting approval authority** — an authorized approver (e.g., senior underwriter /
  underwriting manager) who grants or declines the approval checkpoint. Modeled as an
  authorization role (Phase B), not a separate persona archetype.
- **Distribution user** — moves submissions forward, coordinates broker responses, and tracks
  pipeline movement toward a decision.

## Business Objective

- **Goal:** Complete the core submission operating workflow inside Nebula.
- **Metric:** Quote turnaround time, approval-cycle time, quote-to-bind ratio, and workflow visibility.
- **Baseline:** Intake may exist, but the downstream quote and approval lifecycle is incomplete.
- **Target:** Users can manage submission progression from intake through decision with auditability.

## Problem Statement

- **Current State:** Submission handling is incomplete if Nebula stops at intake and triage.
- **Desired State:** Quote, proposal, approval, and final decision handling are structured and visible.
- **Impact:** Better underwriting control, faster status answers, and stronger workflow traceability.

## Scope & Boundaries

**In Scope:**
- Submission progression from triage to quote and bind decision (full lifecycle through Bind).
- Approval checkpoints and approval visibility (single authorized approver in MVP).
- Proposal or quote-package status handling (the submission-bound quote/proposal packet).
- Submission-bound quote/proposal packet record, status, and workflow evidence needed for approval and bind decisions.
- Final decision states (`Bound`, `Declined`, `Withdrawn`) and auditable workflow transitions.
- Submission archive/deactivate behavior for downstream-decided records that should leave active operational queues while preserving audit history.

**Out of Scope (Non-Goals):**
- **Any underwriting-workbench capability** — no rating engine, no premium/pricing calculation, no
  risk scoring, no quote comparison or illustration generation, no carrier rating round-trip.
  Quote figures are **recorded reference values**, never derived by Nebula (see boundary guardrail).
- Carrier system rating integration.
- Billing and issuance accounting.
- External broker self-service quoting.
- Reusable outbound template engine, COI generation, and generic ACORD document generation outside the submission-bound quote/proposal packet (owned by F0027).
- Physical purge or audit-destructive deletion of submission records.

**Boundary Guardrail — CRM workflow vs. underwriting workbench:**
- F0019 is a **CRM status/workflow coordination layer**. It records *what a quote is* and *where a
  submission sits in the workflow*; it never *produces* the quote.
- **Forbidden in F0019:** rating/pricing computation, premium derivation, risk/eligibility scoring,
  quote optimization or comparison engines, coverage-illustration generation, and any carrier
  rating integration. These are explicitly out of bounds for this feature and its successors that
  touch `workflow:submission`.
- **Recorded, never computed:** value-bearing quote fields (premium amount, limits, deductibles,
  effective dates, carrier/market) are entered by a user or captured from a document and validated
  only for presence/format — Nebula does not calculate them. Structured product/coverage data is
  sourced from **F0034** product-schema attributes, not re-modeled here.
- This guardrail is enforced through PRD non-goals, the packet contract below, capture/track/
  transition story acceptance criteria, a Phase B architecture decision, and a boundary regression
  expectation (no rating/calculation endpoint or computed-pricing field may exist).

**Boundary Guardrail with F0006:**
- F0006 owns the submission workflow only through `ReadyForUWReview`; it must continue to reject `ReadyForUWReview -> InReview` and all later transitions until F0019 is intentionally delivered.
- F0019 is the feature that activates downstream submission states beginning with `ReadyForUWReview -> InReview`.
- No F0006 closeout, tracker update, or UX polish should be interpreted as permission to expose downstream transitions early.
- The first refined F0019 implementation story (S0001) explicitly covers enabling downstream transitions, related authorization changes, UI exposure, and regression coverage proving the boundary moved deliberately rather than by drift.
- F0006 descoped its unimplemented submission soft-delete requirement; F0019 is therefore the owning feature for the replacement contract, refined here (S0006) as archive/deactivate behavior for terminal downstream states, not as an unrestricted CRUD delete route.

**Boundary Guardrail with F0027:**
- F0019 owns the submission-bound quote/proposal packet as workflow evidence: packet status, approval readiness, document completeness, and bind handoff.
- F0027 owns reusable outbound document generation: COI, generic ACORD output, reusable proposal templates, merge-field governance, rendering, and broader generated-document audit.
- F0019 must not wait for the full F0027 template engine to move submissions through quote and bind, but its packet artifacts should remain compatible with later F0027 rendering and storage patterns.

## Quote/Proposal Packet Contract (recorded, never computed)

The submission-bound quote/proposal packet is a **CRM coordination record**, not a quoting artifact:

| Element | What it holds | Source |
|---------|---------------|--------|
| `status` | Packet lifecycle (e.g., `Draft → ReadyForApproval → Approved → BindRequested`) | Nebula workflow |
| `linkedDocuments[]` | References to submission-parented documents (F0020 / ADR-012) | F0020 documents |
| `readiness signal` | Document completeness + approval state → "ready for approval / bind" | Derived from links + approval state (status only, not pricing) |
| `recorded reference facts` | Premium amount, limits, deductibles, effective dates, carrier/market | **Recorded** (manual entry or captured from a document) — never computed |
| structured coverage/product attributes | LOB-specific attribute values | **Reused from F0034** product-schema attributes, not re-modeled |

Rules:
- Value-bearing fields are validated for **presence/format only**. No calculation, rating, or scoring.
- The packet links F0020 documents; it does not store or render document binaries (F0027 owns rendering).
- The readiness signal reflects *coordination state* (docs present + approval granted), never a priced/eligibility computation.

## Success Criteria

- Users can track submission status all the way through quote and bind decision.
- Approval bottlenecks become visible and auditable.
- Submission workflow supports timely broker updates and internal accountability.
- No rating/pricing/scoring capability is introduced (boundary held).

## Risks & Assumptions

- **Risk:** Workflow scope expands into carrier-side processing, document generation, or rating/pricing too early.
- **Risk:** Teams accidentally expose downstream submission transitions while closing out F0006 because the `Submission` aggregate is shared across both features.
- **Risk:** "Quote packet" is misread as a quoting/rating tool, pulling underwriting-workbench scope into a CRM feature.
- **Risk:** Submission removal semantics remain ambiguous after F0006 descopes soft delete, leading to accidental introduction of a generic delete endpoint without lifecycle or audit rules.
- **Assumption:** Intake (F0006), policy (F0018), document (F0020), and product-schema (F0034) foundations exist and are available (all are done + archived).
- **Mitigation:** Keep scope centered on internal workflow orchestration and decision visibility; enforce the underwriting-workbench guardrail at PRD, story, and architecture levels.
- **Mitigation:** Treat `ReadyForUWReview -> InReview` enablement as an explicit F0019 deliverable (S0001) with dedicated tests and signoff, not as a passive side effect of shared workflow-state changes.
- **Mitigation:** Define submission end-of-life behavior explicitly as archive/deactivate semantics (S0006) tied to terminal states, list visibility, and audit retention.

## Dependencies

- F0006 Submission Intake Workflow (intake boundary owner; done + archived)
- F0018 Policy Lifecycle & Policy 360 (downstream policy creation/correlation after bind; done + archived)
- F0020 Document Management & ACORD Intake (packet's linked documents; done + archived)
- F0034 Product Schema Registry & Dynamic LOB Attributes (structured coverage/product attributes; done + archived; F0034-S0007 established the F0019 handoff)

F0019 depends on F0006 having preserved the intake boundary; downstream transitions still return `409 invalid_transition` until F0019 (S0001) deliberately replaces that behavior. F0019 owns the future submission archive/deactivate contract that replaces F0006's descoped soft-delete claim. F0027 is a future capability enhancer for reusable outbound generation, not a prerequisite for F0019's submission-bound quote/proposal packet workflow.

## Architecture & Solution Design

> Requirements-level intent only; the authoritative design is produced in Phase B (Architect).

### Solution Components

- Extend the submission domain with dedicated quoting, proposal, approval, and decision-handling services rather than treating everything as generic status changes.
- Add an approval component that supports a single underwriting approval checkpoint with decision ownership, designed to accommodate future maker-checker or authority-limit logic.
- Introduce a submission-scoped quote/proposal packet record that links documents, records reference facts, and exposes approval/readiness status — a coordination record, not a quoting calculator.
- Keep the quote/proposal packet model intentionally submission-scoped so it can later delegate reusable rendering and template management to F0027 without a workflow rewrite.
- Keep final policy issuance and billing creation as downstream handoff points rather than embedding them directly into this workflow module.

### Data & Workflow Design

- Expand the submission state machine to cover downstream states `InReview`, `Quoted`, `BindRequested`, `Bound`, `Declined`, and `Withdrawn`, with business-approved transitions and guards (per ADR-011).
- Activation of downstream transitions is part of F0019 delivery (S0001), not a prerequisite for it. Until F0019 implementation begins, the runtime must continue enforcing the F0006 intake-only transition set.
- Define what it means for a submission to leave active operational queues after downstream decisions: archive/deactivate is allowed only for terminal states and archived submissions remain discoverable for audit and reporting (S0006).
- Record approval requests, approval decisions, bind decisions, and workflow transitions as append-only history (per ADR-011) to preserve traceability and internal accountability.
- Store approval metadata such as approver, authority reason, decision timestamp, and blocking conditions as first-class records rather than free-form comments.
- Design for human-in-the-loop processing where workflows can wait on broker response, underwriter action, or approval authority without losing auditability.

### API & Integration Design

- Expose explicit transition, approval, quote-packet, and bind-action endpoints instead of allowing unrestricted field edits on the submission record.
- If F0019 reuses the existing submission transition endpoint, refinement must explicitly document the code, authorization, and UI changes that move the boundary beyond F0006. Reuse is allowed; implicit enablement is not (S0001).
- Submission archive/deactivate is an explicit lifecycle action with clear state preconditions and audit semantics — not a generic delete endpoint (S0006).
- Integrate with F0020 for proposal artifacts and document completeness, and with F0018 for downstream policy creation or policy-link correlation after bind.
- Emit workflow events for approval pending, approval granted, quote ready, bind requested, and final decision so notifications and reporting remain decoupled.
- Keep carrier rating, issuance accounting, rating/pricing computation, and external broker self-service outside the API contract.

### Security & Operational Considerations

- Enforce authorization with role, assignment, and approval-authority context so only authorized actors can move a submission through sensitive transitions.
- Maintain separation between ordinary workflow edits and approval decisions because those actions may have different audit and review requirements.
- Ensure archive/deactivate actions cannot erase workflow evidence and are constrained to clearly authorized users and business-valid terminal lifecycle states.
- Track workflow latency, approval-cycle time, and stuck-in-state conditions as first-class operational metrics.
- Ensure approval and bind actions are idempotent enough to handle retries or duplicate user submissions without double-processing the business outcome.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Quote/proposal packet record, approval checkpoint, and bind-decision handlers | PRD only (Phase B to confirm) |
| Reuses: Established Component/Pattern | CRM workflow state machine plus append-only workflow and approval audit history | [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) (Accepted) |
| Extends: Cross-Cutting Component | Quote and proposal artifacts rely on the shared document architecture | [ADR-012](../../architecture/decisions/ADR-012-shared-document-storage-and-metadata-architecture.md) (Accepted) |
| Reuses: Established Component/Pattern | Structured coverage/product attributes via product-schema registry | F0034 product-schema attributes (handoff: F0034-S0007) |

## Screen Layouts (ASCII)

UI-bearing feature. Primary screens extend F0006's submission surfaces. (Detailed visual design is a
frontend concern; these layouts capture required structure and the workflow controls.)

### Submission Detail — Downstream Workflow (Desktop)

```
┌───────────────────────────────────────────────────────────────────────────┐
│ Submission SUB-10472 · Acme Mfg · GL+Cyber          Status: [ Quoted ▾ ]    │
│ Owner: j.uw   Broker: Marsh   Effective: 2026-07-01      [ Archive ]        │
├───────────────────────────────┬───────────────────────────────────────────┤
│ Workflow                      │ Quote / Proposal Packet                     │
│  ● ReadyForUWReview           │  Status: ReadyForApproval                   │
│  ● InReview                   │  Documents (F0020):  3 linked · 1 missing   │
│  ◉ Quoted                     │   - ACORD 125 ✓   - Loss Run ✓   - SOV ⚠    │
│  ○ BindRequested              │  Recorded facts (entered/captured):         │
│  ○ Bound                      │   Premium $48,500 · Limit $1M · Ded $25k    │
│  ─ Declined / Withdrawn       │   (recorded — not calculated by Nebula)     │
│                               │  Readiness: ✓ docs  ✗ approval              │
│ Actions:                      │  [ Edit packet ]  [ Request approval ]      │
│  [ Move to BindRequested ]    ├───────────────────────────────────────────┤
│  [ Decline ] [ Withdraw ]     │ Approval                                    │
│  (gated by approval+readiness)│  State: Pending · Approver: —               │
│                               │  [ Grant ] [ Decline ]  (authorized only)   │
├───────────────────────────────┴───────────────────────────────────────────┤
│ Activity Timeline (append-only): InReview→Quoted by j.uw · packet updated…  │
└───────────────────────────────────────────────────────────────────────────┘
```

### Submission Detail — Downstream Workflow (Narrow / mobile)

```
┌─────────────────────────────┐
│ SUB-10472 · Acme Mfg        │
│ Status: [ Quoted ▾ ]        │
├─────────────────────────────┤
│ ▸ Workflow                  │
│   ◉ Quoted → BindRequested  │
│   [ Decline ] [ Withdraw ]  │
│ ▸ Packet  (ReadyForApproval)│
│   Docs 3/4 · Prem $48,500   │
│   [ Edit ] [ Request appr. ]│
│ ▸ Approval  Pending         │
│   [ Grant ] [ Decline ]     │
│ ▸ Timeline …                │
└─────────────────────────────┘
```

### Downstream Submission Pipeline List (Desktop)

```
┌───────────────────────────────────────────────────────────────────────────┐
│ Submissions  [Status: InReview|Quoted|BindRequested|Bound|Declined|Withdrawn▾]│
│ [ ] Approval pending   [ ] Stuck > SLA   [ ] Include archived                │
├──────────┬─────────────┬───────────┬────────────┬───────────┬──────────────┤
│ Ref      │ Insured     │ Status    │ Owner      │ Age (days)│ Approval      │
├──────────┼─────────────┼───────────┼────────────┼───────────┼──────────────┤
│ SUB-10472│ Acme Mfg    │ Quoted    │ j.uw       │ 4 ⚠       │ Pending       │
│ SUB-10468│ Globex      │ BindReq.  │ a.uw       │ 9 ⛔ stuck │ Approved      │
│ SUB-10455│ Initech     │ Bound     │ j.uw       │ —         │ Approved      │
│ SUB-10401│ Umbrella Co │ Declined  │ a.uw       │ —  (archd)│ —             │
└──────────┴─────────────┴───────────┴────────────┴───────────┴──────────────┘
```

(Narrow variant: single-column cards with Ref, Insured, Status pill, Age/stuck flag, Approval chip.)

## Related User Stories

Story breakdown (Phase A). MVP = full lifecycle through Bind + archive. See `STATUS.md` for the live checklist.

| Story | Title | Priority | Notes |
|-------|-------|----------|-------|
| [F0019-S0001](./F0019-S0001-activate-downstream-submission-workflow.md) | Activate downstream submission workflow | Critical | **Mandated first story** — moves the F0006 boundary deliberately; enables `ReadyForUWReview → InReview` + downstream state machine, authz, UI, regression. |
| [F0019-S0002](./F0019-S0002-submission-quote-proposal-packet-lifecycle.md) | Submission quote/proposal packet lifecycle | Critical | **Mandated packet story** — coordination record; recorded-not-computed; links F0020 docs; reaches `Quoted`; defers rendering to F0027. |
| [F0019-S0003](./F0019-S0003-underwriting-approval-checkpoint.md) | Underwriting approval checkpoint | Critical | Single authorized approver; grant/decline + reason + authority metadata; append-only approval history; gates bind. |
| [F0019-S0004](./F0019-S0004-bind-decision-and-policy-handoff.md) | Bind decision and policy handoff | Critical | `Quoted → BindRequested → Bound`; requires approval + readiness; idempotent; F0018 policy-creation handoff correlation. |
| [F0019-S0005](./F0019-S0005-decline-and-withdraw-terminal-decisions.md) | Decline and withdraw terminal decisions | High | `Declined` / `Withdrawn` transitions with required reason codes, guards, authz, audit. |
| [F0019-S0006](./F0019-S0006-submission-archive-and-deactivate.md) | Submission archive and deactivate | High | **Mandated archive story** — terminal-state-only, explicit lifecycle action (not delete), list visibility, audit retention; replaces F0006 soft-delete. |
| [F0019-S0007](./F0019-S0007-downstream-submission-pipeline-list-and-workflow-visibility.md) | Downstream submission pipeline list & workflow visibility | High | Downstream status filtering, approval-pending + stuck-in-state visibility, archived discoverability. |
| [F0019-S0008](./F0019-S0008-downstream-submission-workflow-timeline-and-audit-trail.md) | Downstream submission workflow timeline & audit trail | Medium | Surfaces all downstream transitions, approval/bind decisions, packet changes, and archive actions on the submission timeline (append-only). |
