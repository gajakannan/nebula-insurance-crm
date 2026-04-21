# ADR-008: Adopt Native Casbin Enforcer for Authorization

**Status:** Accepted
**Date:** 2026-03-08
**Accepted:** 2026-03-09
**Owners:** Platform / API Team
**Related Features:** F0002, F0009

## Context

The current authorization implementation in `Nebula.Infrastructure.Authorization.PolicyAuthorizationService`
uses a hand-rolled parser and evaluator over `policy.csv`. This approach currently supports only:

- exact role/resource/action matching
- `true` conditions
- one hardcoded expression: `r.obj.assignee == r.sub.id`

This diverges from the project direction to use Casbin ABAC as the policy engine of record.
It also creates a reliability and security risk:

- policy syntax and semantics can drift from Casbin behavior
- new policy conditions require manual parser code changes
- authorization correctness depends on custom code that is difficult to validate against Casbin docs/tools

## Decision

Adopt the official Casbin enforcer runtime in the backend and retire the hand-rolled policy parser/evaluator.

The API authorization layer will:

- load model from `planning-mds/security/policies/model.conf`
- load policies from `planning-mds/security/policies/policy.csv` (or embedded equivalent)
- evaluate all access decisions through Casbin `Enforce(...)`
- keep existing endpoint-level permission checks (`broker:create`, `contact:update`, `timeline_event:read`, etc.)
- preserve existing BrokerUser scope isolation flow from F0009 as a separate query-layer concern

## Scope

This ADR applies to authorization enforcement in API paths for:

- brokers
- contacts
- timeline events
- existing dashboard/task flows that already call `IAuthorizationService`

## Non-Goals

- redesigning the authorization matrix
- changing role semantics in `policy.csv`
- replacing query-layer scope filters with policy-only enforcement

## Implementation Notes

- Introduce Casbin-backed implementation of `IAuthorizationService`.
- Keep interface contract stable to minimize endpoint churn.
- Add attribute hydration rules for conditions that require resource context.
- Add deterministic startup validation for policy/model loading.
- Add integration tests for allowed/denied matrices and selected condition rules.

## Consequences

### Positive

- Authorization behavior matches true Casbin semantics.
- New policy conditions can be added without custom parser logic.
- Reduced security risk from parser/evaluator defects.

### Negative

- Adds external dependency/runtime complexity.
- Requires migration and test hardening to avoid regressions.

## Migration Plan

1. Add Casbin enforcer implementation behind `IAuthorizationService`.
2. Run side-by-side verification tests for key endpoint permissions.
3. Switch DI binding from hand-rolled service to Casbin-backed service.
4. Remove deprecated parser/evaluator code.
5. Update F0002 status/story artifacts and release notes.

## Validation

- Integration tests verify endpoint permission matrix against `policy.csv`.
- Negative tests verify denied actions for each major role.
- Condition-based tests verify `assignee == subjectId` behavior remains correct.
- Smoke tests confirm no auth regressions in dashboard/broker/contact flows.

