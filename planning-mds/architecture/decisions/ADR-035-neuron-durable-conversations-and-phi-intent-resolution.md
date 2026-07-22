---
template: adr
version: 1.1
applies_to: architect
---

# ADR-035: Neuron Durable Conversations and Local Phi Structured Intent Resolution (Fail-Closed)

## Status

- [ ] Proposed
- [x] Accepted (F0039 Phase B planning gate, run 2026-07-21-6eeb172f)
- [ ] Superseded
- [ ] Rejected

> Concretizes F0039 on top of [ADR-027](./ADR-027-neuron-companion-a2a-orchestration.md) (A2A-aligned
> orchestration) and [ADR-028](./ADR-028-neuron-companion-persistence-and-outreach-authorization.md)
> (Neuron owns/writes `neuron.*` directly; engine is the CRM source of truth + authorization boundary).
> ADR-028 remains authoritative on persistence ownership; this ADR extends it with the durable-conversation
> delta and adds the decision to replace F0038's mock/deterministic intent seam with a locally hosted
> Microsoft Phi structured scope-and-intent resolver behind deterministic, fail-closed validation.

## Context

F0038 shipped the companion shell with (a) an in-memory operation store behind the `neuron.*` repository
interface (only the 6-table scaffold migration `0001_neuron_schema.sql` is checked in) and browser-memory
transcripts, and (b) a deterministic keyword scope guard + intent classifier sitting behind a `MockProvider`
model seam. F0039's roadmap commitment is the **real** conversation store and thread-management UX; the
authored design spec (`neuron-phi-intent-security-implementation-spec.md`, v1.1.0, local Phi runtime verified
2026-07-21) additionally specifies replacing the mock intent seam with a local Phi model.

Two facts force the decisions here:

