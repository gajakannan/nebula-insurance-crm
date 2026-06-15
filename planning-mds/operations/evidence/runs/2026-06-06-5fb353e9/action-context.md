# Action Context — plan — F0017 — run 2026-06-06-5fb353e9

## Run Identity

- **Action:** `plan` (`agents/actions/plan.md`)
- **Run id:** `2026-06-06-5fb353e9` (format `YYYY-MM-DD-[a-z0-9]{8}`, suffix from `secrets.token_hex(4)`)
- **Profile:** Base run (non-feature) per §8 — six base files, no manifest
- **Feature:** F0017 — Broker/MGA Hierarchy, Producer Ownership & Territory Management
- **Feature slug:** `broker-mga-hierarchy-and-producer-ownership`
- **Feature path:** `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership`
- **Run folder:** `planning-mds/operations/evidence/runs/2026-06-06-5fb353e9`
- **Recorded on:** 2026-06-06T09:56:17-04:00

## Inputs

- `FEATURE_ID = F0017`
- `PHASE = A+B` (Phase A = PM requirements; Phase B = Architect architecture; sequential)
- `FEATURE_MODE = existing` (folder already contains `PRD.md` + `STATUS.md` skeleton — confirmed)
- `PRODUCT_ROOT = /mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` (sister-repo default per AGENT-USE → Session Setup)
- Contract effective date: 2026-05-19 (base run evidence contract)

## Assumptions

- F0017 registry status is `Planned` (non-terminal): plan action produces only the base run package; no feature evidence package / `latest-run.json`.
- Source precedence: raw feature folder, ADRs, API/schema, data-model, policy artifacts outrank KG mappings on conflict.
- KG lookup reports F0017 `excluded` ("ontology mapping backfill has not started") → Phase A seeds a minimal feature-mapping stub; Phase B (Architect) completes canonical bindings.
- Dependencies per PRD: F0002 (Broker & MGA Relationship Management), F0023 (Global Search/Reporting). Consumed by F0022 (routing/queues) and F0008 (Broker Insights, must land after F0017). F0025 owns commission economics (out of scope here).
- Per product memory: Nebula records status/facts; it does not compute pricing/rating/commission — F0017 is the structural distribution model only.

## Scope Boundaries

- **In scope (this run):** PM Phase A planning artifacts (PRD refinement, personas, stories with testable acceptance criteria, STATUS skeleton, feature-mapping stub, tracker sync) and Architect Phase B (assembly architecture, ADRs/API/schema deltas, canonical-node + feature-mapping bindings, ontology sync).
- **Out of scope:** Implementation (`engine/`, `experience/`, `neuron/`), feature evidence package, role reports (`g0-*`, `test-*`, `code-review-*`), `feature-assembly-plan.md` content is a Phase B planning artifact for this feature but per plan deliverables contract the executable assembly plan belongs to `feature.md` Step 0 — Phase B here produces architecture + bindings, not the feature action's evidence.
- **Ownership:** PM owns PRD/personas/stories/STATUS skeleton/feature-mappings stub + trackers; Architect owns `feature-assembly-plan.md`, ADRs, API/schema, `canonical-nodes.yaml`, `solution-ontology.yaml`, finalized `feature-mappings.yaml`. `STATUS.md` story-provenance rows are append-only and must not be mutated.

## Lifecycle Stage

Planning (`plan` action). Gates: `G1 CLARIFICATION` → `G2 TRACKER SYNC (A)` →
`G3 PHASE A APPROVAL` → `G4 ONTOLOGY SYNC (B)` → `G5 PHASE B APPROVAL`.
Current position: **G1 CLARIFICATION** (awaiting operator answers).
No `validate-feature-evidence.py` stage applies at plan (first `--stage G0`
runs during the later feature action).
