# Run Summary

- Run ID: `2026-07-07-773e619c`
- Action: `validate`
- Harness: `nebula-agents`
- Product root: `/Users/msig4/Documents/NEBULA/nebula-insurance-crm`
- Date: 2026-07-07
- Status: `blocked`

Full-project baseline validation was run because no active feature is nominated in `lifecycle-stage.yaml` or `planning-mds/features/REGISTRY.md`.

# Status

Blocked at the validation approval gate. Critical/high findings exist in requirements/planning readiness and product-local implementation gates.

# Evidence Index

- `action-context.md`
- `artifact-trace.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`
- `pm-validation-report.md`
- `architect-validation-report.md`
- `implementation-validation-report.md`

# Validation Summary

- Product Manager report: `FAIL`
- Architect report: `FAIL`
- Implementation validator report: `FAIL`
- Gate state: `blocked`

# Open Follow-ups

- Run the `plan` action for the next selected feature, starting with the roadmap `Next` item if approved by the operator.
- Refresh KG coverage via `python3 scripts/kg/validate.py --write-coverage-report`, then rerun `python3 scripts/kg/validate.py`.
- Regenerate or restore the missing frontend visual evidence artifact referenced by the product frontend quality gate.
- Reconcile stale `BLUEPRINT.md` status rows against `REGISTRY.md` and `ROADMAP.md`.
