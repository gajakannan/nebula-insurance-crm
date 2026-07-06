# KG Reconciliation — F0028

## Binding Delta

F0028 bindings were updated to include backend domain/application/infrastructure/API symbols, frontend carrier market symbols, tests, schemas, and planning artifacts. `coverage-report.yaml` was refreshed before final validation.

## Canonical Nodes

Canonical nodes for F0028 carrier market concepts remain in `planning-mds/knowledge-graph/canonical-nodes.yaml` and are mapped from `planning-mds/knowledge-graph/feature-mappings.yaml`.

## Validator Results

- `scripts/kg/validate.py --write-coverage-report` passed.
- `scripts/kg/validate.py` passed with documented pre-existing warnings.
- `scripts/kg/validate.py --check-drift` passed with documented pre-existing warnings.

Warnings include older unknown-symbol references for renewal/test stubs and a low-confidence inferred dependency edge involving F0028. These are not blocking for F0028 closeout.

## Handoff to Closeout

KG reconciliation is complete for G7/G8. No F0028-specific uncovered feature mapping remains.
