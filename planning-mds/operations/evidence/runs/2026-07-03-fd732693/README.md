# Feature Evidence README - F0008-broker-insights run 2026-07-03-fd732693

## Run Summary

This feature action implements F0008 Broker Insights as a governed vertical slice using the Nebula agents harness. The run began from approved Phase A+B planning in plan run `2026-07-03-4b9ca863` and produced backend, frontend, quality, deployability, review, security, KG reconciliation, PM closeout, and archive correction evidence.

## Status

Final state for this run: `approved` / `Archived`.

## Evidence Index

- `evidence-manifest.json` - schema v1 manifest.
- `action-context.md` - run identity, inputs, assumptions, scope boundaries, and lifecycle stage.
- `artifact-trace.md` - read/written artifacts and run environment notes.
- `gate-decisions.md` - gate decisions from G0 through G8.
- `commands.log` - JSON Lines command log.
- `lifecycle-gates.log` - lifecycle validator summary.
- Role and gate reports - populated as each gate completes.

## Validation Summary

Closeout validation passed after PM archive correction. The only remaining validator note is the non-blocking `commands_log_absolute_cwd_warns` warning from earlier command telemetry.

## Open Follow-ups

- EF migration designer/model snapshot regeneration remains a non-blocking future hygiene task before future migration authoring.
