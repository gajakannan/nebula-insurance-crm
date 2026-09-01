# Feature Assembly Plan — F0040: Neuron Second Specialist Head (Broker Activity)

**Created:** 2026-08-31
**Author:** Architect (plan run `2026-08-30-af27c9c1`)
**Status:** Approved — Phase B gate `2026-08-30-af27c9c1`

> **Purpose:** Implementation execution plan for F0040. It is
> the primary build-order specification for Backend, Frontend, AI Engineer, QE,
> Security, Code Review, and Architect roles. Raw approved stories remain authoritative
> for product outcomes; any implementation-time reconciliation is recorded in workstate.
>
> **Authoritative references (read them, do not duplicate them):**
> - `PRD.md`, `F0040-S0001..S0003`, and `acceptance-criteria-checklist.md`
> - `planning-mds/architecture/decisions/ADR-027-neuron-companion-a2a-orchestration.md`
> - `planning-mds/architecture/decisions/ADR-028-neuron-companion-persistence-and-outreach-authorization.md`
> - `planning-mds/architecture/decisions/ADR-035-neuron-durable-conversations-and-phi-intent-resolution.md`
> - `planning-mds/architecture/decisions/ADR-037-neuron-second-specialist-head-contract.md`
> - `planning-mds/api/nebula-api.yaml`, `planning-mds/api/neuron-api.yaml`
> - `planning-mds/schemas/neuron-broker-activity-list.schema.json`, `timeline-event.schema.json`,
>   `neuron-zone-payload.schema.json`, `neuron-message-envelope.schema.json`,
>   `neuron-agent-card.schema.json`, `neuron-orchestration-plan.schema.json`, and
>   `neuron-companion-telemetry-event.schema.json`
> - `planning-mds/architecture/SOLUTION-PATTERNS.md`

## Overview

F0040 activates the existing Broker activity stub as Neuron's second live specialist
head. The same read-only newest-20 feed is available in Day at a Glance and through
`broker_activity.list`; the direct response is persisted as a registered app part and
replays without re-querying. The first second consumer also extracts a shared,
fail-fast `HeadExecutor` contract for lifecycle, timeout, component validation,
provenance, failure isolation, and telemetry.

The engine remains the CRM source of truth and authorization boundary. The implementation
repairs the existing internal Broker timeline projection so record scope is applied before
count/order/limit and the Broker name is returned. It does not add a Neuron-specific feed,
a policy rule, a business entity/event, a database migration, or a broker write.

## Phase B Reconciliation Finding

The approved Phase A assumption says the existing Broker feed is authorization-scoped.
The OpenAPI and archived F0001/F0002 stories agree, but the as-built internal
`TimelineService`/`TimelineRepository` path currently checks coarse
`timeline_event:read` only, does not apply broker-id scope, and maps `entityName` to null.
F0040 cannot safely reuse that path unchanged. ADR-037 therefore makes query-time scope
and broker-name projection the first build step. This is a conformance repair to the
existing contract, not a new product capability.

## Build Order

| Step | Scope | Stories | Why this order |
|------|-------|---------|----------------|
| 1 | Engine: secure the existing internal Broker timeline projection and contract tests | S0001 | Neuron must never receive an unauthorized row; every other layer depends on the corrected response. |
| 2 | Neuron: component contract registry, explicit live-head factories, shared `HeadExecutor`, card/plan hardening | S0003 | Establish the two-consumer runtime contract before activating the second consumer. |
| 3 | Neuron: Broker activity tool/head/card/plan/catalog route, replay, telemetry/evals | S0001, S0002, S0003 | Runs on Steps 1–2 and uses one adapter for both entry points. |
| 4 | Frontend: registered Broker list + shared timeline presentation + retry/auth states | S0001, S0002 | Render only the validated component emitted by Step 3. |
| 5 | Cross-tier QE/Security/Code Review/Architect evidence | all | Prove role scope, replay, two-way failure isolation, safe rendering, telemetry minimization, and p95. |

## Existing Code That Must Be Modified

### Engine

