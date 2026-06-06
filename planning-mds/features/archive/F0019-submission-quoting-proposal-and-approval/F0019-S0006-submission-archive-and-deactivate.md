# Submission Archive and Deactivate

## Story Header

**Story ID:** F0019-S0006
**Feature:** F0019 — Submission Quoting, Proposal & Approval Workflow
**Title:** Submission archive and deactivate
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** underwriter or distribution user
**I want** to archive a decided submission so it leaves active operational queues while staying discoverable
**So that** queues stay focused on live work without losing audit history

## Context & Background

This is the **mandated archive story**. F0006 descoped its unimplemented submission soft-delete
claim, so F0019 owns the replacement end-of-life contract. It is delivered as an explicit,
audit-preserving **archive/deactivate lifecycle action** — not a generic delete endpoint and not a
physical purge. Archive is allowed only for **terminal** submissions (`Bound`, `Declined`,
`Withdrawn`); archived submissions are removed from active queues by default but remain discoverable
for audit, reporting, and historical lookup.

## Acceptance Criteria

**Happy Path:**
- **Given** a submission in a terminal state (`Bound`, `Declined`, or `Withdrawn`)
- **When** an authorized user archives it
- **Then** the submission is marked archived/deactivated, it disappears from the default active list, an append-only `ActivityTimelineEvent` ("archived") is written, and **all** workflow/approval/transition history is preserved unchanged.

**Discoverability / reactivate:**
- **Given** an archived submission
- **When** a user enables "Include archived" in the list or opens it directly
- **Then** the submission and its full history are visible (read), clearly flagged as archived.
- An authorized user may deactivate-archive's inverse (un-archive/reactivate) back into active queues; reactivation is itself audited.

**Alternative Flows / Edge Cases:**
- Archive attempted on a non-terminal submission (e.g., `InReview`, `Quoted`) → `409` (only terminal states may be archived).
- Unauthorized actor attempts archive → `403 Forbidden`.
- Any attempt at a hard/physical delete route → not provided; such a request is `404`/`405` (no generic delete exists).
- Archiving an already-archived submission → `409` (idempotent; no duplicate event).
- Submission not found → `404`.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Submission Detail → "Archive" | Click, confirm | Enabled only for terminal states + authorized actor | `isArchived=true`; removed from default list; history preserved | Reload: not in default list; visible with "Include archived"; timeline shows "archived" | Underwriter/Admin (configurable in Phase B authz) |
| Submission List → "Include archived" + open → "Reactivate" | Click | Enabled for archived + authorized | `isArchived=false`; returns to active queues; audited | Reload shows back in active list; timeline shows "reactivated" | Underwriter/Admin |

Required checks:
- [ ] Render-only behavior cannot satisfy this story — archive state persists and changes list membership after reload.
- [ ] Archive is restricted to terminal states; bad state → `409`; unauthorized → `403`.
- [ ] Archive/reactivate append append-only audit/timeline records; no history is mutated or removed.
- [ ] Tests prove an archived submission is excluded from the default list but discoverable with the archived filter.

## Data Requirements

**Required Fields:**
- `submissionId`, `isArchived` (boolean lifecycle flag), `archivedByUserId`, `archivedAt`
- `archiveReason` (optional in MVP; controlled list if provided)

**Validation Rules:**
- Archive permitted only when current state ∈ {`Bound`, `Declined`, `Withdrawn`}.
- Archive never deletes binaries, documents, or transition/approval history (audit-preserving).
- No physical delete path exists for submissions.

## Role-Based Visibility

**Roles that can archive/reactivate:**
- Underwriter — permitted (ABAC `submission:archive`)
- Admin — permitted
- Distribution user — view archived (read) only in MVP; archive permission set in Phase B authz
- BrokerUser — no access

**Data Visibility:**
- InternalOnly content: archive metadata, preserved history.
- ExternalVisible content: none in MVP.

## Non-Functional Expectations

- Performance: archive/reactivate < 500ms p95; default list query excludes archived without scan penalty.
- Security: distinct `submission:archive` permission; unauthorized → `403`.
- Reliability: archive flag + audit event atomic; fully reversible (reactivate), never destructive.

## Dependencies

**Depends On:**
- F0019-S0005 (and S0004) — terminal states must exist before a submission can be archived.
- ADR-011 — append-only history must remain intact through archive.

**Related Stories:**
- F0019-S0007 — list "Include archived" filter and archived flagging.

## Business Rules

1. **Replaces F0006 soft-delete:** F0019 owns submission end-of-life as archive/deactivate, traceable back to F0006's descoped soft-delete claim.
2. **Terminal-only:** active submissions cannot be archived; archive is not a way to abandon live work (use Withdraw, S0005).
3. **Audit-preserving, reversible:** archive hides from active queues but preserves all evidence and can be reversed; no physical purge or audit-destructive deletion.
4. **No generic delete:** there is no CRUD delete route for submissions.

## Out of Scope

- Physical purge, retention-driven destruction, or audit-destructive deletion (explicit non-goal).
- Bulk archive operations (Future; MVP is per-submission).
- Archiving of non-submission entities.

## UI/UX Notes

- Screens involved: Submission Detail ("Archive"/"Reactivate"), Submission List ("Include archived" toggle + archived flag).
- Key interactions: confirm-archive dialog; archived submissions visibly flagged when shown.

## Questions & Assumptions

**Open Questions:**
- [ ] Should distribution users be allowed to archive, or is archive underwriting/admin-only in MVP? (Default assumption: underwriting/admin-only; finalize in Phase B authorization.)

**Assumptions (to be validated):**
- A boolean `isArchived` lifecycle flag (plus audit) is sufficient for MVP; no separate archive store is needed.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (`409`/`403`/`404`, no-delete-route)
- [ ] Permissions enforced (`submission:archive`)
- [ ] Audit/timeline logged (archive + reactivate; history preserved)
- [ ] Boundary regression: no physical/generic delete route exists
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated if story file was added/renamed/moved
