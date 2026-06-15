# Action Context — Plan Run 2026-06-01-2ac02e13

> Plan action (`agents/actions/plan.md`) under the **base run** evidence contract
> (`feature-evidence-package-standardization-plan-v2.md`, effective 2026-05-19).
> Plan runs BEFORE the feature evidence package exists: it produces planning
> artifacts in `{FEATURE_PATH}` and a base run evidence package per §8, but **no
> feature evidence package**. No `latest-run.json`, no `evidence-manifest.json`,
> no signoff ledger, no feature closeout. The feature evidence package is created
> later by `agents/actions/feature.md` for the same `FEATURE_ID`.

## Run Identity

| Field | Value |
|-------|-------|
| `PLAN_RUN_ID` | `2026-06-01-2ac02e13` |
| `PRODUCT_ROOT` | `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` |
| `PLAN_RUN_FOLDER` | `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-06-01-2ac02e13` |
| `Lifecycle Authority` | `none` (base run) |
| Session working dir | `/mnt/c/Users/gajap/sandbox/nebula/nebula-agents` |
| Run-id generation | `python3 -c "import secrets; print(secrets.token_hex(4))"` → `2ac02e13` (NOT uuid4) |

## Action Scope

| Input | Value |
|-------|-------|
| `FEATURE_ID` | `F0019` — Submission Quoting, Proposal & Approval Workflow |
| `FEATURE_SLUG` | `submission-quoting-proposal-and-approval` (from `REGISTRY.md`) |
| `FEATURE_PATH` | `{PRODUCT_ROOT}/planning-mds/features/F0019-submission-quoting-proposal-and-approval` |
| `PHASE` | `A+B` — Phase A (PM requirements) then Phase B (Architect architecture), sequential |
| `FEATURE_MODE` | `existing` — folder already contains `PRD.md` (10.9 KB) + `STATUS.md` skeleton (+ README, GETTING-STARTED) |
| Compatibility | `A+B` + `existing` → update planning artifacts, then architecture (valid; not rejected) |
| `FEATURE_INDEX_ROOT` | `{PRODUCT_ROOT}/planning-mds/operations/evidence/features/F0019-…` — **NOT created at plan** (feature.md owns it) |

## Plan Start Prerequisite (resolved)

`python3 {PRODUCT_ROOT}/scripts/kg/validate.py` must exit 0 before Phase A starts.

- **At baseline: exit 1** — `coverage-report.yaml` reported stale. Investigated: product
  repo is git-clean (no dirty KG files); the staleness is content/hash drift (the committed
  report predated recent source-file edits), NOT caused by F0019 and NOT masking uncommitted edits.
- **Repair:** `validate.py --write-coverage-report` (the documented fix) → `[PASS]`. Diff inspected:
  319+/319- lines, all derived freshness metadata (`source_hash`, `last_modified`,
  `hotspot_rank`/`hotspot_score`); **0 feature/story IDs added or removed**. Fully reversible
  (`git checkout`) and re-generated again at exit-validation regardless.
- **Re-confirm:** `validate.py` exit 0. Prerequisite satisfied.
- **Open non-blocking warning (pre-existing, unrelated to F0019):** low-confidence inferred
  edge (0.4) on `feature:F0028` in `feature:F0018.depends_on`. Warning, not error; does not block.

## Gates (plan action G1–G5)

| Gate | Step | Owner | Description |
|------|------|-------|-------------|
| G1 CLARIFICATION | 1.5 | PM | Resolve open requirement questions before Phase A approval |
| G2 TRACKER SYNC (A) | 1.75 | PM | REGISTRY / ROADMAP / BLUEPRINT / STORY-INDEX synchronized; story + tracker validators pass |
| G3 PHASE A APPROVAL | 2 | User | User reviews requirements; PM records decision in `gate-decisions.md` |
| G4 ONTOLOGY SYNC (B) | 3.5 | Architect | feature-mappings / canonical-nodes / solution-ontology aligned; `validate.py --check-drift` exit 0 |
| G5 PHASE B APPROVAL | 4 | User | User reviews architecture; Architect records decision in `gate-decisions.md` |

## Roles Active This Run

- **Product Manager (Phase A)** → `PRD.md`, persona files, acceptance criteria, story breakdown,
  `STATUS.md` skeleton (Required Role Matrix + empty/append-only Story Provenance), minimal
  `feature-mappings.yaml` stub. Owns `{PRODUCT_ROOT}/planning-mds/knowledge-graph/feature-mappings.yaml`,
  stories, PRD, personas, planning trackers.
- **Architect (Phase B)** → ADRs, API/schema updates, `canonical-nodes.yaml` updates,
  `solution-ontology.yaml` updates (only if vocabulary changes), finalized `feature-mappings.yaml`
  bindings, feature `README.md` (ERD/C4), `GETTING-STARTED.md`. (`feature-assembly-plan.md` is NOT a
  plan deliverable — it belongs to `agents/actions/feature.md` Step 0.)

## Constraints

- Stop at each gate; do not proceed past an approval/ontology-sync gate without an explicit token.
- `STATUS.md` story provenance rows are append-only (existing-mode); do not mutate prior rows.
- Non-architect roles must not edit `canonical-nodes.yaml` / `solution-ontology.yaml`.
- Raw artifacts win over KG/lookup mappings on conflict.
- Do NOT produce role reports (`g0-*`, `test-*`, `code-review-*`) — those belong to the feature action.
- Do NOT create `{FEATURE_INDEX_ROOT}/`, `latest-run.json`, `evidence-manifest.json`, or `current-run.json`.
- Record every shell command in `commands.log` (JSONL §13); do not hide failed commands.

## F0019 Scope Snapshot (from PRD + ROADMAP + KG lookup)

- Sole roadmap **Now** item; all dependencies done+archived (F0006 intake, F0018 policy, F0020
  documents, F0034 product schema, F0035 session).
- Extends `workflow:submission` downstream of F0006's intake boundary (`ReadyForUWReview`).
  Governed by **ADR-011** (state machines + append-only transition history) and **ADR-012**
  (shared document storage) — both Accepted.
- Affects entities: submission, policy, document, workflow-transition, activity-timeline-event.
- Hard boundary guardrails with **F0006** (intake stays closed at `ReadyForUWReview` until F0019
  deliberately enables downstream transitions) and **F0027** (F0019 owns submission-bound packet;
  F0027 owns reusable outbound document generation).
