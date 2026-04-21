# Manual Run Evidence

This directory stores evidence packages for human-orchestrated action runs.

## Structure

- One folder per run:
  - `planning-mds/operations/evidence/<run-id>/`

Each run folder should contain:
- `action-context.md`
- `artifact-trace.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`

## Example Package

- `plan-2026-02-08-preview-walkthrough/` demonstrates the required format for preview release readiness.

## Frontend UX Audit Evidence

- For frontend UI changes, add UX evidence files under:
  - `planning-mds/operations/evidence/frontend-ux/`
- Use:
  - `planning-mds/operations/evidence/frontend-ux/TEMPLATE.md`
- CI validates this requirement via the framework validator run from the `nebula-agents` session root (`frontend-developer/scripts/validate-frontend-ux-evidence.py`). The product repo does not carry a local copy.

## Frontend Quality Gate Evidence

- Solution-owned frontend validation evidence is tracked under:
  - `planning-mds/operations/evidence/frontend-quality/`
- The lifecycle gate consumes:
  - `planning-mds/operations/evidence/frontend-quality/latest-run.json`
- The manifest must distinguish:
  - component
  - integration
  - accessibility
  - coverage
  - visual
- The manifest must point to concrete artifacts, not review summaries alone.

For execution requirements, consult the framework repo ([gajakannan/nebula-agents](https://github.com/gajakannan/nebula-agents)) — specifically `docs/MANUAL-ORCHESTRATION-RUNBOOK.md` inside that repo. The product repo does not carry a local copy.
