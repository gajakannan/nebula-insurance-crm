---
template: feature
version: 1.1
applies_to: product-manager
---

# F0026: Billing, Invoicing & Reconciliation

**Feature ID:** F0026
**Feature Name:** Billing, Invoicing & Reconciliation
**Priority:** Medium
**Phase:** Brokerage Platform Expansion

## Feature Statement

**As a** finance-facing operations user
**I want** billing, invoicing, and reconciliation support
**So that** Nebula can extend into brokerage operating workflows beyond relationship management

## Business Objective

- **Goal:** Add finance-adjacent operating visibility to Nebula.
- **Metric:** Billing workflow coverage, invoice visibility, and reconciliation completeness.
- **Baseline:** Nebula does not currently address billing operations.
- **Target:** Billing and policy-related financial transactions are visible and trackable in Nebula.

## Problem Statement

- **Current State:** Finance and reconciliation processes sit outside the current CRM experience.
- **Desired State:** Nebula can support billing-facing workflows linked to policies and production.
- **Impact:** Stronger operational continuity, but outside strict CRM scope.

## Scope & Boundaries

**In Scope:**
- Invoice and billing event records
- Reconciliation workflow visibility
- Policy-linked billing context
- Finance-facing operational tracking

**Out of Scope:**
- Full accounting platform replacement
- General ledger
- Bank integration

## Success Criteria

- Billing workflow can be modeled and tracked in Nebula.
- Finance-facing users can connect billing activity to policies and relationships.
- The feature stays clearly bounded from full accounting replacement.

## Risks & Assumptions

- **Risk:** This feature drives the product too deeply into AMS/accounting territory too early.
- **Assumption:** Billing visibility can be useful without replacing finance systems.
- **Mitigation:** Keep scope workflow-oriented, not ledger-oriented.

## Dependencies

- F0018 Policy Lifecycle & Policy 360
- F0025 Commission, Producer Splits & Revenue Tracking

## Architecture & Solution Design

### Solution Components

- Introduce billing and invoice services that track billable events, invoices, receivable status, and reconciliation state without replacing a general ledger.
- Add reconciliation workflow components that compare expected billing state against imported or recorded payment and adjustment data.
- Provide finance-facing operational views over invoice status, exceptions, unapplied items, and reconciliation backlog.
- Keep accounting-system ownership external where appropriate, with Nebula acting as an operational visibility layer and integration participant.

### Data & Workflow Design

- Model invoices, billing events, payment references, reconciliation records, and exception reasons as distinct entities so finance workflows stay explainable.
- Use explicit billing and reconciliation status models instead of overloaded free-form fields on policy or commission records.
- Link invoices back to policy, account, producer, and commission context through stable identifiers to support drill-down and operational traceability.
- Preserve imported-versus-user-entered provenance on billing data so reconciliation disputes can be investigated cleanly.

### API & Integration Design

- Expose invoice list/detail, billing-event tracking, reconciliation action, and exception-management endpoints.
- Integrate with policy lifecycle and commission modules through reference data and event hooks, while leaving bank connectivity and GL posting to later or external systems.
- Support batch import and export boundaries for finance operations instead of assuming all billing data originates inside Nebula.
- Keep the contract explicit about finance operational state rather than promising end-to-end accounting ownership.

### Security & Operational Considerations

- Restrict billing and reconciliation features to finance-authorized users with clear separation from general CRM users.
- Audit invoice creation, adjustments, reconciliation decisions, and write-offs because finance-adjacent actions require stronger change traceability.
- Track outstanding balances, unreconciled counts, import failures, and exception aging as operational metrics.
- Design batch operations to be resumable and idempotent because billing feeds and reconciliation imports will often be retried.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Feature-Local Component | Invoice service, billing-event records, reconciliation workflow, and exception views | PRD only |
| Reuses: Established Component/Pattern | Explicit workflow-state modeling for reconciliation lifecycle | [ADR-011](../../architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md) (Proposed) |
| Extends: Cross-Cutting Component | Finance-facing external exchange is expected to flow through the integration hub over time | [ADR-015](../../architecture/decisions/ADR-015-integration-hub-canonical-contracts-and-outbox.md) (Proposed) |

## Related User Stories

- To be defined during refinement
