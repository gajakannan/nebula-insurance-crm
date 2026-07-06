# ADR-013: Introduce Operational Routing and Queue Execution Engine

**Status:** Accepted
**Date:** 2026-03-23
**Updated:** 2026-07-03
**Owners:** Architect
**Related Features:** F0017, F0022, F0032

## Context

Nebula is moving beyond personal task lists into managed operational routing for submissions, renewals, and other work. Routing rules, queue assignment, backup coverage, and rebalancing need deterministic execution and explainable outcomes.

If each workflow module implements its own routing rules independently, the system will drift and become difficult to operate or govern.

## Decision

Introduce a shared routing and queue execution engine with:

- explicit queue definitions
- deterministic rule evaluation
- auditable assignment outcomes
- configurable coverage and fallback behavior

Administrative control of rules may be exposed later through product admin surfaces, but execution is governed by this shared engine.

### F0022 Execution Contract

F0022 implements the first durable version of this engine inside the modular monolith as an `OperationsRouting` application boundary. The engine owns these persistent concepts:

- `WorkQueue` — named queue definition and manager-visible fallback queue.
- `WorkQueueMember` — effective-dated user membership for queue assignment eligibility.
- `AssignmentRule` — versioned deterministic rule with ordered conditions and outcome metadata.
- `CoverageWindow` — explicit backup coverage / out-of-office window. Inactivity never creates coverage.
- `QueueWorkItem` — one active queue placement per source work item.
- `RoutingDecisionLog` — append-only routing, fallback, skip, reassignment, and rebalance evidence.

Rule evaluation order is fixed for MVP:

1. explicit manual assignment override
2. coverage/out-of-office rule
3. territory/ownership rule
4. workload balancing
5. fallback queue

The fallback queue is a required system queue named `Unassigned Operations Queue`. No-match work is never randomly assigned. Routing is idempotent by `(SourceType, SourceId, RuleSetVersion)` and must not create duplicate active queue entries under repeated source workflow events.

### Service Boundary

`OperationsRoutingService` evaluates route-eligible tasks, submissions, and renewals by reading source work summaries through narrow adapters:

- `ITaskRoutingSource`
- `ISubmissionRoutingSource`
- `IRenewalRoutingSource`
- `IDistributionOwnershipResolver` for F0017 territory and ownership context
- `IQueueAssignmentRepository` for queue, rule, membership, coverage, and queue item persistence

The source workflow modules keep ownership of their lifecycle states. F0022 may update source assignment fields only through existing assignment service methods or source-specific assignment ports, so existing validation, `If-Match`, ABAC, and timeline behavior remain authoritative.

### Governance Boundary With F0032

F0022 ships local manager/admin controls and durable tables. F0032 later supplies centralized draft/validate/publish governance over the same queue/rule model; it must not replace F0022 data structures. Until F0032 exists, F0022 rule edits create new rule versions directly and mark prior active versions superseded.

## Scope

This ADR governs:

- queue and routing execution
- assignment audit records
- fallback and coverage precedence
- module integration boundaries for routing triggers

## Consequences

### Positive

- Routing behavior becomes consistent and observable.
- Queue-based operations can scale across multiple workflow domains.
- Administrative governance can evolve without changing execution semantics.

### Negative

- Adds a shared operational subsystem with its own complexity.
- Rule precedence and debugging expectations must be documented clearly.
- Queue and rule mutations become security-sensitive because they can change ownership and visibility.
- Repeated source workflow events require idempotency keys and deterministic duplicate handling.

## Follow-up

- Reference this ADR from routing, hierarchy, and admin-configuration PRDs.
- Rule versioning and no-match handling conventions are defined by the F0022 execution contract above.
- Add operational metrics and support guidance in runbooks.
