# F0002-S0009: Adopt Native Casbin Enforcer (Replace Hand-Rolled Policy Evaluator)

**Story ID:** F0002-S0009  
**Feature:** F0002 — Broker & MGA Relationship Management  
**Title:** Replace custom authorization parser with native Casbin enforcer  
**Priority:** Critical  
**Phase:** MVP Hardening

## User Story

**As a** Platform Security Engineer  
**I want** authorization decisions to be evaluated by native Casbin runtime  
**So that** policy behavior is correct, maintainable, and aligned with the declared ABAC model

## Context & Background

Current API authorization uses a hand-rolled `PolicyAuthorizationService` that parses `policy.csv`
and evaluates a very small set of condition expressions. This creates policy drift risk and blocks
safe extension of ABAC conditions.

F0002 now depends on broad permission checks across broker/contact/timeline endpoints. To harden these
flows, enforcement must use actual Casbin semantics rather than a custom evaluator.

## Acceptance Criteria

- **Given** `model.conf` and `policy.csv` are valid
- **When** the API starts
- **Then** a native Casbin enforcer is initialized and used by `IAuthorizationService`

- **Given** an endpoint calls `IAuthorizationService.AuthorizeAsync(...)`
- **When** authorization is evaluated
- **Then** decision logic uses Casbin `Enforce` with model/policy semantics (not custom parser logic)

- **Given** the role/action matrix in `planning-mds/security/policies/policy.csv`
- **When** broker/contact/timeline endpoints are tested
- **Then** allow/deny results match policy definitions for supported roles

- **Given** condition-based rules (for example, task ownership)
- **When** resource attributes are provided
- **Then** Casbin condition evaluation behaves as expected and is covered by tests

- **Given** policy/model files are missing or invalid
- **When** the service initializes
- **Then** startup fails deterministically with actionable error output

- Edge case: unknown role or unknown action → deny by default with 403 at endpoint layer

## Data Requirements

**Policy Inputs:**
- `planning-mds/security/policies/model.conf`
- `planning-mds/security/policies/policy.csv`

**Runtime Inputs:**
- Subject role(s)
- Object/resource type
- Action
- Optional resource attributes map (for condition expressions)

## Role-Based Visibility

No new roles are introduced. Existing role semantics in `policy.csv` remain unchanged.

## Non-Functional Expectations

- Security: deny-by-default behavior is preserved
- Reliability: policy loading and evaluation are deterministic across environments
- Maintainability: no hand-maintained parser/evaluator branches for new condition syntax
- Performance: authorization checks remain within existing endpoint latency budgets

## Dependencies

**Depends On:**
- ADR-008 (`planning-mds/architecture/decisions/ADR-008-casbin-enforcer-adoption.md`)
- Existing policy artifacts (`model.conf`, `policy.csv`)
- Existing endpoint integration tests

**Related Stories:**
- F0002-S0001 through F0002-S0008 (all authorization-gated broker/contact/timeline paths)
- F0009-S0004 (BrokerUser scope isolation remains query-layer behavior)

## Out of Scope

- Rewriting policy matrix definitions
- Role redesign
- UI permission rendering changes

## Definition of Done

- [ ] Native Casbin-backed `IAuthorizationService` implemented and wired in DI
- [ ] Hand-rolled policy parser/evaluator removed or fully deprecated (no runtime use)
- [ ] Broker/contact/timeline authorization tests pass against policy matrix
- [ ] Condition-based authorization tests pass for supported expressions
- [ ] Startup failure behavior validated for invalid policy/model inputs
- [ ] F0002 docs/status updated with implementation evidence

