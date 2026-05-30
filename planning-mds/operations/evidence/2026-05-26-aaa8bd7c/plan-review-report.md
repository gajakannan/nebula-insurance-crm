# Plan Review Report

Scope: feature F0036 — Form Engine and Form-State Preservation (RHF + AJV + Widget Registry)
Run ID: 2026-05-26-aaa8bd7c
Date: 2026-05-26
Review Question: Is this plan ready to build?

> Independence note: this read-only audit re-derived its findings from raw source artifacts (backend validator code, ADRs, the shipped Cyber bundle, the current panel), not from the plan run's summaries or approval tokens — including challenging artifacts produced by the immediately-prior `plan` run (`2026-05-25-51ff2a92`). No plan, tracker, story, KG, schema, or architecture file was edited by this action.

## Decision

- **Status: NOT READY**
- **Rationale:** One **critical** build-readiness gap: the plan's central Workstream-A validation mechanism is not buildable as specified. F0036-S0003 and the ADR-021 amendment (§2–§3, written during the prior plan run) assert that the client reaches *0-disagreement backend parity* by "evaluating `rules.json` per ADR-023 (JsonLogic)." The shipped reality contradicts this: (a) the backend (`engine/src/Nebula.Application/Services/LobAttributeService.cs`) validates Cyber attributes — and the cross-field rules — with **hardcoded C#**, not by evaluating the bundle's schema/rules; (b) `rules.json` is **not loaded anywhere in code** (grep of `engine/` and `experience/` returns no consumer) and does **not** conform to ADR-023's JsonLogic envelope; (c) ADR-022/023's "two equivalent schema/rules-driven validators proven by parity tests" platform is, like ADR-021 was, **Accepted-but-unimplemented**. An implementer beginning S0003 cannot proceed without a new architecture decision about how to define and prove client/backend parity against a hardcoded backend, and F0036's stated "no backend change" scope constrains that decision. A compounding **high** gap (S0005 conditional gating) shares this root cause.
- **Next Action:** Repair via `plan.md` Architect Phase B rework (targeted — Workstream A only), then re-run `plan-review.md`. Workstream B (preservation) and the requirements/tracker/KG structure are otherwise build-ready.

```json
{
  "gate": "plan_review",
  "question": "is_this_plan_ready_to_build",
  "status": "not_ready",
  "findings": { "critical": 1, "high": 1, "medium": 2, "low": 1 },
  "can_start_feature_action": false,
  "requires_risk_acceptance": false,
  "available_actions": ["fix_findings", "cancel"]
}
```

## Findings By Severity

### Critical

- **[critical] Client/backend validation parity (S0003) is not buildable as specified; the parity mechanism in the plan rests on an unimplemented platform.**
  - Location: `planning-mds/features/F0036-dynamic-product-attribute-form-engine/F0036-S0003-schema-driven-rendering-ajv-parity.md` (AC "Backend parity" + "Cross-field rules (rules.json)"); `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md` Plan Amendment §2–§3; `acceptance-criteria-checklist.md` "Open Items Deferred to Phase B #2".
  - Evidence (raw artifacts win): `engine/src/Nebula.Application/Services/LobAttributeService.cs` hardcodes Cyber validation in C# — required-field checks, enum checks, and the cross-field rules `recordsHeld >= 1_000_000 → mfaEnabled required` (lines ~220-225) and the retention rule — referencing "the active Cyber schema bundle" by literal field names, **not** by evaluating `data-schema.json`/`rules.json`. A repo-wide grep (`rules.json`, `jsonlogic`, `json-logic`, `JsonLogic`) finds **zero** consumers in `engine/` or `experience/`. The shipped `cyber/1.0.0/rules.json` uses `{id, severity, when:"<infix string>", path, message}`, which does **not** match ADR-023's governed envelope `{id, code, pointer, severity, expression(JsonLogic), description}`. So `rules.json` is an unwired, non-conformant artifact; the backend's source of truth is imperative C#.
  - Impact: "The client evaluates `rules.json` for parity, 0 disagreements" is not implementable — there is no portable rules artifact both runtimes share, and the backend isn't schema/rules-driven. Achieving parity requires a new architecture decision (e.g. hand-port the C# Cyber rules into a shared TS module plus a parity-test harness against actual backend responses, OR expand scope to realize ADR-023's portable rules — which conflicts with F0036's "no backend change" boundary). This is a build-critical decision with no current owner-made resolution; the prior plan's ADR amendment documents a mechanism that does not exist.
  - Owner: Architect.
  - Recommendation: Rework ADR-021 amendment §3 to describe the *actual* parity strategy against the hardcoded backend within (or with an explicit expansion of) F0036 scope; restate S0003 ACs accordingly; either realize ADR-023 (separate, larger decision) or explicitly record that client rules are a hand-maintained TS port with a CI parity gate and accept the divergence risk. Re-run plan-review after rework.

