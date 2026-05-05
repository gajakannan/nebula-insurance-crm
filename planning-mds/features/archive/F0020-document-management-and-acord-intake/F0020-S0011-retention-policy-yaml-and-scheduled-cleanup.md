---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0011: Retention policy YAML and scheduled cleanup

**Story ID:** F0020-S0011
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Retention policy YAML and scheduled cleanup (MVP cap = 10 days)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** Nebula operator
**I want** documents and their sidecar JSON to be removed when they exceed a per-type retention policy defined in canonical YAML
**So that** the demo CRM repository stays bounded, retention rules are auditable as code, and the system has a structural placeholder that production retention can extend

## Context & Background

In G1 the user explicitly capped MVP retention at 10 days, with a canonical YAML file as the source of truth and the policy file colocated in a reserved `configuration/` folder inside the document repository. This is not the production retention rule — insurance recordkeeping needs years — but it is the MVP demo rule, and it is enforced as a hard ceiling so we can never silently retain too long. Longer-horizon retention is Future scope.

## Acceptance Criteria

**Policy file location and shape:**
- **Given** the document repository at `<docroot>`
- **Then** `<docroot>/configuration/document-retention-policies.yaml` is the only retention source of truth
- **And** the file matches the schema:
  ```yaml
  version: 1
  defaultRetentionDays: 10
  perType:
    acord: 10
    loss-run: 7
    financials: 7
    supplemental: 3
    template: 10
  ```
- **And** values exceeding 10 are rejected at load time with a structured error

**Cleanup pass — happy path:**
- **Given** documents whose latest version's `uploadedAt` is older than the per-type retention
- **When** the scheduled sweeper runs (default: every 1 hour)
- **Then** every binary version file *and* the sidecar JSON is removed from disk
- **And** an audit row is written to `<docroot>/configuration/retention-sweeps.jsonl` with `{ documentId, parent, type, classification, lastUploadedAt, sweptAt, byPrincipal: "system:retention-sweeper" }`

**Cleanup pass — partial removal forbidden:**
- **Given** a document with multiple versions where some are within retention and some are not
- **When** the sweeper evaluates the document
- **Then** the document is treated as a single unit — sweep applies only when the latest version's `uploadedAt` exceeds retention
- **And** older binaries are never removed independently of their sidecar JSON

**Hard ceiling enforcement:**
- **Given** a `perType` value greater than 10 (or `defaultRetentionDays > 10`) at load time
- **Then** the loader rejects the file, keeps the prior policy in force, and logs a structured error
- **And** an unbounded retention is never possible in MVP

**Dry-run mode:**
- **Given** an admin runs the sweeper with `--dry-run`
- **When** the pass executes
- **Then** the JSONL audit log records the *intended* deletions with `dryRun: true` and no files are removed
- **And** the result is also returned to the operator console (CLI in MVP)

**Observability:**
- Each sweep records start, end, total scanned, total swept, and per-type counts.

**Forbidden during sweep:**
- The sweeper acquires the same per-document lock used by S0007 / S0008 to prevent racing with replace or metadata edits.

**Alternative Flows / Edge Cases:**
- Sidecar JSON missing (orphan binary) → sweeper removes the orphan binary, logs the orphan in the JSONL audit, no document-level row produced.
- Binary missing (orphan sidecar JSON) → sweeper removes the orphan JSON, logs the orphan, no document-level row produced.
- Retention reduced from 10 to 3 days mid-run → already-loaded documents are evaluated against the active policy at the time of evaluation; documents queued before the reload finish under the previous policy.

**Checklist:**
- [ ] Policy file lives at `<docroot>/configuration/document-retention-policies.yaml`
- [ ] Policy schema validated; load fails closed-by-default
- [ ] Hard ceiling: any value > 10 rejected at load
- [ ] Sweeper runs at a configurable schedule (default 1 h, configurable 5 m - 24 h)
- [ ] Sweep audit JSONL at `<docroot>/configuration/retention-sweeps.jsonl`
- [ ] Per-document lock prevents racing with S0007/S0008
- [ ] Dry-run mode supported

## Data Requirements

**`document-retention-policies.yaml` schema fields:**
- `version` (int)
- `defaultRetentionDays` (int 1-10)
- `perType` (map of taxonomy `type` → int 1-10)

**`retention-sweeps.jsonl` row fields:**
- `documentId, parent, type, classification, lastUploadedAt, sweptAt, byPrincipal, dryRun? `

**Validation Rules:**
- All ints in `1..10` inclusive.
- `perType` keys must exist in `configuration/taxonomy.yaml`.

## Role-Based Visibility

**Roles that can read the policy file:**
- Admin and DevOps.

**Roles that can run the sweeper manually / in dry-run:**
- Admin and DevOps.

## Non-Functional Expectations

- Performance: A scheduled sweep over a repository with 1,000 documents completes in ≤ 30 s.
- Security: Sweeper runs as a system principal but evaluates against S0009; security log records each removal.
- Reliability: JSONL audit append is durable (fsync after each row); a crashed sweeper resumes safely on next tick.

## Dependencies

**Depends On:**
- F0020-S0001, S0003, S0007, S0008 — sidecar JSON shape and event mutations.
- F0020-S0009 — security audit hook.

**Related Stories:**
- F0020-S0012 — Templates also live under the same repository and are subject to the policy (separate `template` retention key).

## Business Rules

1. **YAML is the policy:** No retention rule is set in code; the YAML file is the authoritative source.
2. **Hard ceiling at 10 days:** The loader rejects any value above 10. MVP cannot silently retain longer than 10 days.
3. **Document is a single unit:** Sweeps remove an entire document (all versions + sidecar JSON) or none of it; per-version selective removal is not allowed.
4. **Auditable sweeps:** Every sweep produces a JSONL audit row; orphans are logged distinctly.
5. **Future scope is explicit:** Long-horizon retention (insurance compliance) is a Future story; the MVP cap is documented as MVP-only.

## Out of Scope

- Per-tenant retention overrides (Future).
- Legal hold / retention exemption (Future).
- Automatic move-to-archive instead of delete (Future).
- Retention longer than 10 days at MVP (Future — hard ceiling enforced now).

## UI/UX Notes

- Screens involved: none in MVP. The sweeper is a backend job; CLI dry-run is the only user surface.
- Key interactions: operator runs the CLI in dry-run mode before changing policy.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- 10-day MVP cap is acceptable for the demo CRM workspace.
- A scheduled sweeper (vs. event-driven cleanup) is acceptable at MVP scale.
- Templates (S0012) follow the same retention model with their own `template` key.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (orphans, mid-run policy reload)
- [ ] Permissions enforced (admin/DevOps only for manual operations)
- [ ] Audit/timeline logged (`retention-sweeps.jsonl` per row)
- [ ] Tests pass (loader hard-ceiling, sweep dry-run, lock with S0007/S0008)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0011-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
