# Code Quality Review Report - F0037

Result: PASS

Scope: G3 re-review after remediation of hierarchy-aware access scoping and distribution rollup blockers.
Date: 2026-07-06

## Summary

- Assessment: APPROVED FOR SECURITY REVIEW.
- Files reviewed: distribution scope service/repository/interface, direct-read endpoints, projection repositories, operational report rollups, search/report/broker insight services, policy artifacts, and focused tests.
- Prior G3 blockers: 4 blocking issues remediated.

## Remediation Review

1. Authoritative role scope resolution is now implemented before request filters are accepted.
   - `DistributionScopeService` resolves non-admin authority from repository-derived managed broker, region broker, producer, and territory context.
   - Requested `rootNodeId`, `territoryId`, and `producerUserId` filters must intersect with proven authority; outside-authority requests return `HasScope=false`.
   - `DistributionScopeServiceTests` cover sibling exclusion, outside-authority fail-closed behavior, and authorized territory/producer narrowing.

2. Direct hierarchy and territory reads now use no-leak F0037 scope checks.
   - `DistributionEndpoints.GetAncestors` and `ListDescendants` call `CanReadDistributionNodeAsync` and return not-found for hidden nodes.
   - `TerritoryEndpoints.ListMembers` and `GetAssignmentForMember` call territory/broker/producer scope checks before materializing direct-read responses.

3. Rollup `metricFamily` is now honored.
   - Workflow rollups continue to aggregate operational report projections.
   - Production and Activity rollups use broker insight projections filtered by the resolved visibility scope.
   - Focused tests verify Activity totals come from broker insight rows and hidden rows remain excluded.

4. Required risk-area tests were expanded.
   - Focused backend regression suite now includes 23 passing tests across distribution scope, operational rollups, search/report visibility plumbing, broker insights, and policy parity.
   - Existing frontend rollup component tests and production build remain valid from G2.

## Residual Notes

- Program-specific authority remains constrained by the currently available product model; no explicit user-to-program assignment source exists in the implementation. The resolver therefore avoids granting arbitrary program scope and stays fail-closed unless region/managed/producer/territory authority is proven.
- Endpoint-level no-leak checks are implemented for the direct-read surfaces named in the failed G3 review. Additional source-detail drilldown endpoint coverage should be expanded during G6/G7 hardening if new direct detail routes are introduced.
- Existing unrelated nullable warnings remain in legacy tests and dashboard repository code.

## Validation Reviewed

- API project build: PASS.
- Test project build: PASS, existing nullable warnings only.
- Focused F0037 backend regression test filter covering distribution scope, operational reports, search, broker insights, and policy parity: PASS, 23 passed.

## Verdict

PASS. The prior G3 code-quality blockers have been remediated sufficiently to proceed to security review/signoff under the nebula-agents feature lifecycle.
