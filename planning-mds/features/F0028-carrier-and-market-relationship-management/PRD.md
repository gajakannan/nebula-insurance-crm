---
template: feature
version: 1.1
applies_to: product-manager
---

# F0028: Carrier & Market Relationship Management

**Feature ID:** F0028
**Feature Name:** Carrier & Market Relationship Management
**Priority:** Medium
**Phase:** CRM Release MVP+

## Feature Statement

**As a** distribution leader or underwriter
**I want** carrier and market relationship records
**So that** Nebula supports appetite management, appointment context, and placement strategy

## Business Objective

- **Goal:** Extend CRM visibility beyond brokers to the carrier and market side of the business.
- **Metric:** Market relationship coverage, appetite visibility, and placement planning support.
- **Baseline:** Carrier and market intelligence are not first-class CRM records today.
- **Target:** Users can manage both broker-side and market-side relationship context in Nebula.

## Problem Statement

- **Current State:** Teams track carrier appetite and underwriter contacts outside the CRM.
- **Desired State:** Market relationships become structured, searchable, and reportable in Nebula.
- **Impact:** Better placement decisions and stronger market strategy.

## Scope & Boundaries

**In Scope:**
- Carrier and market records
- Underwriter and market contact relationships
- Appetite notes and appointment context
- Market-side activity visibility

**Out of Scope:**
- Carrier API integration
- Full rating engine
- Reinsurance workflows

## Success Criteria

- Users can access carrier and market relationship context inside Nebula.
- Market strategy becomes more structured and searchable.
- Carrier-side context can support submission and production workflows.

## Risks & Assumptions

- **Risk:** Market management scope expands into deep carrier integration too early.
- **Assumption:** Relationship and appetite visibility delivers value before systems integration.
- **Mitigation:** Start with CRM-side market records and defer carrier data exchange.

## Dependencies

- F0019 Submission Quoting, Proposal & Approval Workflow
- F0023 Global Search, Saved Views & Operational Reporting

## Architecture & Solution Design

### Solution Components

- Introduce carrier and market domain services for managing carrier records, market contacts, appetite notes, appointment context, and market activity history.
- Add market-intelligence read models that support placement planning, underwriter relationship visibility, and carrier comparison without turning this feature into a rating engine.
- Separate carrier master data from submission-specific placement activity so reusable market context is not trapped inside individual workflows.
- Keep reinsurance and automated carrier connectivity outside the first component set.

### Data & Workflow Design

- Model carrier, market, underwriter contact, appetite note, appointment record, and market activity as distinct but related concepts.
- Preserve effective dating or clear historical versioning for appetite guidance and appointment status because these facts change over time and affect placement decisions.
- Link carrier and market context to submissions, policies, producers, and territories through stable references so the same relationship model supports both placement and reporting.
- Capture source and confidence metadata for market intelligence where data may be entered manually from conversations or external materials.

### API & Integration Design

- Expose carrier and market CRUD, search, relationship, and activity endpoints with filters for geography, appetite, status, and ownership.
- Integrate with submission workflow and reporting modules through linked references and derived rollups rather than direct embedding of carrier details everywhere.
- Keep rating, quoting, and carrier API synchronization outside the contract while preserving identifiers and adapter seams for later integration work.
- Support deep links from market records into related submissions, policies, and producer relationships.

### Security & Operational Considerations

- Apply access control carefully to appetite notes, underwriter relationships, and appointment context because some market intelligence may be commercially sensitive.
- Audit edits to appetite and appointment data because those changes can materially affect placement strategy and producer behavior.
- Monitor data freshness and ownership because stale market intelligence is operationally misleading even if the UI remains functional.
- Index by carrier, market segment, appetite attributes, geography, and producer ownership to keep search and planning views responsive.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Carrier aggregate, appetite-note service, appointment context, and market activity feed | PRD only |
| Reuses: Established Component/Pattern | Search and reporting substrate for market relationship discovery and rollups | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Proposed) |
| PRD-Only Traceability | No separate carrier-domain ADR is required unless market relationships expand into a shared external connectivity platform | None currently required |

## Related User Stories

- To be defined during refinement
