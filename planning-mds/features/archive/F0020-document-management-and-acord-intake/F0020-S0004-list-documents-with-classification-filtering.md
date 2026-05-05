---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0020-S0004: List documents on a parent record with classification filtering

**Story ID:** F0020-S0004
**Feature:** F0020 — Document Management & ACORD Intake
**Title:** List documents on a parent record with classification filtering
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution user, underwriter, coordinator, broker, or MGA contact
**I want** to see every document linked to an account, submission, policy, or renewal with classification and type visible at a glance
**So that** I can find what I need quickly and ignore documents I am not allowed to see

## Context & Background

The list view is the primary entry point to a parent record's documents. It powers the Documents List screen (Desktop and narrow variants) defined in the PRD ASCII layouts. Listing reads the colocated sidecar JSON files; binaries themselves are never enumerated directly. Classification filtering both narrows results and respects the per-user access rules from S0009.

## Acceptance Criteria

**Happy Path — Default list:**
- **Given** a user with `document:read` for the parent
- **When** they request `GET /api/documents?parent={id}` with no filters
- **Then** the response returns up to the page size of documents the user is allowed to see, ordered by `latestUpload desc`
- **And** quarantined documents are included with `status: quarantined` and no preview/download links
- **And** the response includes a `pagination` envelope with `page`, `pageSize`, `total`

**Filter — By classification:**
- **Given** the user requests `?classification=public,confidential`
- **When** the list is computed
- **Then** the result excludes `restricted` documents
- **And** classifications the user is forbidden from (per S0009) are excluded regardless of the filter

**Filter — By type:**
- **Given** the user requests `?type=acord,loss-run`
- **When** the list is computed
- **Then** only documents whose sidecar JSON `type` matches one of the values are returned

**Pagination:**
- **Given** the parent has more documents than the default page size (20)
- **When** the user requests `?page=2&pageSize=20`
- **Then** the second page of 20 is returned and `pagination.total` reflects the total visible to that user

**Empty state:**
- **Given** a parent record with no documents
- **When** the list is requested
- **Then** the response is `200 OK` with `documents: []` and `pagination.total: 0`

**Performance:**
- p95 list latency ≤ 500 ms when the parent has ≤ 10 documents on a developer laptop / typical CI runner.

**Alternative Flows / Edge Cases:**
- Sidecar JSON missing for a logical document (orphan binary) → entry omitted from the list and a warn-level log emitted; the entry is *not* surfaced as a corrupt-row in MVP.
- Sidecar JSON present but `versions[]` empty → entry omitted and warn-logged.
- A binary in `failed_promote` state → list shows the row with a `failed_promote` badge and no actions other than read-only metadata.

**Checklist:**
- [ ] Endpoint exists at `GET /api/documents` with `parent`, `classification`, `type`, `page`, `pageSize` query params
- [ ] Default order: `latestUpload desc`
- [ ] Default page size: 20; cap: 100
- [ ] `restricted` documents excluded from default and explicit filters when the user lacks access
- [ ] Quarantined and failed-promote rows surfaced with status, never as actionable rows
- [ ] p95 ≤ 500 ms for ≤ 10 documents

## Data Requirements

**Per-row response shape:**
- `documentId`, `logicalName`, `type`, `classification`, `latestVersion`, `status`, `latestUpload: { atUtc, byUserId }`, `parent: { type, id }`

**Validation Rules:**
- `pageSize` clamped to 1-100; out-of-range returns HTTP 400.
- `classification` query values must be a subset of `public,confidential,restricted`; unknown values return HTTP 400.

## Role-Based Visibility

**Roles that can list:**
- Any role granted `document:read` on the parent record. Classification gating from S0009 narrows individual rows.

**Data Visibility:**
- External brokers / MGA contacts see only `public` rows on parent records they can read; never `confidential` or `restricted`.

## Non-Functional Expectations

- Performance: p95 ≤ 500 ms for ≤ 10 docs; p95 ≤ 1500 ms for 100 docs.
- Security: Filter-bypass prevention — server-side filter cannot widen the effective set beyond what S0009 allows.
- Reliability: Orphan/corrupt sidecar entries are logged but never surface as broken UI rows.

## Dependencies

**Depends On:**
- F0020-S0001 — Sidecar JSON shape.
- F0020-S0003 — Status states.
- F0020-S0009 — Classification ABAC.

**Related Stories:**
- F0020-S0005 — Detail view linked from each row.
- F0020-S0010 — Completeness signal computed from the same sidecar JSON corpus.

## Business Rules

1. **Sidecar JSON is the truth:** Listing never enumerates binaries; only sidecar JSONs.
2. **Classification gates first, filters second:** Server applies the user's S0009 access mask before applying user-supplied filters.
3. **Quarantined rows are visible but inert:** They show status only; no actions are available until promotion completes.

## Out of Scope

- Free-text search across metadata (Future — see F0023 global search integration).
- Cross-parent document search (Future).
- Saved views / persisted filters (Future).

## UI/UX Notes

- Screens involved: Documents List (Desktop + narrow variants).
- Key interactions: classification dropdown, type dropdown, pagination controls, click-through to detail.

## Questions & Assumptions

**Open Questions:**
- None.

**Assumptions (to be validated):**
- Default sort `latestUpload desc` is the operationally useful order for MVP.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A — read-only)
- [ ] Tests pass (including p95 perf check on ≤ 10 doc fixture)
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0020-S0004-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
