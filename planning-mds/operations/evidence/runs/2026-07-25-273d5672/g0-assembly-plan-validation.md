# G0 — Assembly Plan Validation — F0039 run `2026-07-25-273d5672`

**Gate:** G0 — Architect assembly plan authoring and validation
**Role:** Architect (`agents/architect/SKILL.md` activated)
**Feature:** F0039 — Neuron Durable Conversations & Local Phi Intent Resolution
**Primary spec:** `planning-mds/features/F0039-neuron-multi-thread-conversations/feature-assembly-plan.md`
**Mode:** `clean` · **Slice order source:** `assembly-plan`
**Date:** 2026-07-25

## Step 0 — Authoring disposition

`feature-assembly-plan.md` **already exists**, authored by the Architect during the F0039 plan run
`2026-07-21-6eeb172f` (merged to main as `37fe95e`, PR #64). Per the G0 judgment rule, this gate therefore
**reconciles** the existing plan against the as-built source rather than overwriting it. The plan's Build
Order, Story → Component Map, Dependency Order, Integration Checkpoints, and Knowledge-Graph Binding Plan are
adopted as authored; the deltas below are appended to it under *G0 Reconciliation (feature run
2026-07-25-273d5672)*.

## Run scope decision (operator, 2026-07-25)

This run delivers **S0001–S0008**. **S0009 (contextual adjudicator) is gated-deferred** — the assembly plan's
own Phase 4 condition opens it only after the S0001–S0008 direct-routing and context gates pass, so it cannot
be delivered inside the run that produces those gate results. S0009 is recorded `not-in-scope` in the
STATUS.md Story × Role Progress matrix and carried as an explicit deferral into PM closeout.

## Step 0.5 — Validation checklist

| Check | Result | Basis |
|-------|--------|-------|
| Scope split matches feature story requirements | **PASS (with 4 reconciliations)** | Story → Component Map cross-read against all 8 in-scope story files; every story has named primary modules and a contract. Four as-built gaps found (D1–D4 below) — additive, none contradict a story. |
| Dependencies between agents are identified | **PASS** | Dependency Order is explicit: S0001→S0002→S0003 (durable half); S0004→S0005→S0006→S0007 (intent half); S0008 gates S0007→direct routing. S0003 and the intent half parallelize after S0002. Backend owns S0001/S0002/S0007, Frontend owns S0003, AI Engineer owns S0004–S0006/S0008. |
| Integration checkpoints are feasible | **PASS (runtime-conditional)** | Four checkpoints (post-Phase 0/1/2, pre-Phase 3) are each observable from tests. Phase 0 needs Postgres; Phases 1–3 need the local Phi vLLM endpoint. Both are runtime dependencies carried into G1, not plan defects. |
| No missing or conflicting artifact ownership | **PASS** | Architect-owned contracts (`neuron-api.yaml`, 3 schemas, ADR-035) were authored at plan and are **read-only inputs** here. No implementation agent writes a canonical contract; schema changes route back to Architect. No two roles own the same artifact. |

## Findings — plan ↔ as-built reconciliations

Severity key: these are **plan-completeness** findings recorded at G0, not code defects.

### D1 — `NeuronRepository` interface must be **extended**, not only implemented behind (S0001, S0002)

The plan says "implement `postgres.py` `NeuronRepository` **behind the existing interface**". The as-built
ABC at `neuron/app/persistence/repository.py:21` declares only `create_thread`, `get_thread`, `add_message`,
`get_messages` plus the A2A trace methods. It has **no** `list_threads`, `rename_thread`, `delete_thread`,
and `get_messages` takes no cursor/limit — all four are required acceptance criteria of S0002, and
`add_message` has no idempotency-key parameter required by S0001.

**Resolution:** the interface is extended with the new abstract methods, and **both** implementations
(`in_memory.py` and the new `postgres.py`) satisfy it, so the F0038 in-memory backend stays a valid runtime
selection. Recorded as an additive contract change, not a breaking one — no existing caller signature changes.

### D2 — Sync repository vs. async dispatcher (S0001 NFR)

`MessageDispatcher.dispatch` is `async` (`neuron/app/messages.py:46`) but calls `rt.repository.add_message`
synchronously (`messages.py:61`, `:152`). S0001's non-functional expectation is that repository operations
"never block the event loop". The plan flags the phased-async risk but does not decide it.

**Decision (Architect, this gate):** the Postgres repository is implemented **async-first** (`async def` on
the thread/message surface behind an async pool); the in-memory implementation gains matching async methods
so both satisfy one interface, and `MessageDispatcher` awaits them. This removes the worker-pool fallback
rather than deferring it. Logged via `workstate.py decision --topic plan-story-reconcile`.

### D3 — `mock_provider` must satisfy the extended provider Protocol (S0004)

The plan lists `models/router.py`, `openai_compatible_provider.py`, `scripted_provider.py`, and `errors.py`
for the structured-provider seam but omits `neuron/app/models/mock_provider.py`. `ModelProvider` is a
`runtime_checkable` `Protocol` (`neuron/app/models/router.py:34`); adding `complete_structured` to it makes
the existing mock provider non-conformant unless it is updated too.

**Resolution:** `mock_provider.py` is added to the S0004 modified-file set and implements
`complete_structured` deterministically, keeping the mock profile usable for tests without the GPU runtime.

### D4 — New schemas must be vendored into `neuron/app/contracts/` (S0005)

`neuron/tests/test_schema_drift.py` asserts every vendored contract in `neuron/app/contracts/` is byte-equal
to its `planning-mds/schemas/` source, over a hard-coded `_VENDORED` list. The three schemas authored at plan
(`neuron-scope-decision`, `neuron-intent-decision`, `neuron-intent-resolution`) exist only under
`planning-mds/schemas/`. The plan's "New Files" list mentions `neuron/app/contracts/*.schema.json` but does
not connect it to the drift test.

**Resolution:** the three schemas are vendored into `neuron/app/contracts/` and added to `_VENDORED`, so
schema drift stays enforced for the intent contracts exactly as it is for the F0038 envelope contracts.

## Additional scope notes carried into implementation

- **Thread anchoring:** `MessageDispatcher._open_thread` (`messages.py:77`) hard-codes
  `anchor_type="domain"`, `anchor_ref="day-at-a-glance"`, `title="Day at a Glance"`. S0002 requires
  owner-created threads with an **immutable** anchor and auto-title, including `free_form` threads. The
  thread-open path is generalized under S0002 rather than left implicit.
- **Endpoint contract already exists:** `planning-mds/api/neuron-api.yaml` already defines all six thread
  operations (`createThread`, `listThreads`, `getThread`, `renameThread`, `deleteThread`,
  `getThreadHistory`). S0002 implements to that contract; it does not author it.

## Scope-boolean sequencing (recorded, not silent)

`runtime_bearing`, `frontend_in_scope`, and `deployment_config_changed` are set **true** at this gate — they
force artifacts (`g1-runtime-preflight.md`, `deployability-check.md`) at G1/G2, which is correct and
achievable. `security_sensitive_scope` is deliberately left **false until G2**: once true, the validator
checks the manifest `security_scans{}` block at *every* stage, and the four scan classes (`dependency`,
`secrets`, `sast`, `dast`) cannot run before implementation exists. It is flipped to true at G2 together with
the scan results, exactly as the contract's scope-boolean reconciliation rule prescribes. The Security
Reviewer role is **not** weakened by this: STATUS.md lists `Security Reviewer` as `Required = Yes`, which
independently forces the security review from G3 onward.

## Required signoff roles

Initialized in the plan run and **re-affirmed** at this gate against the discovered scope — the STATUS.md
`Required Role Matrix` requires **Quality Engineer, Code Reviewer, Architect, AI Engineer, Security** (all
`Required = Yes`). The manifest `required_roles[]` is set to match. Scope booleans forcing these roles
(`frontend_in_scope`, `runtime_bearing`, `security_sensitive_scope`, `deployment_config_changed`) are
provisionally set from the assembly plan's declared surfaces and are **reconciled against actual
`changed_paths[]` at G2**, per the contract's scope-boolean rule.

## Knowledge-Graph binding baseline

The plan's Knowledge-Graph Binding Plan is adopted unchanged as the G7 baseline: 7 new capabilities, the
`neuron-thread-*` endpoints, 3 new schemas, governed by `adr:035` (+ `adr:027`, `adr:028`).
`code-index.yaml` bindings to as-built `neuron/` + `experience/` source were deliberately deferred at plan
(no implementation code existed) and are authored at **G7**, diffed against this baseline.

## Verdict

**PASS.** The feature assembly plan is scope-complete, dependency-ordered, and ownership-clean for
S0001–S0008. Four additive reconciliations (D1–D4) are appended to the plan and carried into implementation;
none contradicts a story or a canonical contract. Two runtime dependencies (Postgres, local Phi vLLM) are
handed to G1.

**Validator:** `validate-feature-evidence.py --stage G0` — see `lifecycle-gates.log`.
