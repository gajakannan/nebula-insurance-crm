---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0001: Admin configuration catalog

**Story ID:** F0032-S0001
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Admin configuration catalog
**Priority:** High
**Phase:** MVP

## User Story

**As a** Admin
**I want** to see a catalog of governed configuration domains and their current state
**So that** I know which operational settings can be safely managed from Nebula

## Context & Background

F0032 starts with supported domains that already have source ownership: F0022 queue/routing configuration, workflow SLA thresholds, F0023 saved-view/report defaults, and F0027 template metadata. The catalog prevents the admin console from becoming an unrestricted system administration surface.

## Acceptance Criteria

**Happy Path:**
- **Given** I am an Admin in the authenticated app shell
- **When** I open Admin Configuration
- **Then** I see supported configuration domains grouped by Queue/Routing, Workflow SLA, Search/Reports, and Templates
- **And** each supported domain shows current published version, draft state, last validation status, last publisher, and last publish timestamp when available

**Alternative Flows / Edge Cases:**
- Unsupported domains appear only as read-only unavailable entries with the reason "Not governed in this release".
- If no draft exists for a supported domain, the domain shows "No active draft".
- If the current user's role cannot manage configuration, the entry point is hidden or disabled and direct navigation returns a permission-safe denial.
- If the catalog cannot load, the screen shows a retryable system-error state without exposing restricted configuration details.

**Checklist:**
- [ ] The catalog distinguishes supported domains from out-of-scope infrastructure, identity-provider, and policy-authoring domains.
- [ ] Published, draft, validation-failed, and unsupported states are visually distinct.
- [ ] Domain rows link only to domains the user is authorized to view.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only story.

## Data Requirements

**Required Fields:**
- Domain key: stable identifier for the supported configuration domain.
- Domain display name: user-facing label.
- Published version: current authoritative version when one exists.
- Draft status: none, draft, validation failed, validated, or publish pending.
- Last changed by and last published by: display names or safe identifiers for authorized users.

**Optional Fields:**
- Downstream refresh status: pending, refreshed, failed, or not applicable.
- Unsupported reason: text for unavailable domains.

**Validation Rules:**
- Domain keys must come from the supported first-release domain list.
- Unsupported domains must not expose edit or publish actions.

## Role-Based Visibility

**Roles that can view:**
- Admin — can view all supported domains.
- Configuration Steward — can view delegated domains.
- Operations Manager — can view queue/routing and SLA domains for review.
- Compliance or Quality Lead — can view audit-facing domain status.

**Data Visibility:**
- InternalOnly content: all configuration domain state and audit metadata.
- ExternalVisible content: none.

## Non-Functional Expectations

- Performance: catalog read follows the standard read endpoint budget in BLUEPRINT §4.6.
- Security: server-side authorization filters domains and actions before response.
- Reliability: empty and system-error states are distinguishable.

## Dependencies

**Depends On:**
- F0022 — Queue/routing domain exists.
- F0023 — Search/report saved-view defaults exist.
- F0027 — Template metadata domain exists.
- F0034 / ADR-014 Workflow SLA — workflow SLA thresholds exist.

**Related Stories:**
- F0032-S0002 — drafts reference and SLA configuration.
- F0032-S0003 — drafts queue/routing configuration.
- F0032-S0006 — audits configuration behavior.

## Business Rules

1. Catalog domain list is allow-listed for the first release; it is not inferred from database tables.
2. Published version remains authoritative until a later publish action succeeds.

## Out of Scope

- Editing configuration values.
- Publishing, rollback, or validation.
- User provisioning, IdP administration, Casbin policy authoring, secrets, and infrastructure settings.

## UI/UX Notes

- Screens involved: Admin Configuration Catalog.
- Key interactions: open catalog, select domain, distinguish unsupported domains.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- First-release domains remain limited to those listed in this story unless the operator approves a scope change.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only catalog)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0001-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.