### High

- **[high] S0005 "schema-driven, no hardcoded logic" cannot reproduce the MFA-maturity conditional from the frozen bundle.**
  - Location: `F0036-S0005-replace-cyber-panel-five-screen-regression.md` (AC "Conditional gating preserved"); ADR-021 amendment §4.
  - Evidence: the conditional "MFA maturity is required/enabled only when MFA enabled" is hardcoded in **both** runtimes — frontend `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx:135-136` (`required={!readOnly && value.mfaEnabled}`, `disabled={readOnly || !value.mfaEnabled}`) and backend `LobAttributeService.cs:210-211`. It is absent from `data-schema.json` (mfaMaturity is merely a nullable enum), from `ui-schema.json` (layout/labels only), and from `rules.json`. ADR-022's restricted profile also **forbids** `if/then/else` and `dependentRequired` in bundle schemas, so this conditional cannot be expressed in the data-schema even if the bundle were editable.
  - Impact: S0005 simultaneously requires "no hardcoded JSX logic," "no bundle change," and "no behavior regression" — mutually unsatisfiable for this conditional. The engine has no governed source from which to derive it.
  - Owner: Architect.
  - Recommendation: Decide and document the conditional-gating source (an engine-level governed conditional convention, a permitted additive `ui-schema` extension, or an explicit scoped exception that keeps a small declared conditional map) before S0005 is built. Shares root cause with the critical finding (bundle is not the authoritative source for Cyber behavior).

### Medium

- **[medium] Workstream B mutation-form inventory may be non-exhaustive, weakening the "closes F0035 finding #1 for ALL in-scope forms" claim.**
  - Location: `F0036-S0007-...md` / `F0036-S0008-...md` (inventory of 6 surfaces); PRD "Phase A Clarification Resolution #1".
  - Evidence: a mutation-form sweep (`onSubmit`/`handleSubmit`/`useMutation` in `experience/src`) surfaced `experience/src/pages/PoliciesPage.tsx` (has an `onSubmit` at line 94) in addition to the six named forms. The inventory was confirmed by naming six components, not by an exhaustive sweep for all dirty-able mutation forms (inline edits, detail-page edits, page-level forms).
  - Recommendation: Before claiming finding #1 fully closed, enumerate every dirty-able mutation form (sweep, not name-list) and either include or explicitly exclude each with a reason in S0007.

- **[medium] KG binding asserts a governed capability (`capability:lob-rules-governance` / ADR-023) that is not implemented in code — semantic drift the structural validators cannot catch.**
  - Location: `planning-mds/knowledge-graph/feature-mappings.yaml` `feature:F0036` `affects`/`governed_by` (added in the prior plan run); `canonical-nodes.yaml` `capability:lob-rules-governance`.
  - Evidence: `kg/validate.py --check-drift` exits 0 (paths/IDs resolve), but ADR-023's rules platform is unimplemented and `rules.json` is unwired (see critical). The binding therefore points F0036 at a capability whose code substrate does not exist.
  - Recommendation: Keep the binding but record the ADR-023 implementation gap as an explicit assumption/risk on F0036, so the feature action does not assume a working portable-rules platform.

### Low

- **[low] PRD persona table references personas not defined in the personas source file.**
  - Location: PRD "Personas & Jobs" (`Schema Steward`, `Frontend Platform Engineer`); `planning-mds/examples/personas/nebula-personas.md`.
  - Evidence: `grep -ci "schema steward|frontend platform" nebula-personas.md` returns 0.
  - Recommendation: Add these personas to the personas file or footnote them as role archetypes local to F0036; non-blocking for build.

## Product Readiness

- **Requirements quality:** Strong. PRD has clear user value, explicit non-goals, two well-delineated workstreams, and a Phase A Clarification Resolution. No vague-word or TODO gaps in build-critical areas.
- **Story testability:** Good. All 8 stories pass `validate-stories.py`; ACs are Given/When/Then with quantified bounds (256 KB cap, 1h TTL, latency budgets, "0 disagreements"). Caveat: the S0003 "0 disagreements" target is well-specified but rests on the critical-finding mechanism that isn't buildable.
- **Mutation contracts:** Present and specific for all mutation stories (entry points, editable/read-only states, persistence evidence, roles, no-auto-replay). Inventory-completeness caveat (medium-1).
- **UI/screen readiness:** Adequate — PRD has an ASCII layout; the five consuming screens are named. Engine is a render-internals swap, so screen specs are bounded.
- **Tracker state:** Clean — REGISTRY/ROADMAP reflect F0036; STORY-INDEX regenerated (133 files); `validate-trackers` PASS.

