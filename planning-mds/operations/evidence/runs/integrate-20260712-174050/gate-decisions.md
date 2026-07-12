# Gate Decisions — integrate-20260712-174050

- **I0 review-verdict gate — PASS.** REVIEW_VERDICT_REF=2026-07-12-5e39956e (TRULY DONE) verified.
- **I1 branch verification (bounce check) — BOUNCE.** Regenerating pr-57's generated KG
  projections from its own content does not reproduce its committed copies:
  - symbol-index.yaml        10463+/22257-
  - coverage-report.yaml      1621+/1589-
  - unbound-but-referenced.yaml   5+/717-
  - decisions-index.yaml         2+/3-
  Committed ≠ regenerated → bounce. I2–I6 not run. Nothing merged. Nothing pushed.
- Root cause: F0032/pr-57 is authored on the **pre-F0006 KG model** (hand-maintained YAML,
  no `kg-source/**`, no `compile.py`); its generated projections are not deterministically
  regenerable, so they cannot pass the train's committed==regenerated bounce gate.
