# Implementation Validation Report — run 2026-07-03-88c2e668

> Produced by `agents/actions/validate.md`. Script-driven validator output. Lives under `{PRODUCT_ROOT}/planning-mds/operations/evidence/runs/2026-07-03-88c2e668/` (§14). Not inside any feature evidence package.

## Run Identity

- Run ID: 2026-07-03-88c2e668
- Date: 2026-07-03
- Reviewer: Implementation Validator / Orchestrator

## Validator Invocations

| Validator | Command | Exit Code | Output |
|-----------|---------|-----------|--------|
| validate-feature-evidence | `.venv/bin/python agents/product-manager/scripts/validate-feature-evidence.py --product-root /Users/wallstreet289/Documents/workspace/nebula-insurance-crm-sagar --feature F0028 --run-id 2026-07-02-736e7854 --stage G8 --json-out .../artifacts/feature-evidence.json` | 0 | `artifacts/feature-evidence.json` |
| validate-trackers | `.venv/bin/python agents/product-manager/scripts/validate-trackers.py --product-root /Users/wallstreet289/Documents/workspace/nebula-insurance-crm-sagar --feature F0028 --run-id 2026-07-02-736e7854` | 0 | console evidence |
| validate-stories | `.venv/bin/python agents/product-manager/scripts/validate-stories.py .../F0028-carrier-and-market-relationship-management` | 0 | console evidence |
| validate-templates | `.venv/bin/python agents/scripts/validate_templates.py` | 0 | console evidence |
| kg validate | `.venv/bin/python scripts/kg/validate.py` | 0 | console evidence |
| kg drift | `.venv/bin/python scripts/kg/validate.py --check-drift` | 0 | console evidence |
| diff hygiene | `git diff --check` | 0 | console evidence |

## Findings By Rule ID

| Rule ID | Severity | Count | File |
|---------|----------|------:|------|
| `kg_unknown_symbol_warns` | warning | 1 class | `planning-mds/knowledge-graph/code-index.yaml` |
| `kg_low_confidence_inferred_edge_warns` | warning | 1 | `planning-mds/knowledge-graph/feature-mappings.yaml` |
| `nu1903_microsoft_openapi_advisory` | warning | 1 | `engine/src/Nebula.Api/Nebula.Api.csproj` |
| `vite_chunk_size_warning` | warning | 1 | `experience` build output |
| `node_module_register_deprecation` | warning | 1 | `experience` build/test output |

## Runtime And Test Evidence

- Docker Compose services are up: `db`, `authentik-server`, `authentik-worker`, `api`.
- `/healthz` returned `200 Healthy`.
- `/carrier-markets` returned expected unauthenticated `401 Unauthorized`.
- API build passed with known `NU1903` warning.
- Test project build passed with known `NU1903` warning.
- Focused backend tests passed: 111 passed, 0 failed.
- Frontend build passed with existing Vite warnings.
- Focused frontend route tests passed: 15 passed, 0 failed.

## Recommendations (when `WITH RECOMMENDATIONS`)

- [medium] Upgrade inherited `Microsoft.OpenApi 2.0.0` advisory outside F0028 — owner: Security Reviewer; follow-up: dependency maintenance backlog.
- [medium] Run full regression before broad release merge if release policy requires it — owner: Quality Engineer; follow-up: release validation checklist.

## Result

PASS WITH RECOMMENDATIONS.
