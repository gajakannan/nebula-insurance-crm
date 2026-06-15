# Plan Review Report

Scope: feature **F0019 — Submission Quoting, Proposal & Approval Workflow**
Run ID: 2026-06-02-c25391e4
Date: 2026-06-02
Review Question: **Is this plan ready to build?**

## Decision

- **Status: CONDITIONALLY READY**
- **Rationale:** The F0019 plan is substantively build-ready. All 8 stories carry specific,
  testable acceptance criteria with interaction contracts on every mutation story, edge cases
  expressed as HTTP status codes, role-based visibility, and quantified NFRs. ADR-025 fully
  specifies the downstream state machine, data model, API contract (method/route/Casbin
  action/status codes/concurrency), authorization deltas, and the CRM-not-workbench boundary.
  `policy.csv` and the authorization matrix already carry the new `submission:approve` /
  `submission:archive` actions. All five plan-review validators pass (exit 0) and the knowledge
  graph is green with no drift. **However**, three trackers a `feature.md` run reads first —
  ADR-025 (`Status: Proposed`), `STATUS.md` (`awaiting G5 architecture approval`), and `README.md`
  (`architecture pending`) — all contradict the recorded **G5 Phase B approval** (plan run
  `2026-06-01-2ac02e13`, user-approved 2026-06-01T22:55, closeout exit-validation PASS). Because
  `feature.md`'s documented prerequisite is *"architecture signed off"* and these labels currently
  read as not signed off, this is a single **high** finding → CONDITIONALLY READY.
- **Next Action:** Apply the three label fixes (Architect flips ADR-025 → **Accepted**; PM refreshes
  `STATUS.md` overall-status and `README.md` status to reflect the recorded G5 approval) via a
  targeted owning-role rework, **or** capture explicit user risk acceptance. Then start
  `feature.md` Step 0 (G0 assembly planning), where the OpenAPI YAML endpoints are finalized.

---

## Findings By Severity

### Critical
- None.

### High
- **[high] Stale approval-state across three trackers contradicts the recorded G5 Phase B approval.**
  - Location: `ADR-025-…archive.md:3` (`**Status:** Proposed`); `F0019…/STATUS.md:3`
    (`Overall Status: … awaiting G5 architecture approval`); `F0019…/README.md:3`
    (`Status: Planning — Phase A refined (architecture pending)`).
  - Impact: The plan run `2026-06-01-2ac02e13` `gate-decisions.md` records **G5 PHASE B APPROVAL =
    APPROVED** (user, 2026-06-01T22:55) plus a full closeout exit-validation PASS. `feature.md`'s
    prerequisite is *"plan action completed (stories + architecture signed off)."* As written, the
    three trackers assert the architecture is **not** signed off — a fresh implementer or
    orchestrator doing the prerequisite check would be blocked or misled, even though the design is
    complete and approved. This is a governance/provenance contradiction, not missing design.
  - Owner: Architect (ADR-025 status) + Product Manager (STATUS/README status).
  - Recommendation: Flip ADR-025 `Status` to **Accepted** (ADR-011/ADR-012 are Accepted; ADR-025 is
    the governing decision for an approved plan); set `STATUS.md` overall-status to "Phase A + Phase B
    complete and approved (G5)"; set `README.md` status to architecture-complete. No code/design
    change required.

### Medium
- **[medium] ADR-025 `Related ADRs` / lifecycle hygiene — confirm ADR-011 follow-up linkage on acceptance.**
  - Location: `ADR-025-…archive.md` "Follow-up" (`ADR-011 Follow-up references this ADR …`).
  - Recommendation: When ADR-025 is accepted, verify the ADR-011 follow-up back-reference is in place
    so the state-machine lineage is navigable. Defer-able to the same edit that flips status. Owner: Architect.
- **[medium] S0008 INVEST "Independent" warning is unannotated.**
  - Location: `F0019-S0008-…audit-trail.md`; `validate-stories.py` warning (artifacts/validate-stories.txt).
  - Impact: Benign — S0008 is a read-only consolidation/visibility slice that surfaces events
    *emitted by* S0001–S0006, so it legitimately sequences after them. But the unannotated warning
    invites a reviewer to re-litigate it each cycle. Recommendation: add a one-line note in the story
    acknowledging the intentional surfacing-after-emit sequencing. Owner: Product Manager.
- **[medium] Cross-tracker status drift is a repeating pattern worth a closeout convention.**
  - Location: STATUS.md / README.md / ADR status labels (see High finding).
  - Recommendation: Treat "refresh feature-local status labels + governing ADR status" as part of the
    plan closeout step so approval state never lags the recorded gate decision. Owner: Product Manager.

