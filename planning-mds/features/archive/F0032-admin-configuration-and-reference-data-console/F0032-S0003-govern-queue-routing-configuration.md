---
template: user-story
version: 1.2
applies_to: product-manager
---

# F0032-S0003: Govern queue and routing configuration drafts

**Story ID:** F0032-S0003
**Feature:** F0032 — Admin Configuration & Reference Data Console
**Title:** Govern queue and routing configuration drafts
**Priority:** High
**Phase:** MVP

## User Story

**As a** platform Admin
**I want** to draft governed queue and routing configuration changes over the F0022 model
**So that** queue/routing behavior can be prepared centrally without replacing the existing routing engine

## Context & Background

F0022 owns the durable OperationsRouting model and local manager controls. F0032 adds centralized draft governance over that model while preserving F0022 rule precedence, fallback queue semantics, coverage-window activation, and routing audit expectations.

## Acceptance Criteria

**Happy Path:**
- **Given** F0022 queue/routing configuration exists
- **When** I open the Queue/Routing domain and create a draft change
- **Then** I can draft supported changes to queue metadata, assignment-rule status/order, coverage-window metadata, and fallback-queue visibility
- **And** the draft references the existing F0022 queue/routing records instead of creating a replacement model

**Alternative Flows / Edge Cases:**
- If a draft would remove the required `Unassigned Operations Queue`, validation marks the draft as blocking before publish.
- If a draft would violate F0022 rule precedence categories, validation marks the draft as blocking before publish.
- If a queue/rule has changed since the draft was opened, the user sees a stale-draft warning before saving or validating.
- If the user is an Operations Manager without Admin rights, they can review the draft but cannot publish it.

**Checklist:**
- [ ] Draft queue/routing changes preserve F0022 rule precedence order.
- [ ] Draft changes preserve explicit coverage-window activation; inactivity alone cannot become coverage.
- [ ] Draft changes preserve fallback queue behavior.
- [ ] Draft changes include a reason.
- [ ] Draft saves create audit history and do not write queue work-item timeline events before publish.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Admin Configuration Catalog -> Queue/Routing domain | Create Draft / Edit Draft / Save Draft | Enabled for Admin; review-only for Operations Manager | Draft queue/routing configuration version is created or updated over existing F0022 records | Reloading the Queue/Routing domain shows the same draft version, changed items, actor, and reason | Admin only for draft mutation unless delegated; published F0022 runtime remains unchanged |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story unless the story is explicitly read-only.
- [ ] The save path has validation and error behavior specified.
- [ ] A successful mutation has an audit/timeline/event expectation or an explicit N/A reason.
- [ ] Tests prove the user can perform the action from the named entry point and observe persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Queue/routing domain key.
- Referenced F0022 queue/rule/coverage identifiers.
- Draft version.
- Changed fields and prior published values.
- Change reason.

**Optional Fields:**
- Stale-source warning summary.
- Downstream impact note.

**Validation Rules:**
- Draft must reference existing F0022 routing records.
- Draft must not remove the fallback queue.
- Draft must preserve F0022 rule precedence categories.

## Role-Based Visibility

**Roles that can draft:**
- Admin — can draft and later publish queue/routing configuration.
- Operations Manager — can review queue/routing drafts when authorized but cannot publish through this story.

**Data Visibility:**
- InternalOnly content: queue/routing configuration drafts and validation details.
- ExternalVisible content: none.

## Non-Functional Expectations

- Security: draft reads and writes are permission-filtered.
- Reliability: stale draft detection prevents accidental overwrite of newer queue/routing changes.

## Audit & Timeline Requirements

- Log a queue/routing configuration draft event with referenced F0022 records, actor, timestamp, draft version, changed-field summary, and change reason.
- Do not write queue work-item timelines for draft edits because draft configuration has no runtime routing effect until publish.

## Dependencies

**Depends On:**
- F0022 Work Queues, Assignment Rules & Coverage Management.
- ADR-013 Operational Routing and Queue Engine.

**Related Stories:**
- F0032-S0004 — validates and compares queue/routing drafts.
- F0032-S0005 — publishes or rolls back validated sets.
- F0032-S0006 — audits routing configuration changes.

## Business Rules

1. F0032 governs F0022 configuration; it does not replace F0022 execution semantics.
2. No-match work remains assigned to the required fallback queue, never randomly assigned.
3. Coverage is activated only by explicit coverage windows.

## Out of Scope

- New routing algorithm design.
- Queue worklist implementation.
- Reassignment and rebalance operations owned by F0022.

## UI/UX Notes

- Screens involved: Configuration Domain Detail, Configuration Draft Editor, Validation and Compare Drawer.
- Key interactions: draft queue/routing changes, save draft, review stale-source warnings.

## Questions & Assumptions

**Open Questions:**
- [ ] None.

**Assumptions (to be validated):**
- First-release queue/routing governance uses the F0022 data model and does not require new queue/routing business concepts.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0032-S0003-...`)
- [ ] Story index regenerated if story file was added/renamed/moved

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
