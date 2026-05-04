---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0009: Classification-based access control on document operations

**Story ID:** F0020-S0009
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Classification-based access control layered on parent ABAC
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** Nebula security owner
**I want** document operations to enforce both the parent record's ABAC rules and the document's classification tier
**So that** an external broker can never see a `restricted` financial spreadsheet even when they can see the parent submission

## Context & Background

The PRD ties access to "permissions of the parent record plus document classification sensitivity". This story owns that combined gate. The classification rules live in a canonical YAML at `configuration/casbin-document-roles.yaml` so the policy is auditable as code, and the runtime evaluator combines the parent ABAC verdict with the classification verdict using AND semantics.

## Acceptance Criteria

**Combined gate semantics:**
- **Given** a document with classification `C` on a parent record `P`
- **When** a user `U` attempts operation `Op` (read | create | replace | update_metadata | download | declassify)
- **Then** the system grants access only when both:
  - `parent_abac(U, P, parentOp(Op))` allows
  - `classification_policy(U.role, C, Op)` allows
- **And** denial of either dimension yields HTTP 403 with `{code, dimension: "parent_abac" | "classification_policy"}`

**Tier semantics:**
- `public`: every role with parent-read can read; create/replace requires parent-create.
- `confidential`: internal roles only (Distribution, Underwriter, Coordinator, Admin); external broker / MGA roles are denied even with parent-read.
- `restricted`: a named subset (Underwriter, Admin) has read; create/replace requires explicit `document:create:restricted`; declassification requires `document:declassify`.

**Source of truth — YAML:**
- **Given** `configuration/casbin-document-roles.yaml`
- **When** the file is loaded at startup or on hot-reload
- **Then** the policy table maps `(role, classification, operation) → allow|deny`
- **And** the runtime evaluator uses only this table; no inline hardcoded role names

**Hot reload:**
- **Given** an authorised admin replaces `configuration/casbin-document-roles.yaml`
- **When** the policy loader detects a change
- **Then** the new policy is applied within 60 seconds without a restart
- **And** the loader logs the policy version applied

**Validation on load:**
- **Given** a YAML file that fails the schema (missing tier, unknown role, non-allow/deny verdict)
- **When** the loader runs
- **Then** it rejects the change, keeps the prior policy in force, and surfaces a structured error log

**Observable evaluation:**
- **Given** any document operation
- **When** access is evaluated
- **Then** the security log records `(actor, op, documentId, parentVerdict, classificationVerdict, finalVerdict)` (without leaking sensitive metadata)

**Forbidden — bypass attempt:**
- **Given** a request that targets a `restricted` document
- **When** the user has parent-read but not the classification permission
- **Then** the system denies the request with `dimension: "classification_policy"` and never returns the document body or metadata

**Alternative Flows / Edge Cases:**
- Role missing from YAML → treat as deny (closed-by-default).
- Classification missing from YAML → treat as deny.
- Unknown operation → deny with HTTP 400.
- A user holds two roles, one allow, one deny → allow wins (classic Casbin precedence) but the security log records the contributing roles.

**Checklist:**
- [ ] `configuration/casbin-document-roles.yaml` is the only source of classification policy
- [ ] Combined gate uses AND of parent-ABAC and classification policy
- [ ] Closed-by-default for missing entries
- [ ] Hot reload within 60 s on file change
- [ ] Security audit log row per evaluation
- [ ] Schema validation rejects malformed policy

## Data Requirements

**`configuration/casbin-document-roles.yaml` schema (canonical):**
- `version: 1`
- `tiers: [public, confidential, restricted]`
- `roles: [admin, underwriter, distribution-user, coordinator, broker-user, mga-user, external-user]` (sourced from existing project roles; new roles `document:create:restricted`, `document:declassify` introduced)
- `policy: [{role, tier, op, verdict}]` rows

**Validation Rules:**
- All listed roles must exist in the project's role registry.
- `tier` ∈ defined tiers.
- `op` ∈ defined ops.
- `verdict` ∈ `allow | deny`.

## Role-Based Visibility

**Roles that can read the policy YAML:**
- Admin only.

**Roles that can change the policy YAML:**
- Admin only; configuration management is owned by DevOps signoff.

## Non-Functional Expectations

- Performance: Combined gate evaluation < 5 ms per operation in p95; YAML parsed once and cached.
- Security: No back door — even system processes that act on a user's behalf must pass the combined gate (with the user's principal).
- Reliability: Loader keeps the prior policy in force on a bad reload; never silently accepts an empty policy.

## Dependencies

**Depends On:**
- Project Casbin policy infrastructure (existing).

**Related Stories:**
- F0020-S0001, S0002, S0004, S0005, S0006, S0007, S0008 — every operation funnels through the combined gate.
- F0020-S0011 — Retention sweep runs as a system principal but still records evaluations for audit.

## Business Rules

1. **Combined gate is non-bypassable:** No code path bypasses the classification check, even for system principals.
2. **YAML is the policy:** Hardcoded role names in code are forbidden; the policy table is loaded from YAML and validated on load.
3. **Closed by default:** Anything not explicitly allowed is denied.
4. **Restricted requires explicit grant:** `document:create:restricted` and `document:declassify` are separate, not implied by general document-create / metadata-edit grants.

## Out of Scope

- Per-tenant policy overrides (Future).
- Time-bound classification (e.g., declassify after 30 days; Future).
- Field-level redaction within a binary (Future — not feasible without rendering changes).

## UI/UX Notes

- Screens involved: indirectly visible — denied documents simply do not appear in lists; `restricted` actions show a "permissions insufficient" toast on attempt.
- Key interactions: none direct; this story is mostly server-side enforcement.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Existing role registry covers MVP needs; the two new permissions (`document:create:restricted`, `document:declassify`) can be added without disruption.
- 60 s hot-reload latency is acceptable; an event-driven reload is Future.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (the entire story IS the enforcement)
- [ ] Audit/timeline logged (security log entry per evaluation)
- [ ] Tests pass (closed-by-default + hot reload + schema validation)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0009-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
