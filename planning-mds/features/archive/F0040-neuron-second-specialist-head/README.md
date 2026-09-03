# F0040 — Neuron Second Specialist Head (Broker Activity)

**Status:** Planned — Phase B approved
**Priority:** Medium
**Phase:** Neuron Companion — Next

## Overview

Activates Broker activity as Neuron's second live specialist domain. Relationship and Distribution users receive the established newest-20, authorization-scoped broker event feed in the Day-at-a-Glance zone and through the direct `broker_activity.list` conversational route. The feature also hardens the specialist-head platform against two real live consumers while retaining independent zones, safe registered rendering, and no broker write surface.

## Documents

| Document | Purpose |
|----------|---------|
| [PRD.md](./PRD.md) | Phase A product requirements, boundaries, personas, workflows, and screen layouts |
| [acceptance-criteria-checklist.md](./acceptance-criteria-checklist.md) | Per-story acceptance-quality review |
| [STATUS.md](./STATUS.md) | Planning tracker and proposed required-signoff matrix |
| [GETTING-STARTED.md](./GETTING-STARTED.md) | Existing dependencies and end-to-end verification outline |
| [feature-assembly-plan.md](./feature-assembly-plan.md) | Phase B build order, contracts, role ownership, tests, compatibility, and rollback |
| [ADR-037](../../architecture/decisions/ADR-037-neuron-second-specialist-head-contract.md) | Accepted two-live-head and Broker activity read-boundary decision |

Phase B artifacts passed G4/G5 validation and were approved by the operator on
2026-08-31 for plan run `2026-08-30-af27c9c1`.

## Stories

| ID | Title | Status |
|----|-------|--------|
| [F0040-S0001](./F0040-S0001-live-broker-activity-zone.md) | Live Broker activity Day-at-a-Glance zone with Broker 360 drill-through | Not Started |
| [F0040-S0002](./F0040-S0002-conversational-broker-activity-routing.md) | Direct broker-activity routing with durable replay | Not Started |
| [F0040-S0003](./F0040-S0003-two-live-head-platform-hardening.md) | Two-live-head platform hardening, failure isolation, and telemetry | Not Started |

**Total Stories:** 3
**Completed:** 0 / 3

## Product Boundaries

- Broker activity is read-only; no broker/contact/timeline mutation is added.
- Results reuse F0001-S0004: newest 20 authorized Broker events, newest first, with Broker 360 navigation.
- Renewals and Broker activity remain independent; cross-zone ranking/composition stays deferred.
- Tasks and Pipeline remain visible inactive zones.

## Dependencies

- F0038 — Day-at-a-Glance shell, zone dispatch, registered components, and Renewals head.
- F0039 — durable threads and trusted fail-closed intent routing.
- F0001-S0004 / F0002-S0007 — broker feed and Broker 360 timeline behavior.

Epic intake: [`../archive/F0038-neuron-day-at-a-glance-shell/intake-brief.md`](../archive/F0038-neuron-day-at-a-glance-shell/intake-brief.md).
