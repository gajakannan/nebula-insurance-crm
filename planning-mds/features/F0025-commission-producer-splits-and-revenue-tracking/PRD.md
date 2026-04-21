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

## Feature Statement

**As a** distribution leader or finance-facing operations user
**I want** commission and producer split visibility
**So that** Nebula can support brokerage economics and production accountability

## Business Objective

- **Goal:** Extend Nebula from CRM workflow into brokerage revenue visibility.
- **Metric:** Commission visibility coverage, split accuracy, and production attribution completeness.
- **Baseline:** Revenue and commission context are outside the current CRM feature set.
- **Target:** Nebula supports commission-aware operational reporting and ownership.

## Problem Statement

- **Current State:** Users can manage relationships and workflows without seeing the economics behind them.
- **Desired State:** Commission and split data become part of operational and management visibility.
- **Impact:** Better producer accountability and brokerage performance understanding.

## Scope & Boundaries

**In Scope:**
- Commission rate modeling
- Producer splits and attribution
- Revenue tracking linked to policy and production records
- Management reporting on earned or expected economics

**Out of Scope:**
- Full accounting ledger
- Payments and reconciliation
- Carrier billing integration

## Success Criteria

- Nebula can represent producer attribution and expected commission accurately.
- Management reporting connects production and revenue more directly.
- The feature supports brokerage economics without replacing a full accounting system.

## Risks & Assumptions

- **Risk:** This feature pulls the product too far into finance systems prematurely.
- **Assumption:** Revenue visibility can exist without full accounting parity.
- **Mitigation:** Keep early scope focused on operational visibility and attribution.

## Dependencies

- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management
- F0018 Policy Lifecycle & Policy 360
- F0028 Carrier & Market Relationship Management

## Architecture & Solution Design

### Solution Components

- Introduce commission and revenue services that calculate expected or earned economics from policy, producer, and market context without turning Nebula into a full ledger.
- Add a split allocation component that resolves producer and hierarchy-based attribution using effective-dated ownership inputs from F0017.
- Provide management reporting projections for commission visibility, production attribution, and economic rollups by producer, broker, territory, and carrier.
- Keep payment execution and full accounting settlement outside the module, even when revenue status is visible here.

### Data & Workflow Design

- Model commission schedules, producer splits, revenue records, effective dates, and adjustment history as first-class economic records linked to policy and producer context.
- Preserve historical ownership and split basis so changed producer assignments do not retroactively corrupt prior revenue attribution.
- Represent overrides and manual adjustments as auditable records with reason, approver, and effective period rather than silently overwriting computed results.
- Keep derived economics separate from policy source data so recalculation and what-if logic do not mutate core policy facts.

### API & Integration Design

- Expose commission and revenue query endpoints, adjustment actions, and producer or carrier rollup views with clear separation between derived metrics and source records.
- Consume hierarchy, policy, and market data from upstream modules rather than duplicating structural ownership models locally.
- Provide downstream hooks for billing and reconciliation workflows while avoiding premature coupling to full accounting integrations.
- Preserve idempotent recalculation boundaries so repeat imports or policy changes do not duplicate revenue records.

### Security & Operational Considerations

- Restrict access to commission and revenue data to appropriate finance, management, and authorized producer roles because economics are more sensitive than general CRM workflow data.
- Audit overrides, split changes, recalculations, and manual corrections with a stronger evidentiary trail than ordinary UI edits.
- Monitor recalculation cost on back-dated policy or ownership changes because revenue recomputation can become expensive.
- Define reconciliation flags and anomaly reporting early so downstream finance workflows can trust the commission layer's outputs.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Commission rules service, split engine, revenue records, and economic rollups | PRD only |
| Reuses: Established Component/Pattern | Effective-dated producer ownership and territory attribution from hierarchy models | PRD only |
| PRD-Only Traceability | No separate cross-cutting ADR is required unless commission logic becomes a shared finance platform beyond Nebula CRM scope | None currently required |

## Related User Stories

- To be defined during refinement
