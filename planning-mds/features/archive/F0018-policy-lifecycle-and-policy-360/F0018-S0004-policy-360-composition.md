---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0018-S0004: Policy 360 Composed Workspace

**Story ID:** F0018-S0004
**Feature:** F0018 — Policy Lifecycle & Policy 360
**Title:** Policy 360 composed workspace (versions, endorsements, coverages, renewals, documents, activity)
**Priority:** Critical
**Phase:** CRM Release MVP

## User Story

**As a** underwriter, distribution user, distribution manager, or relationship manager
**I want** a single composed workspace that shows current terms, versions, endorsements, coverages, renewals, documents, and activity for a policy
**So that** I can make underwriting, servicing, and renewal decisions from one page without hopping between modules

## Context & Background

Policy 360 is the business value of F0018. It composes records owned by the Policy aggregate (versions, endorsements, coverages, timeline) with records owned by other modules (renewals via F0007, documents via F0020 when live). Each rail loads independently so initial paint is fast and degraded downstream modules don't take the page down.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated user with `policy:read` in scope
- **When** they open Policy 360 for any policy (Pending, Issued, Expired, Cancelled)
- **Then** the overview section renders ≤ 500 ms with: policy number, status badge, account reference (fallback-aware), LOB, carrier, effective/expiration dates, total current premium, version count, endorsement count, reinstatement deadline (when applicable), last activity date

- **Given** the overview has rendered
- **When** the user selects a rail (Versions, Endorsements, Coverages, Renewals, Documents, Activity)
- **Then** that rail calls its own paginated endpoint and renders independently

**Alternative Flows / Edge Cases:**
- Downstream list endpoint is slow or unavailable → rail shows a "Temporarily unavailable" state; other rails continue to render
- Policy on merged account → overview shows fallback-aware account reference (tombstone-forward to survivor) with a muted badge
- Policy on deleted account → overview renders with a fallback tombstone name + `[Deleted]` badge; rails still load
- `Cancelled` policy within reinstatement window → overview surfaces a "Reinstate (within N days)" action entry (actual reinstate workflow is S0008)
- `Expired` policy → overview is read-only; all rails remain browseable
- F0020 not yet live → Documents rail renders a "Documents module coming soon" placeholder
- Empty rail (no records) → rail shows an empty state with the relevant create-action when applicable (e.g., "Add Endorsement")

**Checklist:**
- [ ] Overview endpoint `GET /api/policies/{id}/summary` returns overview metrics + summary counts in one call
- [ ] Versions rail: `GET /api/policies/{id}/versions?page=&pageSize=`
- [ ] Endorsements rail: `GET /api/policies/{id}/endorsements?page=&pageSize=`
- [ ] Coverages rail (current): `GET /api/policies/{id}/coverages` (returns current-version coverages; historical coverages reachable via `versions/{versionId}/coverages`)
- [ ] Renewals rail: `GET /api/policies/{id}/renewals` (delegates to F0007 queries filtered by `PolicyId` or `PredecessorPolicyId` / `BoundPolicyId`)
- [ ] Documents rail: placeholder or delegated call to F0020 when live
- [ ] Activity rail: `GET /api/policies/{id}/timeline?page=&pageSize=` (owned by F0018-S0010)
- [ ] Each rail is independently paginable and cache-safe
- [ ] Overview surfaces reinstatement deadline countdown for Cancelled policies within window
- [ ] Account reference in overview honors F0016 fallback contract

## Data Requirements

**Summary payload fields:**
- `id`, `policyNumber`, `status`, `accountId`, `accountDisplayName`, `accountStatusAtRead`, `lineOfBusiness`, `carrierName`, `carrierRefId`, `effectiveDate`, `expirationDate`, `totalCurrentPremium`, `premiumCurrency`, `currentVersionNumber`, `versionCount`, `endorsementCount`, `reinstatementDeadline`, `lastActivityAt`, `rowVersion`

**Rail list-item fields:**
- Versions: id, versionNumber, versionReason, effectiveDate, createdAt, createdByUserName, endorsementId (nullable)
- Endorsements: id, endorsementNumber, endorsementReasonCode, endorsementReasonDetail, effectiveDate, resultingVersionNumber, createdAt, createdByUserName
- Coverages (current): id, coverageCode, coverageFormReference, limitPerOccurrence, limitAggregate, deductible, deductibleType, premium, notes
- Renewals: id, status, policyExpirationDate, assignedUser, urgencyBadge, boundPolicyId (nullable)
- Documents: id, fileName, fileType, uploadedAt, uploadedByUserName (via F0020 when live)
- Activity: id, eventType, summary, actorName, occurredAt

**Validation Rules:**
- All rail queries enforce ABAC (rails see only what the user can see even on a policy they can access at the overview level)

## Role-Based Visibility

- Overview: any role with `policy:read` in scope
- Rails: scoped per rail (renewals rail applies F0007's scope; documents rail applies F0020's scope when live)

**Data Visibility:** InternalOnly.

## Non-Functional Expectations

- Performance: overview p95 ≤ 500 ms; each rail page p95 ≤ 400 ms
- Reliability: rail isolation — one rail failure must not take down the page
- No N+1: all rails implemented as single paginated queries; overview uses denormalized summary columns when available

## Dependencies

**Depends On:**
- F0018-S0003 (detail page host)
- F0018-S0005 (versions rail), F0018-S0006 (endorsements rail), F0018-S0010 (activity rail), F0018-S0011 (summary projection)
- F0007 (renewals rail data)
- F0016-S0009 (fallback contract for account reference in overview)

**Related Stories:**
- F0020 (documents rail when live)

## Out of Scope

- Inline editing of versions / endorsements / coverages / renewals from the rails (users click through or use endorsement flow)
- Diff / redline across versions beyond side-by-side display (follow-up)
- Dashboard-grade visualizations inside Policy 360
- Policy-scoped reporting (deferred)

## UI/UX Notes

- Tabs or rails layout — design decision deferred to architecture + frontend
- Each rail has its own pagination and loading state
- Status-aware quick-action buttons pinned to overview: Pending → "Issue"; Issued → "Endorse", "Cancel"; Cancelled (within window) → "Reinstate"; Expired → (read-only)
- Reinstatement countdown surfaces as a prominent header chip when applicable

## Questions & Assumptions

**Assumptions:**
- Summary counts are query-time for MVP (materialized projection is a follow-up)
- Timeline rail reuses the existing `ActivityTimelineEvent` schema with `policyId` filter
- Version-compare UI (two-version side-by-side) lives within the Versions rail and is covered by F0018-S0005

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled (merged/deleted account, cancelled-within-window, expired, F0020 unavailable)
- [ ] Permissions enforced (ABAC per rail)
- [ ] Audit/timeline logged: No (read-only composition)
- [ ] Tests pass (including rail-isolation integration + fallback-account paths)
- [ ] Documentation updated (OpenAPI for summary + each rail)
- [ ] Story filename matches `Story ID` prefix
- [ ] Story index regenerated
