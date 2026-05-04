---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0010: Document completeness signal endpoint

**Story ID:** F0020-S0010
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** Document completeness signal endpoint (read-only summary by category and classification)
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** consumer feature (e.g., F0006 Submission Intake, F0018 Policy Lifecycle)
**I want** a soft, read-only signal showing how many documents of each type and classification are linked to a parent record
**So that** I can surface a completeness hint without each feature reinventing its own document inventory logic

## Context & Background

ADR-012 calls out a "completeness check" capability that other features can reuse rather than each one inventing its own checklist. The user explicitly told us in G1 that this is a soft signal, not a hard gate ("brokers send a lot of things"). This story exposes a small read-only endpoint that returns counts grouped by `type` and `classification`. Consumers decide whether to highlight, warn, or ignore based on their own logic.

## Acceptance Criteria

**Happy Path — Summary returned:**
- **Given** a user with `document:read` on the parent
- **When** they request `GET /api/documents/completeness?parent={id}`
- **Then** the response returns:
  ```json
  {
    "parent": { "type": "submission", "id": "..." },
    "totals": { "available": 7, "quarantined": 1, "failed_promote": 0 },
    "byType": [
      { "type": "acord", "count": 1 },
      { "type": "loss-run", "count": 2 },
      { "type": "financials", "count": 3 },
      { "type": "supplemental", "count": 2 }
    ],
    "byClassification": [
      { "classification": "public", "count": 2 },
      { "classification": "confidential", "count": 5 },
      { "classification": "restricted", "count": 1 }
    ]
  }
  ```
- **And** counts respect the user's classification access mask (a user without restricted access sees `restricted` count as 0)

**Soft signal:**
- The endpoint never returns `isComplete: true|false`. Consumers compose their own completeness logic over the structured counts.

**Forbidden:**
- **Given** the user lacks `document:read` on the parent
- **When** they request the summary
- **Then** the response is HTTP 403 `{code: "parent_access_denied"}`

**Empty parent:**
- **Given** the parent has no documents
- **When** the summary is requested
- **Then** the response is `200 OK` with all counts at 0

**Performance:**
- p95 ≤ 300 ms on a developer laptop / typical CI runner for parents with ≤ 50 documents.

**Alternative Flows / Edge Cases:**
- A document with `versions[]` empty (corrupt sidecar) → counted as `failed_promote: 1` and a warn log emitted; never reported as `available`.
- F0020 deployed but no documents linked → still returns `200 OK` with zeros (consumers should not have to differentiate "feature unavailable" from "empty").

**Checklist:**
- [ ] Endpoint at `GET /api/documents/completeness?parent={id}`
- [ ] Returns `totals`, `byType`, `byClassification`
- [ ] Counts respect S0009 classification mask
- [ ] Read-only; never mutates state
- [ ] No `isComplete` boolean in MVP

## Data Requirements

**Response shape:**
- `parent`: `{ type, id }`
- `totals`: `{ available, quarantined, failed_promote }`
- `byType`: array of `{ type, count }` sorted by `count desc, type asc`
- `byClassification`: array of `{ classification, count }` ordered as `public, confidential, restricted`

**Validation Rules:**
- `parent` is required; reject with 400 when omitted.

## Role-Based Visibility

**Roles that can read:**
- Any role granted `document:read` on the parent.

**Data Visibility:**
- Counts visible to a user are filtered by their classification access; the response does not leak the existence of higher-tier documents.

## Non-Functional Expectations

- Performance: p95 ≤ 300 ms for ≤ 50 documents on the parent.
- Security: Counts are masked per user; cannot be used to enumerate what a user is forbidden to see.
- Reliability: Read-only; no side effects.

## Dependencies

**Depends On:**
- F0020-S0001, S0003 — Sidecar JSON shape and statuses.
- F0020-S0009 — Classification access mask.

**Related Stories:**
- Cross-feature consumers (F0006 Submission completeness, F0018 Policy 360 documents rail) will integrate with this endpoint independently of this feature.

## Business Rules

1. **Soft signal only:** The endpoint never makes a "complete vs incomplete" judgement; consumers compose their own rules.
2. **Visibility-respecting counts:** A user's view of the summary matches what they would see in the list view; restricted counts hide from disallowed users.
3. **No checklist of mandatory types in MVP:** Required-document checklists are owned by consumer features (e.g., F0006-S0005 submission completeness), not by F0020.

## Out of Scope

- Required-document policy per workflow stage (consumer features own this).
- Per-LOB completeness rules (Future, in consumer features).
- Cached completeness results (read fresh in MVP; cache layer is Future).

## UI/UX Notes

- Screens involved: none directly in F0020. The Documents List can optionally surface the totals row for context.
- Key interactions: none direct; mostly an integration surface for other features.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Consumer features will translate the structured counts into their own completeness logic.
- Reading sidecar JSONs on demand is performant enough for MVP (≤ 50 docs target).

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only)
- [ ] Tests pass (visibility masking + perf check)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0010-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