| File | Current state | Required change |
|------|---------------|-----------------|
| `engine/src/Nebula.Api/Endpoints/TimelineEndpoints.cs` | BrokerUser has a special safe branch; internal branch checks only coarse Casbin and accepts generic paging. | Parse `internalOnly`; reject invalid combinations and external principals; retain existing BrokerUser branch; route internal Broker reads to the scoped projection. |
| `engine/src/Nebula.Application/Services/TimelineService.cs` | Generic unscoped repository call; `entityName=null`; actor fallback exists. | Resolve default `ProjectionVisibility` via `IDistributionScopeService`; call scoped Broker projection; preserve stored description; return Broker name and `Unknown User`. |
| `engine/src/Nebula.Application/Interfaces/ITimelineRepository.cs` or a focused new read interface | Generic domain-event paging only. | Prefer a focused `IBrokerActivityFeedRepository` so unrelated timeline fakes do not gain a Neuron-shaped concern; return the existing `TimelineEventDto` page. |
| `engine/src/Nebula.Infrastructure/Repositories/TimelineRepository.cs` | Filters only by `EntityType`/`EntityId`. | Apply visibility before count/order/page and join `Broker` for legal name in one query. Keep `OccurredAt DESC`; no N+1 lookup. |
| `engine/tests/Nebula.Tests/{Unit,Integration}/**Timeline**` | Pagination shape only; no internal role-scope/name matrix. | Add role-scope, excluded-row/count, newest-20, name, actor fallback, BrokerUser/internalOnly denial, and default compatibility coverage. |

### Neuron

| File | Current state | Required change |
|------|---------------|-----------------|
| `neuron/app/bootstrap.py` | Hard-codes Renewals live; every other specialist card becomes a stub even if active. | Replace with explicit live-head factories. Active-without-factory is a startup error; inactive cards alone receive `StubZoneHead`. |
| `neuron/app/orchestration/{agent_card,plan,registries}.py` | Cards/plans resolve agents/tools but not component ownership or step timeout parity. | Load `components` and `timeout_ms`; cross-check active card, handler, skill, tools, output modes, component contracts, and timeout before readiness. |
| `neuron/app/orchestration/zone_heads.py` | Renewals implementation and generic stub share one module. | Retain domain adapters; add `BrokerActivityZoneHead`; move duplicated execution lifecycle out to `HeadExecutor`. |
| `neuron/app/orchestration/glance.py` | Owns run lifecycle and catches every head exception locally. | Delegate execution to shared executor; fan out all four heads concurrently; preserve per-zone results and request-level 401 handling. |
| `neuron/app/messages.py` | Duplicates run/try/catch and turns every head failure into generic text. | Use shared executor; persist registered broker app/empty/error response once; record rejected broker routes without an engine call. |
| `neuron/app/envelope.py` / component contract registry | Flat id allow-list; no props validator association. | Register component id → vendored schema validator; include `broker_activity.recent_list`; enforce card ownership and props before app-part emission. |
| `neuron/app/tools/engine_tools.py` | Five Renewals/telemetry tools. | Add `engine.timeline.list_broker_activity` for the existing timeline route; forwarded user token only. |
| `neuron/crm_agents/cards/crm.{renewals,broker_activity}.head.card.yaml` | Renewals live with no component declaration; Broker activity inactive/auth none. | Version the affected cards; declare component ids; activate Broker activity with user-token auth and its one read tool. |
| `neuron/orchestration/plans/day-at-a-glance.plan.yaml` | Version 1.0; Broker activity step has no tool; no step timeout. | Version 1.1; Renewals/Broker activity tools and 2000 ms timeouts; Tasks/Pipeline remain inactive. |
| `neuron/config/intent-catalog.yaml` | Broker domain/action reserved but inactive. | Activate only domain `broker_activity` and action `broker_activity.list`; keep all writes absent and Tasks/Pipeline inactive. |
| `neuron/app/telemetry.py` | Renewal adoption events only. | Emit `specialist-head-outcome` for glance/conversation terminal outcomes; no raw data. |
| `neuron/app/contracts/**` + `neuron/tests/test_schema_drift.py` | Existing planning schemas vendored for offline runtime validation. | Vendor the changed agent-card/plan/zone/telemetry schemas and new Broker list schema; extend drift/compile coverage. |
| `neuron/evals/intent/v1/**` | Broker domain is inactive in shipped evaluation expectations. | Add read variants plus filtered/write/unknown/cross-domain rejection cases without weakening F0039 thresholds. |

