---
template: adr
version: 1.1
applies_to: architect
---

# ADR-037: Neuron Second Specialist Head Contract and Broker Activity Read Boundary

**Date:** 2026-08-31

## Status

- [ ] Proposed
- [x] Accepted (F0040 Phase B planning gate, run `2026-08-30-af27c9c1`)
- [ ] Superseded
- [ ] Rejected

> Accepted by the operator at the F0040 Phase B checkpoint on 2026-08-31. This
> decision extends ADR-027's deliberately thin head contract on its first real second consumer;
> ADR-027, ADR-028, and ADR-035 remain authoritative for orchestration, persistence
> ownership, and fail-closed intent resolution.

## Context

F0038 shipped one live Renewals head and three inert stubs. Its bootstrap binds the
Renewals card by id and sends every other specialist card to `StubZoneHead`; its plan
validates that named tools exist but does not prove that an active head has a live
handler, registered components, or a bounded execution timeout. A stub could not test
those constraints. F0040 activates the existing `crm.broker_activity.head` identity and
therefore supplies the planned second consumer that must harden the shared contract.

F0040 must also reuse the established F0001-S0004/F0002-S0007 broker feed rather than
create a Neuron-specific definition. The existing `GET /timeline/events` contract already
has the right public DTO and declares authorization-scoped results. The as-built internal
path, however, currently checks only `timeline_event:read`, queries all matching events,
and maps `entityName` to null. That is insufficient for F0040's record-level scope and
Broker 360 link. The BrokerUser branch is intentionally different: it returns a broker-
tenant-scoped, broker-safe field subset and must remain available to the external portal,
while F0040 explicitly denies BrokerUser/external access to the Neuron result.

## Decision Drivers

- Reuse one broker activity business contract and one engine endpoint.
- Keep the engine as the only CRM authorization and data boundary.
- Prove an active head is executable, tool-complete, component-safe, and timeout-bounded
  before Neuron reports ready.
- Preserve stored F0038/F0039 envelope replay without a schema-version migration.
- Keep Renewals and Broker activity independent; no cross-zone ranking or composition.
- Record outcome/latency telemetry without raw user or broker-event content.

## Decision

### 1. Harden the existing broker timeline read; do not add another feed

`GET /timeline/events` remains the sole engine read used by the Dashboard, Broker 360,
and the F0040 head. For internal `entityType=Broker` reads, the engine resolves the
caller's current `ProjectionVisibility` through `IDistributionScopeService` and applies
the resulting broker-id scope **inside the query before count, ordering, and limit**.
Admin retains `SeeAll`; an empty scope returns an empty page and leaks no counts or ids.

The broker projection joins `Broker` in the same query so `TimelineEvent.entityName` is
the broker legal name, preserves the stored `EventDescription`, maps a missing actor to
`Unknown User`, orders `OccurredAt DESC`, and caps the F0040 request at page 1 / 20 rows.
No cache is introduced: results are user-scoped, append-only operational facts and must
reflect current authorization without a cache-key leakage risk.

Add the optional `internalOnly=true` query flag to the existing endpoint. It is valid
only with `entityType=Broker`; the engine rejects BrokerUser and external principals on
that branch before returning data, then applies the existing `timeline_event:read`
Casbin decision and record scope. Neuron's tool always sends this flag. The existing
BrokerUser `limit` branch and broker-safe DTO remain unchanged, so F0040 adds neither a
new policy rule nor a parallel endpoint.

### 2. Make the specialist-head contract explicit and fail-fast

Add two optional, backwards-compatible fields to the checked-in orchestration contracts:

- Agent Card `components`: component ids the agent may emit.
- Plan step `timeout_ms`: bounded execution time for an engine/model step.

At bootstrap, an active specialist head must have an explicit handler factory, use
`auth_mode: user_token` when it names engine tools, resolve every card/plan tool, declare
only registered component ids, and agree with its plan step's skills/output modes/tools.
An active card may never silently bind to `StubZoneHead`. Inactive heads must have no
engine tool/component reference and keep the typed inactive handler. Any mismatch is a
configuration error and prevents readiness.

Introduce one shared `HeadExecutor` used by glance and conversational dispatch. It owns
run creation/completion, plan-step timeout, zone validation, component-id/props
validation, tool/run provenance, safe error classification, and outcome telemetry. Head
implementations remain small domain adapters. This removes the duplicate lifecycle logic
currently split between `GlanceAssembler` and `MessageDispatcher` without introducing a
general workflow framework.

The Day-at-a-Glance plan advances to 1.1.0. Renewals and Broker activity are active,
declare their tools/components, and use a 2000 ms step timeout. Tasks and Pipeline stay
inactive and make no engine read. A timeout or upstream failure becomes only that head's
typed error; an upstream 401 propagates to the existing request-level authentication
flow. A 403 returns no data and is recorded/presented as a bounded rejection.

### 3. Activate one read-only Broker activity adapter

