# KG Reconciliation - F0008 Broker Insights

Result: PASS

## Binding Delta

F0008 implementation binds to the existing KG target `feature:F0008` and the planned surfaces:

- `entity:broker-insight-projection`
- `capability:broker-insights`
- `endpoint:broker-insight-scorecards`
- `endpoint:broker-insight-trends`
- `endpoint:broker-insight-benchmarks`
- `endpoint:broker-insight-snapshot`
- `policy_rule:broker-insight-read`

Runtime files added under `engine/src/**` and `experience/src/features/broker-insights/**` are consistent with ADR-029 and the F0008 feature assembly plan.

## Canonical Nodes

`python3 scripts/kg/lookup.py F0008 --tier 1 --run-id 2026-07-03-fd732693 --telemetry-file .kg-state/telemetry.jsonl` resolved F0008 with:

- Status: `architecture-complete`
- Governing ADRs: `adr:014-search`, `adr:026`, `adr:029`
- Dependencies: `feature:F0006`, `feature:F0007`, `feature:F0017`, `feature:F0019`, `feature:F0023`
- API contract: `api:nebula-rest`
- Schemas: broker insight scorecard, trend, benchmark, and snapshot schemas

## Validator Results

`python3 scripts/kg/validate.py --write-coverage-report` passed and refreshed `planning-mds/knowledge-graph/coverage-report.yaml`.

Known warnings remain in unrelated existing symbol references for renewal/test stub symbols and one low-confidence dependency edge outside F0008. No KG blocker was introduced by F0008.

## Handoff to Closeout

KG reconciliation is complete for the feature-action closeout. Remaining runtime follow-up is operational: rebuild/restart API before manual endpoint testing so the running container includes the F0008 route and migration.
