# F0036 KG Reconciliation

## Binding Delta

No KG node, binding, or mapping change was made by this remediation run. Historical F0036 KG context remains in the archived feature and prior evidence package.

## Canonical Nodes

No canonical node was added, removed, or renamed for this remediation.

## Validator Results

`python3 scripts/kg/validate.py --check-drift` passed and is captured at `artifacts/test-results/kg-validate-check-drift-correct-path.txt`.

`python3 scripts/kg/validate.py` failed because the derived `coverage-report.yaml` is stale and recommends `--write-coverage-report`; this is captured at `artifacts/test-results/kg-validate-correct-path.txt`. The remediation did not rewrite KG coverage because the task is evidence-only and the drift check for current bindings passed.

## Handoff to Closeout

Treat the stale KG coverage report as a repo-level derived-artifact follow-up outside this evidence remediation. It is not attributed to F0036 product behavior.
