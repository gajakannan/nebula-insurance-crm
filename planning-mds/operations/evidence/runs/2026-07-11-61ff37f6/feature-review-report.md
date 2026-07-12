# Feature Review Report — F0032

| Field | Value |
| --- | --- |
| **Feature ID** | F0032 — Admin Configuration & Reference Data Console |
| **Feature run ID (reviewed)** | 2026-07-06-f0ef8526 (approved, per `latest-run.json`) |
| **Feature review run ID** | 2026-07-11-61ff37f6 |
| **Date** | 2026-07-11 |
| **Mode** | closeout-audit (read-only) |
| **Diff range** | `e2f78be..pr-57` (merge-base of `origin/main` & PR #57 → head `6693510`); 107 files, +9464/−1261 |
| **Product root** | `/home/gajap/uSandbox/repos/nebula/nebula-insurance-crm-f0032` (read-only worktree of PR #57) |
| **DevOps lane** | RUN_DEVOPS=auto → **ran** (migration + endpoints + deployability evidence in scope) |

## Review question

Is F0032 **truly done** — i.e., do the delivered code, tests, and closeout evidence prove the feature is complete and merge/release-ready, with all required evidence validation passing?

## Decision

# 🔴 NOT DONE

**Primary trigger (FR4 rule 1 — failed required evidence validation):** two of the FR2-required validators fail against the delivered PR tree:

1. `validate-feature-evidence.py --stage closeout` → **exit 1** (feature-run `commands.log` cites three `coverage.cobertura.xml` artifacts that do not resolve under the product root).
2. `scripts/kg/validate.py --check-drift` **and** `--check-symbols` → **exit 1** (KG still bound to the pre-archive feature path while the feature was moved to `features/archive/`; archive dir neither mapped nor excluded).

No **critical** finding exists (server-side authorization is enforced on every endpoint; backend/frontend build green; API + UI E2E green). The verdict is driven by failed required evidence validation, compounded by three HIGH findings. Because a required validator fails, the gate cannot resolve to CONDITIONALLY DONE or TRULY DONE.

## Rationale

- The feature is **functionally implemented and exercised**: `AdminConfigurationEndpoints.cs` enforces `.RequireAuthorization()` + per-handler policy checks (9 handlers), and the API-lifecycle + UI E2E (`e2e-test-execution.md`) pass across catalog, draft guards, validation, compare, publish, stale-validation `409`, append-only rollback, audit filters, and non-Admin `403`. Functionally, this is not a "not built" verdict.
- It is an **evidence-integrity + delivered-artifact-consistency** verdict:
  - The closeout evidence package references transient, git-ignored coverage artifacts (`engine/tests/**/TestResults/*/coverage.cobertura.xml`, `.gitignore:32`) that were present at author time (the feature run's own `commands.log` shows `--stage closeout` exiting 0 on 2026-07-06) but are not persisted in the repo, so a fresh checkout fails the required closeout validator.
  - The delivered KG is internally inconsistent: `kg-reconciliation.md` records `--check-drift` as **PASS**, but the raw validator now **FAILS** in the PR tree (raw evidence wins over the summary per the contract's conflict rule).
  - The EF Core model snapshot was knowingly shipped stale (see H3), a defect the feature's own gates flagged and deferred rather than resolved.
- Per the operator prompt: "Don't declare TRULY DONE while required evidence validation fails"; FR4 rule 1 maps a failed required validator to **NOT DONE**.

## Next action

Owning lifecycle roles repair via `feature.md` / `review.md` / `test.md`; **no re-implementation required** — the runtime is proven. To reach TRULY DONE, a follow-up run must:

1. **Architect / KG (H2):** re-bind F0032 in `canonical-nodes.yaml`, `code-index.yaml`, `feature-mappings.yaml`, and `symbol-index.yaml` to `planning-mds/features/archive/F0032-…`, and map or exclude the archive directory, until `scripts/kg/validate.py --check-drift` and `--check-symbols` exit 0.
2. **DevOps / Code Reviewer (H3):** regenerate/reconcile `AppDbContextModelSnapshot.cs` so it reflects the six F0032 tables added by `20260706140000_F0032_AdminConfiguration` (verify `dotnet ef migrations has-pending-model-changes` is clean).
3. **Quality Engineer / PM (H1):** repackage coverage evidence so the closeout validator passes — persist coverage into the run's `artifacts/`, or update the feature-run `commands.log`/manifest to stop referencing git-ignored `TestResults` paths — until `validate-feature-evidence.py --stage closeout` exits 0.
4. **(Recommended, pre-release)** land the deferred hardening as tests/scans, not just accepted recommendations: non-Admin/`403` xUnit coverage (M2), focused F0032 branch coverage (M3), executed or formally re-waived dependency/secret/SAST/DAST scans (M1), audit ABAC redaction (M4), domain-specific semantic validation (M5).

---

## Findings by severity

### Critical
_None._ Authorization is enforced server-side; builds and E2E pass.

### High

**H1 — Closeout evidence validation fails (missing coverage artifacts).** `python3 agents/product-manager/scripts/validate-feature-evidence.py --product-root {worktree} --feature F0032 --stage closeout` exits 1. The reviewed feature run's `commands.log` (lines 27, 46, 64) references `engine/tests/Nebula.Tests/TestResults/{ad9e2d29…,85febe8f…,8679bb63…}/coverage.cobertura.xml`, none of which exist under the product root (`TestResults/` is git-ignored, `.gitignore:32`). The manifest-declared coverage artifact (`coverage-report.md`) is present, but the commands.log references are unresolvable. *Owner: Quality Engineer / PM. Evidence: `artifacts/validate-feature-evidence.txt`.*

**H2 — Knowledge-graph drift in the delivered tree; contradicts committed evidence.** `python3 scripts/kg/validate.py --check-drift` and `--check-symbols` exit 1. All F0032 errors point at the pre-archive path `planning-mds/features/F0032-…` for `source_docs`/feature/story bindings while the feature was moved to `planning-mds/features/archive/F0032-…`, and "Feature directory is neither mapped nor excluded: …/archive/F0032-…". The feature run's `kg-reconciliation.md` records `--check-drift` as PASS — raw validator output now contradicts it (raw wins; recorded as evidence drift). *Owner: Architect. Evidence: `artifacts/kg-validate-drift.txt`, `artifacts/kg-validate-symbols.txt`.* Consistent with the pre-F0006 (compile-from-kg-source) authoring noted by the operator.

**H3 — EF Core model snapshot shipped stale.** Migration `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260706140000_F0032_AdminConfiguration.cs` creates six tables (`ConfigurationDomains`, `ConfigurationDrafts`, `ConfigurationValidationResults`, `PublishedOperationalConfigurationSets`, `ConfigurationRefreshStatuses`, `ConfigurationAuditEvents`), but `AppDbContextModelSnapshot.cs` was **not changed** and contains **zero** references to any of them → the model snapshot is out of sync with the migration, breaking future `dotnet ef migrations add`. This is the feature's own code-review HIGH ("Regenerate/reconcile `AppDbContextModelSnapshot.cs` before G5"), knowingly deferred (see `code-review-report.md`, `deployability-check.md:20`, `kg-reconciliation.md:58`, `pm-closeout.md:20`, `signoff-ledger.md:62`). Deferral is documented with owner + target ("before production release"), so it is a valid accepted HIGH — but it remains an unresolved delivered-tree defect. *Owner: DevOps / Code Reviewer.*

### Medium

**M1 — Security scans never executed.** `evidence-manifest.json` shows `dependency`, `secrets`, `sast`, `dast` all `ran: false`, waived at G0 and still waived at G3 (`security-review-report.md:28-30`) on a `security_sensitive_scope: true` feature. Documented owner (Security Reviewer) + target ("before final production release").

**M2 — No automated negative-authorization test.** `AdminConfigurationEndpointTests.cs` contains only two xUnit tests (`ListDomains_AsAdmin_…`, `CreateDraft_WithoutReason_…`); there is no non-Admin/`403` xUnit test. Non-Admin denial is proven only by the run-folder E2E `.mjs` runner, which is not part of the CI test suite. Flagged by both Security and Code review; accepted as follow-up.

**M3 — No focused F0032 branch coverage.** `coverage-report.md` states smoke coverage only, "does not yet provide focused F0032 branch coverage," with unit/endpoint/frontend coverage listed as follow-ups.

**M4 — Audit ABAC redaction deferred (security).** Audit-only users with `admin-configuration:audit` but lacking underlying source-module read are not redacted (`security-review-report.md:37`); accepted as security-hardening follow-up.

**M5 — Coarse semantic validation.** Draft validation is JSON-validity only; domain-specific allowlists (queue/routing, SLA thresholds, search/report defaults, template metadata) are deferred (`code-review-report.md:33`, `security-review-report.md:38`).

### Low / observational (out of F0032 scope — recorded, not attributed to the feature)

**L1 — `validate_templates.py` fails** on nebula-agents templates (`feature-automation-safe.md` / `feature-operator-friendly.md` missing `compile.py` exit-validation entries). This is the agents tooling repo, not the F0032 product tree.

**L2 — `validate-trackers.py` (default resolution) fails** on `ROADMAP.md` for **F0037** ("Feature F0037 in Next should not link to archive path"). Default resolution targeted the main checkout, not the PR; the **worktree-scoped** run (`--product-root {worktree} --feature F0032`) **PASSES** with 0 errors. Not an F0032 defect.

---

## Completion Checks

| Lane | Result | Basis |
| --- | --- | --- |
| **Product Manager** | Conditional | 6/6 stories Done; REGISTRY/ROADMAP/STORY-INDEX synced; `latest-run.json` run matches reviewed run; archive move correct. But closeout evidence validator fails (H1) and multiple HIGH/MEDIUM items were *accepted as deferred follow-ups* rather than resolved (`pm-closeout.md`, `signoff-ledger.md`). |
| **Architect** | Fail | Assembly-plan/API/schema/ADR-032 present and matched; KG reconciled at author time — but KG validators now fail in the delivered tree (H2); `kg-reconciliation.md` PASS contradicted by raw output. |
| **Quality Engineer** | Conditional | Backend smoke (17), AdminConfig endpoint (2), vitest (2), API + UI E2E all pass. Gaps: no negative-authz xUnit (M2), no focused branch coverage (M3), closeout coverage artifacts unresolvable (H1). |
| **Code Reviewer** | Conditional | No blocking compile/runtime defects; scope clean and coherent. Unresolved HIGH: stale model snapshot (H3); semantic-validation hardening deferred (M5). |
| **Security** | Conditional | Server-side authz on all 9 handlers; publish requires matching validation hash; stale-validation `409`; append-only rollback. Gaps: scans never ran (M1), audit ABAC redaction deferred (M4), negative-authz proof only via non-CI E2E (M2). |
| **DevOps** | Conditional | API/tests/frontend build green; no new service/secret/env; in-process refresh. Risk: startup migrator applies F0032 tables but model snapshot is stale (H3), acknowledged in `deployability-check.md:20`. |

## Validation Evidence (FR2)

| Command | Result | Artifact |
| --- | --- | --- |
| `validate-feature-evidence.py --product-root {worktree} --feature F0032 --stage closeout` | **FAIL (exit 1)** — 3 missing coverage artifacts (H1) | `artifacts/validate-feature-evidence.txt` |
| `validate-trackers.py` (default resolution) | FAIL (exit 1) — F0037 ROADMAP, out of scope (L2) | `artifacts/validate-trackers.txt` |
| `validate-trackers.py --product-root {worktree} --feature F0032 --skip-feature-evidence` | **PASS (exit 0)** | `artifacts/validate-trackers-worktree.txt` |
| `scripts/kg/validate.py --check-symbols` | **FAIL (exit 1)** — archive-path drift (H2) | `artifacts/kg-validate-symbols.txt` |
| `scripts/kg/validate.py --check-drift` | **FAIL (exit 1)** — archive-path drift (H2) | `artifacts/kg-validate-drift.txt` |
| `agents/scripts/validate_templates.py` | FAIL (exit 1) — agents-repo templates, out of scope (L1) | `artifacts/validate-templates.txt` |
| `generate-story-index.py` | Not run — read-only audit; it is a generator that writes `STORY-INDEX.md`; worktree-scoped `validate-trackers` already confirms tracker/story-index consistency (PASS). | — |

All executed commands are appended to `commands.log`.

## Artifact Trace

**Feature evidence consulted (read-only, run `2026-07-06-f0ef8526`):** `evidence-manifest.json`, `coverage-report.md`, `test-execution-report.md`, `e2e-test-execution.md`, `code-review-report.md`, `security-review-report.md`, `pm-closeout.md`, `signoff-ledger.md`, `kg-reconciliation.md`, `deployability-check.md`, `commands.log`, `latest-run.json`.

**Source consulted (PR #57 tree):** `engine/src/Nebula.Api/Endpoints/AdminConfigurationEndpoints.cs`, `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260706140000_F0032_AdminConfiguration.cs`, `engine/src/Nebula.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`, `engine/tests/Nebula.Tests/Integration/AdminConfigurationEndpointTests.cs`, `.gitignore`, KG YAMLs under `planning-mds/knowledge-graph/`, `planning-mds/features/{REGISTRY,ROADMAP,STORY-INDEX}.md`, `planning-mds/features/archive/F0032-…/**`.

**This review's captured artifacts:** `artifacts/changed-files.txt`, `artifacts/diffstat.txt`, `artifacts/validate-feature-evidence.txt`, `artifacts/validate-trackers.txt`, `artifacts/validate-trackers-worktree.txt`, `artifacts/kg-validate-symbols.txt`, `artifacts/kg-validate-drift.txt`, `artifacts/validate-templates.txt`.

**Integrity:** No implementation, feature, tracker, KG, or feature-evidence artifact was edited by this review. Only review run folder `2026-07-11-61ff37f6` was written.
