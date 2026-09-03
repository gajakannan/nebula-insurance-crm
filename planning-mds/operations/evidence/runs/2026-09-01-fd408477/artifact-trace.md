# Artifact Trace — F0040-neuron-second-specialist-head run 2026-09-01-fd408477

> Required per §8. Captures what was read, written, generated, referenced externally, and explicitly omitted/waived.

## Artifacts Read

Bulleted list of files the action consulted (planning docs, prior evidence, role inputs).

- `planning-mds/operations/evidence/runs/2026-09-01-fd408477/action-context.md`
- `planning-mds/features/F0040-neuron-second-specialist-head/feature-assembly-plan.md`
- `planning-mds/features/F0040-neuron-second-specialist-head/F0040-S0001-live-broker-activity-zone.md`
- `planning-mds/operations/evidence/runs/2026-09-01-fd408477/g1-runtime-preflight.md`

## Artifacts Created Or Updated

- `evidence-manifest.json` — updated the restored G1 and DevOps verdicts to `PASS` and reconciled `required_roles`.
- `g1-runtime-preflight.md` — retained the failed probes and added the successful Docker/runtime retry.
- `commands.log` — recorded the resume, retry, runtime restoration, and health checks.

## Generated Evidence

Tool-produced outputs: coverage XML, test result XML, screenshots, scan exports. Cite the path within `artifacts/` or the external location.

## External Or Global Evidence References

References to global lanes (§20) or to other features' evidence that this run depends on. Each reference must resolve when validated.

- `planning-mds/operations/evidence/frontend-quality/latest-run.json`
- `planning-mds/operations/evidence/frontend-ux/ux-audit-YYYY-MM-DD.md`

## Omissions And Waivers

Mirror the manifest `omissions[]` and `waivers` entries for human review. Per §18 only non-required artifacts may be omitted.

## Run Environment (conditional)

Required only when `commands.log` carries an absolute `cwd`. One bullet per justified absolute path:

```text
- Absolute cwd: /workspace/some/path — sandboxed CI runner; PRODUCT_ROOT not stable
```
