# Coverage Artifact Repair — 2026-07-12

## Context

Feature-review run `2026-07-12-91439f3e` (closeout-audit of F0025) found that the
closeout evidence validator failed on the committed tree: `commands.log` (the
15:09, 15:24, and 15:45 focused test entries) referenced three
`coverage.cobertura.xml` artifacts at the raw path
`engine/tests/Nebula.Tests/TestResults/<guid>/…`, which is **gitignored**. Those
raw coverage outputs were never committed and are not recoverable from the
original build machine, so the required validator
(`validate-feature-evidence.py --feature F0025 --stage closeout`) exited 1 with
`commands_log_artifact_missing_fails` / `command_artifact_missing_fails`.

Every other feature commits coverage under its run's `artifacts/` folder; F0025
alone pointed at the gitignored TestResults location.

## Repair (operator-authorized 2026-07-12)

The maintainer waived the standing "no evidence edits" rule for this repair only.
The fix is a truthful re-derivation, not a fabrication:

1. Re-ran the identical focused suite on the committed pr-58 code:
   `dotnet test … --filter "FullyQualifiedName~CommissionRevenue|FullyQualifiedName~CasbinAuthorizationServiceTests.CommissionPolicy"`
   → **22 passed, 0 failed** (same 22 tests the original evidence cites), with
   coverlet producing a real `coverage.cobertura.xml`.
2. Committed that real coverage at
   `artifacts/coverage/commission-revenue.cobertura.xml` (now tracked, not gitignored).
3. Repointed the three `commands.log` coverage references from the gitignored
   `TestResults/<guid>/coverage.cobertura.xml` paths to the committed
   `artifacts/coverage/commission-revenue.cobertura.xml`.
4. Removed the stray out-of-scope file `f0025_textfile.md` from the repo root.

## Fidelity note

The committed coverage is a 2026-07-12 re-derivation of the same code and same
test filter, not the byte-identical original per-run TestResults outputs (which
were gitignored and lost). Line-rate reflects the focused-filter run measured
against the whole assembly. No gate decisions, signoffs, or verdicts in the
original run were altered by this repair.
