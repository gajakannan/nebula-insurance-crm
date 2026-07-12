# PR #57 (F0032) — merge plan, caveat dispositions, branch state

Date: 2026-07-12 · Companion to feature-review `2026-07-12-5e39956e` (TRULY DONE) and `remediation-note.md`.

## Caveat dispositions

### H1 (coverage) → folded into M3
The closeout **validation failure** is resolved (dangling transient cobertura pointers removed; `coverage-report.md` honest). The remaining concern — persisted, verifiable coverage with real F0032 branch depth — is **tracked as part of M3** (focused branch coverage), owned by Quality Engineer, to be produced with Docker/Testcontainers PostgreSQL up **before production release**. No further action now.

### Caveat 2 (self-remediation-then-review) → resolved by human PR review gate
The mandatory maintainer review of PR #57 before merge is the independent human checkpoint. The remediation is an 8-file diff (`scratchpad/F0032-remediation.patch`); reviewers should confirm: KG archive-path swaps (mechanical), `AppDbContextModelSnapshot.cs` regen (build-verified, 0 pending), and the three `commands.log` pointer removals.

## PR #57 cannot merge as-is — conflicts with main (F0006 KG-model divergence)

`origin/main` (now `c7edca3`) adopted the **F0006 compile-from-kg-source** KG model (`scripts/kg/compile.py` + `decompile.py`); F0032 predates it. Trial merge of `origin/main` into `pr-57` produced **8 conflicts**:

| File | Type | Resolution |
| --- | --- | --- |
| `planning-mds/features/REGISTRY.md` | tracker | Manual — take **both** archival changes: F0032 → Archived (pr-57) **and** F0037 → archive path (main/#60). |
| `planning-mds/features/ROADMAP.md` | tracker | Manual — same: reflect F0032 Completed + main's F0037/other moves. |
| `planning-mds/features/STORY-INDEX.md` | tracker | Manual — merge F0032 archive story links + main's entries. |
| `planning-mds/architecture/feature-assembly-plan.md` | doc | Manual — combine F0032 plan section with main's changes. |
| `planning-mds/knowledge-graph/symbol-index.yaml` | **generated** | Regenerate — do not hand-merge. |
| `planning-mds/knowledge-graph/decisions-index.yaml` | **generated** | Regenerate. |
| `planning-mds/knowledge-graph/coverage-report.yaml` | **generated** | Regenerate. |
| `planning-mds/knowledge-graph/unbound-but-referenced.yaml` | **generated** | Regenerate. |

**Notably auto-merged cleanly** (no conflict): the source KG YAMLs `canonical-nodes.yaml`, `code-index.yaml`, `feature-mappings.yaml` — but under the F0006 model these are **compiled from source**, so treat them as regenerable too.

### Recommended resolution sequence (on a pr-57 worktree, not pushed until reviewed)
1. `git merge origin/main` and resolve the **4 tracker/doc** conflicts manually (take both features' archival state).
2. For all KG artifacts, don't hand-merge — after the tracker/source inputs are settled, run the **F0006 KG pipeline**: `python3 scripts/kg/compile.py` then `python3 scripts/kg/validate.py --regenerate-symbols --regenerate-decisions --write-coverage-report` (per that repo's KG regen convention), and `git checkout --theirs`/regenerate the 4 conflicted generated files so they reflect F0032 in the F0006 model.
3. Re-run: `kg/validate.py --check-drift`/`--check-symbols`, `validate-trackers.py`, `validate-feature-evidence.py --stage closeout`, and `dotnet ef migrations add` (expect empty) → all green.
4. Human review, then push to update PR #57.

> ⚠️ My H2 hand-edits to the KG source YAMLs are **branch-local and will be superseded** by `compile.py` at merge time — the *finding* (F0032 must be archive-bound) stands; the F0006 compile flow is the real mechanism. H3 (snapshot) and H1 (evidence) fixes are model-independent and carry through the merge unaffected.

## Branch state (as of this session)

**Deleted (were already in main):** local `feat/F0006-phase-B-compiled-projection`, `chore/merge-3-PRs`. **Fast-forwarded:** local `main` → `c7edca3`.

**Kept — do NOT delete until merged:**
- `pr-57` (F0032, `a83dd7e`) — the only local copy of F0032; remote copy is the fork branch `nebula-insurance-crm-vamshi` backing **open PR #57**. Delete nothing here until #57 merges.
- `pr-56` (F0037, `27a5162`) — **superseded**: F0037 landed in main via PR #60 (`c7edca3`). The feature is in main, but this exact commit is not an ancestor of main. Likely safe to delete after you confirm #60 fully covers it.
- `fix/F0037-scope-review` (current checkout) — F0037 scope-review work; likely also covered by #60. Verify before deleting; switch off it first.

**Other open PRs:** #57 (F0032), #58 (F0025) — both still open.
