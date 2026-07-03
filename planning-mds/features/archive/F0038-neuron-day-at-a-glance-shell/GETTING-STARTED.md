# F0038 — Neuron Day-at-a-Glance Shell — Getting Started

> Skeleton. Implementing agents fill in runtime/setup detail during the `plan`
> and `feature` runs.

## Prerequisites

- [ ] Read [`intake-brief.md`](./intake-brief.md) — the signed-off stakeholder intake (authoritative scope, locked decisions L1–L8, reserved seams).
- [ ] Read the current release framing in [ROADMAP.md](../ROADMAP.md) and the epic context in [README.md](./README.md).
- [ ] Confirm the **AI Engineer role buildout** is complete before starting this feature's `plan`/`feature` runs (epic prerequisite).
- [ ] Refine this feature into vertically-sliced stories and an architecture
  (ADRs + endpoints) via the `plan` action before coding. Do **not** create
  `feature-assembly-plan.md` out of order.
- [ ] Architect must make AI Engineer required and define the backend/frontend/
  Neuron ownership split in the feature assembly plan.
- [ ] Architect must explicitly choose simple versioned YAML orchestration for
  F0038, apply the A2A-aligned internal delegation profile from ADR-027, and
  call out that MS Agent Framework / heavier external orchestration is out of
  scope.
- [ ] Architect must define user-token forwarding for Neuron-to-engine calls.
  Neuron acts on behalf of the user; the engine remains the authorization
  authority.
- [ ] Architect must define **Neuron-owned** operation persistence — the Python
  service owns its `neuron` schema + migrations and writes them directly (no engine
  pass-through): threads, messages, message parts, agent runs, tool calls, and
  provenance (+ prompt/card version references). The Neuron *service* stays stateless.
- [ ] Architect and AI Engineer must use the solution package convention
  `neuron/crm_agents/` for specialist heads; avoid hyphenated Python packages.
- [ ] Architect must define the versioned message envelope and component/action
  registry contract consumed by the frontend.
- [ ] Read
  [`ADR-027`](../../architecture/decisions/ADR-027-neuron-companion-a2a-orchestration.md)
  and the [Neuron Companion C4 ASCII sketches](../../architecture/c4-neuron-companion.md).

## Services to Run (anticipated — confirm at plan/feature)

```bash
# docker compose up -d postgres
# dotnet run --project {PRODUCT_ROOT}/engine/...      # .NET engine (source of truth)
# (neuron) Python service under {PRODUCT_ROOT}/neuron/
# pnpm --dir {PRODUCT_ROOT}/experience dev            # hosts the experience/src/features/neuron panel
```

## How to Verify (target end-to-end, defined fully at plan)

1. Open the companion; the Day-at-a-Glance shell renders with Renewals live and other zones showing "not yet active".
2. The Renewals zone lists the user's renewals needing attention (`Identified`/`Outreach`, within 90 days of expiry).
3. Click the draft CTA on a renewal → an editable, AI-generated, `InternalOnly` outreach draft appears in-chat and is persisted as an `ActivityTimelineEvent`.
4. Mock-send → the renewal transitions `Identified → Outreach` (real `WorkflowTransition` + timeline event), no real email is dispatched.
5. A non-CRM query is politely redirected to CRM topics.
6. Neuron responses use the versioned multi-part envelope and registered
   component identifiers/props; no model-generated markup is rendered.
7. Backend persistence records replayable messages plus agent-run/tool-call/
   prompt-version provenance in the Neuron-owned operation store.
8. Versioned YAML orchestration definitions validate, every referenced
   specialist head/tool/agent has a registered handler, and A2A-shaped task
   traces are persisted through the Neuron-owned operation store.
9. Telemetry events for both baseline timestamps + the minimal secondary metrics are emitted.

## Notes

- **Architecture seams to honor** (even though full features ship later): thread_id keys conversation state; persistence-home ADR; versioned multi-part message envelope; zone-dispatch/head contract; classifier out-of-scope guard. See PRD + intake brief §5.
- **Token forwarding (on-behalf-of):** the architect must validate the engine accepts the user's authentik token on neuron-originated calls so RBAC is enforced unchanged.
- **Guardrail:** assembly, not composition — no cross-zone ranking in v1 (that is the deferred Day-at-a-Glance brain).
- **MCP scope:** F0038 uses component architecture for in-CRM MCP/tool-style
  apps. Complex MCP-UI, sandboxed resources, and external hosts remain Later.

## Neuron Companion runtime — F0038-S0001 (implemented)

The first runnable slice is the **Neuron service bootstrap**: stateless FastAPI
runtime, Agent-Card + tool registries, versioned YAML orchestration plans validated
at startup (fail-fast), the Neuron-owned `neuron.*` operation store (in-memory for
F0038 behind the repository interface; durable schema scaffolded in
`neuron/app/persistence/migrations/0001_neuron_schema.sql`), an engine client that
forwards the user token, and a mocked deterministic model provider.

Layout (`neuron/`):

```
app/                FastAPI service + framework-agnostic runtime core
  main.py           /health, /ready (S0001); /v1/glance|messages|actions return 501 until S0002+
  bootstrap.py      fail-fast assembly of the validated runtime
  orchestration/    agent cards, registries, plan loader/validator, A2A task manager
  persistence/      neuron.* models, repository interface, in-memory impl, migration SQL
  models/           model router + deterministic mock provider
  tools/            MCP-shaped engine tool descriptors (forward the user token)
  engine_client.py  calls the .NET engine AS THE USER; typed upstream errors
  contracts/        vendored JSON Schemas (drift-guarded against planning-mds/schemas)
crm_agents/cards/   Agent Card source assets (ADR-027 §9)
orchestration/plans/day-at-a-glance.plan.yaml
tests/              contract + unit tests
```

### Run the contract/unit tests (no service or Docker needed)

```bash
cd neuron
python3 -m unittest discover -s tests -t .   # 55 tests; needs only pyyaml + jsonschema
# or, with the dev extras installed:
pip install -e '.[dev]' && pytest
```

### Run the service locally

```bash
cd neuron
pip install -e .                              # fastapi, uvicorn, httpx, pyyaml, jsonschema
uvicorn app.main:app --port 8200
curl -s localhost:8200/health | jq            # {"status":"healthy","heads":[...4],"tools":[...5]}
curl -s localhost:8200/ready  | jq            # readiness + loaded plans/agents
```

Config (env overrides): `NEURON_ENGINE_BASE_URL` (default `http://localhost:8080`),
`NEURON_MODEL_PROVIDER` (default `mock` via `config/models.yaml`),
`NEURON_PERSISTENCE` (default `memory`). Invalid/missing orchestration assets make
the service **fail fast** at startup (it will not serve half-configured).
