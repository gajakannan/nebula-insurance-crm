# ADR-032: Admin Configuration Console Contract

**Status:** Proposed
**Date:** 2026-07-06
**Owners:** Architect
**Related Features:** F0032

## Context

F0032 introduces a central admin console for operational configuration domains that already exist in module-owned features: F0022 queue/routing settings, workflow SLA thresholds, F0023 saved-view/report defaults, and F0027 document template metadata. ADR-016 already establishes that runtime business configuration is governed through published operational configuration sets, but it does not define the console contract, API shape, draft storage, or refresh handshake.

The console must make routine configuration changes possible without direct database edits while avoiding a parallel execution engine for queues, reports, templates, or workflow state.

## Decision

F0032 will implement a governed configuration facade over existing module-owned configuration surfaces.

- `ConfigurationDomain` is a catalog entry that declares supported domains, owning module, editable schema, validation strategy, refresh strategy, and rollback support.
- `ConfigurationDraft` stores a draft payload for exactly one domain and base published version.
- `ConfigurationValidationResult` stores the latest validation outcome, blocking errors, warnings, compare summary, and the draft hash validated.
- `PublishedOperationalConfigurationSet` records immutable published versions per domain.
- `ConfigurationRefreshStatus` records the downstream refresh handshake for consumers that cache published values.
- `ConfigurationAuditEvent` records create, update, validate, publish, rollback, refresh, and failure actions as append-only audit evidence.

The API surface is under `/admin/configuration-*` and follows the existing REST, ProblemDetails, ABAC, `If-Match`, and row-version conventions. The facade calls domain adapters owned by the source modules:

- Queue/routing adapter validates and publishes `WorkQueue`, `AssignmentRule`, `CoverageWindow`, and fallback constraints without replacing F0022 routing execution.
- Workflow SLA adapter validates `WorkflowSlaThreshold` payloads against submission/renewal status references and line-of-business rules.
- Saved-view/report defaults adapter validates F0023 default scopes and criteria but does not execute search or reports.
- Template metadata adapter validates document template status/family/version visibility but does not upload, render, issue, or store generated artifacts.

## Consequences

### Positive

- Admin configuration has one consistent draft/validate/publish/rollback/audit lifecycle.
- Existing module execution remains authoritative.
- The same API and JSON Schema contracts can drive backend validation and frontend dynamic forms.
- Published runtime state has explicit refresh visibility and rollback evidence.

### Negative

- Adds facade and adapter complexity over already-existing module endpoints.
- Cached consumers need a refresh-status contract even when refresh is currently in-process.
- Rollback remains domain-specific and must be refused when the target domain marks a prior version ineligible.

## Implementation Notes

- F0032 does not create `feature-assembly-plan.md` during planning. Implementation sequencing belongs to the later feature action.
- First release uses in-process adapters and in-memory cache invalidation by default; Redis or cross-instance invalidation requires a later DevOps/runtime decision.
- All mutation endpoints must append `ConfigurationAuditEvent` and `ActivityTimelineEvent` entries in the same unit of work where the domain mutation is committed.
