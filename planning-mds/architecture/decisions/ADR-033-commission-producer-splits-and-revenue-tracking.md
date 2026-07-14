---
template: adr
version: 1.1
applies_to: architect
---

# ADR-033: Commission, Producer Splits, And Revenue Tracking

## Status

- [ ] Proposed
- [x] Accepted (operator G5 Phase B approval - plan run `2026-07-07-8a9b2629`, 2026-07-07)
- [ ] Superseded
- [ ] Rejected

## Context

F0025 adds brokerage economics visibility to Nebula after F0017, F0018, and F0028 established producer ownership, policy lifecycle, and carrier/market context. The approved Phase A scope is operational visibility: commission schedules, producer splits, expected commission review, approved adjustments, and management rollups.

Nebula must not become the accounting, billing, payment, producer-payout, tax, statement, or reconciliation system in this slice. Commission values are internal economic data and require stronger authorization than ordinary policy context.

## Decision

1. **Commission module boundary.** Introduce a `CommissionRevenue` module inside the existing modular monolith. The module owns commission schedules, split assignments, expected commission records, adjustment requests, and revenue attribution projections. It reads policy, carrier/market, producer ownership, territory, and operational-report context through existing module ports.

2. **Expected commission is persisted with source snapshot fields.** Expected commission records are persisted as CRM review records, not ledger entries. Each record stores policy, policy version, carrier/market, premium basis, schedule basis, split basis, expected gross commission, producer allocation summary, exception status, and source snapshot timestamps. Recalculation creates or updates the review record with a new row version and timeline event.

3. **Commission schedules are effective-dated reference rows.** `CommissionSchedule` rows are scoped to carrier/market, line of business, optional state, optional product code, and effective period. Overlapping active schedules for the same scope are rejected with 409. Schedules support rate percent and flat amount basis; complex tiering, contingent commission, and bonus programs are out of MVP scope.

4. **Producer splits are effective-dated and policy-scoped.** `ProducerSplitAssignment` rows are tied to a policy and effective period. Child `ProducerSplitParticipant` rows identify producer nodes and percentages. Percentages must total exactly 100.0000 for an active assignment. Conflicting active assignments for the same policy/effective period are rejected.

5. **Adjustment workflow is single-step approval for MVP.** `CommissionAdjustment` starts in `Pending`, then transitions to `Approved` or `Rejected`. The request requires amount, reason, effective date, requester, and source commission record. The decision requires approver and decision note. Same-user request and approval is disallowed in MVP. Approved adjustments affect the adjusted expected amount and rollup projections; they do not mutate policy premium, schedule, or split source data.

6. **Rollups are read-side projections.** Revenue attribution rollups use `RevenueAttributionProjection` rows or a materially equivalent view. Rollups aggregate expected gross commission, approved adjustments, adjusted expected commission, allocation amounts, and exception counts by producer, broker, territory, carrier/market, policy period, line of business, and source freshness. Query-layer authorization filters source records before totals, counts, facets, or drilldowns are returned.

7. **Authorization is resource-specific.** Add Casbin resources/actions for `commission`: `read`, `schedule_manage`, `split_assign`, `calculate`, `adjustment_request`, `adjustment_approve`, and `rollup_read`. External roles receive no policy lines. Internal users must also pass source-record read checks for linked policy, producer ownership, territory, and carrier/market context.

8. **Audit uses existing timeline.** Every schedule, split, calculation, adjustment request, adjustment decision, and projection refresh mutation emits an immutable `ActivityTimelineEvent` with actor, timestamp, affected commission id, policy id, and non-sensitive summary. Rejected validations emit no timeline event.

## Options Considered

1. **Persist expected commission review records (chosen).**
2. **Compute expected commission only on demand.**
3. **Delegate all commission data to an external accounting system.**

## Consequences

- Phase C must create new domain entities, repositories, services, validators, API endpoints, JSON Schemas, Casbin seed rows, and timeline payload definitions.
- The first release supports operational review and attribution, not cash settlement.
- Approved adjustments remain explainable because source data and adjustment deltas are separated.
- Rollup freshness must be visible because management reporting can lag source writes.
- Security Reviewer signoff is mandatory for F0025 because economic data is InternalOnly and role-restricted.

## References

- F0025 PRD and stories in `planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/architecture/data-model.md`
- `planning-mds/security/authorization-matrix.md`
- ADR-026, ADR-018, ADR-014-search, and F0028 architecture artifacts