### Low
- **[low] New OpenAPI endpoints not yet in `nebula-api.yaml` (by design).**
  - Location: `planning-mds/api/nebula-api.yaml` (only `/submissions/{id}/transitions` present);
    contract specified in `ADR-025` "API Contract" table; deferral recorded in plan run
    `2026-06-01-2ac02e13/artifact-trace.md` "Omissions And Waivers".
  - Impact: **Not build-blocking and not a downgrade of build-critical detail** — the full design
    contract (method, route, Casbin action, status codes, `If-Match`/`RowVersion`→412, ProblemDetails)
    exists in ADR-025; an implementer would not invent it. The project convention (and `feature.md`
    G0) finalizes the YAML wiring when code exists. Recommendation: finalize `quote-packet`,
    `approval`, `bind`, `archive`, `reactivate` paths in `nebula-api.yaml` at `feature.md` G0.
    Owner: Architect (at feature.md G0).
- **[low] Pre-existing out-of-scope KG warning.**
  - Location: `kg/validate.py` output — low-confidence inferred edge (0.4) `feature:F0028` in
    `feature:F0018.depends_on` (artifacts/kg-validate.txt).
  - Impact: Not introduced by F0019; does not block. Recommendation: separate KG-hygiene pass. Owner: Architect.
- **[low] Endpoint/event canonical KG nodes finalized at implementation (by design).**
  - Location: ADR-025 "Follow-up" and feature-mappings; `capability:submission-workflow` carries the
    boundary rationale now. Recommendation: bind endpoint/event canonical nodes + `code-index.yaml`
    globs at `feature.md` G7 reconciliation. Owner: Architect (at feature.md G7).

---

## Product Readiness
- **Requirements quality:** Strong. PRD has explicit user value, locked scope decisions (2026-06-01),
  in-scope/out-of-scope lists, a dedicated CRM-vs-underwriting-workbench guardrail, and a
  recorded-never-computed packet contract table. Non-goals are explicit (no rating/pricing/scoring,
  no carrier integration, no doc generation — delegated to F0027).
- **Story testability:** Strong. 8/8 stories pass `validate-stories.py`. Each has Given/When/Then
  happy paths, alternative/edge flows as concrete HTTP codes (`400`/`403`/`404`/`409`/`412`),
  data requirements, validation rules, role-based visibility, and quantified NFRs (e.g. transition
  < 500ms p95, packet save < 700ms p95, list < 1s p95).
- **Mutation contracts:** Strong. Every mutation story (S0001–S0006) includes the required
  Interaction Contract table (entry point, action, editable state, save/mutation result,
  reload/persistence evidence, role/status constraints) with explicit "render-only cannot satisfy
  this story" checks. No "display or capture / view or edit / manage" ambiguity remains. Read-only
  stories (S0007, S0008) correctly declare `N/A` with justification.
- **UI/screen readiness:** Adequate. PRD `Screen Layouts (ASCII)` covers Submission Detail
  (desktop + narrow) and the Downstream Pipeline List; stories reference the specific panels and controls.
- **Tracker state:** Mixed. `REGISTRY.md` consistent (F0019 Planned). **`STATUS.md` overall-status
  and `README.md` status are stale** vs the recorded G5 approval (High finding). `validate-trackers.py`
  passes (0/0) because it checks links/paths/counts, not prose status labels.

## Architecture Readiness
- **API/schema readiness:** Ready at design level. ADR-025 carries the full API contract table;
  referenced schemas (`submission*.schema.json`, `activity-event-payloads`, `line-of-business`,
  `submission-transition-request`) exist. New OpenAPI *paths* are deliberately deferred to feature.md
  G0 (Low finding) — the design contract is complete, so no invention is required to start.
- **Data/workflow readiness:** Strong. ADR-025 §1 defines the full transition table with guards and
  app-layer roles; §"Data Model" adds `SubmissionQuotePacket` (1:1, RowVersion), append-only
  `SubmissionApprovalDecision`, and `Submission.IsArchived/ArchivedAt/ArchivedByUserId` (distinct from
  `IsDeleted`), reusing existing `WorkflowTransition` + `ActivityTimelineEvent`. Downstream states are
  already declared on `workflow:submission`.
- **Authorization readiness:** Strong. Two new Casbin actions (`submission:approve`,
  `submission:archive`) are present in `policy.csv` (Underwriter, Admin) and documented in
  `authorization-matrix.md` §2.8; per-transition role refinement is correctly placed at the app layer.
