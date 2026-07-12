---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0006: Audit and permission-safe configuration behavior

**Story ID:** F0032-S0006
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Audit and permission-safe configuration behavior
**Priority:** High
**Phase:** MVP

## User Story

**As a** Compliance or Quality Lead
**I want** permission-safe audit history for configuration changes
**So that** operational setting changes can be reviewed without exposing restricted configuration data to unauthorized users

## Context & Background

Configuration changes can alter routing, reporting, templates, and workflow SLA behavior. F0032 must make those changes reviewable while enforcing internal-only visibility and role-specific permissions.

## Acceptance Criteria

**Happy Path:**
- **Given** configuration draft, validate, publish, rollback, and failure events exist
- **When** I open Configuration Audit with an authorized role
- **Then** I can filter by domain, action, actor, status, and date range
- **And** each audit row shows action, actor, timestamp, domain, version, reason, and before/after summary when authorized

**Alternative Flows / Edge Cases:**
- If I lack audit permission, the audit workspace is unavailable and direct navigation returns a permission-safe denial.
- If an audit row references a restricted domain outside my authorization, the row is hidden or redacted according to my role.
- Failed validation and failed publish attempts appear with failure reason summaries.
- Audit history remains visible after rollback; rollback does not erase prior publish records.

**Checklist:**
- [ ] Audit includes create draft, update draft, validate, validation failed, publish, publish failed, rollback, and rollback failed actions.
- [ ] Audit filters do not leak hidden-domain counts to unauthorized users.
- [ ] Permission denials do not expose restricted values.
- [ ] Audit history is append-only.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A — read-only audit story.

## Data Requirements

**Required Fields:**
- Domain key.
- Action type.
- Actor.
- Timestamp.
- Version.
- Result status.
- Reason or failure summary.

**Optional Fields:**
- Before/after summary.
- Downstream refresh status.
- Rollback target version.

**Validation Rules:**
- Audit query filters must be permission-scoped before counts and rows are returned.
- Audit rows must preserve events after rollback.

## Role-Based Visibility

**Roles that can view audit:**
- Admin — full configuration audit for supported domains.
- Compliance or Quality Lead — audit review where authorized.
- Operations Manager — queue/routing and SLA audit where delegated.

**Data Visibility:**
- InternalOnly content: all audit rows and details.
- ExternalVisible content: none.

## Non-Functional Expectations

- Security: counts, filters, rows, and detail payloads are computed after authorization.
- Reliability: audit history is append-only.
- Performance: bounded filtered audit queries follow standard list endpoint expectations.

## Dependencies

**Depends On:**
- F0032-S0002 — draft events.
- F0032-S0004 — validation events.
- F0032-S0005 — publish and rollback events.

**Related Stories:**
- F0032-S0001 — catalog state links to audit.

## Business Rules

1. Configuration audit is internal-only.
2. Rollback never deletes or rewrites previous audit history.
3. Unauthorized users must not infer hidden configuration domains from counts, filters, or error messages.

## Out of Scope

- External audit export.
- SIEM integration.
- Editing or deleting audit rows.

## UI/UX Notes

- Screens involved: Configuration Audit Workspace.
- Key interactions: filter audit rows, open audit detail, review redacted states.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- Audit review is internal-only for the first release.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (audit rows are the primary evidence)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0006-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.