## Architecture Readiness

- **API/schema readiness:** No new API/schema needed (frontend-only); consumes existing F0034 contracts. Adequate **for the frontend**, but the "backend parity" framing assumes a schema/rules-driven backend that does not exist (critical).
- **Data/workflow readiness:** Adequate — no workflow/state changes; pin-during-edit `(productVersionId, stage)` is well-defined (S0004).
- **Authorization readiness:** Adequate — no new authz; host-screen auth inherited; forced re-auth uses F0035 401-classification.
- **ADR and NFR readiness:** **Gap.** ADR-021 was amended this plan run, but its §2–§3 parity claims are not implementable (critical); ADR-022/023 are Accepted-but-unimplemented and the shipped bundle/`rules.json` does not conform to ADR-023. NFRs are otherwise measurable.
- **KG/ontology alignment:** Structurally clean (`kg/validate` + `--check-drift` exit 0) but contains a semantic binding to an unimplemented capability (medium-2).

## Buildability Challenge

- **Vertical slice size:** Reasonable as two workstreams; S0001→S0006 (A) and S0007→S0008 (B) sequence cleanly. Workstream B is independently buildable today.
- **Role handoffs:** Clear and frontend-centric; QE/CR/Security signoff matrix confirmed. DevOps correctly No.
- **Testability:** Strong for B (E2E forced-re-auth, per-form regression) and for A's rendering; **weak for S0003 parity** — the parity fixture matrix cannot be authored until the parity mechanism is decided (critical).
- **Dependency and sequencing clarity:** Strong — F0034 and F0035 are built and archived (dependencies are done, not pending), and the F0035 API was verified to map cleanly onto RHF.
- **Risk hotspots:** S0005 (five-screen regression + conditional gating, high-1) and S0003 (parity, critical) are the hotspots; S0007 (six-form migration) is broad but mechanically straightforward.

## Validation Evidence

- `validate-stories.py {FEATURE_PATH}`: **PASS** (exit 0; 8/8 stories; non-blocking INVEST warnings only). Output: `artifacts/validate-stories.txt`.
- `validate-trackers.py`: **PASS** (exit 0; result PASS). Output: `artifacts/validate-trackers.txt`.
- `kg/validate.py`: **PASS** (exit 0). Output: `artifacts/kg-validate.txt`.
- `kg/validate.py --check-drift`: **PASS** (exit 0; pre-existing F0028 + Casbin warnings carried forward). Output: `artifacts/kg-check-drift.txt`.
- `validate_templates.py`: **PASS** (exit 0). Output: `artifacts/validate-templates.txt`.
- **Interpretation (important):** all five structural validators PASS, yet the plan is NOT READY. These validators check story/tracker/KG/template *structure*; they cannot detect that a planned validation mechanism rests on an unimplemented architecture. The readiness decision is driven by raw-artifact inspection (the backend validator and ADRs), as the contract requires (raw artifacts win over generated checklists and prior approval tokens).

## Artifact Trace

- `engine/src/Nebula.Application/Services/LobAttributeService.cs` — established the backend validates Cyber attributes + cross-field rules in hardcoded C# (critical, high-1).
- `planning-mds/lob-schemas/cyber/1.0.0/{data-schema,ui-schema,rules}.json` — confirmed `rules.json` shape (non-JsonLogic) and that ui-schema is layout-only.
- `planning-mds/architecture/decisions/ADR-022-...md`, `ADR-023-...md` — parity + rules-governance contracts; confirmed unimplemented and that the restricted profile forbids `if/then/else`/`dependentRequired` (high-1).
- `planning-mds/architecture/decisions/ADR-021-...md` (amended) — confirmed amendment §2–§4 parity/conditional claims vs reality (critical, high-1).
- `experience/src/features/lob-attributes/components/DynamicAttributePanel.tsx` — hardcoded MFA-maturity gating + bundle-as-status-string usage (high-1).
- `F0036-S0001..S0008-*.md`, `PRD.md`, `STATUS.md`, `acceptance-criteria-checklist.md` — requirements/story readiness review.
- `feature-mappings.yaml`, `canonical-nodes.yaml` — KG binding review (medium-2); `lookup.py F0036` used as a routing aid (raw artifacts authoritative).
- `experience/src/pages/PoliciesPage.tsx` + mutation-form sweep — inventory completeness (medium-1).
- `planning-mds/examples/personas/nebula-personas.md` — persona traceability (low-1).
- Grep (`rules.json`/`jsonlogic` consumers) — confirmed `rules.json` is unwired (critical).
