# Test Plan - F0037

Result: PASS

## Scope

The G2 test plan covers hierarchy-aware projection visibility, distribution rollup aggregation, policy parity, frontend rollup rendering, buildability, and harness tracker/KG validation.

## Backend Tests

- Operational report service tests cover scoped-role visibility, distribution rollup aggregation, drilldown URL generation, and external-role fail-closed behavior.
- Search service tests cover admin full scope and scoped-role region visibility under the new distribution scope service.
- Broker insight tests cover the new scope-service dependency while preserving visibility-filtered aggregation behavior.
- Casbin policy tests cover `distribution_rollup:read` ALLOW/DENY parity for internal leader roles, DistributionUser/Underwriter denial, BrokerUser denial, and ExternalUser denial.

## Frontend Tests

- Distribution rollup component test covers authorized rollup metrics, drilldown link rendering, hidden-record exclusion copy, and scoped-away empty state.
- Production build covers TypeScript route/page/query integration for report/search filters and saved-view reapplication.

## Harness Validation

- Story validation for all F0037 stories.
- Story index regeneration.
- Tracker validation after G2 artifacts.
- KG drift validation.
- G2 evidence validation.

## Deferred Tests

- Authenticated DAST against a rebuilt live API container is deferred to post-G3/G4 operator validation.
- Full dependency vulnerability audit is waived in G2 due restricted registry/network access.
