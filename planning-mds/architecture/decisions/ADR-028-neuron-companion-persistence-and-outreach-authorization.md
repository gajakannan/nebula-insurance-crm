---
template: adr
version: 1.1
applies_to: architect
---

# ADR-028: Neuron Companion Persistence Schema, Cross-Store Write Consistency, and Outreach-Draft Authorization

**Date:** 2026-06-30

## Status

- [ ] Proposed
- [x] Accepted (F0038 Phase B planning gate, run 2026-06-30-d1dd91f7)
- [ ] Superseded
- [ ] Rejected

> Executes the four deferred Phase B follow-ups of
> [ADR-027](./ADR-027-neuron-companion-a2a-orchestration.md): the concrete
> `neuron.*` persistence schema, the cross-store write-consistency pattern, the
> private Agent Card / YAML plan schemas, and the OpenAPI/JSON Schema contract
> surfaces. It also resolves the F0038 outreach-commit authorization conflict
> with the existing F0007 renewal ownership model. ADR-027 remains the framing
> decision; this ADR is its concretion for F0038.

## Context

ADR-027 fixed the orchestration foundation (A2A-aligned internal profile, Neuron
owns its `neuron.*` operation store directly, engine is the CRM source of truth)
but explicitly deferred the exact table/column design, the cross-store
write-consistency rule, the manifest/plan schemas, and the contract surfaces to
F0038 Phase B. F0038's eight stories (`F0038-S0001..S0008`) now pin those down.

Two facts force concrete decisions in this ADR:

1. **A single user gesture writes both stores.** Drafting writes a Neuron
   `agent_run` + provenance **and** an engine renewal `ActivityTimelineEvent`.
   Mock-send writes an engine `WorkflowTransition` + a "sent (simulated)" event
   **and** a Neuron operation record. These cannot be one ACID transaction
   across the .NET engine and the Python Neuron runtime.

2. **F0038 locks "Underwriter-only" outreach, but mock-send commits
   `Identified → Outreach`.** In the existing F0007 model
   (`authorization-matrix.md` §2.9, `WorkflowStateMachine.ValidateRenewalTransition`,
   ADR-011), `Identified → Outreach` is a **Distribution-owned** transition and
   **Underwriters are denied Distribution-owned transitions**. The signed-off
   F0038 intake brief (decisions C and B) corrects the persona — in commercial
   P&C the **Underwriter owns the renewal** — and locks that the Underwriter (and
   only the Underwriter) drafts and mock-sends, where mock-send commits the real
   `Identified → Outreach`. The requirement is fixed; only the authorization
   mechanism is an architect decision.

## Decision Drivers

- Keep Neuron stateless-as-a-service while it owns durable operation state.
- Make a partial cross-runtime failure reconcilable, never corrupting.
- Honor the locked intake (Underwriter-only outreach) without silently widening
  the existing F0007 transition-ownership model (least privilege).
- Keep definitions (Agent Cards, prompts) as checked-in source assets, not DB
  authoring rows (ADR-027 §9).
- Replayable, auditable message envelopes and agent runs from day one.

## Decision

### 1. `neuron.*` operation persistence schema

Neuron owns and writes (directly, not via the engine) a `neuron` Postgres schema
with six tables. Audit columns (`created_at`, `updated_at`) are on every table by
convention. All ids are UUID.

