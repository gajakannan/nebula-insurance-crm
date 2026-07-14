---
template: feature
version: 1.1
applies_to: product-manager
---

# F0025: Commission, Producer Splits & Revenue Tracking

**Feature ID:** F0025
**Feature Name:** Commission, Producer Splits & Revenue Tracking
**Priority:** Medium
**Phase:** Brokerage Platform Expansion
**Planning Status:** Phase B approved 2026-07-07; ready for feature action/build harness entrypoint.

## Feature Statement

**As a** distribution leader or finance-facing operations user
**I want** commission, producer split, and revenue attribution visibility
**So that** Nebula can support brokerage economics and production accountability without becoming a full accounting system

## Business Objective

- **Goal:** Extend Nebula from CRM workflow visibility into brokerage revenue visibility.
- **Metric:** Commission visibility coverage, split attribution completeness, and exception review completion.
- **Baseline:** Commission, split, and revenue context are outside the current CRM feature set.
- **Target:** Users can see expected commission, producer attribution, and management rollups tied to policy and market context.

## Problem Statement

- **Current State:** Users can manage policies, producer ownership, carrier/market context, and CRM workflow outcomes without seeing the economics behind those outcomes.
- **Desired State:** Commission schedules, split assignments, expected revenue, adjustments, and rollups are visible in Nebula for authorized internal users.
- **Impact:** Distribution leadership, finance operations, and producer managers gain traceable revenue accountability without waiting for a separate billing or accounting module.

## Personas

- **Distribution leader:** Reviews book economics, producer performance, and carrier/territory rollups.
- **Finance operations user:** Reviews expected commission records, exception states, and approved adjustments.
- **Producer manager:** Confirms producer split attribution and explains production credit.
- **Admin:** Maintains controlled reference values and role access boundaries during rollout.

## Scope & Boundaries

**In Scope:**
- Commission schedule visibility for policy and carrier/market context.
- Producer split assignment and effective-date visibility.
- Expected commission calculation review tied to policy premium and rate basis.
- Manual adjustment request and approval capture with audit evidence.
- Management rollups by producer, broker, territory, carrier/market, and policy period.
- Exception states for missing rate, missing split, conflicting effective dates, and stale source context.

**Out of Scope:**
- Full accounting ledger.
- Payments, invoicing, reconciliation, receivables, payables, and cash application.
- Carrier billing integration.
- Producer payout execution.
- Tax reporting, statement generation, and general ledger export.
- External broker or producer self-service compensation portal.

## Success Criteria

- Authorized users can find commission and split context for active policy records.
- Expected commission records show source policy, carrier/market, rate basis, producer split basis, and calculation status.
- Producer attribution remains tied to effective-dated ownership context from F0017.
- Adjustments require reason, approver, and audit evidence.
- Rollups show totals and exception counts without exposing unauthorized economic data.
- The feature stays bounded to operational visibility and attribution, not financial settlement.

## Dependencies

| Dependency | State | Why It Matters |
|------------|-------|----------------|
| F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management | Done and archived | Provides effective-dated producer, hierarchy, and territory attribution inputs. |
| F0018 Policy Lifecycle & Policy 360 | Done and archived | Provides policy, premium, version, renewal, and lifecycle context. |
| F0028 Carrier & Market Relationship Management | Done and archived | Provides carrier/market context for commission schedule visibility. |

## Story Map

| Story | Title | Slice Type | User Value |
|-------|-------|------------|------------|
| F0025-S0001 | Commission workspace search and policy context | Read visibility | Users can find authorized commission context before acting. |
| F0025-S0002 | Commission schedule maintenance | Reference setup | Finance operations can maintain rate basis and effective periods. |
| F0025-S0003 | Producer split assignment | Attribution setup | Producer managers can maintain split participants and percentages. |
| F0025-S0004 | Expected commission calculation review | Calculation visibility | Users can review expected commission and source inputs. |
| F0025-S0005 | Commission adjustment and approval | Controlled correction | Authorized users can correct exceptions with audit evidence. |
| F0025-S0006 | Revenue attribution rollups | Management reporting | Leaders can review producer, broker, territory, and carrier economics. |

## Screen Responsibilities

| Screen / Surface | Responsibility |
|------------------|----------------|
| Commission Workspace | Search, filter, and open authorized commission records by policy, producer, broker, carrier/market, status, and exception state. |
| Commission Detail | Show policy context, premium basis, schedule, producer splits, expected amount, exceptions, and audit history. |
| Schedule Maintenance Panel | Capture effective-dated commission schedule data for authorized finance/admin users. |
| Split Assignment Panel | Capture effective-dated producer participants and split percentages. |
| Adjustment Review Panel | Request, approve, reject, and audit manual commission adjustments. |
| Revenue Rollup View | Show management rollups and exception counts by producer, broker, territory, carrier/market, and policy period. |

