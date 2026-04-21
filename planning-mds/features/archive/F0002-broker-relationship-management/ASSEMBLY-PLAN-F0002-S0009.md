# F0002-S0009 Assembly Plan — Native Casbin Enforcer Adoption

**Story:** F0002-S0009  
**Feature:** F0002 — Broker & MGA Relationship Management  
**Related ADR:** `planning-mds/architecture/decisions/ADR-008-casbin-enforcer-adoption.md`  
**Status:** Planned  
**Last Updated:** 2026-03-08

## Objective

Replace the runtime hand-rolled authorization parser/evaluator with native Casbin enforcement while preserving existing endpoint behavior for F0002/F0009 except where true Casbin semantics are intended.

## In Scope

- Backend authorization engine migration behind `IAuthorizationService`
- DI wiring and startup validation for policy/model loading
- F0002 endpoint authorization verification (broker/contact/timeline)
- Regression tests for condition-based policies (for example `assignee == subjectId`)
- Documentation/status updates for F0002-S0009

## Out of Scope

- Policy matrix redesign (`policy.csv` semantics change)
- Role model redesign
- Frontend role-visibility redesign
- Non-authorization feature refactors

## Execution Slices

1. **Slice A — Baseline and Safety Net**
   - Inventory current auth call sites (`AuthorizeAsync`) and role/action mappings.
   - Add/expand tests to lock current expected allow/deny outcomes for F0002 endpoints.
   - Capture startup behavior expectations for missing/invalid policy artifacts.

2. **Slice B — Casbin Enforcer Implementation**
   - Introduce Casbin-backed implementation of `IAuthorizationService`.
   - Load `model.conf` + `policy.csv` using deterministic runtime pathing.
   - Implement attribute-based enforcement contract for condition rules.
   - Keep interface stable unless a blocker is proven.

3. **Slice C — Runtime Switch**
   - Update DI bindings to use Casbin-backed service.
   - Ensure old parser/evaluator is not used at runtime.
   - Validate startup failure behavior is actionable and deterministic.

4. **Slice D — Verification and Hardening**
   - Run integration tests for broker/contact/timeline allow/deny.
   - Run task condition tests (`assignee == subjectId`) to verify parity.
   - Confirm BrokerUser scope-isolation path remains query-layer enforced.

5. **Slice E — Documentation + Status**
   - Update F0002 `STATUS.md` and story notes with implementation evidence.
   - Record any residual risks/follow-ups.

## Gate Checklist

- **Gate 1 (Design Ready):**
  - Casbin implementation approach reviewed.
  - Attribute hydration contract documented.

- **Gate 2 (Code Ready):**
  - Casbin service implemented.
  - DI switch complete.
  - Legacy parser/evaluator removed from runtime path.

- **Gate 3 (Validation Ready):**
  - F0002 allow/deny tests passing.
  - Condition-based tests passing.
  - Startup failure tests passing.

- **Gate 4 (Docs Ready):**
  - F0002-S0009 status updated.
  - ADR references and evidence complete.

## Test Plan

- **Unit tests:**
  - Casbin adapter/service behavior for allow/deny and attribute conditions.
  - Policy/model loading failure behavior.

- **Integration tests:**
  - Broker endpoint matrix: create/search/read/update/delete/reactivate.
  - Contact endpoint matrix: read/create/update/delete.
  - Timeline endpoint read authorization.
  - Task ownership condition parity (where currently used).

- **Regression checks:**
  - Existing F0002 integration suites.
  - Existing F0009 BrokerUser access boundary suites.

## Evidence to Capture

- Test run logs with passing outcomes.
- Diff references showing DI switch and runtime enforcement path.
- Any updated policy-loading diagnostics.
- Updated F0002 `STATUS.md` entries for S0009 completion.

## Rollback Plan

- Keep migration in a single reversible commit set.
- If critical regression appears:
  1. Revert DI binding to previous implementation.
  2. Retain failing test evidence and incident notes.
  3. Re-open S0009 with narrowed remediation scope.

## Risks and Mitigations

- **Risk:** Casbin semantics differ from custom evaluator in edge conditions.  
  **Mitigation:** Pre-switch matrix tests and condition tests.

- **Risk:** Runtime path resolution for model/policy fails in certain environments.  
  **Mitigation:** deterministic path strategy + startup failure tests.

- **Risk:** Hidden dependency on custom evaluator behavior.  
  **Mitigation:** remove runtime references and validate all known call sites.