| Table | Key columns | Purpose |
|-------|-------------|---------|
| `neuron.threads` | `id`, `owner_user_id`, `anchor_type` (`domain`\|`record`\|`free_form`), `anchor_ref`, `title`, `deleted_at` | Conversation thread; **owner-scoped** (private to creator). Maps to A2A `contextId`. |
| `neuron.messages` | `id`, `thread_id`→threads, `role` (`user`\|`assistant`), `in_reply_to_message_id`, `envelope_version` | One chat message; replayed via the versioned envelope. |
| `neuron.message_parts` | `id`, `message_id`→messages, `ordinal`, `part_type` (`text`\|`app`\|`status`\|`sources`\|`actions`), `content_json` | Ordered envelope parts (`neuron-message-envelope.schema.json`). |
| `neuron.agent_runs` | `id`, `thread_id`→threads, `parent_run_id`→agent_runs, `plan_id`, `plan_version`, `card_id`, `card_version`, `card_content_hash`, `state` (A2A task state), `engine_ref_type`, `engine_ref_id` | A2A task / subtask trace. References the active plan + Agent Card by id+version+hash (definitions stay in source). `engine_ref_*` references the authoritative engine write (see §2). |
| `neuron.tool_calls` | `id`, `agent_run_id`→agent_runs, `tool_name`, `request_digest`, `status`, `latency_ms` | MCP/tool invocation trace under a task. No raw args/PII. |
| `neuron.provenance_events` | `id`, `agent_run_id`→agent_runs, `model`, `prompt_id`, `prompt_version`, `content_hash`, `trace_id`, `cost`, `latency_ms` | Draft/mock-send provenance and run metadata. **No** raw prompts, raw LLM responses, or customer PII (ADR-027 security notes). |

A thin **read-only** `neuron.prompt_versions` lookup (id → semantic version →
content hash, projected from the checked-in assets at deploy) is **optional** and
added only if runtime id→hash resolution is needed (ADR-027 §9). F0038 may run on
an in-memory implementation behind the same repository interface (F0038-S0001);
the durable home is this `neuron` schema.

### 2. Cross-store write consistency — engine-first, idempotent Neuron record

For any gesture that writes both stores:

1. The **engine business write is authoritative and commits first** (as the user,
   Casbin ABAC): the draft `ActivityTimelineEvent`, or the mock-send
   `WorkflowTransition` + "sent (simulated)" event.
2. The engine returns its id (timeline-event id / transition id).
3. Neuron writes its operation record (`agent_runs.engine_ref_id` +
   `provenance_events`) **referencing that engine id**, written **idempotently**
   keyed on `agent_run_id` so a retry cannot double-write.

If step 3 fails after step 1 commits, the engine state is correct and the Neuron
record is reconciled on retry (the engine id is the reconciliation key); there is
never a "sent" engine event without a committed transition (engine-side atomicity
covers transition + both events), and never a Neuron record claiming an engine
write that did not happen. A full outbox is **not** introduced in F0038 (single
interactive gesture, low volume); the idempotency key is the F0038 floor and the
seam where an outbox lands later if reconciliation needs become continuous.

### 3. Outreach-draft authorization — dedicated, least-privilege `renewal:draft_outreach`

- A **new Casbin action** `renewal:draft_outreach` is added on the **Underwriter**
  role (and Admin, unscoped). Distribution does **not** receive it in v1
  ("refer to Distribution for review" is a future feature).
- This permission gates **both** the persist-draft endpoint (F0038-S0005) and the
  **mock-send** endpoint (F0038-S0006). It is **distinct from** `renewal:transition`.
- The engine's `WorkflowStateMachine` gains a **narrow exception**: the
  `Identified → Outreach` move is valid for an **Underwriter** **only** when it is
  performed through the **outreach-mock-send path under `renewal:draft_outreach`
  authority**. The general `renewal:transition` ownership split (Distribution owns
  `Identified ↔ Outreach ↔ InReview`; Underwriter owns the downstream states) is
  **unchanged** for every other path.
- Rationale: the locked intake makes the Underwriter the renewal owner and the
  sole outreach author. Granting a dedicated, single-purpose action — rather than
  broadening the Underwriter's general transition rights — keeps least privilege,
  is independently reviewable by Security, and leaves the F0007 model intact.

> **This is the F0038 ↔ F0007 reconciliation. It is the #1 item for operator
> ratification at the Phase B approval gate (G5).** It changes the renewal
> authorization surface; it does not change the F0038 PRD/intake (which is
> authoritative and unchanged).

### 4. Contract surfaces (executes ADR-027 follow-ups #1 and #3)

- **Neuron service OpenAPI:** `planning-mds/api/neuron-api.yaml` (`api:neuron-rest`)
  — message send, action callback, glance assembly, health. Message envelope
  replay is the Neuron operation contract.
