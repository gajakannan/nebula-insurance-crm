# ADR-011: Standardize CRM Workflow State Machines and Append-Only Transition History

**Status:** Accepted
**Date:** 2026-03-26 (finalized during F0007 architecture review; originally proposed 2026-03-23)
**Owners:** Architect
**Related Features:** F0006, F0007, F0019, F0024, F0026

## Context

Multiple planned features introduce business workflows with explicit states, guarded transitions, ownership changes, and auditable status history. Without a shared rule set, each feature could model transitions differently, weakening audit consistency and increasing implementation drift.

Nebula already documents workflow transitions as append-only in solution patterns, but the planned CRM feature set now depends on this becoming an explicit architecture decision rather than an implied convention.

## Decision

Standardize business workflows on explicit state machines with:

- declared valid transitions
- guard and authorization checks before transition
- append-only transition history
- immutable timestamps, actor identity, and transition reason

Immediate user-driven transitions remain application-service concerns. Durable orchestration engines such as Temporal may trigger those transitions, but they do not replace the state-machine contract.

## Scope

This ADR governs:

- submission workflow transitions
- renewal workflow transitions
- service and reconciliation workflows where explicit state exists
- transition audit history and correlation expectations

## Consequences

### Positive

- Workflow rules stay explicit, testable, and auditable.
- Invalid state changes can be rejected consistently.
- Reporting and analytics can rely on structured transition history.

### Negative

- Features must invest in transition modeling rather than ad hoc status edits.
- Shared workflow terminology and validation contracts must be maintained carefully.

## F0007 Renewal Workflow Specification

The renewal workflow is the first full application of this ADR beyond submission workflows. Key design decisions:

### State Machine

```
Identified → Outreach → InReview → Quoted → Completed
                          │                    │
                          └──► Lost ◄──────────┘
```

Six states: Identified, Outreach, InReview, Quoted (non-terminal); Completed, Lost (terminal).

### Guard Conditions

Guards are evaluated **before** the transition is applied:

| Transition | Guards |
|------------|--------|
| Any → Lost | `reasonCode` required; `reasonDetail` required when `reasonCode=Other` |
| Quoted → Completed | `boundPolicyId` or `renewalSubmissionId` required |
| Creation (null → Identified) | Valid PolicyId, no active renewal for policy, region alignment |

### Role-Based Transition Authorization

Transition authorization is a **two-layer check**:
1. **Casbin ABAC:** `renewal:transition` action verified per role (see policy.csv §2.4)
2. **Application-layer guard:** Per-transition role check (e.g., only Underwriter/Admin for InReview→Quoted)

This layering means Casbin gates the broad "can this role transition renewals" check, while the application service enforces which specific transitions each role is allowed to perform. The per-transition role matrix is defined in the F0007 README transition matrix.

### Transition Record Structure

Every successful renewal transition appends:
1. One `WorkflowTransition` record: `WorkflowType='Renewal'`, `EntityId=renewal.Id`, `FromState`, `ToState`, `Reason` (transition comment or reasonCode for Lost), `ActorUserId`, `OccurredAt`
2. One `ActivityTimelineEvent` record: `EntityType='Renewal'`, `EntityId=renewal.Id`, `EventType='RenewalTransitioned'`, pre-rendered `EventDescription`, `ActorUserId`, `OccurredAt`

Both records are created atomically in the same database transaction as the Renewal status update.

### Transition + Audit Atomicity

The transition, WorkflowTransition append, and ActivityTimelineEvent append must occur in a single database transaction. If any part fails, the entire operation rolls back. This is enforced by the application service wrapping all three operations in `SaveChangesAsync()`.

## Follow-up

- ~~Create workflow specifications for each major domain process.~~ Renewal workflow specified (F0007 README). Submission workflow carries forward from existing implementation.
- Reference this ADR from workflow-bearing feature PRDs.
- Keep transition history append-only across modules.