`BrokerActivityZoneHead` invokes the registered
`engine.timeline.list_broker_activity` tool with:

```text
GET /timeline/events
  ?entityType=Broker&page=1&pageSize=20&internalOnly=true
```

It validates the paginated response, emits `empty` for no rows, and otherwise returns
component `broker_activity.recent_list`. The props use the existing timeline field names
(`id`, `entityType`, `entityId`, `eventType`, `eventDescription`, `entityName`,
`actorDisplayName`, `occurredAt`) so the frontend can share the established timeline
presentation without a second DTO. Event payload JSON never crosses into the component.

The component props are governed by
`neuron-broker-activity-list.schema.json`: one through twenty Broker events, all display
and navigation fields required, no additional properties. The schema is vendored into
the Neuron and frontend runtime bundles with drift tests. Server and React component
registries both fail closed on an unknown id or invalid props.

### 4. Use the same head for glance and direct conversation

Activate only `broker_activity` / `broker_activity.list` in the trusted intent catalog.
Both the deterministic/shadow production path and a future direct Phi path resolve the
trusted catalog entry to the same registered head and `HeadExecutor`; neither model nor
user input supplies a tool/head id. Unsupported broker writes and broker/date/type-
filtered requests return application-owned unsupported-capability copy and invoke no
tool. Evaluation fixtures cover unqualified reads, filters, writes, unknown actions,
cross-domain actions, and inactive domains.

The existing owner-scoped message store persists the terminal assistant envelope.
Historical app parts replay byte-for-byte from the stored message parts and never
re-query the engine. Adding a component id is additive, so message envelope version 1 and
the zone payload shape remain unchanged; no persistence migration is needed.

### 5. Extend existing companion telemetry with one generic head outcome

Extend `neuron-companion-telemetry-event.schema.json` with
`specialist-head-outcome`. It carries only `user_id`, `thread_id`, optional
`head_run_id`, `zone_id`, `entry_point` (`glance|conversation`), `terminal_result`
(`content|empty|error|rejected`), `latency_ms`, and timestamp/version. It contains no raw
message text, event description, component props, bearer token, prompt, or model output.
Emission remains best-effort and cannot change a head or user-visible result.

## Architecture Sketch

```text
Glance or validated broker_activity.list
        |
        v
shared HeadExecutor -- plan timeout / run / schema / telemetry
        |
        v
BrokerActivityZoneHead
        |  forwarded user token
        v
GET /timeline/events (...internalOnly=true, pageSize=20)
        |
        +-- engine principal audience guard + timeline_event:read
        +-- DistributionScopeService -> broker ids (query-time filter)
        +-- ActivityTimelineEvent JOIN Broker; newest first; actor fallback
        |
        v
broker_activity.recent_list props schema
        |
        +-- glance zone (independent of Renewals)
        `-- persisted message app part (replay does not re-read)
```

## Options Considered

1. **Add a Neuron-only broker endpoint.** Rejected: duplicates F0001 feed semantics and
   creates a drift surface.
2. **Let Neuron filter unauthorized rows.** Rejected: Neuron must never receive excluded
   rows or recreate CRM authorization.
3. **Activate the card and keep the current bootstrap fallback.** Rejected: the active
   broker card would still receive the inert stub handler and startup could report a
   false-ready configuration.
4. **Bump the message envelope version.** Rejected: the change is an additive registered
   component with the existing app-part shape; a bump would create needless migration
   work and replay risk.
5. **Cache the broker list in Neuron.** Rejected: user scope and current authorization
   make a direct bounded engine read safer; the p95 target is met by the indexed query.

## Consequences

- Backend work is required to make the existing Broker timeline query truly scoped and
  to resolve broker names; the public route/DTO and policies remain stable.
- AI/Neuron work adds the broker tool/head, shared executor, startup validation, catalog
  activation, telemetry, and regression/evaluation coverage.
- Frontend work adds one registered component and shares the existing timeline list/link
  presentation; no new page or route is introduced.
- QE, Security, Code Review, AI Engineer, and Architect signoffs are required. DevOps is
  not required because there is no migration, service, port, secret, or environment
  contract change.

## Security and Compliance Notes

- Record scope is applied before counts/order/limit; filtered records reveal no ids,
  names, descriptions, or counts.
- BrokerUser's existing broker-safe timeline API is not widened. The Neuron tool's
  internal-only branch rejects external principals and returns no data.
- Only stored event descriptions are rendered. No model sees or rewrites broker events.
- Logs/provenance/telemetry carry bounded ids, codes, hashes, outcome, and latency only.
- F0040 adds no broker/contact/timeline mutation and creates no CRM audit event.

## Follow-up Actions

- F0040 feature action implements the plan and binds as-built engine/Neuron/frontend
  paths in the KG code index.
- A third live head, cross-zone composition, public Agent Cards, external MCP UI, and
  any broker write require separate product scope and architecture review.
