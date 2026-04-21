---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0005: Policy Version History (Immutable Snapshots)

**Story ID:** F0018-S0005
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Immutable policy version snapshots and version history
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or distribution manager
**I want** an append-only history of policy versions with full profile, coverages, and premium snapshots at each issue, endorsement, and reinstatement
**So that** I can audit what was in force at any moment in time, answer broker and insured questions about prior terms, and support disputes or claims with complete historical context

## Context & Background

Versioning is the backbone of policy auditability. Every issue, endorsement, and reinstatement produces a `PolicyVersion` row with a `VersionNumber` that is monotonic within a policy, plus full `ProfileSnapshot`, `CoverageSnapshot`, and `PremiumSnapshot` jsonb captures. `PolicyCoverageLine` rows are also materialized for the version to support performant current-version queries. The version list, version detail, and side-by-side version compare live in Policy 360's Versions rail.

## Acceptance Criteria

**Happy Path:**
- **Given** an `Issued` policy with one endorsement applied
- **When** a user opens the Versions rail
- **Then** they see two rows: v1 (`IssuedInitial`) and v2 (`Endorsement`), newest first, each showing version number, reason, effective date, created-by, created-at, endorsement id (when applicable)

- **Given** any policy version
- **When** a user opens `GET /api/policies/{id}/versions/{versionId}`
- **Then** the response returns the full profile snapshot, coverage snapshot (as structured `PolicyCoverageLine` rows), and premium snapshot

- **Given** two versions of the same policy
- **When** the user selects "Compare" with v1 and v2
- **Then** the UI renders a side-by-side view with changed fields highlighted

**Alternative Flows / Edge Cases:**
- Policy with only v1 → Versions rail shows a single row; Compare affordance is hidden
- `Pending` policy → no version exists yet (v0 draft is not exposed as a version row); rail shows "No versions yet — issue this policy to create v1"
- Cancellation does not by itself produce a new version (cancellation is a state transition only); reinstatement DOES produce a new version (`Reinstatement` reason)
- Versions are immutable — any attempt to update a version returns 405 `code=version_immutable`

**Checklist:**
- [ ] `GET /api/policies/{id}/versions?page=&pageSize=` — paginated version list (newest first)
- [ ] `GET /api/policies/{id}/versions/{versionId}` — full version snapshot
- [ ] `GET /api/policies/{id}/versions/{versionId}/coverages` — coverage lines for that version
- [ ] Version creation is a side-effect of issue (S0002 / PRD), endorse (S0006), reinstate (S0008) — this story owns the read surface + the immutability guarantee
- [ ] `VersionNumber` monotonic within a policy (enforced via ordered insert under row lock or unique constraint + retry)
- [ ] `VersionReason` ∈ {`IssuedInitial`, `Endorsement`, `Reinstatement`}
- [ ] Compare endpoint: `GET /api/policies/{id}/versions/compare?a={versionIdA}&b={versionIdB}` (optional — UI may compute diff client-side from two detail payloads)
- [ ] Versions are read-only (no PUT / PATCH / DELETE exposed)
- [ ] Cancellation does not create a version; reinstatement does
- [ ] ABAC `policy:read` enforced; same scope as the parent policy

## Data Requirements

- See PRD "Entity: PolicyVersion" section
- `ProfileSnapshot` jsonb captures the full set of Policy profile fields at version time
- `CoverageSnapshot` jsonb mirrors the `PolicyCoverageLine` rows for the version (redundant with the materialized rows; provides snapshot replay even if the row schema later evolves)
- `PremiumSnapshot` captures `TotalPremium` at version time

**Validation Rules:**
- A version row is never updated after creation
- Deleting a policy does not cascade-delete versions in MVP (soft-delete on parent only; versions remain for audit). Policy soft-delete is out of MVP scope; parents stay alive for audit.
- `ProfileSnapshot` and `CoverageSnapshot` MUST contain all fields required to reconstruct the version without any other table reads (audit replay guarantee)

## Role-Based Visibility

| Role | Read |
|------|------|
| Distribution User | Yes (scoped via parent policy) |
| Distribution Manager | Yes (scoped via parent policy) |
| Underwriter | Yes (scoped via parent policy) |
| Relationship Manager | Yes (scoped via parent policy, read-only) |
| Program Manager | Yes (scoped via parent policy, read-only) |
| Admin | Yes |

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: version list p95 ≤ 300 ms for policies with ≤ 50 versions; version detail p95 ≤ 300 ms
- Reliability: snapshots are complete — audit replay must not require joining the live row
- Correctness: `VersionNumber` is unique and monotonic within a policy; concurrent endorsements cannot produce duplicate numbers

## Dependencies

**Depends On:**
- F0018-S0002 (policy creation path), F0018-S0006 (endorsement creates versions), F0018-S0008 (reinstatement creates versions)

**Related Stories:**
- F0018-S0004 (Versions rail in Policy 360)

## Out of Scope

- Field-level diff / redline tooling (MVP: side-by-side display only)
- Version rollback / revert (not in MVP; compensating endorsement required)
- Multi-party version review workflows
- Cross-policy version comparison

## UI/UX Notes

- Versions rail shows newest first; each row links to version detail
- Version detail renders profile + coverages in a structured read-only panel
- Compare view: left = earlier version, right = later version; changed fields highlighted with neutral emphasis (no green/red — MVP uses a subtle marker)

## Questions & Assumptions

**Assumptions:**
- `ProfileSnapshot` and `CoverageSnapshot` may include fields that later become non-existent on the live row; the audit replay guarantee takes precedence over schema cleanliness
- The materialized `PolicyCoverageLine` rows are a read-performance optimization; the jsonb snapshot is the source of truth for audit replay

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (immutability, empty versions list, single-version Compare hidden)
- [ ] Permissions enforced (ABAC read via parent policy)
- [ ] Audit/timeline logged: No (version creation is emitted by issue/endorse/reinstate stories)
- [ ] Tests pass (including concurrent-endorsement version-number uniqueness)
- [ ] Documentation updated (OpenAPI for version list + detail + compare)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
