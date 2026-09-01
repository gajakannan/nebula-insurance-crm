---
template: feature-prd
version: 2.0
applies_to: product-manager
---

# F0040: Neuron Second Specialist Head (Broker Activity)

## Feature Header

**Feature ID:** F0040
**Feature Name:** Neuron Second Specialist Head (Broker Activity)
**Epic:** Neuron Companion (AI Conversational Layer)
**Priority:** Medium
**Phase:** Neuron Companion — Next
**Status:** Draft — Phase A ready for approval

> **G1 decision (2026-08-30, restored on resume 2026-08-31):** activate
> `broker_activity` as the second live specialist domain. This replaces the
> provisional Accounts/Brokers choice without changing the feature boundary.

## Feature Statement

**As a** Relationship Manager or Distribution user
**I want** Broker activity to be live in Neuron's Day-at-a-Glance and conversational experience
**So that** I can see authorized relationship changes and recent interactions without visiting each broker record

## Business Objective

- **Goal:** Turn the existing Broker activity stub into Neuron's second live specialist head while preserving Renewals behavior and proving the shared head platform against two real consumers.
- **Primary metric:** Percentage of eligible Day-at-a-Glance loads in which the Broker activity zone reaches a typed `content` or `empty` result instead of `inactive`.
- **Baseline:** 0%; the zone and `broker_activity.list` intent are currently registered but inactive.
- **Target:** 100% of eligible authenticated loads reach `content` or `empty`; unauthorized records disclosed = 0.
- **Secondary signals:** Broker activity direct-intent route success, zone result (`content` / `empty` / `error`), per-head latency, and Broker 360 click-through count. Initial usage establishes the adoption baseline; no adoption-growth percentage is asserted before that baseline exists.

## Problem Statement

- **Current State:** The CRM already has an authorization-scoped broker activity feed and Broker 360 timelines, but Neuron shows Broker activity as a non-interactive "not yet active" slot. A direct broker-activity request resolves only to an inactive domain.
- **Desired State:** Authorized users see the newest broker events in the existing Neuron surface and can request the same read conversationally. Renewals and Broker activity operate independently through one reusable specialist-head contract.
- **Impact:** Distribution users gain broker relationship awareness without leaving the companion, and the product validates the multi-head extension point before investing in cross-zone intelligence or additional domains.

## Personas & Jobs To Be Done

| Persona | Priority | Job to be done |
|---------|----------|----------------|
| Relationship Manager / Distribution user | Primary | See the newest authorized broker relationship events and open the relevant Broker 360 record. |
| Distribution Manager / Program Manager | Secondary | Monitor recent broker activity within the brokers and programs allowed by existing authorization scope. |
| Underwriter | Secondary | See activity for brokers linked to work the underwriter is authorized to access. |
| Admin | Supporting | Verify the complete internal feed when operating with existing unscoped administrative access. |

## Scope & Boundaries

**In Scope:**
- Change the Day-at-a-Glance `broker_activity` slot from `inactive` to a live read that shows up to the 20 newest authorized `Broker` timeline events, ordered by `OccurredAt` descending.
- Show each event's type/description, broker name, actor display name, and relative timestamp; use `Unknown User` when the actor no longer resolves.
- Navigate from a broker activity item to that broker's Broker 360 view.
- Activate the existing read-only `broker_activity.list` conversational intent and route it to the same live specialist head and registered component behavior.
- Preserve owner-scoped thread persistence and replay for broker-activity responses.
- Harden the shared specialist-head contract, registry, orchestration, component validation, failure isolation, and telemetry on the first two-live-head configuration.
- Keep Tasks and Pipeline visible as inert `inactive` zones.

**Out of Scope:**
- Creating, editing, deleting, assigning, approving, or otherwise mutating brokers, contacts, notes, follow-ups, or timeline events from Neuron.
- Filtering/searching by broker, date range, event type, producer, hierarchy, or free-text query; grouping or pagination beyond the newest 20 items.
- AI-generated broker summaries, recommendations, risk scoring, next-best-action, or cross-zone ranking/composition.
- Activating Tasks, Pipeline, or any third specialist head.
- Changing broker visibility rules, Casbin policies, source-of-truth ownership, or the existing Dashboard/Broker 360 feed behavior.
- Contextual intent adjudication owned by F0041, external hosts/MCP-UI, and real outbound communication.

## Acceptance Criteria Overview

