# Implementation Validation Report - run 2026-07-07-773e619c

> Produced by `agents/actions/validate.md`. Script-driven validator output. Lives under `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-07-07-773e619c/`. Not inside any feature evidence package.

## Run Identity

- Run ID: `2026-07-07-773e619c`
- Date: 2026-07-07
- Reviewer: Implementation Validator / Orchestrator

## Validator Invocations

| Validator | Command | Exit Code | Output |
|-----------|---------|-----------|--------|
| knowledge_graph_sync | `python3 scripts/kg/validate.py` | 1 | `lifecycle-gates.log` |
| solution_contract | `python3 planning-mds/testing/validate-nebula-api-contract.py planning-mds/api/nebula-api.yaml` | 0 | `lifecycle-gates.log` |
| frontend_quality | `python3 planning-mds/testing/validate-frontend-quality-gate.py planning-mds/operations/evidence/frontend-quality/latest-run.json` | 1 | `lifecycle-gates.log` |
| validate-trackers | `python3 agents/product-manager/scripts/validate-trackers.py --product-root /Users/msig4/Documents/NEBULA/nebula-insurance-crm` | 0 | `commands.log` |
| validate-stories | `python3 agents/product-manager/scripts/validate-stories.py --product-root /Users/msig4/Documents/NEBULA/nebula-insurance-crm` | 1 | `commands.log` |
| validate-architecture | `python3 agents/architect/scripts/validate-architecture.py /Users/msig4/Documents/NEBULA/nebula-insurance-crm/planning-mds/BLUEPRINT.md` | 0 | `commands.log` |
| validate-api-contract | `python3 agents/architect/scripts/validate-api-contract.py /Users/msig4/Documents/NEBULA/nebula-insurance-crm/planning-mds/api/nebula-api.yaml` | 0 | `commands.log` |
| validate-templates | `python3 agents/scripts/validate_templates.py` | 0 | `commands.log` |
| validate-agent-map | `python3 agents/scripts/validate_agent_map.py` | 0 | `commands.log` |

## Findings By Rule ID

| Rule ID | Severity | Count | File |
|---------|----------|------:|------|
| `kg_coverage_report_stale` | high | 1 | `planning-mds/knowledge-graph/coverage-report.yaml` |
| `frontend_quality_visual_artifact_missing` | high | 1 | `planning-mds/operations/evidence/f0015/artifacts/playwright-report/index.html` |
| `repo_wide_story_template_failures` | medium | 1 | `planning-mds/features/archive/**` |
| `api_contract_warnings` | medium | 67 | `planning-mds/api/nebula-api.yaml` |

## Recommendations

- Refresh KG coverage and rerun the product lifecycle gates.
- Restore or regenerate the frontend visual report artifact for the global frontend quality lane.
- Decide whether archived legacy stories should be migrated or excluded from current-template story validation.

## Result

`FAIL`
