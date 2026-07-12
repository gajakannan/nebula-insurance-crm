# Bounce Report — F0032 (pr-57) → integrate/2026-07-12-train

**Gate:** I1 (branch verification). **Owner:** contributor (F0032 branch owner) + Architect (KG).
**Nothing was merged or pushed.**

## Why it bounced
`origin/main` adopted the **F0006 compiled-KG model** (`scripts/kg/{compile,decompile}.py`,
deterministic projections from `kg-source/**`). pr-57 predates it and carries **hand-maintained
KG projections on the old model**, which do not regenerate byte-for-byte:
- symbol-index / coverage-report / unbound-but-referenced / decisions-index all differ committed-vs-regenerated.
- The old model embeds non-deterministic fields (timestamps, hotspot ranks) and depends on extractors
  not guaranteed in every environment, so it can never satisfy `committed == regenerated`.

## Required contributor rework (on the F0032 branch, before re-invoking the train)
1. Merge `origin/main` into the F0032 branch (brings the F0006 KG tooling + `kg-source/**` layout).
   Resolve the 4 tracker/doc conflicts manually: `REGISTRY.md`, `ROADMAP.md`, `STORY-INDEX.md`,
   `architecture/feature-assembly-plan.md` — take **both** archival states (F0032 archived + F0037 archived).
2. Migrate F0032's KG into the F0006 `kg-source/**` model (author/port the source annotations), then
   `python3 scripts/kg/compile.py` to regenerate ALL projections deterministically. Do not hand-edit projections.
3. Verify committed == regenerated (re-run compile → zero diff), then:
   `scripts/kg/validate.py`, `--check-drift`, `--check-symbols`; `validate-trackers.py --skip-feature-evidence`;
   `validate-feature-evidence.py --stage closeout`; `dotnet ef migrations add` (expect empty). All green.
4. Re-invoke the integrate train (new RUN_ID) with the updated branch.

## Note on the prior remediation (commit a83dd7e)
- H3 (EF snapshot) and H1 (closeout evidence) fixes are model-independent and carry through — keep them.
- H2 (KG archive re-bind) was done by hand on the **old** model. It is superseded by step 2 above
  (compile.py re-derives the KG in the F0006 model). The finding stands; the hand-edits do not survive.
