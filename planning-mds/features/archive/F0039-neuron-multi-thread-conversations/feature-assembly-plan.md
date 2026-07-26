# Feature Assembly Plan — F0039: Neuron Durable Conversations & Local Phi Intent Resolution

**Created:** 2026-07-21
**Author:** Architect (plan run `2026-07-21-6eeb172f`)
**Status:** Draft → active for the F0039 `feature` action (authored at plan; belongs to the feature action's G0)

> **Purpose:** Implementation execution plan for F0039. Primary spec for the Backend, Frontend, and AI
> Engineer agents. Where it conflicts with raw story text, this plan wins (log reconciliation via
> `workstate.py decision`). It condenses the authored design spec into a build-ordered plan; the design spec
> remains the detailed reference.
>
> **Authoritative references (read them — do not duplicate):**
> - `planning-mds/features/F0039-neuron-multi-thread-conversations/neuron-phi-intent-security-implementation-spec.md` (detailed design)
> - `planning-mds/architecture/decisions/ADR-027-*.md`, `ADR-028-*.md` (framing + persistence ownership),
>   `ADR-035-neuron-durable-conversations-and-phi-intent-resolution.md` (this feature's decision)
> - `planning-mds/api/neuron-api.yaml` (`api:neuron-rest` — thread/history/message surface)
> - `planning-mds/schemas/neuron-scope-decision.schema.json`, `neuron-intent-decision.schema.json`,
>   `neuron-intent-resolution.schema.json`, `neuron-message-envelope.schema.json`, `problem-details.schema.json`
> - `planning-mds/architecture/SOLUTION-PATTERNS.md`

## Overview

F0039 turns F0038's shell into a durable conversation experience and replaces the mock/deterministic intent
seam with a local Microsoft Phi structured scope-and-intent resolver behind deterministic, fail-closed
validation. The engine remains the sole CRM authorization boundary; Neuron owns/writes `neuron.*` directly
(ADR-028). Model outputs never grant authorization; every model output is schema- and registry-validated and
fails closed. Delivery is vertical and reversible: durable conversations first, then the intent layer in
shadow → direct → gated-adjudication phases.

## Build Order (maps to rollout §33 / ADR-035 §6)

1. **Phase 0 — Durable conversations** (S0001, S0002, S0003): Postgres repo + `0002` migration; owner-scoped
   thread/history API; conversation-first panel + Daily Brief envelope. Keep the current deterministic guard.
2. **Phase 1 — Contracts + provider seam** (S0004, S0005): structured provider + local Phi profile; intent
   catalog, versioned prompts, schemas, invariants. No production routing change.
3. **Phase 2 — Shadow mode** (S0006 resolver + S0008 shadow): deterministic guard decides production; Phi
   resolver runs recorded-only; compare disagreements.
4. **Phase 3 — Direct routing** (S0007 dispatcher + S0008 gates): enable Phi direct routing only after the
   §30.4 gates pass; on `adjudicate`, clarify. Deterministic resolver stays as immediate rollback.
5. **Phase 4 — Gated adjudication** (S0009): enable contextual adjudication only after durable-context +
   contextual-evaluation gates pass.

## Existing Code (Must Be Modified)

- `neuron/app/persistence/` — implement `postgres.py` `NeuronRepository` behind the existing interface; apply
  `migrations/0001_neuron_schema.sql`; add `migrations/0002_message_sequence_and_idempotency.sql`.
- `neuron/app/models/router.py` — add async `complete_structured` + `StructuredModelResult`/`ModelProvenance`.
- `neuron/app/bootstrap.py` — register the structured provider + resolver; build order/health check.
- `neuron/app/messages.py` — async resolver integration; persist-first, resolve-before-dispatch.
- `neuron/app/scope_guard.py` — remains as the deterministic shadow baseline + rollback (not deleted).
- `neuron/config/models.yaml`, `neuron/app/config.py` — Phi + intent settings, feature flags.
- `experience/src/features/neuron/` — conversation-first panel + thread list; server-owned history hooks.

## New Files (high level; per §36 of the design spec)

- Backend: `neuron/app/threads.py`; `neuron/app/intent/{catalog,contracts,context,preflight,prompt_registry,`
  `resolver,response_policy,validation}.py`; `neuron/app/models/{scripted_provider,openai_compatible_provider,`
  `errors}.py`; `neuron/app/contracts/*.schema.json`; `neuron/config/intent-catalog.yaml`;
  `neuron/crm_agents/cards/crm.{intent_resolver,scope_guard,intent_classifier,intent_adjudicator}.card.yaml`;
  `neuron/prompts/crm-*/1.0.0/{system.md,metadata.yaml}`; `neuron/evals/intent/v1/*`; `neuron/tests/test_*`.
- Frontend: `NeuronConversation.tsx`, `ThreadList.tsx`, `useNeuronThreads.ts`, `useNeuronMessages.ts`.
- Planning contracts (authored at plan): `planning-mds/api/neuron-api.yaml` (thread/history endpoints);
  `planning-mds/schemas/neuron-{scope-decision,intent-decision,intent-resolution}.schema.json`.

## Story → Component Map

| Story | Primary modules | Contracts |
|-------|-----------------|-----------|
| S0001 | `persistence/postgres.py`, `migrations/0002_*.sql` | server sequence + idempotency indexes |
| S0002 | `threads.py`, thread endpoints | `neuron-api.yaml` `/v1/threads*`; ProblemDetails |
| S0003 | `experience/.../neuron/*` | message envelope; Daily Brief `app` parts |
| S0004 | `models/{router,openai_compatible_provider,scripted_provider,errors}.py`, `config` | structured provider contract; provenance |
| S0005 | `intent/{catalog,contracts,prompt_registry,validation}.py`, cards, prompts | intent-catalog + scope/intent/resolution schemas + invariants |
| S0006 | `intent/{preflight,resolver,response_policy,validation}.py` | composed resolution; reliability matrix §27 |
| S0007 | `messages.py`, `runtime.py`, A2A run records | persist-first / resolve-before-dispatch; provenance |
| S0008 | `evals/intent/v1/*`, shadow hook, flags | datasets; §30.4 gates; rollback |
| S0009 | `intent/{context,resolver}.py` (gated), adjudicator card/prompt | bounded context envelope; no re-adjudication |

## Dependency Order

S0001 → S0002 → S0003 (durable half); S0004 → S0005 → S0006 → S0007 (intent half, on the durable store);
S0008 gates S0007→direct routing; S0009 gated behind S0001–S0008. S0003 and the intent half can proceed in
parallel after S0002.

## Integration Checkpoints

- **After Phase 0:** created thread + messages survive restart; reload/switch replay server history in order;
  rename/delete owner-scoped; Daily Brief persisted as an assistant envelope. Cross-user access fails closed.
- **After Phase 1:** provider contract suite green across mock/scripted/OpenAI-compatible; catalog/prompt/
  schema invalid → not ready; regression fixtures (`redirect`+`renewals.list_attention`, invented action)
  rejected.
- **After Phase 2 (shadow):** Phi decisions recorded, never executed; no extra engine call; no user-visible
  prose; disagreements inspectable.
- **Before Phase 3 (direct):** §30.4 gates pass (0 unregistered/authz-bypass routes, 100% fail-closed, ≥98%
  schema-valid, ≥95% domain accuracy, ≥90% action match, ≥95% redirect & injection). Rollback tested.

## Knowledge-Graph Binding Plan (baseline for the feature action's KG reconciliation)

- Feature `feature:F0039` and stories `story:F0039-S0001..S0009` mapped in `feature-mappings.yaml` (this run).
- New canonical capabilities: `neuron-conversation-store`, `neuron-thread-management`,
  `neuron-structured-model-provider`, `neuron-intent-catalog`, `neuron-intent-resolution`,
  `neuron-intent-adjudicator`, `neuron-intent-evaluation`. New endpoints: `neuron-thread-*`. New schemas:
  `neuron-scope-decision`, `neuron-intent-decision`, `neuron-intent-resolution`. Governed by `adr:035`
  (+ `adr:027`, `adr:028`).
- `code-index.yaml` bindings to the as-built `neuron/` + `experience/` source are **deferred to the feature
  action's KG gate** (no implementation code exists at plan) — matches the F0038 precedent.

## Risks and Blockers

- **Local GPU/vLLM dependency:** operational cost; pin image digest + model revision at the feature action.
- **Eval dataset quality:** the §30.4 gates are only as good as the reviewed labeled set — assemble with
  security review of the adversarial split.
- **Async conversion:** if phased, ensure sync Postgres ops run in a bounded worker pool (no event-loop block).
- **Latency budget:** the composed call must fit the combined scope+intent budget on target hardware; splitting
  is a later optimization only if evaluation justifies it.

## JSON Serialization Convention

Neuron endpoints return the versioned message envelope (not persistence row shapes). Thread/history responses
follow `neuron-api.yaml`. Where the engine returns snake_case for companion endpoints (F0038 precedent), heads
map to camelCase component props at the boundary.

## G0 Reconciliation (feature run `2026-07-25-273d5672`)

Appended by the Architect at the feature action's G0 after diffing this plan against the as-built F0038
source. Additive only — nothing below contradicts a story or a canonical contract. Full rationale:
`planning-mds/operations/evidence/runs/2026-07-25-273d5672/g0-assembly-plan-validation.md`.

**Run scope:** S0001–S0008. **S0009 is gated-deferred** — this plan's Phase 4 condition opens it only after
the S0001–S0008 gates pass, which this run produces.

- **D1 — extend, don't just implement, `NeuronRepository`.** The as-built ABC has no `list_threads`,
  `rename_thread`, `delete_thread`, no history cursor/limit, and no idempotency-key parameter. S0001/S0002
  require all of them. Add the abstract methods and satisfy them in **both** `in_memory.py` and the new
  `postgres.py`, so the in-memory backend stays a valid runtime selection.
- **D2 — async-first repository.** `MessageDispatcher.dispatch` is async but calls the sync repository. The
  thread/message surface becomes `async def` over an async pool on both implementations, and the dispatcher
  awaits it. This settles the plan's "phased async" risk rather than deferring it to a worker pool.
- **D3 — `mock_provider.py` joins the S0004 file set.** `ModelProvider` is a `runtime_checkable` Protocol;
  adding `complete_structured` to it makes the existing mock non-conformant unless it implements the method
  too. A deterministic mock keeps the provider suite runnable without the GPU runtime.
- **D4 — vendor the three intent schemas.** `neuron/tests/test_schema_drift.py` enforces byte-equality
  between `neuron/app/contracts/` and `planning-mds/schemas/` over a hard-coded list. Vendor
  `neuron-scope-decision`, `neuron-intent-decision`, `neuron-intent-resolution` and add them to `_VENDORED`.
- **Thread anchoring (S0002).** `MessageDispatcher._open_thread` hard-codes the Day-at-a-Glance anchor;
  generalize it so owner-created threads carry an immutable anchor + auto-title, including `free_form`.
- **Endpoints already contracted.** All six thread operations exist in `neuron-api.yaml` (authored at plan).
  S0002 implements to that contract; it does not author it.
