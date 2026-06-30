---
template: adr
version: 1.1
applies_to: architect
---

# ADR-027: Neuron Companion A2A-Aligned Orchestration Foundation

## Status

- [ ] Proposed
- [x] Accepted (operator direction, 2026-06-30)
- [ ] Superseded
- [ ] Rejected

> **Review checkpoint:** Accepted now as operator direction to frame the epic.
> It is **reviewed and ratified (or refined) at the F0038 Phase B planning gate**
> (architecture approval in the `plan` run), where the Phase B follow-ups below
> are executed against the actual Phase A stories. Treat this ADR as a frame for
> that session, not a scope ratchet — stories justify what F0038 builds.

## Context

The Neuron Companion epic introduces a conversational CRM assistant that lives in
the React CRM host and is powered by the Python `neuron/` service. F0038 is the
first runnable slice: Day-at-a-Glance shell, Renewals live, inert stub zones,
one draft-outreach write, mock-send, CRM scope guard, and telemetry. F0039 adds
real multi-thread conversation UX. F0040 flips a second specialist head live and
hardens the multi-head platform.

The target operating model is no longer a single assistant prompt. It needs a
top-level intent classifier and CRM-scope guard, specialist heads, goal-oriented
subagents, and MCP/tool calls. We also know the platform will later need richer
agent collaboration and potentially separately deployed agents. Designing this
as ad hoc Python delegation now would create a migration cliff when those agents
need to communicate across process or host boundaries.

The Agent2Agent (A2A) Protocol is an open standard for communication between
independent AI agent systems. Its specification covers capability discovery,
modality negotiation, collaborative task management, secure information
exchange, core operations such as send message / get task / cancel task, a data
model with Task, Message, Part, Artifact, and Agent Card objects, HTTP+JSON and
JSON-RPC bindings, and security expectations for authorization scoping. The
specification also clarifies that A2A complements MCP: A2A is for agent
delegation/collaboration, while MCP is for tools, APIs, data sources, and other
resources.

References:

- A2A Protocol Specification 1.0.0 — https://a2a-protocol.org/latest/specification/
- A2A and MCP relationship — https://a2a-protocol.org/latest/specification/#appendix-b-relationship-to-mcp-model-context-protocol
- F0038 source intake — `planning-mds/features/F0038-neuron-day-at-a-glance-shell/intake-brief.md`
- F0038/F0039/F0040 PRDs — `planning-mds/features/F0038-*`, `F0039-*`, `F0040-*`

## Decision Drivers

- Avoid reworking the orchestration contract when Neuron gains more heads and
  independently deployable agents.
- Keep F0038 shippable by adopting an A2A-aligned internal profile rather than
  a full public multi-agent network.
- Preserve the engine as the source of truth and authorization boundary.
- Keep MCP/tool use separate from agent-to-agent delegation.
- Make thread, task, message, component, prompt, and provenance persistence
  replayable and auditable from day one.
- Allow simple versioned YAML workflow assets to evolve without binding Neuron
  to MS Agent Framework or another heavyweight orchestration runtime.

## Decision

Adopt an **A2A-aligned orchestration foundation** for Neuron Companion starting
in F0038.

1. **A2A internal profile first.** F0038 implements a private/internal
   A2A-shaped profile inside the Neuron service boundary. It models specialist
   heads and goal-oriented subagents as agents with capability manifests, task
   lifecycle, messages, parts, artifacts, and traceable delegation. It does not
   expose a public external-agent federation surface in F0038.

2. **A2A public surface deferred but contract-shaped.** Neuron's first runtime
   may call specialist heads in-process, but the interface is shaped so those
   heads can later move behind HTTP+JSON or JSON-RPC A2A endpoints without
   changing the caller contract. Any externally reachable A2A endpoint, public
   Agent Card, push notification integration, or cross-product/third-party
   agent access requires an explicit later ADR amendment.

3. **Simple YAML remains the workflow asset format.** Versioned YAML plans
   define intent routing, specialist-head dispatch, and goal-agent delegation.
   The YAML schema references A2A concepts (`agent`, `task`, `message`,
   `artifact`, `accepted_output_modes`, `terminal_state`) but does not require
   MS Agent Framework. All YAML plans are schema-validated and every referenced
   agent/head/tool must be registered.

4. **Agent Card / capability registry.** Neuron maintains a registry of private
   Agent Card-like capability manifests for:
   - `neuron.orchestrator`
   - `crm.scope_guard`
   - `crm.intent_classifier`
   - specialist heads such as `crm.renewals.head`
   - goal agents such as `crm.renewals.outreach_drafter`

   F0038 stores these as versioned code-reviewed assets. Public Agent Card
   discovery is deferred.

