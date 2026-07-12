---
template: feature
version: 1.1
applies_to: product-manager
---

# F0032: Admin Configuration & Reference Data Console

**Feature ID:** F0032
**Feature Name:** Admin Configuration & Reference Data Console
**Priority:** High
**Phase:** Platform Operations
**Status:** Draft - Phase A refinement complete, pending approval

## Feature Statement

**As an** internal administrator
**I want** a governed console for operational configuration and reference data
**So that** Nebula can change routine business settings safely without direct code changes, manual database edits, or module-specific admin drift

## Business Objective

- **Goal:** Introduce the first governed runtime configuration surface for high-impact operational settings.
- **Primary metric:** Administrators can draft, validate, publish, rollback, and audit the first-release configuration domains from inside Nebula.
- **Baseline:** F0022, F0023, F0027, and F0034 establish module-owned settings and governance foundations, but administration remains spread across local controls, seed data, or module-specific surfaces.
- **Target:** Admins manage the selected first-release configuration domains through one permission-safe console with explicit draft/validated/published states and traceable publish history.

## Target Personas

- **Admin:** Owns configuration governance, publish/rollback decisions, and audit accountability.
- **Operations Manager:** Reviews queue/routing and SLA setting impacts before requesting or approving changes.
- **Compliance or Quality Lead:** Reviews configuration history and confirms changes are traceable.
- **Configuration Steward:** Maintains reference values and operational setting metadata when delegated by Admin.

## Problem Statement

- **Current State:** Operational settings are either seeded, governed locally by a module, or changed through narrow manager controls. As the product grows, routine changes risk becoming engineering tickets, direct database edits, or inconsistent module-specific admin workflows.
- **Desired State:** Admins use a central console to discover supported configuration domains, draft changes, validate impact, publish approved versions, roll back recent publications, and audit who changed what.
- **Impact:** Safer operational change, fewer engineering dependencies for routine settings, and a consistent governance model for queue/routing, workflow SLA thresholds, saved-view/report defaults, and template metadata.

## Scope & Boundaries

### In Scope for First Release

- Admin Configuration Console shell with a catalog of supported configuration domains and their current state.
- Published configuration set lifecycle: draft, validation result, published version, rollback target, and audit history.
- First-release domains:
  - F0022 queue/routing governance over existing `WorkQueue`, `AssignmentRule`, `CoverageWindow`, and fallback queue contracts.
  - Workflow SLA thresholds for submission and renewal status timing, including default and line-of-business-specific values already modeled by `WorkflowSlaThreshold`.
  - F0023 saved-view/report default governance where team defaults and operational report defaults already exist.
  - F0027 template metadata governance for template status, family, version, and publish visibility; template upload/rendering remains owned by F0027.
- Validation and compare behavior before publish, including conflict and missing-field feedback.
- Publish and rollback actions for supported domains.
- Permission-safe audit history for create, update, validate, publish, rollback, and failed validation attempts.

### Out of Scope

- First implementation of queue/routing entities, rule evaluation, queue worklists, reassignment, or local manager controls owned by F0022.
- Replacing F0022 durable data structures or rule execution semantics.
- Free-form custom rule designer beyond the approved queue/routing and SLA configuration shapes.
- Identity-provider administration, Casbin policy authoring, user provisioning, secrets, infrastructure settings, or deployment configuration.
- Full no-code product schema authoring for F0034 schema bundles.
- Document generation, template upload, preview, issue, or artifact storage owned by F0027.
- External broker/MGA administration surfaces.

## Boundary Guardrails

- **F0022 boundary:** F0032 governs the existing queue/routing contracts from F0022. It must not recreate queue execution, rule precedence, fallback routing, coverage activation, or queue worklists.
- **F0023 boundary:** F0032 may govern team/global defaults and report setting visibility. Search execution, saved-view CRUD behavior, report projection freshness, and permission-safe query filtering remain F0023-owned.
- **F0027 boundary:** F0032 may surface template metadata publish status. Template upload, generated document preview, issue, retrieval, regeneration, and source-record document rails remain F0027-owned.
- **F0034 boundary:** F0032 does not become a no-code product-schema console. Product schema registry activation and dynamic attribute rendering remain governed by F0034/ADR-020/ADR-021/ADR-023.
- **ADR-016 boundary:** Runtime business configuration uses published configuration sets with validation, versioning, refresh expectations, and audit. Infrastructure configuration remains outside product admin scope.

## Success Criteria

