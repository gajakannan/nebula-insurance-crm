---
template: user-story
version: 1.1
applies_to: product-manager
---

# F0006-S0005: Submission Completeness Evaluation

**Story ID:** F0006-S0005
**Feature:** F0006 — Submission Intake Workflow
**Title:** Submission completeness evaluation
**Priority:** High
**Phase:** CRM Release MVP

## User Story

**As a** distribution user
**I want** to see a clear completeness checklist for a submission showing which required fields are populated and which required document categories are linked
**So that** I know exactly what is missing before I advance the submission to underwriting review

## Context & Background

Completeness evaluation serves two purposes: (1) a read-side projection displayed on the submission detail view showing the current pass/fail state of each requirement, and (2) a transition guard that blocks advancement to ReadyForUWReview when requirements are not met. This prevents underwriters from receiving incomplete submissions that require follow-up, which is the primary pain point in the current email-based workflow.

The completeness policy evaluates two categories:
- **Field completeness:** Required fields on the submission entity itself (AccountId, BrokerId, EffectiveDate, LineOfBusiness, AssignedToUserId).
- **Document completeness:** Required document categories linked to the submission via F0020 (Application, at least one supporting document). This is a soft dependency — when F0020 is not available, document checks are skipped and the completeness panel shows a placeholder.

## Acceptance Criteria

**Happy Path — All Complete:**
- **Given** a submission with all required fields populated and required document categories linked
- **When** the completeness evaluation is requested (via detail view or transition guard)
- **Then** the result shows all items as passed; overall completeness is `true`

**Happy Path — Field Missing:**
- **Given** a submission where LineOfBusiness is null
- **When** the completeness evaluation is requested
- **Then** the result shows LineOfBusiness as `missing`; overall completeness is `false`; the specific missing field is identified

**Happy Path — Document Category Missing:**
- **Given** a submission with all required fields but no Application document linked (F0020 available)
- **When** the completeness evaluation is requested
- **Then** the result shows Application category as `missing`; overall completeness is `false`

**Happy Path — F0020 Not Available:**
- **Given** F0020 document management is not yet deployed
- **When** the completeness evaluation is requested
- **Then** document completeness checks are skipped; overall completeness is based on field completeness only; the completeness panel shows "Document management not yet configured" in the document section

**Transition Guard:**
- **Given** a submission in Triaging state with overall completeness = `false`
- **When** the user attempts to transition to ReadyForUWReview
- **Then** the transition is blocked with HTTP 409, `code=missing_transition_prerequisite`, and the response body lists each missing item

**Alternative Flows / Edge Cases:**
- Submission has all fields but AssignedToUserId does not reference a user with Underwriter role → completeness includes "Assigned underwriter required" as missing
- Required field was previously populated then cleared (update set to null) → completeness re-evaluates; field shows as missing
- Multiple items missing → all missing items listed in a single structured response

**Checklist:**
- [ ] Completeness evaluation returns structured result with field-level and document-level pass/fail status
- [ ] Required fields checked: AccountId, BrokerId, EffectiveDate, LineOfBusiness, AssignedToUserId (must be Underwriter role)
- [ ] Required document categories checked (when F0020 available): Application, at least one supporting document (loss runs, financials, or supplemental)
- [ ] Overall completeness is `true` only when all field and document requirements pass
- [ ] Completeness evaluation is a read-side projection (does not mutate data)
- [ ] Completeness result displayed on submission detail view (F0006-S0003 completeness panel)
- [ ] Completeness enforced as transition guard for Triaging→ReadyForUWReview and WaitingOnBroker→ReadyForUWReview
- [ ] When F0020 is not available, document checks are soft-skipped with a clear indicator
- [ ] Transition rejection includes structured list of all missing items (not just the first one)

## Data Requirements

**Required Fields (completeness result):**
- `isComplete` (boolean): Overall pass/fail
- `fieldChecks` (array): List of `{ field, required, status: "pass" | "missing" }`
- `documentChecks` (array): List of `{ category, required, status: "pass" | "missing" | "unavailable" }`
- `missingItems` (array of strings): Human-readable list of what is missing (used in transition guard error messages)

**Validation Rules:**
- Completeness is evaluated in real-time (not cached) to ensure accuracy
- AssignedToUserId check verifies the referenced user has Underwriter role, not just that the field is non-null

## Role-Based Visibility

**Roles that can view completeness:**
- All roles that can read the submission (completeness is a read-side projection on the detail view)

**Data Visibility:**
- Completeness data is internal-only in MVP
- No separate permission required beyond submission:read

## Non-Functional Expectations

- Performance: Completeness evaluation completes in < 200ms (it is called on every detail view render and on transition attempts)
- Security: Completeness evaluation does not expose document metadata beyond category presence/absence
- Reliability: Graceful degradation when F0020 is unavailable; field completeness always works

## Dependencies

**Depends On:**
- F0006-S0002 — Submissions must exist with field data
- F0020 (soft dependency) — Document metadata for category-level completeness checks

**Related Stories:**
- F0006-S0003 — Completeness panel on detail view
- F0006-S0004 — Completeness as transition guard for ReadyForUWReview

## Business Rules

1. **Completeness Is a Read-Side Projection:** Completeness evaluation does not mutate data. It is computed fresh on each request against current submission and document state. There is no cached or stored completeness field.
2. **Underwriter Role Validation:** The AssignedToUserId completeness check verifies that the referenced user has the Underwriter role, not merely that the field is non-null. A submission assigned to a DistributionUser will fail the underwriter check.
3. **Document Completeness Soft Dependency (F0020):** When F0020 document management is not deployed, document category checks are soft-skipped. Field completeness remains fully enforced. The completeness panel shows "Document management not yet configured" for the document section. This ensures F0006 can ship independently of F0020.
4. **Uniform Required Fields Across LOBs:** All lines of business share the same required field set in MVP (AccountId, BrokerId, EffectiveDate, LineOfBusiness, AssignedToUserId with Underwriter role). Per-LOB completeness customization is Future scope.
5. **Structured Missing-Item Response:** When completeness fails as a transition guard, the HTTP 409 response body lists every missing item — not just the first failure. This allows intake users to address all gaps in a single pass.

## Out of Scope

- Custom completeness rules per LOB (future — all LOBs share the same required fields in MVP)
- Completeness percentage or score (pass/fail only in MVP)
- Notifications when completeness changes (future)
- Document content validation (checking that an uploaded document actually contains the expected data)

## UI/UX Notes

- Screens involved: Submission Detail — Completeness Panel
- Key interactions: Collapsible checklist on the detail view; green check for passed items, red indicator for missing items; "unavailable" label for document checks when F0020 is not deployed
- Completeness panel updates when the page is refreshed or after a field edit
- Missing items shown as actionable hints (e.g., "Add Line of Business" links to edit form)

## Questions & Assumptions

**Open Questions:**
- None

**Assumptions (to be validated):**
- All LOBs share the same required field set in MVP; per-LOB completeness rules are Future scope
- "At least one supporting document" means any document in category: Loss Runs, Financials, or Supplemental
- Completeness is evaluated fresh on each request (not cached or stored as a field)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced (inherits submission:read scope)
- [ ] Audit/timeline logged (not applicable — read-only evaluation)
- [ ] Tests pass
- [ ] Documentation updated (if needed)
- [ ] Story filename matches `Story ID` prefix (`F0006-S0005-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