5. **Task and thread mapping.** A2A `contextId` maps to Neuron `thread_id`.
   A2A task ids map to backend-persisted `neuron.agent_runs`. Subtasks map to
   child `agent_runs` with parent ids. Tool/MCP calls map to
   `neuron.tool_calls`. This keeps Neuron stateless while preserving replay and
   audit.

6. **Message and artifact mapping.** A2A messages/parts/artifacts map to the
   Neuron versioned message envelope:
   - text parts -> `text`
   - data parts for registered app payloads -> `app`
   - task status updates -> `status`
   - retrieved references / citations -> `sources`
   - user-invokable callbacks -> `actions`

   The frontend renders only registered components from validated props. Neuron
   never returns executable markup for the CRM host.

7. **MCP remains the tool protocol.** Specialist heads and goal agents use MCP
   or MCP-shaped tools for CRM reads/writes and other capabilities. A2A
   delegates work between agents; MCP invokes tools/resources underneath an
   agent task.

8. **Auth and source-of-truth boundary.** User-scoped Neuron work forwards the
   user's authentik token to the engine. The engine enforces Casbin ABAC and
   remains the CRM source of truth. Neuron does not duplicate authorization
   decisions or store CRM business state.

9. **Persistence ownership — Neuron owns its operation store directly.** The
   Neuron Python service owns durable Neuron *operation* state end-to-end: its own
   `neuron` Postgres schema, its own migrations, and its own persistence/repository
   API. It writes these tables **directly** — **not** through an engine pass-through:
   - `neuron.threads`
   - `neuron.messages`
   - `neuron.message_parts`
   - `neuron.agent_runs`
   - `neuron.tool_calls`
   - `neuron.provenance_events`

   This does **not** make Neuron stateful (the *service* stays stateless and
   restart-safe) and does **not** re-implement CRM authz: Neuron only **owner-scopes**
   its own threads to the authenticated user (threads are private to their creator).
   CRM *business* state — renewal reads/writes, the draft persisted as a renewal
   `ActivityTimelineEvent`, the mock-send `WorkflowTransition` — is **not** Neuron's;
   it is written through the engine **as the user** (forwarded token, Casbin ABAC),
   which remains the CRM source of truth (per §8). The `neuron` schema may live in the
   same Postgres instance, but the engine never touches `neuron.*` and Neuron never
   touches the application schemas directly.

   **Cross-boundary writes.** A single user gesture can touch both stores — drafting
   writes a Neuron `agent_run`/provenance record **and** an engine timeline event;
   mock-send writes an engine `WorkflowTransition` **and** a Neuron operation record.
   These cannot be one ACID transaction across the two runtimes. Rule: the **engine
   write is authoritative and commits first**; the Neuron record **references the engine
   id** (timeline-event / transition id) and is written **idempotently**, so a partial
   failure is reconcilable, not corrupting. Exact reconciliation/outbox design is a
   Phase B decision.

   **Definitions are checked-in assets, not DB rows.** Agent Card / capability
   manifests and prompt templates are versioned, code-reviewed source assets
   (under `neuron/`) — that is their source of truth (per §4). The database does
   **not** carry an authoring table for them; instead `agent_runs` /
   `provenance_events` persist a **reference** to the active definition (id +
   semantic version + content hash) so every run is replayable and auditable
   without a repo checkout. A thin **read-only registry/lookup** table (e.g.
   `neuron.prompt_versions`, projected from the checked-in assets at deploy) is
   optional and added only if runtime id→hash resolution is needed — decided at
   Phase B. (Rationale: card/prompt definitions change by PR, not at runtime, so
   a DB authoring store would duplicate the source of truth and invite drift.)

   The exact table/column design is finalized during F0038 Phase B.

10. **F0038 implementation floor.** F0038 must bootstrap the minimum useful
    foundation: FastAPI runtime, private Agent Card registry, YAML plan schema,
    specialist-head/goal-agent registry, A2A-shaped task execution trace,
    Neuron-owned `neuron`-schema persistence (migrations + repository API),
    prompt/version provenance, and contract tests.

## Architecture Sketch (ASCII)

