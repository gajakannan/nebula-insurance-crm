# Authorization SAST Review - F0037

Result: PASS

## Command

`rg -n "AllowAnonymous|RequireAuthorization|HasPermissionAsync|PolicyDenied|distribution_rollup|operational_report|global_search|broker_insight|HasScope|ProjectionVisibility|SeeAll|ExternalUser|BrokerUser" <F0037 changed runtime/test/policy paths>`

## Findings

- Operational report endpoint group requires authorization and rate limiting.
- Distribution rollups require `distribution_rollup:read`.
- Workload and workflow-aging reports continue to require `operational_report:read`.
- Search results continue to require `global_search:read`.
- Distribution scope resolver fail-closes BrokerUser and ExternalUser with `HasScope=false`.
- Search, operational report, and broker insight repositories return no rows when `HasScope=false`.
- Visibility predicates are applied before aggregations, counts, drilldowns, facets, and rollup totals are created.
- Casbin tests assert `distribution_rollup:read` ALLOW/DENY parity.

## Follow-up

Security Reviewer must perform formal G4 review before signoff because F0037 changes access-control behavior.