- **Engine OpenAPI additions** to `planning-mds/api/nebula-api.yaml`: needs-attention
  list, per-renewal companion context, persist outreach draft, outreach mock-send,
  and companion telemetry ingest (mirrors the F0035 SessionTelemetry pattern).
- **JSON Schemas** in `planning-mds/schemas/`: `neuron-message-envelope`,
  `neuron-zone-payload`, `neuron-agent-card`, `neuron-orchestration-plan`,
  `renewal-needs-attention-item`, `renewal-outreach-draft-request`,
  `renewal-outreach-mock-send-request`, `neuron-companion-telemetry-event`.

## Architecture Sketch (ASCII) — cross-store write on mock-send

```text
Underwriter clicks [Send (mock)]
        |
        v
+-------------------+   1. POST /neuron/actions (action_type=mock_send, user token)
| Neuron Runtime    |------------------------------------------------+
| (stateless svc)   |                                                |
+---------+---------+                                                |
          | 2. engine call AS THE USER (forwarded authentik token)   |
          v                                                          |
+-------------------------------------------+                        |
| Engine  POST /renewals/{id}/outreach-mock-send                     |
|  - Casbin: renewal:draft_outreach (Underwriter)                    |
|  - WorkflowStateMachine: Identified->Outreach (outreach path)      |
|  - ATOMIC: WorkflowTransition + "sent (simulated)" timeline event  |
|  - SMTP path NOT invoked (delivery faked)                          |
+----------------------+--------------------------------------------+
          | 3. returns transition_id / timeline_event_id            |
          v                                                          |
+-------------------+   4. write neuron.agent_runs(engine_ref_id=...) |
| Neuron Runtime    |      + provenance_events, idempotent on run id  |
| writes neuron.*   |<------------------------------------------------+
+-------------------+
  engine write authoritative & first; neuron record references its id
```

## Options Considered

1. **Broaden Underwriter `renewal:transition` to include `Identified → Outreach`.**
   - Negative: widens a Distribution-owned surface for all paths; over-grants;
     muddies the F0007 ownership model and Security review.
2. **Dedicated `renewal:draft_outreach` + narrow state-machine exception (CHOSEN).**
   - Positive: least privilege; single-purpose; F0007 model intact; reviewable.
3. **Two-phase commit / distributed transaction across runtimes.**
   - Negative: unjustified complexity for a single interactive gesture.
4. **Full transactional outbox in F0038.**
   - Negative: premature; idempotent engine-id reference is sufficient at F0038
     volume and leaves the outbox seam for later.

## Consequences

- Backend (engine) owns: the four/five new endpoints, the `renewal:draft_outreach`
  policy, the `WorkflowStateMachine` outreach exception, and engine-side atomicity
  of transition + events.
- AI Engineer (Neuron) owns: the `neuron` schema + migrations + repository API,
  owner-scoping of threads, idempotent cross-store record, prompt/card provenance.
- Security review is required (forwarded tokens, new authorization action,
  prompt-injection surface, no model-generated markup).
- Frontend renders only registered components from validated envelope parts.
- No real email is dispatched in v1 under any path.

## Security & Compliance Notes

- `renewal:draft_outreach` is additive and Underwriter-scoped; every draft/mock-send
  is engine-authorized via the forwarded user token (no Python authz).
- The outreach state-machine exception is path-scoped (mock-send under
  `renewal:draft_outreach`), not a general grant.
- Provenance stores ids/versions/hashes/model/cost/latency/trace — never raw
  prompts, raw responses, or customer PII.
- Telemetry carries only correlation ids + metric fields (PII boundary per
  `neuron-companion-telemetry-event.schema.json`).

## Follow-up Actions

- [ ] F0038 `feature` action: implement `neuron` migrations + repository, the
      engine endpoints, the `WorkflowStateMachine` outreach exception, and bind
      `code-index.yaml` to the as-built `neuron/` and `engine/` paths.
- [ ] Decide at implementation whether the optional `neuron.prompt_versions`
      projection table is needed for runtime id→hash resolution.
- [ ] F0039 implements durable thread management UX over this `neuron` schema.
- [ ] Revisit the idempotent-reference pattern vs. a transactional outbox if/when
      cross-store reconciliation becomes continuous (post-F0040).
