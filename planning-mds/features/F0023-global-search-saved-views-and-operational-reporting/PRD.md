---
template: feature
version: 1.1
applies_to: product-manager
---

# F0023: Global Search, Saved Views & Operational Reporting

**Feature ID:** F0023
**Feature Name:** Global Search, Saved Views & Operational Reporting
**Priority:** High
**Phase:** CRM Release MVP

## Feature Statement

**As a** CRM user or manager
**I want** cross-object search, reusable views, and operational reporting
**So that** I can find the right records and manage daily business without manual reporting work

## Business Objective

- **Goal:** Make Nebula searchable, filterable, and operationally useful at scale.
- **Metric:** Search success, saved-view usage, and reduction in manual spreadsheet reporting.
- **Baseline:** Cross-object findability and operational reporting are limited.
- **Target:** Users can search and manage workflows efficiently from Nebula.

## Problem Statement

- **Current State:** Users waste time searching across disconnected lists and building manual reports.
- **Desired State:** Search and saved views provide a consistent working surface across the CRM.
- **Impact:** Faster record access, better oversight, and less operational drag.

## Scope & Boundaries

**In Scope:**
- Global search across key CRM objects
- Saved views and reusable filters
- Operational reports for workflow, workload, and due items
- Search-driven navigation into core 360 and workflow screens

**Out of Scope:**
- Advanced BI and predictive analytics
- External data warehouse tooling
- Full self-service reporting studio in the first release

## Success Criteria

- Users can find key records quickly across objects.
- Managers can use Nebula for daily operational visibility.
- Saved views reduce repetitive filtering and manual reporting work.

## Risks & Assumptions

- **Risk:** Search tries to cover too many edge cases before core data is stable.
- **Assumption:** A focused set of high-value entities and reports delivers most of the value.
- **Mitigation:** Prioritize the most-used objects and operational questions first.

## Dependencies

- F0016 Account 360 & Insured Management
- F0018 Policy Lifecycle & Policy 360
- F0019 Submission Quoting, Proposal & Approval Workflow

## Architecture & Solution Design

### Solution Components

- Introduce a cross-object search and reporting layer built on read models or search indexes, not on direct transactional-table scans from the UI.
- Add a saved-view service that persists reusable query definitions, default filters, and user or team-scoped view metadata.
- Provide operational reporting projections focused on workflow status, aging, due work, and workload visibility rather than full ad hoc BI.
- Separate search indexing, saved views, and reporting projections so each concern can scale and evolve independently.

### Data & Workflow Design

- Build canonical search documents or projections for core entities such as broker, account, policy, submission, renewal, and task.
- Use workflow transitions, due dates, assignment data, and timeline history as inputs for operational aging and backlog metrics.
- Store saved views as structured filter definitions with scope, owner, sharing model, and referenced entity type rather than opaque UI blobs alone.
- Define freshness expectations for search and reporting data so near-real-time behavior is deliberate rather than accidental.

### API & Integration Design

- Expose search endpoints with entity filters, pagination, sorting, and deep-linkable criteria that frontend screens can reuse consistently.
- Expose saved-view CRUD and operational-report endpoints as separate contracts because query definitions and aggregated metrics have different semantics.
- Update search indexes asynchronously from domain changes where appropriate, but keep fallback behavior understandable if indexing lags.
- Avoid building a general external data warehouse or free-form analytics framework into this feature boundary.

### Security & Operational Considerations

- Enforce authorization filtering inside search and reporting, not only after the fact in UI presentation, to prevent data leakage through counts or suggestions.
- Monitor index lag, query latency, high-cardinality filter patterns, and heavy report costs because these workloads can degrade the core transactional system.
- Plan retention and governance for shared saved views because team-level operational views can become business-critical artifacts.
- Use indexing strategies aligned to common dimensions such as owner, status, due date, broker, account, and line of business.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Cross-Cutting Component | Search index, saved-view store, and operational-reporting projections | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |
| Introduces/Standardizes: Cross-Cutting Pattern | Read-optimized operational views over workflow history and backlog metrics | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |
| Extends: Cross-Cutting Component | Search and reporting settings are expected to be governed through published operational configuration | [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) (Proposed) |

## Related User Stories

- To be defined during refinement
