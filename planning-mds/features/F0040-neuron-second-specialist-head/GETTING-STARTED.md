# F0040 — Neuron Second Specialist Head (Broker Activity) — Getting Started

## Prerequisites

- [x] F0038 is delivered: Neuron runtime, Day-at-a-Glance zone dispatch, Renewals head, registered component envelope, token forwarding, and per-zone error isolation.
- [x] F0039 is delivered: owner-scoped durable threads, trusted intent catalog, fail-closed resolver/dispatcher, and replayable messages.
- [x] Broker activity is available in the existing Dashboard/Broker 360 surfaces through authorization-scoped `ActivityTimelineEvent` reads.
- [x] G1 selected `broker_activity` for F0040.
- [x] Phase A requirements approved (G3, 2026-08-31).
- [x] Phase B assembly plan, contracts, and KG bindings approved (G5, 2026-08-31).

## Services to Run

```bash
docker compose up -d db authentik-server api neuron experience
```

The build action may refine the minimal service command if Phase B identifies a narrower verified stack.

## Environment Variables

F0040 introduces no environment variable. Phase B uses a code-reviewed 2000 ms
`timeout_ms` on the two active plan steps and the existing engine/auth configuration.
No service, port, secret, migration, or deployment topology changes; DevOps is not a
required role.

## Seed Data

- Existing internal users covering `DistributionUser`, `DistributionManager`, `RelationshipManager`, `ProgramManager`, `Underwriter`, and `Admin` authorization scopes.
- At least 21 authorized Broker `ActivityTimelineEvent` rows to verify newest-20 capping and descending order.
- At least one event whose actor profile no longer resolves to verify `Unknown User`.
- At least one broker/event outside each scoped user's authorization boundary to prove exclusion.
- One user with no authorized broker events to verify the explicit empty state.

Do not add a new broker business-event type solely for F0040; use existing broker/contact timeline events.

## How to Verify

1. Sign in as each supported internal role and open Neuron's Daily Brief.
2. Verify Renewals and Broker activity are `LIVE`; Tasks and Pipeline remain `not yet active`.
3. Verify Broker activity shows no more than 20 authorized events, newest first, with event type/description, broker, actor, and relative time.
4. Select a broker name and verify the existing Broker 360 route opens.
5. Send `show recent broker activity`; verify the same registered feed renders and persists in the current thread.
6. Reload/resume the thread and verify the historical response replays without a new read or duplicate assistant message.
7. Exercise empty, missing-actor, expired-session, unauthorized-record, engine-error, broker-head-timeout, renewals-head-timeout, invalid-component, and invalid-route cases.
8. Verify a failure in either live head does not block the other, and no broker create/edit/delete/follow-up action is exposed.
9. Verify telemetry distinguishes glance/direct entry points and terminal outcomes without raw user/event/prompt/token content.

## Key Files

| Layer | Current Path | Purpose |
|-------|--------------|---------|
| Product requirements | `planning-mds/features/F0040-neuron-second-specialist-head/PRD.md` | Approved scope, roles, workflows, and non-goals |
| Broker feed precedent | `planning-mds/features/archive/F0001-dashboard/F0001-S0004-view-broker-activity-feed.md` | Authoritative newest-20 feed behavior |
| Broker timeline precedent | `planning-mds/features/archive/F0002-broker-relationship-management/F0002-S0007-view-broker-activity-timeline.md` | Per-broker history and scope |
| Stub head card | `neuron/crm_agents/cards/crm.broker_activity.head.card.yaml` | Existing inactive head identity |
| Intent catalog | `neuron/config/intent-catalog.yaml` | Existing inactive `broker_activity.list` route |
| Zone dispatch | `neuron/app/orchestration/zone_heads.py` | Current Renewals/live and stub behavior |
| Day-at-a-Glance plan | `neuron/orchestration/plans/day-at-a-glance.plan.yaml` | Current zone sequence |
| Registered components | `experience/src/features/neuron/registry/componentRegistry.tsx` | Safe render boundary |
| Existing feed UI | `experience/src/features/timeline/components/ActivityFeed.tsx` | Established broker feed presentation |
| Phase B execution plan | `planning-mds/features/F0040-neuron-second-specialist-head/feature-assembly-plan.md` | Exact build order, role ownership, tests, and rollback |
| Architecture decision | `planning-mds/architecture/decisions/ADR-037-neuron-second-specialist-head-contract.md` | Scoped existing-endpoint reuse and hardened head contract |
| Broker props schema | `planning-mds/schemas/neuron-broker-activity-list.schema.json` | Closed registered component contract |

The feature action must reconcile exact as-built filenames against the approved Phase B
plan before implementation and record any non-conflicting adjustment in workstate.

## Authentication

Use the repository's existing seeded internal users and token flow. Neuron forwards the current user's bearer token to the engine, which remains the sole broker authorization decision point. Never place credentials or tokens in planning artifacts, telemetry, or test snapshots.

## Notes

- A direct broker request can be classified today, but the trusted catalog and head card keep the domain inactive. Activating only the head card is insufficient; the route, tool, component, tests, and rollback state must agree.
- The existing Dashboard feed requests 12 items in its current component even though F0001-S0004 authorizes up to 20. F0040 follows the authoritative story contract and caps at 20; Phase B decides whether to reuse or parameterize presentation code.
- Historical Neuron envelopes must replay from persistence; do not silently refresh their component props on thread reload.
- Cross-zone ranking is a scope violation even when both heads succeed.