- [ ] Broker activity renders as `LIVE` with up to 20 authorized events or an explicit empty state; it never remains `inactive` for an eligible authenticated user.
- [ ] Direct requests for recent broker activity route through `broker_activity.list` to the live broker specialist and return the same registered read component.
- [ ] Event ordering, fields, role scope, empty/error/auth states, and Broker 360 click-through match the established F0001-S0004 broker-feed contract.
- [ ] Renewals and Broker activity both remain functional when the other live head returns an error or times out; Tasks and Pipeline remain inert.
- [ ] Unknown components or invalid props render the existing safe fallback, and model output cannot select an unregistered head or executable action.
- [ ] Broker activity response envelopes persist and replay in the owner-scoped thread without re-querying or changing the historical message.
- [ ] Telemetry distinguishes glance-zone use from direct-intent use and records outcome/latency without raw event descriptions, user messages, tokens, or other sensitive payloads.
- [ ] There is no broker-domain mutation surface and no cross-zone ranking path.

## UX / Screens

| Screen | Purpose | Key Actions |
|--------|---------|-------------|
| Neuron panel — Daily Brief / Day at a Glance | Show Renewals and Broker activity as independent live zones, with Tasks and Pipeline still inactive. | Scan broker events; open Broker 360; retry a failed broker zone. |
| Neuron conversation transcript | Accept a direct request for recent broker activity and render the registered broker list response in the current thread. | Send request; inspect results; open Broker 360. |

**Key Workflows:**
1. **Glance read** — open Neuron → Daily Brief loads → Broker activity returns `content` or `empty` → select an event → Broker 360 opens.
2. **Conversational read** — send a request such as "show recent broker activity" → trusted intent catalog selects `broker_activity.list` → broker head returns the registered list → response persists and replays in the thread.
3. **Isolated failure** — broker read fails or times out → Broker activity shows a typed retryable error → Renewals and inactive zones remain visible and usable.

## Screen Layouts (ASCII)

### Neuron Day at a Glance — Desktop

```text
┌ Neuron · Daily Brief ──────────────────────────────────────────────┐
│ ┌ Renewals ● LIVE ───────────────┐ ┌ Tasks ○ not yet active ────┐ │
│ │ {renewal content}              │ │ Not yet active.            │ │
│ └────────────────────────────────┘ └─────────────────────────────┘ │
│ ┌ Broker activity ● LIVE ────────────────────────────────────────┐ │
│ │ {event type}  {relative time}                                  │ │
│ │ {event description}                                            │ │
│ │ {broker name → Broker 360} · {actor display name}              │ │
│ │ … up to 20 newest authorized events                            │ │
│ └────────────────────────────────────────────────────────────────┘ │
│ ┌ Pipeline ○ not yet active ─────────────────────────────────────┐ │
│ │ Not yet active.                                                │ │
│ └────────────────────────────────────────────────────────────────┘ │
├───────────────────────────────────────────────────────────────────┤
│ Ask about CRM work…                                      [Send]   │
└───────────────────────────────────────────────────────────────────┘
```

### Neuron Day at a Glance — Narrow

```text
┌ Neuron · Daily Brief ───────────┐
│ Renewals ● LIVE                 │
│ {renewal content}               │
├─────────────────────────────────┤
│ Broker activity ● LIVE          │
│ {type} · {relative time}        │
│ {description}                   │
│ {broker → Broker 360} · {actor} │
│ … vertically scrolls            │
├─────────────────────────────────┤
│ Tasks ○ not yet active          │
├─────────────────────────────────┤
│ Pipeline ○ not yet active       │
├─────────────────────────────────┤
│ Ask about CRM work…      [Send] │
└─────────────────────────────────┘
```

### Direct Broker Activity Request

```text
[Current owner-scoped thread]
        │
        ├─ User: "Show recent broker activity"
        │
        └─ Neuron: Broker activity ● LIVE
                   {newest authorized events, up to 20}
                   [broker name → Broker 360]
                           │
                           └─ persisted transcript replays after reload
```

## Data Requirements

**Core Entities:**
- **ActivityTimelineEvent:** Existing append-only source for event type, stored event description, actor, occurrence time, and broker identity.
- **Broker:** Existing source for broker name, authorization scope, and Broker 360 navigation.
- **UserProfile:** Existing source for actor display name; a missing/deactivated actor renders as `Unknown User`.
- **Neuron thread/message:** Existing owner-scoped persistence for the direct request and replayable registered-component response.

