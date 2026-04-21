---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0016-S0008: Account Merge and Duplicate Handling

**Story ID:** F0016-S0008
**Feature:** F0016 — Account 360 & Insured Management
**Title:** Account merge (synchronous) with impact preview and audited history
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** distribution manager or admin
**I want** to merge a duplicate account into a chosen survivor with a preview of the impact and a full audit trail
**So that** the account book converges to a single truth without breaking any dependent submission, policy, renewal, or timeline view

## Context & Background

Account merge is the primary duplicate-resolution tool in F0016. MVP supports synchronous merges for accounts with ≤ 500 linked records; async / Temporal-backed merge is a Future follow-up. Unmerge is not offered in MVP.

## Acceptance Criteria

**Happy Path:**
- **Given** a distribution manager with `account:merge` in scope on both source and survivor accounts
- **When** they call `GET /api/accounts/{sourceId}/merge-preview?survivorId={survivorId}`
- **Then** the response reports the counts of linked submissions, policies, renewals, timeline events, and contacts that would be re-pointed or preserved

- **Given** a manager who has reviewed the preview
- **When** they call `POST /api/accounts/{sourceId}/merge` with `{ survivorAccountId, mergeNotes }` and `If-Match`
- **Then** within one transaction:
  - Source account transitions to `Merged`, `MergedIntoAccountId` = survivorId, `RemovedAt` set, `StableDisplayName` frozen
  - All dependent submissions / policies / renewals / contacts / timeline rows remain pointing at the source id (we rely on the tombstone-forward contract for rendering, not row re-pointing)
  - A timeline event `account.merged_into` is appended on the source referencing the survivor
  - A timeline event `account.merged_from` is appended on the survivor referencing the source
  - A `WorkflowTransition` row is appended on the source

- **Given** a caller opens `/accounts/{sourceId}` after a successful merge
- **When** the frontend receives `status=Merged` + `survivorAccountId`
- **Then** the UI auto-redirects to `/accounts/{survivorAccountId}` per the fallback contract

**Alternative Flows / Edge Cases:**
- Survivor is the same as source → 400
- Survivor is not Active → 409 `invalid_state`
- Source is already Merged or Deleted → 409 `invalid_state`
- Source has more than the MVP threshold of linked records (> 500) → 413 `merge_too_large`; message directs operator to the deferred async path
- Stale rowVersion → 412 `concurrency_conflict`
- Unauthorized actor → 403
- Retry with same `Idempotency-Key` yields same result (idempotent)

**Checklist:**
- [ ] `GET /api/accounts/{id}/merge-preview?survivorId=...` returns impact counts
- [ ] `POST /api/accounts/{id}/merge` performs the merge atomically
- [ ] Source persists historically (`MergedIntoAccountId` + frozen `StableDisplayName`)
- [ ] Timeline entries on BOTH source and survivor
- [ ] `WorkflowTransition` row on source
- [ ] Dependent list endpoints render "<stable source name> → <survivor name>" for source-linked rows (part of F0016-S0009 contract)
- [ ] Merge threshold (≤ 500 linked records) enforced server-side

## Data Requirements

- Uses Account fields from PRD: `Status`, `MergedIntoAccountId`, `RemovedAt`, `StableDisplayName`
- Merge preview payload: `{ submissionCount, policyCount, renewalCount, contactCount, timelineCount, totalLinked }`

**Validation Rules:**
- `survivorAccountId ≠ sourceAccountId`
- Survivor must be `Status=Active`
- Source must be `Status ∈ {Active, Inactive}`

## Role-Based Visibility

- Preview: Distribution Manager + Admin (both scopes required)
- Merge: Distribution Manager + Admin (both scopes required)

## Non-Functional Expectations

- Performance: merge commit p95 ≤ 2 s for ≤ 500 linked records
- Security: Casbin `account:merge` on both source and survivor
- Reliability: single-transaction atomicity; retry-safe via Idempotency-Key

## Dependencies

**Depends On:**
- F0016-S0007 (lifecycle engine reused)
- F0016-S0009 (fallback contract defines how dependent views render Merged)

**Related Stories:**
- F0016-S0010 (timeline), F0016-S0002 (duplicate hint points users toward merge)

## Out of Scope

- Async / Temporal-backed merge for large accounts (Future)
- Unmerge / rollback (Future)
- Bulk merge
- Automated merge on exact tax-id match

## UI/UX Notes

- Merge flow: from source account, choose survivor from search; preview panel shows impact counts; Confirm disabled until user explicitly acknowledges the irreversibility; post-merge toast on survivor 360
- Keyboard shortcut not required in MVP

## Questions & Assumptions

**Assumptions:**
- MVP does not re-point dependent foreign keys; tombstone-forward provides the user-facing experience. This keeps the merge transaction small and idempotent. Re-pointing is a Future follow-up if operational pain emerges.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (409 invalid_state, 413 merge_too_large, 412 concurrency)
- [ ] Permissions enforced (manager+admin, both scopes)
- [ ] Audit/timeline logged: Yes (both source and survivor)
- [ ] Tests pass (transaction atomicity, idempotency)
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
