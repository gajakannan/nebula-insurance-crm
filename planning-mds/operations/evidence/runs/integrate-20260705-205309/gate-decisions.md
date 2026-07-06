# Gate Decisions — integrate PR #51 attempt 1

| Gate | Decision | Decider | Timestamp | Rationale | Blocking |
|------|----------|---------|-----------|-----------|----------|
| I0 feature-review verdict | WAIVED | maintainer (gajakannan) | 2026-07-05 | Train-wide waiver decision; per-run record. | cleared |
| I2 semantic merge | HALT | merge3 (mechanical) | 2026-07-05 | 22 DivergentInsert conflicts: stale pre-archive F0038/Neuron records on the source branch vs post-archive mainline truth. Routed: architect (13 nodes), PM (9 mappings). | yes |
| I6 human test validation | NOT REACHED | — | — | Halted at I2; superseded by fixup re-run. | — |
