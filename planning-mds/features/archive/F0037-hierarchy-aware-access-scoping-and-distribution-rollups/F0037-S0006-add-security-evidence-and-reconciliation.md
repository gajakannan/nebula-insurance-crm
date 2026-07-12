# Add security evidence and reconciliation checks

**Story ID:** F0037-S0006
**Feature:** F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups
**Title:** Add security evidence and reconciliation checks
**Priority:** High
**Phase:** CRM Release MVP+

## User Story

**As a** security compliance owner
**I want** policy parity, access-control evidence, and rollup reconciliation checks for F0037
**So that** we can prove hierarchy-aware visibility is enforced consistently before the feature is closed.

## Context & Background

F0037 is security-sensitive because it changes read visibility and analytics aggregation. The feature needs explicit policy documentation, test evidence, review signoff, and reconciliation checks before G6/G7/G8 closeout in the future feature action.

## Acceptance Criteria

**Happy Path:**
- **Given** F0037 access scoping and rollups are implemented
- **When** validation and review evidence is collected
- **Then** authorization matrix, policy fixture, tests, reconciliation outputs, security review, code review, and QE signoff prove scoped behavior for rows, counts, facets, suggestions, drilldowns, direct detail, and rollups.

**Alternative Flows / Edge Cases:**
- BrokerUser and ExternalUser denial is documented and tested.
- Security review is mandatory and cannot be waived for G6.
- DevOps signoff is required if Phase B introduces materialized rollup jobs, background recomputation, runtime config, or deployment changes.
- Reconciliation failures block closeout until explained and accepted by the required reviewers.
- Coverage/drift validation must pass after ontology and tracker changes.
- Audit evidence records signoff provenance and confirms F0037 introduces no unsupported product-data mutation outside the approved stories.

**Checklist:**
- [ ] `authorization-matrix.md` includes F0037 resource/action names and role decisions.
- [ ] `policy.csv` includes F0037 policy rules or documents reuse of existing rules with F0037 deltas.
- [ ] Backend tests cover Admin full scope, manager subtree, producer/territory, sibling exclusion, external denial, direct no-leak access, and rollup reconciliation.
- [ ] Frontend tests cover filters, rollup panels, drilldowns, no-access/empty/stale/error states, saved-view reapplication, and accessibility.
- [ ] Security review, code review, and QE signoff are recorded in feature evidence during the future `feature` action.
- [ ] KG validation, drift check, and tracker validation pass after implementation.

## Interaction Contract (Required for Capture/Edit/Save/Update Stories)

N/A - process and evidence story; product behavior under review is covered by F0037-S0001 through F0037-S0005.

## Data Requirements

**Required Fields:**
- Resource/action names: F0037 policy identifiers.
- Role matrix: Allowed/denied roles and rationale.
- Test scenarios: Admin, manager subtree, producer/territory, sibling exclusion, external denial, direct hidden access, rollup reconciliation.
- Evidence links: Test logs, review decisions, signoff records, KG validation, tracker validation.

**Optional Fields:**
- DevOps evidence: Required only if Phase B introduces materialized jobs or deployability changes.
- Reconciliation fixture id: References stable test data used for rollup/source-row parity.

**Validation Rules:**
- Security review must be present for G6 in the future feature action.
- Rollup reconciliation evidence must compare totals against scoped source rows, not unfiltered source rows.
- External denial must be explicit in policy documentation and tests.

## Role-Based Visibility

**Roles that can approve F0037 closeout evidence:**
- Quality Engineer - Validates behavior and test evidence.
- Code Reviewer - Reviews implementation correctness.
- Security Reviewer - Reviews access-control enforcement and no-leak behavior.
- Architect - Confirms implementation matches approved architecture.
- DevOps - Conditional if runtime/deployment changes exist.

**Data Visibility:**
- InternalOnly content: Evidence, test fixtures, policy rationale, and review notes.
- ExternalVisible content: None.

## Non-Functional Expectations

- Security: Evidence must prove no-leak behavior across rows, counts, facets, suggestions, drilldowns, direct detail, and rollups.
- Reliability: Reconciliation checks must be repeatable in CI or documented validation commands.
- Auditability: Signoff provenance must identify reviewer, verdict, evidence, date, and notes.

## Dependencies

**Depends On:**
- F0037-S0001 - Scope resolver behavior to validate.
- F0037-S0002 - Direct/read no-leak behavior to validate.
- F0037-S0003 - Search/report/insight scoped behavior to validate.
- F0037-S0004 - Rollup reconciliation behavior to validate.
- F0037-S0005 - UI no-leak states to validate.

**Related Stories:**
- All F0037 stories.

## Business Rules

1. **Security signoff required:** Access-control enforcement cannot close without security review.
2. **Reconcile scoped totals:** Rollups are accepted only when totals match scoped source rows for the same filters and `asOf`.
3. **Evidence before archive:** Feature closeout must include tracker, KG, test, review, and PM evidence.

## Out of Scope

- Implementing the feature behavior itself; this story validates and documents it.
- External audit portal or customer-facing compliance reports.
- Broad policy refactors unrelated to F0037.

## UI/UX Notes

- Screens involved: N/A for end-user UI. Evidence may reference product screenshots or test artifacts during implementation.
- Key interactions: Reviewers inspect evidence and record signoff in the feature evidence package.

## Questions & Assumptions

**Open Questions:**
- [ ] Phase B must decide whether DevOps signoff is required based on the accepted rollup architecture.

**Assumptions (to be validated):**
- Existing feature evidence package conventions can record security, code review, QE, and conditional DevOps signoffs without a new evidence format.

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged (N/A - evidence story; signoff provenance is the audit record)
- [ ] Tests pass
- [ ] Documentation updated
- [ ] Story filename matches `Story ID` prefix (`F{NNNN}-S{NNNN}-...`)
- [ ] Story index regenerated if story file was added/renamed/moved
