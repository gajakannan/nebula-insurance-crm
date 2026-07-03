# Feature Assembly Plan — F0038: Neuron Day-at-a-Glance Shell

**Created:** 2026-07-01
**Author:** Architect Agent (feature run `2026-07-01-90a75ace`)
**Status:** Draft → active for the F0038 `feature` action

> **Purpose:** Implementation execution plan for F0038. It is the primary spec for the
> backend, frontend, and AI Engineer agents. Where it conflicts with raw story text,
> this plan wins (log the reconciliation via `workstate.py decision`).
>
> **Authoritative contracts referenced (do not duplicate — read them):**
> - `planning-mds/architecture/decisions/ADR-027-neuron-companion-a2a-orchestration.md`
> - `planning-mds/architecture/decisions/ADR-028-neuron-companion-persistence-and-outreach-authorization.md`
> - `planning-mds/api/neuron-api.yaml` (`api:neuron-rest`), `planning-mds/api/nebula-api.yaml` (engine additions)
> - `planning-mds/schemas/neuron-*.schema.json`, `renewal-needs-attention-item.schema.json`,
>   `renewal-outreach-draft-request.schema.json`, `renewal-outreach-mock-send-request.schema.json`,
>   `neuron-companion-telemetry-event.schema.json`, `problem-details.schema.json`
> - `planning-mds/architecture/SOLUTION-PATTERNS.md`

## Overview

F0038 stands up the first runnable Neuron companion vertical slice: a stateless Python
`neuron/` FastAPI service (A2A-aligned internal orchestration, registries, versioned YAML
plans, Neuron-owned `neuron.*` persistence), a Day-at-a-Glance React shell with zone-dispatch
and a versioned multi-part message envelope, one live Renewals zone reading the .NET engine
**as the user**, three inert stub zones, one assisted write (renewal outreach draft), a
mock-send that commits the real `Identified → Outreach` transition (fake SMTP), a CRM-scope
guard, and companion telemetry. **LLM is mocked** for this run (deterministic provider behind
the model-router seam). The engine remains the CRM source of truth and authorization boundary
(Casbin ABAC via forwarded authentik token); Neuron re-implements no authorization.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Engine: `renewal:draft_outreach` Casbin action + WorkflowStateMachine outreach exception + 5 endpoints (needs-attention, companion-context, outreach-draft, outreach-mock-send, telemetry ingest) + migration | S0003, S0005, S0006, S0008 (engine side) | Engine is source of truth; Neuron + FE depend on these contracts. Engine-first also satisfies ADR-028 §2 cross-store write order. |
| 2 | Neuron: FastAPI runtime, Agent Card + head + tool registries, YAML plan loader/validator, A2A task manager, `neuron.*` repository (in-memory impl behind interface for F0038), engine client (forwarded token), scope guard + intent classifier, mocked model router | S0001, S0007 | Runtime + registries are the foundation every other Neuron capability plugs into. Scope guard lives in the classifier from day one. |
| 3 | Neuron: Renewals specialist head + stub heads + outreach drafter goal agent + glance assembly + envelope emission + provenance/telemetry emission | S0002 (server side), S0003, S0004, S0005, S0006, S0008 (neuron side) | Heads/agents run on Step 2's runtime and call Step 1's engine endpoints. |
| 4 | Frontend: Day-at-a-Glance shell, zone slots (live Renewals + 3 stubs), component/action registry renderer, in-chat draft editor + `[Send (mock)]`, envelope rendering, telemetry surfaced signals | S0002, S0003, S0004, S0005, S0006 (FE side) | Renders registered components from the validated envelope produced by Steps 2–3. |
| 5 | QE + DevOps: cross-tier E2E, coverage, deployability (new `neuron` service container/compose/env + health), security scans | all | Vertical-slice validation after the tiers integrate. |

## Existing Code (Must Be Modified)