- Admins can see each supported configuration domain, current published version, draft status, last validation result, and last publisher from one console.
- Admins or delegated configuration stewards can create and update draft changes for the supported first-release domains.
- Admins can run validation before publish and see specific blocking errors for unsupported changes, conflicts, missing required values, or stale versions.
- Admins can compare a draft against the current published version before publishing.
- Admins can publish a validated configuration set and see downstream refresh status or a clear pending/error state.
- Admins can roll back to the previous published version when the domain supports rollback.
- Auditors can review who created, edited, validated, published, rolled back, or failed validation for every governed configuration set.
- Unauthorized users cannot see or infer restricted configuration values, drafts, publish failures, audit details, or hidden record counts.

## Release Slicing

| Slice | Stories | Outcome |
|-------|---------|---------|
| Catalog | F0032-S0001 | Admins can discover governed domains and current configuration state. |
| Drafting | F0032-S0002, F0032-S0003 | Admins can draft first-release SLA/reference and queue/routing configuration changes. |
| Validation | F0032-S0004 | Admins can validate and compare changes before publish. |
| Publication | F0032-S0005 | Admins can publish validated sets and roll back recent versions. |
| Oversight | F0032-S0006 | Admin/audit users can review permission-safe configuration history and failures. |

## User Workflows

### Discover Configuration Domains

1. Admin opens Admin Configuration from the authenticated shell.
2. Admin sees supported domains grouped by queue/routing, workflow SLA thresholds, search/report defaults, and template metadata.
3. Admin opens a domain and sees current published version, draft status, validation status, last changed by, and last published time.
4. If the domain is not yet supported, the console shows an explicit unsupported state and no edit control.

### Draft And Validate Configuration

1. Admin opens a supported domain and selects Create Draft or Edit Draft.
2. Admin updates supported fields for that domain.
3. Nebula preserves the current published version while the draft remains unpublished.
4. Admin runs validation and receives pass/fail feedback with field-level or rule-level errors.
5. Admin compares the draft against the current published version before publish.

### Publish And Roll Back

1. Admin opens a validated draft.
2. Admin reviews comparison summary, validation result, and downstream impact statement.
3. Admin publishes the draft.
4. The configuration domain shows a new published version and records audit history.
5. If a recent publish must be reversed, Admin selects a previous eligible version, reviews the rollback summary, and confirms rollback.

### Audit Configuration Changes

1. Admin or Compliance/Quality Lead opens Configuration Audit.
2. User filters by domain, action, actor, status, or date range.
3. User opens an audit row and sees before/after summary, validation result, publish version, rollback target if applicable, and failure reason when validation or publish failed.
4. Audit history does not expose restricted source-record data to unauthorized users.

## Screen Responsibilities

| Screen / Surface | Responsibility | Primary Users |
|------------------|----------------|---------------|
| Admin Configuration Catalog | List supported domains, current state, publish status, and unsupported-domain boundaries. | Admin, Operations Manager |
| Configuration Domain Detail | Show published version, drafts, validation status, compare summary, and available actions for one domain. | Admin, Configuration Steward |
| Configuration Draft Editor | Edit supported configuration fields for SLA thresholds, queue/routing settings, saved-view/report defaults, or template metadata. | Admin, Configuration Steward |
| Validation and Compare Drawer | Present blocking errors, warnings, changed fields, version deltas, and downstream impact notes before publish. | Admin, Operations Manager |
| Publish / Rollback Confirmation | Confirm publish or rollback with version, actor, reason, and expected downstream refresh state. | Admin |
| Configuration Audit Workspace | Review permission-safe history for changes, validation failures, publishes, rollbacks, and downstream refresh outcomes. | Admin, Compliance or Quality Lead |

## Screen Layouts (ASCII)

### Desktop - Admin Configuration Console

```text
--------------------------------------------------------------------------------+
| Admin > Configuration                                      [Audit] [New Draft] |
+----------------------+------------------------------+--------------------------+
| Domains              | Configuration Sets           | Detail                   |
| - Queue/Routing      | Queue Routing Rules          | Published: v12          |
| - Workflow SLA       | Workflow SLA Thresholds      | Draft: v13 validation   |
| - Search/Reports     | Search Report Defaults       | Last publish: A. Chen   |
| - Templates          | Template Metadata            | Actions: Edit Draft     |
|                      |                              | [Validate] [Compare]    |
|                      |                              | [Publish] [Rollback]    |
+----------------------+------------------------------+--------------------------+
| Status: validation failed - 2 blocking issues                                   |
| Audit: v12 published 2026-07-03 by A. Chen                                      |
+--------------------------------------------------------------------------------+
```

### Narrow - Admin Configuration Console

