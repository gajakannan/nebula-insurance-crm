# Knowledge-Graph Reconciliation - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

> Required at gate `G7` per the feature evidence contract. Authored by the Architect after signoff (`G5`) and candidate validation (`G6`), before PM closeout (`G8`).

## Scope

- Feature ID: F0025
- Run ID: 2026-07-07-9859bad4
- Date: 2026-07-07
- Reconciled by: Architect (feature-action)

## Binding Delta

Baseline = the `feature-assembly-plan.md` "Knowledge-Graph Binding Plan" declared at G0.

| Capability / node | code-index binding (glob) | G0-declared? | Action |
|-------------------|---------------------------|--------------|--------|
| capability:commission-revenue-tracking | `engine/src/Nebula.Api/Endpoints/CommissionEndpoints.cs` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Application/DTOs/CommissionDtos.cs` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Application/Interfaces/ICommissionRepository.cs` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Application/Interfaces/IRevenueAttributionRepository.cs` | no; implementation-added companion port | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Application/Services/CommissionRevenueService.cs` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Infrastructure/Persistence/Configurations/CommissionConfiguration.cs` | yes; covered concrete file under planned `Commission*.cs` intent | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Infrastructure/Repositories/CommissionRepository.cs` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/src/Nebula.Infrastructure/Repositories/RevenueAttributionRepository.cs` | no; implementation-added read projection repository | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `engine/tests/Nebula.Tests/Unit/CommissionRevenue/**` | yes; test binding expected by G2/G3 evidence | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `experience/src/App.tsx` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `experience/src/features/commissions/**` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `experience/src/pages/CommissionsPage.tsx` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `experience/src/pages/CommissionDetailPage.tsx` | yes | confirmed-existing-coverage |
| capability:commission-revenue-tracking | `experience/src/features/commissions/tests/**` | yes; test binding expected by G2 evidence | confirmed-existing-coverage |

No new code-index edit was required at G7. `planning-mds/knowledge-graph/code-index.yaml` already binds the as-built F0025 code paths under `capability:commission-revenue-tracking`.

## Canonical Nodes

No new `canonical-nodes.yaml` nodes were introduced at G7. The F0025 semantic nodes already exist and were confirmed through `scripts/kg/lookup.py F0025`, including:

- `capability:commission-revenue-tracking`
- commission endpoint nodes
- commission entity nodes
- commission policy rules
- `workflow:commission-adjustment`
- F0025 schema nodes

## Validator Results

| Check | Command | Result |
|-------|---------|--------|
| symbol regen + check | `.venv/bin/python scripts/kg/validate.py --regenerate-symbols --check-symbols --regenerate-decisions --check-decisions` | PASS (exit 0) |
| drift | `.venv/bin/python scripts/kg/validate.py --check-drift` | PASS (exit 0) |

Notes:

- The regeneration command emitted local extractor warnings (`typescript extractor not available`; C# extractor warning), but the KG validator completed with exit 0 and reported `[PASS] knowledge-graph integrity checks passed`.
- Both checks retained the existing warning for a low-confidence inferred edge on `feature:F0028` in `feature:F0018.depends_on`; this is pre-existing and outside F0025.
- `coverage-report.yaml` was not regenerated at G7. It is deferred to G8 after the feature archive move because it binds path-sensitive feature-document locations.

## Handoff to Closeout

The semantic graph is green and ready for PM closeout to verify, not re-author. If closeout detects a new binding gap, route back to G7 for an Architect delta pass.