| File | Current State | F0038 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Application/Services/WorkflowStateMachine.cs` | `ValidateRenewalTransition` enforces F0007 ownership (`Identified↔Outreach` Distribution-owned; Underwriter denied) | **Expand** — add a path-scoped exception: `Identified → Outreach` valid for Underwriter **only** under the outreach-mock-send path holding `renewal:draft_outreach` (ADR-028 §3). General `renewal:transition` split unchanged. |
| `engine/src/Nebula.Api/Endpoints/RenewalEndpoints.cs` | renewal CRUD + transition routes | **Expand** — add `GET /renewals/needs-attention`, `GET /renewals/{id}/companion-context`, `POST /renewals/{id}/outreach-draft`, `POST /renewals/{id}/outreach-mock-send`. |
| `engine/src/Nebula.Application/Services/RenewalService.cs` | renewal read/transition logic | **Expand** — needs-attention query (Identified/Outreach within 90d, urgency order, no-contact-30d flag), companion-context assembly, outreach-draft persist (ActivityTimelineEvent + provenance), mock-send (atomic transition + "sent (simulated)" event, no SMTP). |
| `engine/src/Nebula.Application/Interfaces/IRenewalRepository.cs` + `Nebula.Infrastructure/Repositories/RenewalRepository.cs` | renewal queries | **Expand** — needs-attention query with expiry-window + last-broker-contact projection. |
| `engine/src/Nebula.Api/Program.cs` (or Casbin policy resource) + `engine/**/policy.csv` embedded resource | Casbin policy set | **Expand** — add `renewal:draft_outreach` on Underwriter + Admin; sync embedded `policy.csv`. |
| `experience/src/features/neuron/components/NeuronPanel.tsx` | minimal chat panel scaffold | **Rewrite** — Day-at-a-Glance shell host with zone slots + chat + registry renderer. |
| `experience/src/features/neuron/hooks/useNeuronChat.ts`, `types.ts`, `index.ts` | chat scaffold | **Expand** — glance query hook, envelope types, action-callback mutation, component registry. |
| `neuron/config/{agents,models,mcp}.yaml` | scaffold configs | **Expand** — head/tool registry entries, mocked model config, MCP engine tool config. |

## New Files (high level; full per-step lists below)

| File | Layer | Purpose |
|------|-------|---------|
| `neuron/pyproject.toml`, `neuron/Dockerfile`, `neuron/app/main.py` | Neuron | FastAPI runtime + deps + container |
| `neuron/app/orchestration/**` | Neuron | plan loader/validator, A2A task manager, registries, scope guard, classifier |
| `neuron/crm_agents/**` (renewals head, stub heads, outreach drafter) | Neuron | specialist heads + goal agents (non-hyphenated package) |
| `neuron/app/persistence/**` | Neuron | `neuron.*` repository interface + in-memory impl + migration scaffolding |
| `neuron/app/engine_client.py` | Neuron | engine client forwarding the user authentik token |
| `neuron/app/models/mock_provider.py` | Neuron | deterministic mocked LLM behind the model router |
| `engine/src/Nebula.Application/DTOs/RenewalNeedsAttentionItemDto.cs`, `RenewalCompanionContextDto.cs`, `RenewalOutreachDraftRequestDto.cs`, `RenewalOutreachMockSendRequestDto.cs`, `NeuronCompanionTelemetryDto.cs` | Engine | request/response DTOs matching the schemas |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/*_F0038_*.cs` | Engine | any engine schema deltas (provenance columns on timeline event if needed) |
| `experience/src/features/neuron/glance/**`, `experience/src/features/neuron/registry/**` | Frontend | Day-at-a-Glance shell, zone slots, component/action registry |

---

## Step 1 — Engine endpoints, authorization, state-machine exception (S0003/S0005/S0006/S0008 engine side)

### Endpoints (additive to `nebula-api.yaml`; response tables per the OpenAPI + schemas)

| Endpoint | Method | Casbin | Purpose |
|----------|--------|--------|---------|
| `/renewals/needs-attention` | GET | `renewal:read` | needs-attention list (schema `renewal-needs-attention-item`) |
| `/renewals/{renewalId}/companion-context` | GET | `renewal:read` | per-renewal drill context |
| `/renewals/{renewalId}/outreach-draft` | POST | `renewal:draft_outreach` | persist AI draft as renewal `ActivityTimelineEvent` + provenance; **no** transition |
| `/renewals/{renewalId}/outreach-mock-send` | POST | `renewal:draft_outreach` | atomic `Identified→Outreach` `WorkflowTransition` + "sent (simulated)" event; **no** SMTP |
| `/internal/telemetry/neuron-companion` | POST | authenticated (mirrors F0035 SessionTelemetry) | companion telemetry ingest (schema `neuron-companion-telemetry-event`) |

### Needs-attention rule (S0003)
`Identified` OR `Outreach` AND `expiryDate` within 90 days (configurable default), ordered by
urgency (soonest expiry / most overdue first), flag `noBrokerContact30d` when last broker contact
> 30 days. Engine enforces Casbin on the forwarded token; excluded renewals never leak.

### Mutation Traceability — outreach draft (S0005)

| Screen / Entry | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Renewals row → "Draft outreach" CTA | Click generate | `POST /renewals/{id}/outreach-draft` | `RenewalService.PersistOutreachDraftAsync` | `ActivityTimelineEvent` (InternalOnly) | `renewal:draft_outreach` (Underwriter/Admin) | none (append event) | 400 `validation_error` (content constraint), 403 `policy_denied`, 404 `not_found`, 409 `invalid_state` (not Identified/Outreach) | `RenewalOutreachDrafted` timeline event w/ actor, ts, model, promptId/version, contentHash | integration: draft persists + survives reload; non-Underwriter → 403 |

### Mutation Traceability — mock-send (S0006)

| Screen / Entry | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| In-chat draft → `[Send (mock)]` | Click send | `POST /renewals/{id}/outreach-mock-send` | `RenewalService.OutreachMockSendAsync` | `Renewal.WorkflowState`, `WorkflowTransition` | `renewal:draft_outreach` + WorkflowStateMachine outreach exception | valid-transition guard | 403 `policy_denied`, 409 `invalid_transition` (not in Identified), atomic rollback on failure | `WorkflowTransition (Identified→Outreach)` + `RenewalOutreachMockSent` "sent (simulated)" event, both atomic | integration: state=Outreach after reload, both events present, **no SMTP invoked**, invalid-state/unauthorized rejected |

### Casbin Enforcement
- New action `renewal:draft_outreach` on `Underwriter` (scoped) and `Admin` (unscoped). Distribution excluded.
- Sync updated `policy.csv` to the embedded resource location (SOLUTION-PATTERNS Casbin sync).
- WorkflowStateMachine: `Identified→Outreach` valid for Underwriter **only** via the mock-send path under `renewal:draft_outreach`; all other `renewal:transition` ownership unchanged (ADR-028 §3).

### Timeline Events
- `RenewalOutreachDrafted` — EntityType `Renewal`, InternalOnly, payload per `activity-event-payloads` (draft provenance: model, promptId/version, contentHash).
- `RenewalOutreachMockSent` — EntityType `Renewal`, "sent (simulated)", provenance identical shape.
- SMTP path guaranteed not invoked (mock-send never resolves a real transport).

---

## Step 2 — Neuron runtime, registries, orchestration, persistence, scope guard (S0001/S0007)

- **FastAPI app** (`neuron/app/main.py`): `/v1/glance` (GET), `/v1/messages` (POST send), `/v1/actions` (POST action callback), `/health` + `/ready` per `neuron-api.yaml`. Fail-fast on invalid config.
- **Registries**: Agent Card registry (`neuron.orchestrator`, `crm.scope_guard`, `crm.intent_classifier`, `crm.renewals.head`, stub heads, `crm.renewals.outreach_drafter`); tool registry (MCP-shaped engine tools). Every YAML-referenced head/tool/terminal-state must resolve or the asset is rejected (S0001 AC).
- **YAML plan loader/validator** against `neuron-orchestration-plan.schema.json`; explicit terminal states; unknown reference → fail validation.
- **A2A task manager**: `contextId → thread_id`; task/subtask → `agent_runs`; tool calls → `tool_calls`; provenance → `provenance_events` (ADR-027 §5, ADR-028 §1).
- **Persistence** (`neuron.*`): repository interface + **in-memory impl for F0038** (behind interface, per S0001/ADR-028 §1) + migration scaffolding for the durable `neuron` schema (6 tables). Owner-scope threads to authenticated user.
- **Engine client**: forwards the user authentik token on every engine call; typed upstream-unavailable error (not a 500 leak).
- **Scope guard + intent classifier** (S0007): `out_of_scope` path → polite CRM redirect via the message envelope; injection attempts treated as out-of-scope; classifier failure defaults to safe bounded path; guard decisions recorded in the operation store.
- **Model router**: **mocked deterministic provider** (`mock_provider.py`) selected by config for this run; real `claude.py` client left injectable behind the router seam for a later live smoke test.

---

## Step 3 — Heads, goal agent, glance assembly, envelope, provenance/telemetry (S0002–S0006, S0008 neuron side)

- **Renewals head** (`crm.renewals.head`): calls engine `needs-attention` + `companion-context` as the user; returns content payload (`neuron-zone-payload`).
- **Stub heads** (Tasks/Pipeline/Broker activity): return typed `inactive` payload; **make no engine call**; expose no action/CTA (S0004).
- **Glance assembly** (S0002): orchestrator enumerates registered heads, dispatches per zone, assembles slots independently (no cross-zone read/rank); per-zone error isolation.
- **Message envelope** (S0002): versioned multi-part (`text|app|status|sources|actions`), carries `thread_id`; only registered component identifiers + validated props (`neuron-message-envelope.schema.json`); no model-generated markup.
- **Outreach drafter goal agent** (S0005): generate-on-action only; content constraint guard (no premium/quote/terms/binding); engine-first persist (draft endpoint) then idempotent Neuron `agent_run`+`provenance_events` referencing the engine timeline-event id (ADR-028 §2); mock-send action → engine mock-send endpoint then idempotent Neuron record referencing the transition id.
- **Telemetry** (S0008): emit `needs_attention_surfaced` (start) + `draft_ready` (end) correlatable per renewal, plus DAU / %-actioned / drafts-generated / mock-sent counts to the engine telemetry ingest; non-blocking; failures logged; no draft body or PII in payloads.

---

## Step 4 — Frontend Day-at-a-Glance shell (S0002–S0006 FE side)

- **Shell** (`NeuronPanel.tsx` rewrite + `glance/`): render one slot per registered head; Renewals live, 3 stub "not yet active" cards; progressive/isolated slot rendering; responsive per PRD ASCII (desktop grid, <768px single column Renewals-first).
- **Component/action registry** (`registry/`): renders only registered component ids from validated props; unknown id → safe fallback, nothing executable.
- **Chat + draft**: single "Day at a Glance" thread; in-chat editable draft with "AI-generated draft" + `InternalOnly` labels; `[Edit]`; `[Send (mock)]` → action callback; edited content survives reload.
- **Data**: TanStack Query hooks for `/v1/glance` + action callbacks; typed error/empty/auth-required states.
- **UX**: apply `agents/frontend-developer/references/ux-audit-ruleset.md`; `pnpm lint`, `lint:theme`, `build`, `test`, `test:visual:theme` (theme change).

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | 5 endpoints, `renewal:draft_outreach` + policy sync, WorkflowStateMachine exception, needs-attention query, draft/mock-send atomicity, telemetry ingest, migration, unit+integration tests | Backend Developer | Pending |
| AI (`neuron/`) | FastAPI runtime, registries, YAML loader, A2A task mgr, `neuron.*` repo (in-memory+migration scaffold), engine client, scope guard/classifier, heads+drafter, mocked model, provenance/telemetry, contract+unit tests | AI Engineer | Pending |
| Frontend (`experience/`) | Day-at-a-Glance shell, zone slots, registry renderer, draft editor + mock-send, envelope rendering, hooks, component tests + a11y | Frontend Developer | Pending |
| Quality | test plan, cross-tier E2E (glance→draft→mock-send happy + error + unauthorized), coverage, 4 security scan classes | Quality Engineer | Pending |
| DevOps/Runtime | new `neuron` service Dockerfile + compose entry + env contract + health check; deployability smoke; no regression to existing services | DevOps | Pending |

## Dependency Order

```
Step 1 (Backend):   engine endpoints + policy + state-machine exception + migration
  ──── Backend checkpoint: endpoints return per-contract; Casbin denies non-Underwriter; mock-send atomic, no SMTP ────
Step 2 (AI):        neuron runtime + registries + persistence + scope guard (needs Step 1 contracts to call)
Step 3 (AI):        heads + drafter + glance + envelope + provenance/telemetry
  ──── Neuron checkpoint: /v1/glance assembles live+inactive; draft/mock-send call engine as user; envelope validates ────
Step 4 (Frontend):  shell + registry renderer + draft/mock-send UI (needs Step 3 envelope)
  ──── Frontend checkpoint: shell renders zones; only registered components; draft edit survives reload ────
Step 5 (QE+DevOps): cross-tier E2E + coverage + deployability + security scans
```

## Integration Checkpoints

### After Step 1 (Engine)
- [ ] `GET /renewals/needs-attention` returns Identified/Outreach ≤90d, urgency-ordered, no-contact-30d flag; excluded rows never leak.
- [ ] `POST /outreach-draft` persists InternalOnly `ActivityTimelineEvent` with provenance; non-Underwriter → 403.
- [ ] `POST /outreach-mock-send` commits atomic `Identified→Outreach` + "sent (simulated)"; invalid state → 409; **no SMTP**.

### After Step 3 (Neuron)
- [ ] `/v1/glance` assembles Renewals content + 3 `inactive` stubs; one zone failing does not blank the shell.
- [ ] Draft + mock-send go engine-first, then idempotent Neuron record referencing the engine id (ADR-028 §2).
- [ ] `out_of_scope` message → polite CRM redirect via envelope; injection does not bypass the guard.

### Cross-Story Verification
- [ ] Full lifecycle: open glance → surfaced (telemetry start) → draft (persist+provenance) → edit → mock-send (transition+event, telemetry end).
- [ ] All Casbin policies enforced (Underwriter allowed; Distribution denied draft/mock-send; ExternalUser denied).
- [ ] Timeline events ordered and correct; provenance carries model/promptId/version/contentHash, no PII/raw prompts.
- [ ] ProblemDetails format consistent (code + traceId).
- [ ] Frontend renders only registered components; no model-generated markup.

## Integration Checklist

- [ ] API contract compatibility validated (`neuron-api.yaml`, `nebula-api.yaml`)
- [ ] Frontend contract compatibility validated (envelope + registry)
- [ ] AI contract compatibility validated (heads/tools/plans registered; A2A internal profile)
- [ ] Test cases mapped to acceptance criteria (all 8 stories)
- [ ] Developer-owned fast-test responsibilities identified by layer
- [ ] Required runtime evidence artifacts identified (coverage/report/log paths)
- [ ] Framework vs solution boundary reviewed (no `agents/**` drift)
- [ ] Mutation traceability tables completed (S0005 draft, S0006 mock-send)
- [ ] Render-only tests not used to close mutation stories
- [ ] Run/deploy instructions updated (GETTING-STARTED.md + neuron README)

## Knowledge-Graph Binding Plan (baseline for G7 reconciliation)

**Intended semantic-graph delta** — the G7 reconciliation diffs the as-built source against this:

- **New capability source surfaces expected to be bound in `code-index.yaml` (directory globs):**
  - `capability:neuron-orchestration-runtime`, `neuron-zone-dispatch`, `neuron-message-envelope`,
    `neuron-scope-guard`, `neuron-operation-persistence` → `neuron/app/**` (+ subglobs `neuron/app/orchestration/**`, `neuron/app/persistence/**`)
  - `capability:neuron-renewals-head`, `neuron-outreach-drafter` → `neuron/crm_agents/**`
  - `capability:neuron-day-at-a-glance-shell`, `neuron-zone-dispatch` (FE) → `experience/src/features/neuron/**`
  - engine endpoints/authorization → existing `engine/src/Nebula.Api/**`, `Nebula.Application/**` globs (confirm coverage, likely no new glob)
- **New canonical nodes:** the `neuron.*` operation-persistence semantics and the `renewal:draft_outreach` action are new shared semantics introduced by ADR-028; confirm whether they need `canonical-nodes.yaml` entries at G7 (likely yes for `renewal:draft_outreach` and the neuron operation store) or are already seeded by Phase B.
- This is a prediction, not a contract. "Reuses existing engine globs for the endpoint code" is expected for the engine side.

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| First runnable Neuron service — new container/env/health contract | High | DevOps authors Dockerfile + compose entry + env + health; deployability smoke before signoff | DevOps |
| Cross-store partial failure (engine committed, Neuron record not) | Medium | ADR-028 §2 engine-first + idempotent Neuron record keyed on run id; no outbox in F0038 | AI Engineer |
| WorkflowStateMachine exception could over-grant if not path-scoped | High (security) | Path-scoped to mock-send under `renewal:draft_outreach`; Security review required; unit tests for every other path staying denied | Backend + Security |
| Prompt-injection escaping CRM scope / markup execution | High (security) | Scope guard + registry-only rendering (no model markup); Security review | AI Engineer + Security |
| LLM mocked → S0005 not exercised against real provider | Low | Deterministic mock behind router seam; live smoke deferred; disclosed in closeout | AI Engineer |
| Docker Hub pull flakiness (TLS timeouts) | Medium | Baseline stack already up + images cached locally; neuron image builds from local base | DevOps |

## JSON Serialization Convention
camelCase JSON (existing engine convention); ISO-8601 UTC timestamps; UUIDs as strings; envelope `part_type` lower-case enum per `neuron-message-envelope.schema.json`.

## DI Registration Changes
- Engine: register new needs-attention/companion-context/outreach-draft/mock-send service methods + repository additions in `Program.cs`/`DependencyInjection.cs`; register `renewal:draft_outreach` in the Casbin policy load.
- Neuron: register registries, plan loader, `neuron.*` repository (in-memory impl for F0038), engine client, mocked model provider, scope guard/classifier in the FastAPI app factory.

## Casbin Policy Sync
`renewal:draft_outreach` added to `policy.csv` (Underwriter + Admin) — **copy the updated `policy.csv` to the embedded resource location** so the running engine loads it (SOLUTION-PATTERNS Casbin embedded-resource rule).
