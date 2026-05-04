---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0003: Quarantine and mock-scan workflow

**Story ID:** F0020-S0003
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Quarantine and mock-scan workflow (60 s hold then promote)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** Nebula operator
**I want** every uploaded binary to sit in a quarantine folder for 60 seconds before becoming visible on the parent record
**So that** the system has a structural placeholder for malware scanning even though MVP uses a mocked timer instead of a real scanner

## Context & Background

ADR-012 calls for a scanning hook in the document subsystem. MVP cannot afford a real scanner integration, so this story implements the structural pipeline — quarantine, hold, promote — and treats the scanner as a 60-second timer. Replacing the timer with a real scanner is an interface swap, not a redesign; the timer + audit events are the contract that real scanners must satisfy.

## Acceptance Criteria

**Happy Path — Promotion after 60 s:**
- **Given** a quarantined binary at `…/quarantine/{upload-id}` with sidecar JSON status `quarantined`
- **When** 60 seconds elapse from the `uploadedAt` timestamp
- **Then** a worker atomically moves the binary to `…/{parent-type}/{parent-id}/{logicalName}-v{N}.{ext}`
- **And** the sidecar JSON `versions[N-1].status` flips to `available`
- **And** an `events: [{kind: "promoted", at, byUserId: "system", version: N}]` row is appended
- **And** the document becomes visible on the parent's documents list (S0004) and detail (S0005)

**Idempotency:**
- **Given** the worker re-runs after a transient failure
- **When** it observes a sidecar JSON already in `available` for the same version
- **Then** it does not re-promote, does not duplicate the event row, and logs a debug skip

**Stuck-in-quarantine indicator:**
- **Given** a quarantine entry older than the configured hold (60 s) plus a 30 s grace
- **When** the documents list is rendered for the parent
- **Then** the row shows the ⏳ scanning… badge with a "longer than expected" hint, and a manual operator action `promote` is exposed in an admin view (admin UI is Future scope; the state hint is in MVP)

**Failure path — Promote fails:**
- **Given** the move from quarantine to target folder fails (e.g., disk error)
- **When** the worker observes the failure
- **Then** the sidecar JSON `events` appends `{kind: "promote_failed", at, error}`
- **And** the binary remains in quarantine
- **And** an alert is surfaced to operators (logging suffices in MVP; alert pipeline is Future)

**Configurability:**
- **Given** a configuration value `documents.quarantine.holdSeconds` in `configuration/document-retention-policies.yaml` (or its sibling)
- **When** the value is changed within a 30-300 s range
- **Then** the worker honours the new value on subsequent uploads
- **And** values outside the range are rejected at config-load time

**Checklist:**
- [ ] Quarantine folder is `…/quarantine/{upload-id}` with no parent-type prefix
- [ ] Promotion worker runs at minimum every 10 s
- [ ] Promotion is atomic (rename or copy-then-delete with verified checksum)
- [ ] Sidecar JSON audit / timeline event row added on every promotion (`promoted` event with actor `system:quarantine-worker`, `at`, and `version`)
- [ ] Idempotent: re-running the worker is safe
- [ ] Configurable hold time bounded to 30-300 s
- [ ] Worker authorization: the quarantine + promote endpoints are not user-callable; only the system principal `system:quarantine-worker` may invoke promotion. A user request hitting an internal promote path is rejected with HTTP 403 `{code: "promotion_internal_only"}`.

## Data Requirements

**Sidecar JSON additions:**
- `versions[].status`: `quarantined | available | failed_promote`
- `events[]`: append `promoted` and `promote_failed` rows.

**Validation Rules:**
- Hold time bounds enforced at YAML load.
- Worker must verify checksum before flipping status to `available`.

## Role-Based Visibility

**Roles that can read quarantine state:**
- Same as parent-record document read (`document:read` on the parent).
- Quarantined documents are visible on the list with a ⏳ badge but cannot be downloaded or previewed.

**Data Visibility:**
- A quarantined binary is never returned by detail (S0005) or download (S0006) until promoted.

## Non-Functional Expectations

- Performance: Worker tick latency ≤ 10 s; observed time-to-promote in tests = 60 s ± 10 s.
- Security: Quarantine folder is not browseable; only the worker process and the upload service can write to it.
- Reliability: Idempotent worker; failure modes are observable through sidecar JSON events.

## Dependencies

**Depends On:**
- F0020-S0001 — Sidecar JSON shape.
- F0020-S0002 — Bulk paths produce one quarantine entry per file.

**Related Stories:**
- F0020-S0004, S0005, S0006 — Read paths gate on `versions[].status = available`.

## Business Rules

1. **Quarantine is structural, not optional:** Every upload, including replacements, passes through quarantine.
2. **Mock is observable:** The 60 s timer emits the same audit shape a real scanner would; integration tests validate the interface, not the scanner internals.
3. **Promotion is atomic:** The system never exposes a partially-promoted binary to read paths.
4. **Failure does not silently retry forever:** After 5 promote failures for the same version, the worker marks `versions[].status = failed_promote` and stops retrying; remediation is manual (operator action; admin UI deferred).

## Out of Scope

- Real malware scanner integration.
- Operator UI for retrying / manually promoting / quarantining (Future).
- Quarantine of replacements re-uploaded by the same user within seconds (Future — currently each upload gets its own quarantine slot).

## UI/UX Notes

- Screens involved: Documents List ⏳ badge, Upload Dialog ("scanning, available in ~60 s" hint).
- Key interactions: passive — the user sees the badge clear automatically once promotion completes.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- 60 s is long enough to demo and short enough to not impede real workflow.
- A simple polling worker is acceptable; an event-driven queue is Future scope.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (read paths gate on status)
- [ ] Audit/timeline logged (`promoted`, `promote_failed` events)
- [ ] Tests pass (including idempotency and failure-mode tests)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
