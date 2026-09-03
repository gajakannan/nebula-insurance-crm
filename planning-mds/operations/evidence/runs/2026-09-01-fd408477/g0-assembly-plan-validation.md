# G0 — Assembly Plan Validation — F0040 run `2026-09-01-fd408477`

**Gate:** G0 — Architect assembly plan authoring and validation  
**Role:** Architect (`agents/architect/SKILL.md` activated)  
**Feature:** F0040 — Neuron Second Specialist Head (Broker Activity)  
**Primary spec:** `planning-mds/features/F0040-neuron-second-specialist-head/feature-assembly-plan.md`  
**Mode:** `clean` · **Slice order source:** `assembly-plan`  
**Date:** 2026-09-01

## Step 0 — Authoring disposition

The primary spec already exists and was approved by the Architect and operator in plan run
`2026-08-30-af27c9c1`. G0 therefore reconciled it against the current source rather than
overwriting it. The approved build order remains:

1. secure the existing internal Broker timeline projection;
2. harden the shared Neuron head execution and startup contract;
3. activate the Broker activity head, direct route, replay, and bounded telemetry;
4. add the registered frontend presentation and retry/auth states;
5. complete cross-tier quality, security, code-review, and architecture evidence.

## Step 0.5 — Validation checklist

| Check | Result | Basis |
|-------|--------|-------|
| Scope split matches the three stories | **PASS** | S0001 maps to engine + Neuron + frontend read behavior; S0002 maps to trusted direct routing and durable replay; S0003 maps to the shared executor, startup validation, failure isolation, and telemetry. No write, filter, third-head, or cross-zone composition work is included. |
| Agent dependencies are explicit | **PASS** | Engine authorization/query repair precedes the Neuron tool; the shared executor precedes Broker activation; the validated component contract precedes frontend rendering; QE/review follows the assembled slice. |
| Integration checkpoints are feasible | **PASS** | Existing application containers cover engine, Neuron, experience, Postgres, and authentik. Runtime health is handed to G1; role-scope, two-head failure, replay, rendering, telemetry, and p95 checks are observable. |
| Artifact ownership is conflict-free | **PASS** | Architect owns ADR/contracts/assembly plan; Backend owns engine changes; AI Engineer owns `neuron/**`; Frontend owns `experience/**`; QE owns tests and G2 reports; Security and Code Reviewer own their G3 reports; PM owns closeout. Shared-semantics changes route back to Architect. |
| Current source matches the plan baseline | **PASS** | `TimelineEndpoints` still exposes the coarse internal path and BrokerUser branch; `TimelineService` still maps `entityName` to null; `TimelineRepository` scopes only by entity type/id; bootstrap still binds non-Renewals specialist heads to stubs; Glance still owns duplicated lifecycle/error handling; frontend registry still lacks Broker activity. These are precisely the plan's intended deltas. |

## Scope and signoff disposition

- `frontend_in_scope=true`: planned code under `experience/src/features/neuron/**` and the shared timeline presentation.
- `runtime_bearing=true`: planned engine and Neuron runtime changes require G1 preflight and G2 deployability evidence.
- `deployment_config_changed=false`: no service, port, environment variable, migration, CI, or topology change is planned.
- `security_sensitive_scope` remains provisional until G2 path reconciliation; Security Reviewer is already required because the feature changes record-level authorization and fail-closed dispatch behavior.
- Required roles re-affirmed: Quality Engineer, Code Reviewer, Security Reviewer, AI Engineer, and Architect. DevOps signoff remains not required, while deployability evidence is still required because the feature is runtime-bearing.

## Knowledge-graph binding baseline

G7 will compare the as-built code against the plan's declared reuse/addition set: the Broker activity head capability, ADR-037, Broker list schema, existing timeline endpoint/policies/entities, and the established Neuron orchestration/telemetry capabilities. Implementation code bindings are intentionally deferred until source paths exist.

## Verdict

**PASS.** The approved plan is scope-complete, dependency-ordered, implementation-ready, and ownership-clean. No plan/story reconciliation or shared-semantics change is required at G0.

**Validator:** `validate-feature-evidence.py --stage G0`; result recorded by the gate runtime.