```text
User / CRM React Host
    |
    | chat message, action callback, user token
    v
+---------------------------------------------------------------+
| Neuron API (Python/FastAPI)                                   |
|                                                               |
|  +-------------------+      +------------------------------+  |
|  | CRM Scope Guard   |----->| Intent Classifier            |  |
|  +-------------------+      +------------------------------+  |
|                                      |                        |
|                                      v                        |
|  +---------------------------------------------------------+  |
|  | A2A-Aware Orchestrator                                 |  |
|  | - loads versioned YAML plans                           |  |
|  | - resolves private Agent Cards / capabilities           |  |
|  | - creates A2A-shaped tasks and child tasks              |  |
|  | - records trace/provenance via backend                  |  |
|  +----------------------+----------------------------------+  |
|                         |                                     |
|              +----------+-----------+                         |
|              | Specialist Head      |                         |
|              | Registry             |                         |
|              +----------+-----------+                         |
|                         |                                     |
|         +---------------+----------------+                    |
|         |                                |                    |
|  +------v------+                  +------v------+             |
|  | Renewals    |                  | Stub Head   | ...         |
|  | Head        |                  | inactive    |             |
|  +------+------+                  +-------------+             |
|         |                                                    |
|  +------v-----------------+                                  |
|  | Goal Agents            |                                  |
|  | - outreach drafter     |                                  |
|  | - renewal summarizer   |                                  |
|  +------+-----------------+                                  |
|         | MCP/tool calls                                      |
+---------+-----------------------------------------------------+
          |
          | forwarded user token
          v
+---------------------------------------------------------------+
| Engine (.NET source of truth)                                 |
| - Casbin authorization                                        |
| - Renewal reads/writes                                        |
| - ActivityTimelineEvent / WorkflowTransition                  |
+---------------------------------------------------------------+
          |
          v
     PostgreSQL  (engine application schemas + Neuron-owned neuron.* schema)
```

> **Persistence note (per Decision §9):** Neuron writes the `neuron.*` operation
> schema **directly** to Postgres — it owns those migrations and its persistence
> API. The engine owns the application schemas; only CRM business reads/writes flow
> Neuron → engine (as the user). The single bottom arrow above is simplified — in
> practice both Neuron and the engine write Postgres, to different, non-overlapping
> schemas.

## C4 Container Sketch (ASCII)

```text
+-------------------+        +--------------------+
| Internal CRM User |        | Authentik          |
+---------+---------+        +---------+----------+
          |                            |
          | browser session / token    |
          v                            |
+-------------------------------------------------+
| experience/ React CRM Host                      |
| - Neuron panel                                  |
| - registered component renderer                 |
| - sends user token to Neuron                    |
+----------------------+--------------------------+
                       |
                       | chat/actions + user token
                       v
+-------------------------------------------------+
| neuron/ Python FastAPI                          |
| - A2A-aware orchestrator                        |
| - specialist heads / goal agents                |
| - YAML plans / private Agent Cards              |
| - MCP/tool clients                              |
+----------------------+--------------------------+
                       |
                       | engine API calls as user
                       v
+-------------------------------------------------+
| engine/ .NET API                                |
| - CRM source of truth                           |
| - Casbin ABAC                                   |
| - Renewal reads/writes (called as the user)     |
+----------------------+--------------------------+
                       |
                       v
+-------------------------------------------------+
| PostgreSQL                                      |
| - application schemas (engine-owned)            |
| - neuron.* schema (Neuron-owned, written direct)|
+-------------------------------------------------+
```

> Persistence ownership is per Decision §9: Neuron writes the `neuron.*` schema
> directly (it owns those migrations + persistence API); the engine owns the
> application schemas and is called **as the user** for CRM business reads/writes.
> A Neuron → Postgres edge is omitted from the sketch above for readability.

## C4 Component Sketch (ASCII)

```text
Container: neuron/ Python FastAPI

+---------------------------------------------------------------+
| API Layer                                                     |
| - /message:send or internal chat endpoint                     |
| - action callback endpoint                                    |
| - health/readiness                                            |
+--------------------------+------------------------------------+
                           |
+--------------------------v------------------------------------+
| Orchestration Layer                                           |
| - CRM scope guard                                             |
| - intent classifier                                           |
| - YAML plan loader/validator                                  |
| - A2A task manager                                            |
| - private Agent Card registry                                 |
| - specialist-head registry                                    |
+-------------+-------------------------------+-----------------+
              |                               |
+-------------v-------------+     +-----------v-----------------+
| Specialist Heads           |     | Goal Agents                 |
| - crm.renewals.head        |     | - outreach drafter          |
| - crm.tasks.stub           |     | - summarizer                |
| - crm.pipeline.stub        |     | - scope redirect responder  |
+-------------+-------------+     +-----------+-----------------+
              |                               |
              +---------------+---------------+
                              |
+-----------------------------v---------------------------------+
| Tool / Model Layer                                             |
| - MCP-shaped engine tools                                      |
| - model router / LLM clients                                   |
| - prompt templates and versions                                |
| - output validators                                            |
+-----------------------------+---------------------------------+
                              |
+-----------------------------v---------------------------------+
| Backend Integration                                            |
| - engine client with forwarded user token                      |
| - operation persistence adapter                                |
| - telemetry/provenance emitter                                 |
+---------------------------------------------------------------+
```