### Frontend

| File | Current state | Required change |
|------|---------------|-----------------|
| `experience/src/features/neuron/registry/componentRegistry.tsx` | Three Renewals/outreach components. | Register `broker_activity.recent_list` with AJV props validation and safe fallback. |
| `experience/src/features/neuron/components/ZoneSlot.tsx` | Typed states, but no per-zone retry callback. | Render Broker error copy and retry control without changing zone payload shape; status remains non-color-only. |
| `experience/src/features/neuron/components/DayAtAGlance.tsx` | Loads one complete glance response. | Wire zone retry to the existing query refetch and retain request-level auth-required behavior. |
| `experience/src/features/timeline/components/{ActivityFeed,ActivityFeedItem}.tsx` | Dashboard-specific list presentation; current Dashboard asks for 12. | Extract/reuse a semantic Broker timeline list/item presentation. Dashboard keeps its current request unless separately changed; Neuron renders up to 20 from props. |
| `experience/src/features/neuron/types.ts` | Generic app/zone types already replay-compatible. | No envelope bump; add local typed props only where useful. |
| `experience/src/features/neuron/contracts/` (new) | No frontend-vendored Broker props contract. | Vendor the Broker list schema used by AJV and add drift coverage against planning source. |

## New Implementation Files

| File | Owner | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Application/Interfaces/IBrokerActivityFeedRepository.cs` | Backend | Focused permission-safe Broker event projection interface. |
| `neuron/app/orchestration/head_executor.py` | AI Engineer | Shared run/timeout/validation/failure/telemetry lifecycle for both live heads and both entry points. |
| `neuron/app/components.py` | AI Engineer | Server component id → schema contract registry and startup validation. |
| `neuron/app/contracts/neuron-broker-activity-list.schema.json` | AI Engineer | Vendored runtime copy of the Phase B props schema. |
| `neuron/tests/test_broker_activity_head.py` | AI Engineer | Mapping, limit/order/empty/error/tool/provenance contract. |
| `neuron/tests/test_head_executor.py` | AI Engineer | Two-head startup, timeout, component, failure, telemetry, and auth classification. |
| `experience/src/features/neuron/components/BrokerActivityList.tsx` | Frontend | Registered semantic list with Broker 360 links and relative time. |
| `experience/src/features/neuron/contracts/neuron-broker-activity-list.schema.json` | Frontend | Vendored AJV contract. |
| `experience/src/features/neuron/components/BrokerActivityList.test.tsx` | Frontend | Schema/render/link/accessibility cases. |

Names may be adjusted to repository conventions during the feature action, but the
ownership boundaries and contract behavior above are fixed.

## Step 1 — Engine Read Boundary (S0001)

### Existing Endpoint Contract

```text
GET /timeline/events
  entityType=Broker
  page=1
  pageSize=20
  internalOnly=true