```text
+--------------------------------------+
| Admin Configuration        [Audit]    |
+--------------------------------------+
| Domain [Queue/Routing v]              |
| Published v12                         |
| Draft v13 - Validation failed         |
+--------------------------------------+
| Blocking issues: 2                    |
| Last publish: A. Chen, 2026-07-03     |
+--------------------------------------+
| [Edit Draft] [Validate]               |
| [Compare] [Publish] [Rollback]        |
+--------------------------------------+
```

## User Stories

| Story | Title | Scope |
|-------|-------|-------|
| [F0032-S0001](./F0032-S0001-admin-configuration-catalog.md) | Admin configuration catalog | MVP |
| [F0032-S0002](./F0032-S0002-draft-reference-and-sla-configuration.md) | Draft reference data and workflow SLA configuration | MVP |
| [F0032-S0003](./F0032-S0003-govern-queue-routing-configuration.md) | Govern queue and routing configuration drafts | MVP |
| [F0032-S0004](./F0032-S0004-validate-and-compare-configuration.md) | Validate and compare configuration before publish | MVP |
| [F0032-S0005](./F0032-S0005-publish-and-rollback-configuration.md) | Publish and roll back configuration sets | MVP |
| [F0032-S0006](./F0032-S0006-audit-and-permission-safe-admin-configuration.md) | Audit and permission-safe configuration behavior | MVP |

## Dependencies

- F0022 Work Queues, Assignment Rules & Coverage Management - durable queue/routing foundation and local manager controls.
- F0023 Global Search, Saved Views & Operational Reporting - saved-view/report defaults and permission-safe reporting behavior.
- F0027 COI, ACORD & Outbound Document Generation - reusable template metadata and published template provenance.
- F0034 Product Schema Registry and Dynamic LOB Attributes - governed product schema precedent and JSON/rules validation posture.
- ADR-013 Operational Routing and Queue Engine.
- ADR-014 Search Index, Saved Views, and Operational Reporting Projections.
- ADR-014 Workflow SLA Threshold Per-LOB Extension.
- ADR-016 Published Operational Configuration Governance.

## Business Rules

1. **Published version remains authoritative:** Draft changes do not affect routing, reporting, templates, or SLA behavior until successfully published.
2. **Validation before publish:** A configuration set cannot be published unless the latest validation result for that draft is passing and matches the draft version being published.
3. **Queue/routing governance boundary:** Queue/routing edits must preserve F0022 rule precedence, fallback queue semantics, explicit coverage-window activation, and idempotent routing expectations.
4. **Rollback creates audit evidence:** Rollback restores an eligible previous published version through an explicit rollback action; it does not erase intervening audit history.
5. **Restricted administration:** Only Admin can publish or roll back. Configuration Steward may draft only when delegated; Operations Manager may review but cannot publish unless also Admin.
6. **Audit immutability:** Create, update, validate, publish, rollback, failed validation, and failed publish events remain append-only.

## Non-Functional Expectations

- **Security:** Admin configuration surfaces are internal-only and must enforce server-side authorization before returning drafts, validation errors, audit details, or publish state.
- **Performance:** Configuration catalog and domain detail reads must meet the standard API read endpoint budget from BLUEPRINT §4.6.
- **Reliability:** Failed validation or publish attempts must preserve the prior published version.
- **Auditability:** Every mutation must record actor, action, domain, version, timestamp, and reason or failure summary.
- **Usability:** Empty, unsupported, draft, validation-failed, published, rollback-eligible, and downstream-refresh-pending states must be visually distinct.

## Architecture Traceability

**Taxonomy Reference:** [Feature Architecture Traceability Taxonomy](../../architecture/feature-architecture-traceability-taxonomy.md)

| Classification | Artifact / Decision | ADR |
|----------------|---------------------|-----|
| Introduces: Cross-Cutting Component | Published operational configuration governance and admin console control surfaces | [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) (Proposed), [ADR-032](../../architecture/decisions/ADR-032-admin-configuration-console-contract.md) (Proposed) |
| Extends: Cross-Cutting Component | Queue and routing administration builds on the shared routing engine | [ADR-013](../../architecture/decisions/ADR-013-operational-routing-and-queue-engine.md) (Accepted) |
| Extends: Cross-Cutting Component | Search/report defaults become governed runtime configuration domains | [ADR-014](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) (Accepted), [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) (Proposed) |
| Extends: Cross-Cutting Component | Workflow SLA thresholds become administrable through governed published configuration sets | [ADR-014 Workflow SLA](../../architecture/decisions/ADR-014-workflow-sla-threshold-per-lob-extension.md) (Accepted), [ADR-016](../../architecture/decisions/ADR-016-published-operational-configuration-governance.md) (Proposed) |