## Options Considered

1. **Plain internal Python delegation only.**
2. **A2A-aligned internal profile with private Agent Cards and task model
   (CHOSEN).**
3. **Full public A2A server/federation in F0038.**
4. **MS Agent Framework orchestration.**

## Pros / Cons

**Option 1 — Plain internal delegation**
- Positive: fastest initial implementation.
- Negative: creates a likely migration cliff once heads or goal agents become
  independently deployed.

**Option 2 — A2A-aligned internal profile**
- Positive: lays the future contract now while keeping F0038 bounded; cleanly
  separates agent delegation from MCP/tool use; supports replayable task traces.
- Negative: more upfront schema/contract work than simple Python calls.

**Option 3 — Full public A2A server/federation**
- Positive: maximum interoperability from day one.
- Negative: too much surface for F0038; increases auth, discovery,
  compatibility, and external security obligations before a second live head.

**Option 4 — MS Agent Framework**
- Positive: mature graph/runtime concepts.
- Negative: unnecessary runtime dependency for F0038; versioned YAML plus
  A2A-aligned task semantics is enough for current needs.

## Consequences

- F0038 Phase B must define the private Agent Card schema, YAML plan schema,
  A2A task-state subset, message-envelope mapping, persistence contracts, and
  test expectations.
- AI Engineer / Neuron service owns the `neuron.*` operation persistence
  (migrations, repository API, owner-scoping of threads) as a stateless A2A-aware
  Python runtime — Neuron writes its own schema **directly**, not via the engine.
- Backend Developer (engine) owns CRM business persistence and the as-the-user
  endpoints Neuron calls (renewal reads/writes, draft timeline event, mock-send
  workflow transition); the engine enforces Casbin ABAC and is the CRM source of
  truth. The engine does **not** proxy Neuron operation persistence.
- Cross-boundary writes (draft, mock-send) commit the engine business write first
  and reference its id from an idempotent Neuron operation record; full
  reconciliation/outbox design is a Phase B task.
- Frontend Developer must render only registered components from validated
  message envelope parts.
- Security Reviewer is required for F0038 due to forwarded user tokens,
  prompt-injection surface, A2A task scoping, and no model-generated markup
  execution.
- DevOps is required for first runnable Neuron service wiring and health checks.

## Security & Compliance Notes

- A2A authorization scoping is adopted as a design rule: every task/list/get/
  cancel/subscribe operation must be scoped to the authenticated caller and
  must not reveal resources outside that caller's authorization boundary.
- User-scoped CRM tasks must forward the user token to the engine. Service
  identity is reserved for machine-owned operations and infrastructure tasks.
- Agent Cards exposed outside Neuron remain private in F0038. If public Agent
  Cards or extended Agent Cards are introduced later, they require access
  control and cache rules.
- Logs/provenance must store ids, versions, hashes, model metadata, cost,
  latency, and trace ids, not raw prompts, raw LLM responses, or customer PII.

## Follow-up Actions

- [ ] F0038 Phase B to create/update OpenAPI and JSON Schema contracts for
      Neuron operation persistence and message envelope replay.
- [ ] F0038 Phase B to define the cross-store write-consistency pattern:
      engine-first authoritative business write + idempotent Neuron operation
      record referencing the engine id (timeline-event / transition id);
      outbox/reconciliation if needed.
- [ ] F0038 Phase B to define the private Agent Card manifest schema and YAML
      plan schema.
- [ ] F0038 Phase B to create a formal Mermaid C4 component diagram if the ASCII
      sketch in this ADR needs renderer-backed publication.
- [ ] F0040 to harden the head/agent contract on the first real second consumer.
- [ ] Later external-host or cross-product agent work to decide whether to expose
      public A2A HTTP+JSON/JSON-RPC endpoints and public Agent Cards.