- **ADR and NFR readiness:** Decisions captured in ADR-025 (governing) applying ADR-011 + ADR-012;
  NFRs are measurable (latency p95 targets, idempotency, atomicity, archive reversibility). **ADR-025
  status label "Proposed" is stale** vs the recorded approval (contributes to the High finding).
- **KG/ontology alignment:** Green. `lookup.py F0019` returns high-confidence (1.0) mappings;
  `kg/validate.py` and `--check-drift` both PASS. New shared semantics (`entity:submission-quote-packet`,
  `entity:submission-approval-decision`, `capability:submission-workflow` carrying the boundary
  rationale, `policy_rule:submission-approve`, `policy_rule:submission-archive`, `adr:025`) are bound.
  Raw artifacts and KG agree.

## Buildability Challenge
- **Vertical slice size:** Reasonable for a feature action delivered in story order. S0001 (boundary
  activation) is the correct mandated first slice; subsequent stories layer cleanly. Each story is an
  independently testable increment.
- **Role handoffs:** Inferable without conflict. Backend (services per README C4: SubmissionWorkflow,
  QuotePacket, Approval, Bind, Archive), Frontend (Submission Detail panels + pipeline list + timeline),
  QE (state-machine, idempotency, boundary regression, authz), Security (approve/archive deltas +
  audit-bearing decision record — already marked Required=Yes in STATUS), DevOps (No — no new infra).
- **Testability:** Strong. ACs map to unit (guards/validation), integration (endpoint + authz +
  `409`/`403`/`412`), component (panels/controls), E2E (transition→packet→approval→bind happy path),
  and a named **boundary regression** (S0001 updates the F0006 `409` guard test deliberately; S0006
  asserts no generic/physical delete route; "no rating/computed-pricing field" assertion).
- **Dependency and sequencing clarity:** Explicit. Story `Depends On` sections + KG `depends_on`
  (F0006/F0018/F0020/F0034, all done+archived) + the F0006 boundary guardrail and the F0018 bind
  handoff seam are all called out. S0007/S0008 correctly depend on the mutation stories that emit
  their data.
- **Risk hotspots:** Identified. (1) The shared `Submission` aggregate / `workflow:submission` boundary
  with F0006 — mitigated by S0001's deliberate-move regression. (2) The F0018 bind-handoff
  eventual-consistency seam — ADR-025 §4 specifies bind is not rolled back on downstream failure;
  handoff recorded pending/retryable. (3) The recorded-never-computed boundary — enforced at 6 levels
  incl. a regression. No undisclosed high-blast-radius surprises.

## Validation Evidence
- `python3 agents/product-manager/scripts/validate-stories.py {FEATURE_PATH}`: **PASS** (exit 0) —
  8/8 stories valid; 1 non-blocking INVEST "Independent" warning on S0008 (expected; read-only
  consolidation slice). → `artifacts/validate-stories.txt`
- `python3 agents/product-manager/scripts/validate-trackers.py`: **PASS** (exit 0) — errors 0,
  warnings 0; feature-evidence validation passed (validated=2). → `artifacts/validate-trackers.txt`
- `python3 {PRODUCT_ROOT}/scripts/kg/validate.py`: **PASS** (exit 0) — 144 code bindings, 2522 symbols;
  1 pre-existing out-of-scope warning (F0028 low-confidence edge). → `artifacts/kg-validate.txt`
- `python3 {PRODUCT_ROOT}/scripts/kg/validate.py --check-drift`: **PASS** (exit 0) — no drift errors.
  → `artifacts/kg-validate-drift.txt`
- `python3 agents/scripts/validate_templates.py`: **PASS** (exit 0) — prompt templates align with
  action contracts. → `artifacts/validate-templates.txt`
- `python3 agents/product-manager/scripts/validate-feature-evidence.py`: **SKIPPED** — not applicable;
  plan-review produces no feature evidence package (owned by feature.md/build.md).

## Artifact Trace
See `artifact-trace.md` for the full read/created/edited inventory. Key sources reviewed (raw artifacts,
which win over KG):
- `{FEATURE_PATH}/PRD.md`, `README.md`, `STATUS.md`, `GETTING-STARTED.md`, `F0019-S0001…S0008`
- `planning-mds/architecture/decisions/ADR-025` (+ ADR-011, ADR-012), `architecture/SOLUTION-PATTERNS.md`
- `planning-mds/api/nebula-api.yaml`, `security/policies/policy.csv`, `security/authorization-matrix.md`, `schemas/`
- `scripts/kg/lookup.py F0019`; prior plan run `2026-06-01-2ac02e13` gate-decisions/artifact-trace (audit input)
- No artifact outside this run folder was edited.
