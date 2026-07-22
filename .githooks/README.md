# Git hooks

Version-controlled hooks for this repo. They are **not** active until you point git at this directory
(git does not auto-run hooks from a tracked folder):

```bash
git config core.hooksPath .githooks
```

Run that once per clone. (CI enforces the same checks regardless, so an unactivated hook only means you
find out later, in the PR, instead of at commit time.)

## `pre-commit` — KG reproducibility

Mirrors the `kg-reproducibility` CI gate locally. The compiled knowledge-graph projections and the
REGISTRY/ROADMAP/STORY-INDEX generated regions are produced from `planning-mds/kg-source/**` by
`scripts/kg/compile.py` and must **never be hand-edited**. The hook runs
`scripts/kg/validate.py --check-reproducible` when a commit touches `planning-mds/kg-source/**`,
`planning-mds/knowledge-graph/**`, or `REGISTRY.md`/`ROADMAP.md`/`STORY-INDEX.md`, and blocks the commit
if a committed generated file no longer equals `compile(source)`.

To fix a failure: edit the shards under `planning-mds/kg-source/**`, then
`python3 scripts/kg/compile.py` (+ `validate.py --write-coverage-report`, + the framework
`generate-story-index.py`), and re-stage the regenerated files.

Emergency bypass (discouraged): `git commit --no-verify`, or add a
`KG-Reproducibility-Override: <reason>` trailer to the head commit (the CI gate honors the same trailer).
