# KG Reconciliation — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Architect · **Stage:** G7 · **Recorded:** 2026-07-02

**Result:** PASS

The as-built F0038 source is now bound into the knowledge graph. The feature→capability→
endpoint mappings existed from Phase B (`feature-mappings.yaml`), and the canonical nodes were
pre-declared; G7 added the missing **code-index.yaml** node→source bindings and regenerated the
coverage report. The KG validator passes.

## Binding Delta

Before G7, `code-index.yaml` carried **no** neuron bindings (the file predates the neuron
runtime). Added **14** `node_bindings` entries binding the pre-declared F0038 canonical nodes to
their as-built implementation paths (total bindings 186 → 200):

**Capabilities (10):**
- `capability:neuron-companion` → `neuron/app/**`, `neuron/crm_agents/**`, `neuron/orchestration/**`, `neuron/Dockerfile`, `docker-compose.yml`
- `capability:neuron-orchestration-runtime` → `neuron/app/runtime.py`, `bootstrap.py`, `app/orchestration/**`, `orchestration/plans/**`, `crm_agents/cards/**`
- `capability:neuron-day-at-a-glance-shell` → `app/orchestration/glance.py`, FE `DayAtAGlance.tsx`, `NeuronPanel.tsx`, `useGlance.ts`
- `capability:neuron-zone-dispatch` → `glance.py`, `zone_heads.py`, `heads.py`, FE `ZoneSlot.tsx`
- `capability:neuron-message-envelope` → `app/envelope.py`, `app/contracts/**`, FE `MessagePartView.tsx`, `registry/**`
- `capability:neuron-renewals-head` → `zone_heads.py` + engine `RenewalEndpoints/RenewalService/Renewal*Dto/RenewalRepository` + FE renewals components
- `capability:neuron-scope-guard` → `app/scope_guard.py`, `app/messages.py`, FE `Composer.tsx`, `useSendMessage.ts`
- `capability:neuron-outreach-drafter` → `app/outreach_drafter.py`, `app/actions.py` + engine `OutreachContentGuard/RenewalOutreachDtos/WorkflowStateMachine` + FE `OutreachDraftEditor.tsx`
- `capability:neuron-operation-persistence` → `app/persistence/**`
- `capability:neuron-companion-telemetry` → `app/telemetry.py` + engine `NeuronCompanionTelemetry{Endpoints,Service,Models}.cs`

**Endpoints (4):**
- `endpoint:neuron-glance` → `app/main.py`, `app/orchestration/glance.py`
- `endpoint:neuron-message-send` → `app/main.py`, `app/messages.py`
- `endpoint:neuron-action-callback` → `app/main.py`, `app/actions.py`
- `endpoint:neuron-companion-telemetry` → engine `NeuronCompanionTelemetryEndpoints.cs`, `app/telemetry.py`

Each binding lists the tests that exercise the node (neuron `unittest` modules, engine
`Nebula.Tests/Unit/{Neuron,Renewals}` + `WorkflowStateMachineOutreachTests` + the telemetry
integration test).

## Canonical Nodes

No new canonical nodes were required — all 14 F0038 nodes were pre-declared by the architect in
Phase B:
- Capabilities: `canonical-nodes.yaml` lines 848–966 (`neuron-companion` … `neuron-companion-telemetry`).
- Endpoints: `canonical-nodes.yaml` lines 2087–2117 (`neuron-companion-telemetry`, `neuron-glance`, `neuron-message-send`, `neuron-action-callback`).
- Security-relevant nodes already canonical: the least-privilege Casbin `renewal:draft_outreach` action gating draft + mock-send, and the Neuron-owned `neuron.*` operation store (ADR-028). No canonical-node additions or renames in this run.

## Validator Results

`python3 scripts/kg/validate.py --write-coverage-report` → **exit 0**. `coverage-report.yaml`
regenerated from current KG state (reflected in `changed_paths`). code-index.yaml parses (200
bindings; 14 neuron).

Pre-existing, **out-of-F0038-scope** warnings remain and are unchanged by this run:
`symbol-index.yaml` (dated pre-F0038) carries symbol-drift warnings for renewal/workflow C#
symbols (e.g., `stub-renewal-repository.add-async` references). These predate F0038, are not
introduced by it, and belong to a separate symbol-index regeneration; the node-level code-index
bindings that G7 owns are complete and clean.

## Handoff to Closeout

The KG is reconciled to the as-built F0038 source and validates. Ready for G8 PM closeout.

**Caveat carried from code review (`[high]`):** the bindings point at as-built working-tree
source that is **not yet committed** (see `code-review-report.md`). The operator will commit all
changes together before merge; once committed, the code-index globs resolve against the
committed tree unchanged (the paths are identical). This does not affect KG validity now but is
a precondition for merge.

**Result:** PASS
