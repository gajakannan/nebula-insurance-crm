# Artifact Trace

Run ID: 2026-07-07-8a9b2629

## Plan Inputs

- `agents/ROUTER.md`
- `agents/agent-map.yaml`
- `agents/docs/AGENT-USE.md`
- `agents/actions/plan.md`
- `{PRODUCT_ROOT}/planning-mds/features/REGISTRY.md`
- `{PRODUCT_ROOT}/planning-mds/features/ROADMAP.md`
- `{PRODUCT_ROOT}/planning-mds/BLUEPRINT.md`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/solution-ontology.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/canonical-nodes.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/feature-mappings.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/code-index.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/coverage-report.yaml`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/**`

## Dependency Evidence Audit

- F0017 Broker/MGA Hierarchy, Producer Ownership & Territory Management: completed and archived; used as dependency for effective-dated producer ownership, hierarchy, and territory attribution.
- F0018 Policy Lifecycle & Policy 360: completed and archived; used as dependency for policy, premium, version, renewal, and lifecycle context.
- F0028 Carrier & Market Relationship Management: completed and archived; used as dependency for carrier/market context and schedule association.

## Phase A Outputs

- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/PRD.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/README.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/GETTING-STARTED.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/STATUS.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0001-commission-workspace-search-and-policy-context.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0002-commission-schedule-maintenance.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0003-producer-split-assignment.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0004-expected-commission-calculation-review.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0005-commission-adjustment-and-approval.md`
- `{PRODUCT_ROOT}/planning-mds/features/F0025-commission-producer-splits-and-revenue-tracking/F0025-S0006-revenue-attribution-rollups.md`
- `{PRODUCT_ROOT}/planning-mds/features/STORY-INDEX.md`
- `{PRODUCT_ROOT}/planning-mds/BLUEPRINT.md`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/feature-mappings.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/coverage-report.yaml`

## Validation Summary

- Story validation: PASS, all six F0025 stories with no warnings.
- Tracker validation: PASS, 0 errors and 0 warnings, using `--skip-feature-evidence`.
- KG validation: PASS after coverage refresh; one existing low-confidence inferred-edge warning remains outside F0025.
- Template validation: PASS with framework venv.

## Phase B Outputs

- `{PRODUCT_ROOT}/planning-mds/architecture/decisions/ADR-032-commission-producer-splits-and-revenue-tracking.md`
- `{PRODUCT_ROOT}/planning-mds/architecture/data-model.md`
- `{PRODUCT_ROOT}/planning-mds/api/nebula-api.yaml`
- `{PRODUCT_ROOT}/planning-mds/security/authorization-matrix.md`
- `{PRODUCT_ROOT}/planning-mds/security/policies/policy.csv`
- `{PRODUCT_ROOT}/planning-mds/schemas/commission-schedule.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/commission-schedule-upsert-request.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/producer-split-assignment.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/producer-split-assignment-upsert-request.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/expected-commission.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/commission-adjustment.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/commission-adjustment-request.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/commission-adjustment-decision-request.schema.json`
- `{PRODUCT_ROOT}/planning-mds/schemas/revenue-attribution-rollup-response.schema.json`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/canonical-nodes.yaml`
- `{PRODUCT_ROOT}/planning-mds/knowledge-graph/feature-mappings.yaml`

## Phase B Validation Summary

- Architecture validation: PASS.
- API contract validation: PASS with pre-existing non-F0025 warnings.
- Story validation: PASS, all six F0025 stories with no warnings.
- Tracker validation: PASS, 0 errors and 0 warnings.
- KG integrity and drift validation: PASS after coverage refresh; one existing low-confidence inferred-edge warning remains outside F0025.
