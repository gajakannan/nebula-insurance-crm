# Neuron Companion — C4 ASCII Sketches

> Status: Planning scaffold for F0038/F0039/F0040. Canonical decision:
> [ADR-027](./decisions/ADR-027-neuron-companion-a2a-orchestration.md).
> Formal Mermaid C4 diagrams can be generated during F0038 Phase B if needed;
> this file captures the architecture shape now in terminal-friendly form.

## C4 L2 — Container View (ASCII)

```text
System: Nebula Insurance CRM

+-------------------+        +--------------------+
| Internal CRM User |        | Authentik          |
| Underwriter /     |        | OIDC identity      |
| Distribution      |        | provider           |
+---------+---------+        +---------+----------+
          |                            |
          | browser session / token    |
          v                            |
+-------------------------------------------------+
| experience/ React CRM Host                      |
| - Neuron panel                                  |
| - registered component renderer                 |
| - component action callbacks                    |
| - sends user token to Neuron                    |
+----------------------+--------------------------+
                       |
                       | chat/actions + user token
                       v
+-------------------------------------------------+
| neuron/ Python FastAPI                          |
| - CRM scope guard                               |
| - intent classifier                             |
| - A2A-aware orchestrator                        |
| - private Agent Card / capability registry      |
| - specialist heads and goal agents              |
| - MCP/tool clients                              |
| - prompt/model runtime                          |
+----------------------+--------------------------+
                       |
                       | engine API calls as user
                       v
+-------------------------------------------------+
| engine/ .NET API                                |
| - CRM source of truth                           |
| - Casbin ABAC                                   |
| - Renewal reads/writes                          |
| - WorkflowTransition / ActivityTimelineEvent    |
+----------------------+--------------------------+
                       |
                       v
+-------------------------------------------------+
| PostgreSQL                                      |
| - application schemas (engine-owned)            |
| - neuron.* schema (Neuron-owned, written direct)|
+-------------------------------------------------+
```

## C4 L3 — Neuron Component View (ASCII)

```text
Container: neuron/ Python FastAPI

+---------------------------------------------------------------+
| API Layer                                                     |
| - chat/message endpoint                                       |
| - component action callback endpoint                          |
| - health/readiness                                            |
+--------------------------+------------------------------------+
                           |
+--------------------------v------------------------------------+
| Scope and Routing                                             |
| - CRM scope guard                                             |
| - out_of_scope redirect path                                  |
| - intent classifier                                           |
+--------------------------+------------------------------------+
                           |
+--------------------------v------------------------------------+
| A2A-Aware Orchestration                                       |
| - YAML plan loader and validator                              |
| - private Agent Card registry                                 |
| - task lifecycle manager                                      |
| - trace/provenance context                                    |
| - fallback/error handling                                     |
+-------------+-------------------------------+-----------------+
              |                               |
+-------------v-------------+     +-----------v-----------------+
| Specialist Head Registry   |     | Goal Agent Registry         |
| - crm.renewals.head        |     | - outreach drafter          |
| - crm.tasks.stub           |     | - renewal summarizer        |
| - crm.pipeline.stub        |     | - scope redirect responder  |
| - crm.broker_activity.stub |     |                             |
+-------------+-------------+     +-----------+-----------------+
              |                               |
              +---------------+---------------+
                              |
+-----------------------------v---------------------------------+
| Tool / Model Layer                                             |
| - MCP-shaped engine tools                                      |
| - model router / LLM clients                                   |
| - prompt templates and prompt versions                         |
| - structured-output validators                                 |
+-----------------------------+---------------------------------+
                              |
+-----------------------------v---------------------------------+
| Backend Integration                                            |
| - engine client with forwarded user token                      |
| - operation persistence adapter                                |
| - telemetry/provenance emitter                                 |
+---------------------------------------------------------------+
```

## C4 L3 — Message And Persistence Flow (ASCII)

```text
1. User sends chat/action from CRM host
2. Neuron creates or resumes thread_id
3. Scope guard accepts CRM intent or returns redirect
4. Intent classifier selects plan/head
5. A2A-aware orchestrator creates task
6. Specialist head delegates to goal agent(s)
7. Goal agent calls MCP/tool clients and model runtime
8. Neuron validates message parts, component props, and actions
9. Persistence — two owners, no cross-runtime transaction:
   - Neuron writes its own neuron.* store directly:
     thread/message/message_parts, agent_run/task trace,
     tool_calls, provenance (+ prompt/card version references)
   - Engine (called as the user) writes CRM business state:
     ActivityTimelineEvent draft, mock-send WorkflowTransition
   - Engine business write is authoritative and commits first;
     the Neuron record references its id and is written idempotently
10. CRM host renders registered envelope parts
```

```text
+-------------+       +-------------------+       +--------------+
| CRM Host    | ----> | Neuron Runtime    | ----> | Engine API   |
+-------------+       +-------------------+       +------+-------+
       ^                       |                         |
       |                       |                         v
       |                       |                  +--------------+
       |                       |                  | PostgreSQL   |
       |                       |                  | neuron.*     |
       |                       |                  +--------------+
       |                       |
       +---- message envelope--+
            text/app/status/
            sources/actions
```

## Boundary Rules

- Neuron is a stateless *service*, but it **owns** its durable agent-operation
  store (`neuron.*` schema: threads, messages, agent_runs, tool_calls,
  provenance) and writes it directly. The engine does not proxy it. Neuron is
  **not** a store for CRM business data.
- User-scoped CRM reads/writes are performed by calling the engine with the
  forwarded user token.
- The engine remains the authorization boundary and CRM source of truth for
  business data. Writes that touch both stores commit the engine business write
  first; the Neuron operation record references the engine id, idempotently.
- A2A is the agent-delegation model; MCP is the tool/resource model.
- F0038 uses private/internal A2A-shaped delegation. Public A2A endpoints and
  external-host/MCP-UI resource surfaces are later decisions.
