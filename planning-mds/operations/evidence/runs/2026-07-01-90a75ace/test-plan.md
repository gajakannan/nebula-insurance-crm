# Test Plan — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Quality Engineer · **Stage:** G2
**Authored:** 2026-07-02 · **Mode:** clean · **LLM:** mocked (deterministic `mock_provider.py` behind the model-router seam)

## Scope Under Test

F0038 delivers a stateless Neuron companion (FastAPI) that assembles a Day-at-a-Glance
across three runtimes — the .NET engine (authoritative CRM state + telemetry ingest), the
Neuron orchestration service, and the React `experience/src/features/neuron` surface. The
eight stories S0001–S0008 span bootstrap, zone-dispatch/envelope, live Renewals read, inert
stub zones, outreach draft, mock-send + workflow transition, CRM scope guard, and companion
telemetry.

## Test Strategy & Tiers

| Tier | Runtime | Tooling | What it proves |
|---|---|---|---|
| Unit | Neuron (Python) | `unittest` | Deterministic logic: classifier, guard policy, plan validation, envelope shaping, persistence repo, telemetry builders, auth/persona extraction |
| Unit | Engine (.NET) | xUnit | Telemetry validation registry, workflow state-machine transitions, outreach content constraints (no Docker) |
| Contract | Neuron | `unittest` (`test_schema_drift`, `test_envelope`, `test_agent_cards`) | Vendored contract schemas match canonical `planning-mds/schemas/*`; message-envelope + agent-card shape |
| Integration | Engine (.NET) | xUnit + **Testcontainers Postgres** | End-to-end HTTP: `/v1/glance` reads, outreach draft persist, mock-send transition atomicity, telemetry ingest 202/403/400, authorization (403) enforced engine-side |
| Component | Frontend (React) | `vitest` + Testing Library + jsdom | Shell renders one slot per head, registry rejects unregistered component ids (safe fallback), composer send flow, drill → thread-message |

**Determinism:** the LLM is mocked for this run, so classifier/draft outputs are fixed —
no network model calls, no provider key. Prompt-injection and scope-guard behavior are
purely deterministic (injection markers checked first), so the guard is testable without a
live model.

## Story → Test-Area Coverage Plan

| Story | Primary test areas | Runtime(s) |
|---|---|---|
| S0001 Bootstrap | `test_bootstrap`, `test_registries`, `test_agent_cards`, `test_plan_validation`, `test_persistence`, `test_task_manager`, `test_engine_client`, `test_schema_drift` | Neuron |
| S0002 Shell + zone-dispatch + envelope | `test_glance`, `test_envelope`; FE `DayAtAGlance.test`, `componentRegistry.test` | Neuron, FE |
| S0003 Live Renewals read | `test_renewals_head`; engine `RenewalNeedsAttentionTests`, `RenewalCompanionContextTests`, `NudgePriorityTests` | Neuron, Engine |
| S0004 Stub zones (inactive) | `test_stub_zones`; FE shell placeholder assertions | Neuron, FE |
| S0005 Outreach draft | `test_actions` (draft); engine `RenewalOutreachTests`; FE draft-editor render | Neuron, Engine, FE |
| S0006 Mock-send + transition | `test_actions` (mock-send); engine `WorkflowStateMachineOutreachTests`, `WorkflowEndpointTests`, `WorkflowServiceTests` | Neuron, Engine |
| S0007 CRM scope guard | `test_scope_guard` (21); FE composer send | Neuron, FE |
| S0008 Companion telemetry | `test_telemetry`, `test_auth` (persona); engine `NeuronCompanionTelemetryServiceTests`, `NeuronCompanionTelemetryEndpointTests` | Neuron, Engine |

## Environments & Commands

- Engine: `dotnet test tests/Nebula.Tests` — Testcontainers spins ephemeral Postgres; Docker required.
- Neuron: `python3 -m unittest discover -s tests -t .` (Python 3.12 runtime; the deployed container image).
- Frontend: `pnpm exec vitest run src/features/neuron` + `tsc -b` + `eslint`.

## Out of Scope for this run

- Live-LLM behavioral testing (LLM mocked by design this run).
- Real SMTP dispatch (mock-send simulates; asserting no real email is a positive test).
- Load/performance and DAST against a deployed environment (platform/CI concern; see `deployability-check.md` and the security-scan notes in `test-execution-report.md`).

**Result:** PASS — test strategy covers every S0001–S0008 acceptance criterion across the three runtimes at the appropriate tier; execution results are in `test-execution-report.md`.
