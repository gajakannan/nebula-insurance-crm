# Security Review Report - F0037

Result: PASS

Scope: Security review for hierarchy-aware access scoping, no-leak direct reads, search/report/broker insight visibility, and distribution rollups.
Date: 2026-07-06

## Summary

- Assessment: APPROVED FOR NEXT GATE.
- Security-sensitive scope: true.
- Primary risk: access-control bypass or hidden-record leakage through counts, facets, direct reads, drilldowns, or rollups.

## Controls Reviewed

- External roles `BrokerUser` and `ExternalUser` fail closed in `DistributionScopeService`.
- Admin remains the only full-scope role in the F0037 resolver.
- Non-admin request filters are intersected with repository-derived authority before projection visibility is returned.
- Projection repositories apply `HasScope` and visibility predicates before materializing rows, counts, rollups, and drilldown source rows.
- Direct hierarchy and territory reads now call F0037 scope checks and return not-found behavior for hidden nodes/members.
- `distribution_rollup:read` policy parity is covered by unit tests and security matrix/policy artifacts.

## Security Evidence

- Secrets scan artifact: artifacts/security/secrets-scan.md
- SAST review artifact: artifacts/security/sast-scan.md
- Focused backend tests: 23 passed, including external denial, sibling exclusion, requested-scope fail-closed behavior, rollup visibility, broker insight visibility, search visibility plumbing, and policy parity.
- Frontend build and component tests passed at G2; no new frontend security-sensitive edits were made during the G3 remediation pass.

## Waivers

- Dependency vulnerability scan remains waived for this sandbox because external registry vulnerability checks are network-dependent.
- Authenticated DAST remains waived until an operator test environment is restarted on this feature branch with seeded credentials.

## Residual Risk

- Program-manager scope is fail-closed where no explicit user-to-program authority mapping exists. This is preferable to over-granting access and should be revisited if a user-program assignment model is added.
- Full browser/API DAST should be performed before production release, after G6 implementation stabilization.

## Verdict

PASS. F0037’s access-control changes are acceptable for the next harness gate with dependency audit and authenticated DAST deferred under recorded waivers.