**Validation Rules:**
- Include only events with `EntityType = "Broker"` and a broker visible under the requesting user's existing engine-enforced authorization scope.
- Return at most 20 events ordered by `OccurredAt` descending; do not group repeated events for the same broker.
- Render the stored event description; do not generate or rewrite business-event text in Neuron.
- An empty authorized result is `empty`, a failed read is `error`, and an expired/invalid session is an authentication-required state. None may disclose filtered records.
- Only registry-known component identifiers with schema-valid props render.

**Data Relationships:**
- `ActivityTimelineEvent.EntityId` → `Broker.Id` when `EntityType = "Broker"`.
- `ActivityTimelineEvent.ActorUserId` → `UserProfile.UserId` when an actor profile exists.
- Broker activity response → current Neuron thread/message envelope for durable replay.

## Role-Based Access

| Role | Access Level | Notes |
|------|--------------|-------|
| DistributionUser | Read | Existing broker scope only. |
| DistributionManager | Read | Existing region/hierarchy scope only. |
| RelationshipManager | Read | Brokers managed or otherwise visible under existing policy. |
| ProgramManager | Read | Brokers within authorized programs. |
| Underwriter | Read | Brokers linked to submissions/accounts visible to the underwriter. |
| Admin | Read | Existing administrative scope. |
| BrokerUser / external user | None | No external Neuron broker-activity surface in F0040. |

Neuron forwards the user's token and the engine remains the authorization decision point. F0040 adds no policy rule and no Python-side authorization substitute.

## Success Criteria

- 100% of eligible authenticated glance loads return Broker activity as `content` or `empty`, not `inactive`.
- Contract and integration tests show zero unauthorized broker events across every supported internal role and zero routes to unknown/inactive domains or actions.
- The Broker activity zone reaches its settled `content` / `empty` / `error` state within p95 < 2 seconds, preserving the established broker-feed target.
- A broker-head failure leaves Renewals usable in every failure-isolation test, and a renewals-head failure leaves Broker activity usable.
- All recorded broker-activity response envelopes replay after reload with the same ordered component data and no duplicate assistant message.

## Risks & Assumptions

- **Risk:** Generalizing the provisional F0038 head contract may regress Renewals. **Mitigation:** require two-head contract tests and bidirectional failure-isolation tests before activation.
- **Risk:** A Neuron-specific broker read could drift from the established dashboard feed. **Mitigation:** F0001-S0004 remains the business contract; reuse its fields, ordering, limit, and authorization behavior.
- **Risk:** Activating a model-routable domain expands the trust boundary. **Mitigation:** the reviewed intent catalog remains authoritative, the action stays read-only, and all resolved heads/actions are registry-validated before dispatch.
- **Assumption:** Existing broker timeline data and Broker 360 navigation remain sufficient; F0040 requires no new broker business event or state.
- **Assumption:** F0039's owner-scoped thread persistence and replay contract is available and remains unchanged.

## Dependencies

- **F0038 — Neuron Day-at-a-Glance Shell:** hard dependency for zone dispatch, message/component envelope, registered rendering, token forwarding, error isolation, and Renewals head.
- **F0039 — Neuron Durable Conversations & Local Phi Intent Resolution:** hard dependency for owner-scoped threads, persisted messages, trusted intent catalog, and deterministic fail-closed routing.
- **F0001-S0004 — View Broker Activity Feed:** authoritative product behavior for list size, ordering, fields, roles, empty/error states, performance, and click-through.
- **F0002-S0007 — View Broker Activity Timeline:** authoritative per-broker history and authorization context.
- **ADR-027:** existing A2A-aligned internal orchestration foundation; Phase B determines whether a revision or follow-on ADR is required.

Dependency evidence audit: F0038 and F0039 are archived as completed; F0001 and F0002 are archived as completed. Their feature evidence remains audit-pending for this base-run-only plan and is not substituted with a repo-wide feature-evidence validation.

## Related Stories

Stories are colocated in this feature folder.

- [F0040-S0001](./F0040-S0001-live-broker-activity-zone.md) — Live Broker activity Day-at-a-Glance zone and Broker 360 drill-through.
- [F0040-S0002](./F0040-S0002-conversational-broker-activity-routing.md) — Direct conversational broker-activity routing with durable replay.
- [F0040-S0003](./F0040-S0003-two-live-head-platform-hardening.md) — Two-live-head platform hardening, failure isolation, and adoption telemetry.

## Rollout & Enablement

- Preserve the typed `inactive` Broker activity result as the deterministic rollback state until the two-head validation gates pass.
- Initial usage establishes the adoption baseline; a later product decision may set a growth target without expanding F0040 scope.
- No user training is required beyond the visible `LIVE` zone and the existing Neuron composer; release notes must state that Broker activity is read-only.