## Screen Layouts (ASCII)

### Desktop - Commission Workspace

```text
+--------------------------------------------------------------------------------+
| Commission Workspace                                                           |
| Search [ policy / producer / broker / carrier ]  Status [v]  Exceptions [v]    |
+----------------------+-------------------------+-------------------------------+
| Results              | Selected Commission     | Context                       |
| Policy | Producer    | Expected amount         | Policy / carrier / market     |
| Status | Exceptions  | Split basis             | Producer ownership snapshot   |
| Row -> opens detail  | Schedule basis          | Audit summary                 |
+----------------------+-------------------------+-------------------------------+
| Rollup preview: Producer | Broker | Territory | Carrier | Exception count       |
+--------------------------------------------------------------------------------+
```

### Narrow - Commission Workspace

```text
+--------------------------------------+
| Commission Workspace                  |
| Search                                |
| Status [v]  Exceptions [v]            |
+--------------------------------------+
| Result card                           |
| Policy / producer / expected amount   |
| Status / exception count              |
+--------------------------------------+
| Detail tabs                           |
| Summary | Schedule | Splits | Audit   |
+--------------------------------------+
```

## Workflow Summary

1. User opens Commission Workspace and searches authorized records.
2. User opens a commission detail record tied to policy, carrier/market, and producer context.
3. Finance/admin user maintains commission schedule values when missing or outdated.
4. Producer manager or authorized operations user maintains split assignments.
5. System presents expected commission and exception status for review.
6. Authorized approver records an adjustment decision when source context or business exception requires correction.
7. Distribution leader reviews rollups and exception counts for management follow-up.

## Risks & Assumptions

- **Risk:** Scope expands into payments, accounting ledger, or reconciliation.
- **Mitigation:** Keep F0025 limited to expected commission, attribution, adjustments, and reporting visibility.
- **Risk:** Commission rules vary by carrier/market and may be underspecified.
- **Mitigation:** Start with explicit schedule capture and exception visibility; Phase B defines the minimum rule contract.
- **Assumption:** Premium and policy context from F0018 is sufficient for expected commission visibility.
- **Assumption:** Producer ownership and territory context from F0017 is sufficient for attribution snapshots.
- **Assumption:** Carrier/market context from F0028 is sufficient for schedule association.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions to Confirm in Phase B:**
- Phase B will define whether expected commission is stored, computed on demand, or both.
- Phase B will define authorization roles and policies for economic data.
- Phase B will define whether adjustment approval is single-step or multi-step for the first release.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | `CommissionRevenue` module with commission schedules, producer split assignments, expected commission review records, adjustments, and revenue attribution projections | [ADR-032](../../architecture/decisions/ADR-033-commission-producer-splits-and-revenue-tracking.md) |
| Reuses: Established Component/Pattern | Effective-dated producer ownership and territory attribution from F0017; policy lifecycle from F0018; carrier/market context from F0028; source-authorized reporting from F0023 | ADR-026, ADR-018, ADR-014-search |
| Phase B Decision | Expected commission is persisted as CRM review data with source snapshots; adjustments are single-step approval; rollups are read-side projections; no ledger/payment/payout scope | [ADR-032](../../architecture/decisions/ADR-033-commission-producer-splits-and-revenue-tracking.md) |

## Architecture Summary

- **Module:** `CommissionRevenue` inside the existing modular monolith.
- **Persistence:** commission schedules, policy-scoped split assignments, expected commission records, adjustment requests/decisions, and revenue attribution projections.
- **API:** `Commissions` endpoints in `planning-mds/api/nebula-api.yaml`.
- **Authorization:** `commission` Casbin resource actions: `read`, `schedule_manage`, `split_assign`, `calculate`, `adjustment_request`, `adjustment_approve`, and `rollup_read`.
- **Audit:** every successful schedule, split, calculation, adjustment, and projection mutation emits an immutable `ActivityTimelineEvent`.
- **Boundary:** expected commission is review/attribution data only; settlement and accounting remain out of scope.

## Related User Stories

- [F0025-S0001](./F0025-S0001-commission-workspace-search-and-policy-context.md) - Commission workspace search and policy context
- [F0025-S0002](./F0025-S0002-commission-schedule-maintenance.md) - Commission schedule maintenance
- [F0025-S0003](./F0025-S0003-producer-split-assignment.md) - Producer split assignment
- [F0025-S0004](./F0025-S0004-expected-commission-calculation-review.md) - Expected commission calculation review
- [F0025-S0005](./F0025-S0005-commission-adjustment-and-approval.md) - Commission adjustment and approval
- [F0025-S0006](./F0025-S0006-revenue-attribution-rollups.md) - Revenue attribution rollups