Authorization: Bearer <forwarded user token>
```

- `internalOnly` is additive and defaults false. It is valid only for Broker queries.
- BrokerUser/external + `internalOnly=true` → 403 and no rows/counts/names/ids.
- Internal roles still require existing `timeline_event:read`; no policy.csv change.
- Default BrokerUser `limit` behavior and broker-safe DTO remain byte-compatible.
- Internal Broker rows are filtered by the latest canonical
  `IDistributionScopeService` authority union before `CountAsync`, order, and take.
- Response remains the existing camelCase paginated `TimelineEvent` envelope.
- `eventDescription` is the stored string, never regenerated.
- `entityName` comes from Broker legal name; missing actor becomes `Unknown User`.
- Query uses/retains the `(EntityType, OccurredAt DESC)` path and scoped Broker join;
  verify p95 <2s with representative data. No cache or migration is planned.

### Authorization Matrix

| Role | Internal-only result |
|------|----------------------|
| DistributionUser | Existing authority-union broker ids only |
| DistributionManager | Existing region/hierarchy authority only |
| RelationshipManager | Managed/owned broker authority only |
| ProgramManager | Authorized program/hierarchy broker authority only |
| Underwriter | Broker authority linked to accessible/assigned work |
| Admin | `SeeAll` internal Broker events |
| BrokerUser / ExternalUser | 403, no data or count |

The exact authority sets come from the engine's verified principal and canonical
distribution scope service; Neuron never interprets roles or broker ids.

## Step 2 — Shared Head Contract (S0003)

### Startup Invariants

For every specialist-head card:

1. Card id is unique and maps to an explicit live handler factory or an inactive stub.
2. `active:true` may not bind a stub; `active:false` may not name tools/components.
3. Engine-touching active cards use `auth_mode:user_token`.
4. Card tools/components, plan tools, skill id, accepted output modes, timeout, and
   handler output contract resolve and agree.
5. Every component schema compiles from the offline vendored registry.
6. Invalid assets fail startup/readiness; no partially valid runtime serves traffic.

### Runtime Contract

`HeadExecutor.execute(head_card_id, thread, token, owner, entry_point)`:

1. Resolve trusted card/handler/plan step.
2. Begin A2A child run and start monotonic timing.
3. Execute under `timeout_ms` without serializing another head.
4. Validate zone shape, allowed component id, and component props.
5. Complete run as `completed` or `failed`; tool calls keep digest-only provenance.
6. Emit best-effort head outcome telemetry.
7. Return typed payload, or rethrow upstream 401 to the existing auth flow.

Glance dispatch uses `asyncio.gather` across all four independently executed heads.
Conversation dispatch invokes the same executor for one trusted head. Neither path reads,
ranks, merges, or rewrites another head's result.

## Step 3 — Broker Head, Intent, Replay, Telemetry (S0001/S0002/S0003)

### Broker Head Mapping

- Tool: `engine.timeline.list_broker_activity` → existing endpoint/params above.
- `data=[]` → `ZonePayload(zone_id=broker_activity, zone_status=empty,
  detail="No recent broker activity.")`.
- One to twenty rows → `content`, component `broker_activity.recent_list`, props
  validated by the new schema.
- Timeout/5xx/transport → `error`, safe `Unable to load broker activity.` detail;
  glance offers explicit refetch, conversation persists one bounded terminal reply.
- 401 → existing request-level authentication-required path.
- 403 → no data and bounded rejected outcome; no fallback to BrokerUser-safe fields.

### Direct Route

- Activate only `broker_activity.list`; the catalog remains the trusted head resolver.
- Deterministic/shadow routing and future direct Phi routing share the same head.
- Unknown/inactive/cross-domain/write actions fail closed with no tool call.
- Named-broker/date/type filters are unsupported; deterministic policy/structured
  entity checks select application-owned unsupported copy rather than returning a
  misleading unfiltered list.
- Preserve F0039 persist-first ordering and client-message idempotency.
- Persist exactly one terminal assistant envelope. History reads return stored parts and
  must not call the engine or replace old props with current data.

### Telemetry Shape

`specialist-head-outcome` requires:

| Field | Rule |
|-------|------|
| `user_id`, `thread_id` | Existing stable internal correlation only |
| `head_run_id` | Present after a run begins; absent for pre-run rejection |
| `zone_id` | Registered id (`broker_activity` for F0040 attempts) |
| `entry_point` | `glance` or `conversation` |
| `terminal_result` | `content`, `empty`, `error`, or `rejected` |
| `latency_ms` | Monotonic whole milliseconds |

Forbidden in telemetry/log/provenance: user text, event description, broker name/id in
metric payloads, component props, token, raw prompt/response, or credentials. Existing
tool-call/run records retain only bounded ids/status/digests. Telemetry failure is logged
by exception class/reason code only and never changes the response.

## Step 4 — Registered Frontend Presentation (S0001/S0002)

- `BrokerActivityList` consumes only schema-valid props and renders semantic list markup.
- Each row shows event type, stored description, broker name, actor, and relative time.
- Broker name uses the existing `getEntityPath("Broker", entityId)` route; no new route.
- Reuse/extract the established timeline item presentation rather than duplicate domain
  formatting. Dashboard's current 12-row request is not silently changed by F0040.
- Unknown component or invalid props renders the static safe fallback and echoes no
  untrusted id/value.
- Empty uses the typed zone/text path, not a zero-row component.
- Error has accessible alert copy and retry; auth failures use the existing session
  continuity/auth-required state. Status meaning is not conveyed by color alone.

## Story-to-Component Map

| Story | Primary implementation | Contracts |
|-------|------------------------|-----------|
| S0001 | Engine scoped projection; Broker tool/head; Broker list component | `nebula-api.yaml`, timeline DTO/schema, Broker list schema, zone schema |
| S0002 | Intent catalog/fixtures; message dispatcher via `HeadExecutor`; stored envelope replay | `neuron-api.yaml`, intent catalog/schemas, message envelope |
| S0003 | Agent card/plan/component registry hardening; executor; telemetry | agent-card, orchestration-plan, zone, telemetry schemas; ADR-037 |

## Persistence and Mutation Traceability

F0040 creates no Broker, Contact, Timeline, WorkflowTransition, or other CRM business
mutation. `GET /timeline/events` is a read and produces no CRM audit event.

The conversational flow does mutate the existing Neuron operation store:

| Trigger | Existing store write | Failure/replay rule |
|---------|----------------------|---------------------|
| Accepted message | Owner-scoped user message with server sequence/idempotency key | Persist-first; failure stops before resolver/tool. |
| Head attempt | Existing agent run + digest-only tool call/provenance | No raw request/event text. |
| Terminal result | One assistant message with text/app/status parts | Persist once; history replays stored parts and never re-runs the head. |
| Telemetry | Existing engine telemetry ingest, best effort | Failure cannot alter stored message or head result. |

No database migration or cross-store business write is added; ADR-028 ownership remains
unchanged.

## Compatibility and Rollback

- Message envelope remains version 1; app-part and zone shapes are unchanged.
- New component/card/plan fields are additive. Runtime vendored schemas update together
  and schema drift tests enforce byte-equivalence to planning sources.
- Existing Renewals drill, draft, mock-send, telemetry, and F0039 thread lifecycle must
  pass unchanged.
- Rollback sets Broker card/catalog/action inactive, removes its plan tool/component from
  the active contract, and binds the existing typed stub. Renewals and stored historical
  Broker app parts remain renderable; rollback does not delete messages or reverse data.
- `internalOnly` remains a harmless additive endpoint parameter even when the head is dark.

## Test and Evidence Plan

### Backend

- Integration matrix for all six internal roles plus BrokerUser/ExternalUser denial.
- Seed at least 21 visible rows plus hidden sibling rows; assert only newest 20, correct
  descending order, scoped totalCount, no hidden values, broker names, actor fallback.
- Assert `internalOnly` validation and default BrokerUser-safe route compatibility.
- Query/performance evidence with representative data; no N+1 and p95 <2s.

### AI / Neuron

- Broker head content/empty/timeout/5xx/401/403/malformed-response tests.
- Startup rejects active-without-handler, unknown tool/component, plan/card mismatch,
  invalid props schema, and missing active-step timeout.
- Two-head tests in both directions; inactive heads make zero engine calls.
- Deterministic/shadow/direct route fixtures; unqualified read succeeds; filter/write/
  unknown/cross-domain/inactive proposals make zero broker tool calls.
- Persist/reload/idempotency test proves historical response is identical and no re-read.
- Telemetry allow-list test and emission-failure non-interference.
- Retain F0039 evaluation thresholds and direct-routing rollout gate status; F0040 does
  not turn on gated contextual adjudication.

### Frontend

- AJV valid/invalid props, unknown component fallback, max-20, semantic list, relative
  time, `Unknown User`, Broker 360 link, empty/error/retry/auth-required states.
- Stored history app part renders through the same registry after reload.
- Existing Dashboard ActivityFeed and Renewals component regressions.
- WCAG keyboard/focus/name checks and responsive narrow-panel coverage.

### Cross-tier

- One glance with both live heads successful.
- Broker failure while Renewals succeeds and inverse.
- One direct Broker request persists and replays after service/browser reload.
- Authorization probe proves no hidden row/count/id/name/description crosses the engine.
- Telemetry capture proves no forbidden raw fields.

Repository quality thresholds remain those in `SOLUTION-PATTERNS.md` (including >=80%
changed-code coverage) and the feature action's required role reports.

## Required Role Matrix

| Role | Required | Scope |
|------|----------|-------|
| Backend | Yes | Secure scoped projection, existing endpoint compatibility, role/performance tests. |
| Frontend | Yes | Registered list, shared timeline UI, retry/auth/accessibility/replay rendering. |
| AI Engineer | Yes | Head/executor/card/plan/catalog/provenance/telemetry/evaluation work. |
| Quality Engineer | Yes | Cross-tier role matrix, failure isolation, replay, p95, regression evidence. |
| Security Reviewer | Yes | Forwarded-token boundary, internal-only denial, query-before-count scope, safe registry/telemetry. |
| Code Reviewer | Yes | Independent review across engine, Neuron, frontend, and contracts. |
| Architect | Yes | ADR-037 conformance, compatibility, and as-built reconciliation. |
| DevOps | No | No service, port, secret, env var, migration, or deployment topology change. |

## Knowledge-Graph Binding Plan

- Promote `feature:F0040` from provisional to planned and map S0001–S0003.
- Add `capability:neuron-broker-activity-head`, `schema:neuron-broker-activity-list`,
  and `adr:037`.
- Reuse `endpoint:timeline-events`, `api:nebula-rest`, `api:neuron-rest`,
  `entity:activity-timeline-event`, `entity:broker`, existing Neuron capabilities,
  `policy_rule:timeline-event-read`, and `policy_rule:broker-read`.
- Update Day-at-a-Glance/zone-dispatch/telemetry/intent capabilities from one-live/
  provisional wording to the two-live-head contract.
- Implementation `code-index.yaml` bindings are deferred to the F0040 feature action,
  when as-built paths exist. This plan run changes ontology source and compiled planning
  projections only.

## Risks and Mitigations

| Risk | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Hidden Broker rows leak through coarse permission | High | Scope at query before count/order/limit; full role/negative matrix; Security signoff | Backend/Security |
| Shared executor regresses Renewals | High | Two-consumer contract tests plus full Renewals/F0039 regression | AI/QE |
| External BrokerUser receives internal description through Neuron | High | `internalOnly` engine guard; never fall back to broker-safe branch; denial test | Backend/Security |
| Stored app part cannot render after rollback | Medium | Additive envelope v1; keep component registered even if head returns inactive | Frontend/Architect |
| Filtered request is falsely answered by newest-20 | Medium | Deterministic capability policy + entity/action eval fixtures; no tool on unsupported | AI/QE |
| Telemetry captures business content | High | Closed schema, allow-list construction, payload-negative tests | AI/Security |
| p95 misses 2s with scoped join | Medium | Indexed query, one projection/no N+1, representative perf evidence; no cache | Backend/QE |

## JSON Serialization and API Conventions

- Engine/React boundary uses camelCase, ISO-8601 UTC timestamps, UUID strings, and the
  existing paginated envelope.
- Neuron internal persisted envelope and zone keys remain their existing snake_case
  contract (`part_type`, `thread_id`, `zone_id`, `zone_status`).
- Errors remain RFC 7807 at HTTP boundaries; head slots/messages carry only bounded,
  application-owned copy.
- No route uses an `/api` prefix; no new public route is introduced.

## DI and Policy Changes

- Engine DI registers the focused Broker activity projection repository if introduced
  and reuses the existing `IDistributionScopeService`.
- Neuron bootstrap registers the component contract registry, explicit Broker handler
  factory, one engine tool, and shared executor.
- Casbin policy files do not change. Existing `timeline_event:read` remains the coarse
  permission; verified internal audience and query-time broker scope complete the
  enforcement for this read.
