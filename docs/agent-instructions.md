# Working in Nebula Insurance CRM

Read `planning-mds/BLUEPRINT.md` for product scope and process. Feature
requirements live under `planning-mds/features/`; evidence requirements live in
`planning-mds/operations/evidence/README.md`. Do not invent product rules when
these sources are incomplete; record an open decision instead.

Framework sessions run from the sibling `nebula-agents` checkout. Always pass
this repository's absolute path with `--product-root` (or set
`NEBULA_PRODUCT_ROOT`). The project check declared in `.nebula-project.yaml`
is additive to framework validators and reviewer approvals.

New evidence packages must preserve their raw outputs under the run directory.
The historical compatibility boundary in `scripts/validate-evidence-package.py`
is a one-off concession for runs recorded before `2026-09-07`; it does not
permit new runs to omit contract identity, durable artifacts, or referenced
files. Never rewrite archived provenance to make it appear current.

Use the product lifecycle runner for gates declared in `lifecycle-stage.yaml`:

```bash
python3 scripts/run-lifecycle-gates.py --list
python3 scripts/run-lifecycle-gates.py
```
