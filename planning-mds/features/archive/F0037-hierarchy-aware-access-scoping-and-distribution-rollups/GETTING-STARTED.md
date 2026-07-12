# F0037 - Hierarchy-Aware Access Scoping & Distribution Rollups - Getting Started

## Prerequisites

- [x] F0017 delivered and archived - provides broker/MGA hierarchy, effective-dated producer ownership, territory assignment, and audit.
- [x] F0023 delivered and archived - provides global search, saved views, operational reporting, projection visibility, and report UI substrate.
- [x] F0008 delivered and archived - provides broker insight projection patterns and permission-safe analytics behavior.
- [x] G3 Phase A approval recorded for `2026-07-06-6e3851ab`.
- [ ] G5 Phase B architecture approval recorded before any feature implementation begins.

## Product Rules to Preserve

1. Resolve a current-user distribution scope from role, hierarchy, territory, producer ownership, source-record permissions, and `asOf`.
2. Apply scope before materializing rows, counts, facets, suggestions, drilldowns, and rollups.
3. Return no-leak not-found behavior for hidden direct records unless the request fails authentication or broad resource authorization first.
4. Keep BrokerUser and ExternalUser denied for F0037-owned internal search/report/rollup visibility unless a later approved gate changes scope.
5. Reuse F0023 projections and reporting/search UI patterns; do not introduce a separate reporting substrate in MVP.

## Key Files to Review Before Phase B

- `planning-mds/features/archive/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md`
- `planning-mds/features/archive/F0023-global-search-saved-views-and-operational-reporting/PRD.md`
- `planning-mds/features/archive/F0008-broker-insights/PRD.md`
- `planning-mds/architecture/authorization-matrix.md`
- `planning-mds/architecture/policy.csv`
- `planning-mds/knowledge-graph/feature-mappings.yaml`
- `planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups/feature-assembly-plan.md`
- `planning-mds/api/nebula-api.yaml`
- `planning-mds/schemas/distribution-rollup-report.schema.json`

## Validation Commands

Run these from the product root unless noted:

```bash
.venv/bin/python ../nebula-agents/agents/product-manager/scripts/validate-stories.py planning-mds/features/F0037-hierarchy-aware-access-scoping-and-distribution-rollups
.venv/bin/python ../nebula-agents/agents/product-manager/scripts/generate-story-index.py planning-mds/features/
.venv/bin/python ../nebula-agents/agents/product-manager/scripts/validate-trackers.py --product-root . --feature F0037
.venv/bin/python scripts/kg/validate.py --write-coverage-report
.venv/bin/python scripts/kg/validate.py
.venv/bin/python scripts/kg/validate.py --check-drift
```

Run template validation from `nebula-agents`:

```bash
.venv/bin/python agents/scripts/validate_templates.py
```

## Phase B Handoff

- New endpoint contract: `GET /operational-reports/distribution-rollups`.
- New policy gate: `distribution_rollup:read`.
- Query-time MVP: no materialized rollup jobs or new runtime infrastructure are approved.
- Feature action must create feature evidence only after G5 approval.
