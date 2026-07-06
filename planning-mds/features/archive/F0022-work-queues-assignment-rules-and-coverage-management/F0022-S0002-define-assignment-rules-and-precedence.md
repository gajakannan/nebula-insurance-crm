# F0022-S0002: Define assignment rules and precedence

**Story ID:** F0022-S0002
**Feature:** F0022 — Work Queues, Assignment Rules & Coverage Management
**Title:** Define assignment rules and precedence
**Priority:** Critical
**Phase:** MVP

## User Story

**As a** Distribution Operations Manager
**I want** to define assignment rules with deterministic precedence
**So that** work is routed consistently and no-match outcomes are visible instead of random

## Context & Background

F0022 must establish durable rule records before F0032 later governs configuration publishing. Rule behavior must be transparent enough for managers, QA, and reviewers to explain each assignment.

## Acceptance Criteria

**Happy Path:**
- **Given** I am authorized to manage routing rules
- **When** I create an active assignment rule for a queue
- **Then** the rule stores work type, match criteria, target queue, optional target member strategy, priority order, and active status
- **And** the rule appears in deterministic evaluation order
- **And** rule creation is audited

**Alternative Flows / Edge Cases:**
- Multiple matching rules -> apply approved precedence: manual override, coverage/out-of-office, territory/ownership, workload balancing, fallback queue.
- No rule matches -> route to `Unassigned Operations Queue`.
- Invalid criteria -> reject save and leave current rules unchanged.
- Inactive rules -> never route work.

## Interaction Contract

| Surface / Entry Point | User Action | Editable State | Save / Mutation Result | Reload / Persistence Evidence | Roles / Status Constraints |
|-----------------------|-------------|----------------|-------------------------|-------------------------------|----------------------------|
| Work Queues -> Rules tab | Create or edit rule | Enabled for manager/admin | Rule record saved with precedence metadata | Reload shows ordered rule; test route preview reflects it | Manager/Admin only |
| Work Queues -> Rules tab | Deactivate rule | Enabled for manager/admin | Rule no longer participates in routing | Reload shows inactive state; new routing skips it | Manager/Admin only |

Required checks for mutation stories:
- [ ] Render-only behavior cannot satisfy the story.
- [ ] Save path has validation and error behavior.
- [ ] Successful mutation records audit/timeline evidence.
- [ ] Tests prove persisted state after reload/query invalidation.

## Data Requirements

**Required Fields:**
- Rule name, work type, target queue, active status, precedence category, match criteria

**Validation Rules:**
- Target queue must exist and be active before activation.
- Rule criteria must be explicit; free-form unvalidated expressions are out of scope.

## Role-Based Visibility

**Roles that can manage rules:**
- Distribution Operations Manager, Program Manager, Admin

**Data Visibility:**
- Rule lists and previews must not reveal unauthorized source records.

## Non-Functional Expectations

- Reliability: invalid rule saves must not alter active routing behavior.
- Observability: rule previews and routing outcomes must expose enough metadata to explain the selected rule.

## Questions & Assumptions

**Open Questions:**
- None blocking for Phase A approval.

**Assumptions (to be validated):**
- MVP rule criteria are constrained fields selected by the product, not arbitrary user-authored expressions.

## Dependencies

**Depends On:**
- F0022-S0001 — queues exist before rules target them.

## Business Rules

1. Precedence is explicit manual override -> coverage/out-of-office -> territory/ownership -> workload balancing -> fallback queue.
2. No-match routing always targets `Unassigned Operations Queue`.

## Out of Scope

- F0032 governed publish workflow
- AI/predictive rule suggestions

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Edge cases handled
- [ ] Permissions enforced
- [ ] Audit/timeline logged
- [ ] Tests pass
- [ ] Story filename matches `Story ID` prefix

## Review Provenance

Story-level signoff provenance is recorded in the parent feature `STATUS.md`.