1. **Durable conversation and intent resolution must land together.** Contextual phrases ("this one", "the
   renewal we discussed") are unreliable until thread history and UI anchors have a durable, owner-scoped
   source. Intent routing on browser-memory transcripts cannot be safely contextualized.
2. **A prompt-only model is not a contract.** The verified smoke test returned a shape-valid but
   contradictory decision (`redirect` carrying `domain=renewals` + `actions=[renewals.list_attention]`) and
   invented the unregistered action `show_renewals_needing_attention`. Structured decoding controls *shape*;
   it cannot enforce registration, authorization, or cross-field meaning. Those must stay deterministic.

## Decision Drivers

- Keep the **engine as the sole authorization boundary** (ADR-027/028) — no model decision grants access.
- Replaceability: any logical model-backed capability must be swappable without changing its callers.
- Fail-closed by default; model outages must cause bounded routing, never an unbounded/general answer.
- Run locally with no paid external inference API; deterministic tests via a scripted provider.
- Preserve F0038's fail-fast startup and A2A-shaped provenance.

## Decision

### 1. Durable conversation persistence (extends ADR-028 §1)

Implement the Postgres-backed `NeuronRepository` and apply `0001_neuron_schema.sql` unchanged. Add a forward
migration `0002_message_sequence_and_idempotency.sql`: a server-assigned `BIGINT` message **sequence**, a
unique `(thread_id, sequence)` index, nullable `client_message_key` + `thread_idempotency_key`, and scoped
partial unique indexes. Order/page messages by sequence (not timestamp). Set `updated_at` transactionally on
append/rename/delete. Prefer an async repository + bounded async Postgres pool; if phased, run sync ops in a
bounded worker pool — **never block the event loop**. Threads are owner-scoped with an **immutable anchor**
(free-form / domain / record) and soft-delete + retention per `ActivityTimelineEvent` policy. Do **not** hold
a Postgres transaction open across a Phi/engine network call; use short transactions + idempotency keys.

### 2. One physical Phi model, logically separate capabilities

Host one `microsoft/Phi-4-mini-instruct` runtime serving `crm.scope_guard`, `crm.intent_classifier`,
`crm.intent_resolver`, and `crm.intent_adjudicator`. The initial production path is **one composed
`crm.intent_resolver` generation** returning separate `scope` and `intent` sections (spending the local
latency budget once), each independently schema-validated and telemetered against **one shared physical-call
provenance**. `crm.intent_adjudicator` is a separate physical call (different, higher-risk context) and is
**gated off** initially. Logical separation does not require one physical call per stage; shared physical
inference does not collapse telemetry, permissions, or failure paths (spec §39.1).

### 3. Deterministic authority boundaries + fail-closed validation

The model owns **none** of: authentication, authorization, permission evaluation, token-forwarding policy,
tool/action/agent/plan registration, schema validation, destructive-action confirmation, optimistic
concurrency, engine business validation, persistence ownership, or audit retention (spec §7.3). Every model
output passes **JSON Schema → deterministic cross-field invariants → dynamic registry membership → required
entity/confirmation policy → trusted-head resolution** before any dispatch. Heads are resolved from the
trusted intent catalog, never from model output. Any schema/invariant/registry/timeout/unavailable failure
**fails closed** to a bounded, application-owned redirect/clarify with **no engine call** (reliability matrix,
spec §27). Response copy is application-owned; the model writes no user-facing prose in this feature.

### 4. Runtime-neutral provider seam; separate local inference service

Extend the `ModelProvider`/`ModelRouter` seam with an **async `complete_structured`** method returning typed
value + provenance (model, revision, prompt/schema/catalog hashes, tokens, latency) and normalized errors.
Providers: `MockProvider` (structured), `ScriptedProvider` (tests), `OpenAICompatibleProvider` (vLLM/Ollama).
Run Phi as a **separate local inference service** (vLLM OpenAI-compatible, bearer auth, `max_model_len=4096`)
— not inside the Neuron FastAPI process — so model lifecycle, GPU memory, and health are independent, and no
heavy ML dependency enters Neuron. The provider is runtime-neutral (vLLM is the reference, not a hard couple).
No user token or PII is ever sent to the model server; the server must not persist prompts.

### 5. Contracts: intent catalog, versioned prompts, JSON schemas, thread API

- Code-reviewed `intent-catalog.yaml` maps active domains/actions → registered head cards, cross-checked at
  startup (head exists, action ids unique, domain-prefix match, entity types registered, no inactive route).
- Versioned prompt fragments + composed resolver prompt loaded with content hashes (provenance).
- JSON schemas: `neuron-scope-decision`, `neuron-intent-decision`, composed `neuron-intent-resolution` (+
  catalog/context/prompt-metadata) reject unknown shape/additional properties; deterministic invariants
  enforce cross-field meaning the schema cannot.
- Neuron API adds the owner-scoped thread/history surface (`/v1/threads`, `/v1/threads/{id}`,
  `/v1/threads/{id}/messages`) — owner derived only from the token; `PATCH` changes title only; soft-delete;
  cursor pagination by `(updated_at DESC, id DESC)` and by message sequence. `POST /v1/messages` requires a
  client-generated `client_message_id` unique within the thread; Daily Brief uses an owner/date idempotency
  key. Persist the user turn before resolution; persist exactly one terminal assistant envelope per turn.

### 6. Rollout, feature flags, and rollback

Phased (spec §33): **Phase 0** durable conversations; **Phase 1** contracts + provider seam (no routing
change); **Phase 2** composed-resolver **shadow mode** (recorded, never executed, no extra engine call, no
user-visible prose); **Phase 3** Phi **direct routing** (only after the §30.4 security + routing gates pass;
on `adjudicate`, clarify); **Phase 4** **gated** contextual adjudication (only after durable-context +
contextual-evaluation gates). Flags: `NEURON_PERSISTENCE`, `NEURON_PHI_DIRECT_RESOLVER_ENABLED`,
`NEURON_PHI_ADJUDICATION_ENABLED`, `NEURON_PHI_SHADOW_MODE`, `NEURON_INTENT_FAIL_CLOSED` (cannot be disabled
in production), `NEURON_INTENT_DIAGNOSTIC_SAMPLING` (default off). Rollback: switch
`intent_resolver.implementation` = `deterministic | phi | shadow` with **no schema/DB reversal**; keep
prompt/model/eval provenance and the shared intent catalog.

## Architecture Sketch (ASCII) — direct-route flow

```text
POST /v1/messages
  -> persist user turn (owner-scoped thread, server sequence, client_message_id idempotent)
  -> deterministic preflight (size/encoding/NFKC/high-certainty markers)  --fail--> bounded redirect (no engine)
  -> ONE Phi complete_structured(scope+intent)   [no tools, no token, no CRM data, no history]
  -> JSON Schema -> invariants -> registry membership -> required-entity/confirmation -> trusted-head resolve
        | redirect/clarify/timeout/invalid -> persist bounded assistant envelope (NO head, NO engine call)
        | route -> registered specialist head -> engine authorizes as the user -> persist routed envelope
  -> emit telemetry (hashes + counts; NO raw text)
Adjudication (gated): on `adjudicate`, if enabled, one bounded Phi call over sanitized <=4 turns / <=6000 chars
  / <=4096 tokens + revalidated UI anchor; result may route/clarify/redirect but not re-adjudicate.
```

## Options Considered

- **Keep the deterministic keyword guard.** Rejected as the product target (misses natural phrasing, compound
  and context-dependent requests, paraphrased injection) — but **retained as the shadow baseline + rollback**.
- **Load Phi inside the Neuron process.** Rejected: couples model lifecycle/GPU/health to the app and imports
  heavy ML deps; a separate OpenAI-compatible service keeps Neuron thin and replaceable.
- **Two physical calls (separate scope + intent).** Rejected initially on latency; the logical contracts stay
  separable so it remains a later optimization if evaluation justifies it.
- **Let the model write the user response / own confirmation.** Rejected: classification only; application-
  owned copy and deterministic confirmation are safer and testable (spec §39.4, §25.8).

## Consequences

- **Positive:** durable, resumable, private conversations; natural-language routing with measured accuracy;
  defense-in-depth security; local, reversible, replaceable model experiment; provenance without PII exposure.
- **Negative / costs:** a new local GPU inference dependency to operate and pin; an evaluation harness +
  labeled datasets to maintain; more contracts (catalog, prompts, schemas) to keep code-reviewed and hashed.
- **Neutral:** `crm.response_composer` (open-ended prose) is explicitly **out of scope** — a later feature.

## Security & Compliance Notes

- **Defense in depth (§25.1):** deterministic input controls + model semantic classification + strict
  structured output + deterministic schema validation + trusted registry resolution + least-privilege
  agent/tool contracts + engine authorization + auditable provenance. No single layer is sufficient; the
  system stays safe even if Phi mislabels (resolver has no tools/token, routes are registry-bounded, writes
  are deterministically confirmed, the engine still authorizes).
- **No token/PII to the model (§25.4, §26):** never send bearer/refresh/cookie/API key/DB string/Casbin/config
  to Phi. Default logs carry ids/hashes/decision/reason/latency/token counts only — never raw message text,
  record identifiers, raw model response, or tokens. Diagnostic raw sampling requires separate privacy ADR
  approval.
- **Network controls (§25.5):** private container network, loopback-only dev port, local service API key,
  restricted egress, Neuron restricted to the configured endpoint.
- **Write actions (§25.8):** `renewals.mock_send` requires explicit user confirmation; vague language is never
  sufficient; the engine authorizes and enforces optimistic concurrency (unchanged from F0038).

## Follow-up Actions

- Feature action: pin + record vLLM image digest, model revision, GPU sizing, latency/concurrency evidence;
  assemble reviewed direct/adversarial/contradiction datasets; run the §30.4 gates before enabling direct
  routing; test the deterministic rollback; reconcile `code-index.yaml` bindings to the as-built `neuron/`.
- Gated: run the contextual-evaluation + token-budget gates before enabling `NEURON_PHI_ADJUDICATION_ENABLED`.
