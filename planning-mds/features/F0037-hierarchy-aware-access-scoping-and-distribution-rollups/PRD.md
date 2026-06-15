---
template: feature
version: 1.1
applies_to: product-manager
---

# F0037: Hierarchy-Aware Access Scoping & Distribution Rollups

**Feature ID:** F0037
**Feature Name:** Hierarchy-Aware Access Scoping & Distribution Rollups
**Priority:** Medium
**Phase:** CRM Release MVP+

> **Placeholder feature.** Created 2026-06-06 (plan run `2026-06-06-5fb353e9`) to
> home the scope explicitly **deferred** from F0017 at its G1 clarification gate.
> Not yet refined — run the `plan` action for F0037 to produce stories and
> architecture.

## Feature Statement

**As a** distribution leader or compliance owner
**I want** access visibility and production reporting to honor the broker/MGA hierarchy, territories, and producer ownership
**So that** users see only what their position in the channel allows and leaders get accurate rolled-up production and activity

## Business Objective

- **Goal:** Turn the structural distribution model from F0017 into enforced visibility and rolled-up insight.
- **Metric:** Access decisions and rollup figures that match the hierarchy/territory/ownership model without manual reconciliation.
- **Baseline:** F0017 models hierarchy, effective-dated ownership, and effective-dated territories as data + audit, but does **not** enforce access or compute rollups.
- **Target:** Hierarchy/territory/ownership drive access scoping and aggregated reporting.

## Problem Statement

- **Current State (after F0017):** The distribution structure exists and is audited, but every internal user can read the whole tree and there are no hierarchy-aware rollups.
- **Desired State:** Visibility is scoped by position in the hierarchy/territory/ownership; production, workflow, and activity roll up across MGA → broker → producer.
- **Impact:** Channel-appropriate confidentiality and trustworthy distribution analytics.

## Scope & Boundaries

**In Scope (to be refined):**
- Hierarchy-aware **access-control enforcement** — parent/child broker visibility, territory scoping, and producer-ownership scoping (consuming the F0017 model).
- Hierarchy-aware **distribution rollup reporting** — production, workflow, and activity rollups across MGA/broker/producer levels, including effective-dated point-in-time accuracy.

**Out of Scope:**
- The structural model itself (owned by F0017).
- Commission/revenue economics (F0025).
- General cross-object search & reporting substrate (F0023) — this feature consumes/extends it for hierarchy-aware rollups rather than rebuilding it.
- External producer portal (F0029).

## Success Criteria

- Access decisions provably honor hierarchy, territory, and producer ownership.
- Rollups reconcile to the underlying effective-dated F0017 records at a chosen "as of" date.

## Risks & Assumptions

- **Risk:** Recursive access checks and rollup recomputation can be expensive on deep trees.
- **Assumption:** F0017 is delivered first and exposes stable identifiers, effective-dated reads, and audit events for this feature to consume.
- **Mitigation:** Sequence after F0017; consider cached ancestry, materialized rollups, and async recomputation during refinement.

## Dependencies

- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management (structural model — prerequisite)
- F0023 Global Search, Saved Views & Operational Reporting (reporting substrate this extends)
- Authorization/policy foundation (Casbin ABAC) for enforcement

## Architecture & Solution Design

To be defined during refinement (Phase B of a future F0037 plan run). Likely
introduces an authorization policy layer keyed on hierarchy/territory/ownership
and a materialized rollup projection over F0017's effective-dated records.

## Architecture Traceability

To be defined during refinement.

## Related User Stories

- To be defined during refinement
