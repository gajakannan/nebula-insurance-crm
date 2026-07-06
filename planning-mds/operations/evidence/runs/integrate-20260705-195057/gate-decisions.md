# Gate Decisions — integrate dry run

| Gate | Decision | Decider | Timestamp | Rationale | Blocking |
|------|----------|---------|-----------|-----------|----------|
| I0 feature-review verdict | SIMULATED WAIVER | dry-run harness (NOT the maintainer) | 2026-07-05T19:51Z | No feature-review exists for F0021. This waiver is valid ONLY for the dry run; a live run must obtain a real verdict or a maintainer-recorded waiver. | yes (live) |
| I2 semantic merge | HALT | merge3 (mechanical) | 2026-07-05T19:54Z | UniqueViolation: feature:F0038 in both coverage.excluded_features and features — PR #47 re-adds the stale exclusion mainline removed. Routed to product-manager+architect (co-sign). | yes |
| I6 human test validation | NOT REACHED | — | — | Run halted at I2. | — |
