# Knowledge-Graph Reconciliation - F0037-hierarchy-aware-access-scoping-and-distribution-rollups run 2026-07-06-76799554

> Required at gate `G7` per Section 10. Authored by the Architect after signoff (`G5`) and candidate validation (`G6`), before PM closeout (`G8`).

## Scope

- Feature ID: F0037
- Run ID: 2026-07-06-76799554
- Date: 2026-07-06
- Reconciled by: Architect (feature-action)

## Binding Delta

Baseline = the F0037 feature assembly plan Knowledge-Graph Binding Plan. The as-built source required additional stable code bindings for the implementation files that landed during feature action.

| Capability / node | code-index binding (glob) | G0-declared? | Action |
|-------------------|---------------------------|--------------|--------|
| capability:distribution-rollup-reporting | `engine/src/Nebula.Application/Services/DistributionScopeService.cs` | no | added |
| capability:distribution-rollup-reporting | `engine/src/Nebula.Infrastructure/Repositories/DistributionScopeRepository.cs` | no | added |
| capability:distribution-rollup-reporting | `engine/src/Nebula.Api/Endpoints/DistributionEndpoints.cs` | no | added |
| capability:distribution-rollup-reporting | `engine/src/Nebula.Api/Endpoints/TerritoryEndpoints.cs` | no | added |
| capability:distribution-rollup-reporting | `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs` | yes | confirmed-existing-coverage |
| capability:distribution-rollup-reporting | `experience/src/features/reports/**` | yes | confirmed-existing-coverage |
| capability:distribution-rollup-reporting | `experience/src/features/search/**` | yes | added |
| endpoint:distribution-rollup-report | `engine/src/Nebula.Api/Endpoints/OperationalReportEndpoints.cs` | yes | added as-built backend binding |
| policy_rule:distribution-rollup-read | `engine/tests/Nebula.Tests/Unit/CasbinAuthorizationServiceTests.cs` | no | added |

## Canonical Nodes

No new canonical node IDs were introduced at G7. F0037 reuses the canonical Phase B nodes already present for:

- `capability:distribution-rollup-reporting`
- `endpoint:distribution-rollup-report`
- `policy_rule:distribution-rollup-read`
- `schema:distribution-rollup-report`

## Validator Results

| Check | Command | Result |
|-------|---------|--------|
| symbol regen + check | `python3 scripts/kg/validate.py --regenerate-symbols --check-symbols` | PASS (exit 0) with C# extractor warning; validator completed with Python/TypeScript symbol index |
| drift | `python3 scripts/kg/validate.py --check-drift` | PASS (exit 0) |

Known warning retained: low-confidence inferred edge (0.4) on `feature:F0028` in `feature:F0018.depends_on`.

`coverage-report.yaml` was not regenerated at this gate; path-sensitive coverage regeneration remains a G8 closeout step after archive/path decisions.

## Handoff to Closeout

The semantic graph is green for G7 and ready for PM closeout verification. If closeout finds a binding gap, it should route back to a G7 delta pass before G8 archive/latest-run mutation.
