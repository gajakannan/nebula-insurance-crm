---
template: feature
version: 1.1
applies_to: product-manager
---

# F0017: Broker/MGA Hierarchy, Producer Ownership & Territory Management

**Feature ID:** F0017
**Feature Name:** Broker/MGA Hierarchy, Producer Ownership & Territory Management
**Priority:** High
**Phase:** CRM Release MVP+

## Feature Statement

**As a** distribution leader or relationship manager
**I want** broker hierarchy, producer ownership, and territory visibility
**So that** I can manage channels, accountability, and regional performance accurately

## Business Objective

- **Goal:** Reflect real commercial P&C distribution structure inside Nebula.
- **Metric:** Ownership clarity, territory reporting coverage, and hierarchy-based workflow accuracy.
- **Baseline:** Broker records exist, but advanced hierarchy and producer ownership are limited.
- **Target:** Nebula supports channel-aware structure and assignment rules.

## Problem Statement

- **Current State:** Advanced broker hierarchy and producer ownership are not fully modeled.
- **Desired State:** MGAs, sub-brokers, producers, and territories are represented explicitly.
- **Impact:** Better governance, reporting, and relationship accountability.

## Scope & Boundaries

**In Scope:**
- MGA to broker hierarchy
- Producer ownership and territory assignment
- Hierarchy-aware visibility and reporting
- Relationship and production rollups

**Out of Scope:**
- Commission calculation
- External producer portal
- Carrier-side appointment detail

## Success Criteria

- Users can model and navigate broker hierarchy accurately.
- Ownership and territory logic support downstream workflows and reporting.
- Hierarchy supports operational and analytical use cases without manual workarounds.

## Risks & Assumptions

- **Risk:** Hierarchy design becomes overly complex before core CRM workflows are stable.
- **Assumption:** Producer ownership is more valuable once account, submission, and reporting foundations exist.
- **Mitigation:** Sequence this feature after CRM Release MVP core workflow delivery.

## Dependencies

- F0002 Broker & MGA Relationship Management
- F0023 Global Search, Saved Views & Operational Reporting

## Architecture & Solution Design

### Solution Components

- Extend the broker domain with hierarchy management services, producer ownership services, and territory assignment components instead of treating hierarchy as display-only metadata.
- Add hierarchy-aware rollup services for production, workflow, and activity reporting across MGA, broker, and producer levels.
- Introduce a territory and ownership policy component that can later be reused by queue routing, reporting, and access-control decisions.
- Keep producer ownership separate from commission calculation so F0017 remains the structural model while F0025 owns the economics.

### Data & Workflow Design

- Model broker hierarchy using a self-referencing relationship, with room for materialized path or cached ancestry data if query depth becomes expensive.
- Represent producer ownership and territory assignment as effective-dated relationships so historical attribution and rollup accuracy are preserved.
- Add hierarchy-aware reporting projections rather than recalculating entire broker trees on each screen load.
- Validate changes to prevent cycles, orphaned children, and overlapping territory rules that would undermine downstream routing and reporting.

### API & Integration Design

- Expose APIs for hierarchy traversal, ownership assignment, territory management, and hierarchy-aware search/report filtering.
- Feed hierarchy and producer context into F0022 routing rules and F0023 reporting without making those modules recalculate structural relationships independently.
- Support drill-down from MGA to broker to producer through consistent identifiers and filter semantics.
- Keep external producer portal and carrier appointment integrations out of the initial boundary.

### Security & Operational Considerations

- Extend authorization checks to account for parent-child broker visibility, territory scoping, and producer-level ownership.
- Audit all hierarchy and ownership changes because they affect access boundaries, reporting rollups, and future commission attribution.
- Monitor rollup recalculation cost and consider asynchronous recomputation if hierarchy updates become expensive.
- Preserve historical ownership snapshots or effective-dated reads so reports remain accurate after organizational changes.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Broker hierarchy service, producer ownership model, and territory rules | PRD only |
| Extends: Cross-Cutting Component | Territory and ownership data become routing inputs for shared queue execution | [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md) (Proposed) |
| Reuses: Established Component/Pattern | Hierarchy-aware rollups across shared search and reporting surfaces | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |

## Related User Stories

- To be defined during refinement
