# Artifact Trace — F0017-broker-mga-hierarchy-and-producer-ownership run 2026-06-07-771a5ef6

> Captures what was read, written, generated, referenced externally, and explicitly omitted/waived.

## Artifacts Read

- `agents/ROUTER.md`, `agents/agent-map.yaml`, `agents/docs/AGENT-USE.md`, `agents/actions/feature.md`
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/PRD.md`
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/STATUS.md`
- `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/F0017-S0001..S0005-*.md`
- `planning-mds/features/REGISTRY.md`
- `planning-mds/architecture/decisions/ADR-026-broker-mga-hierarchy-producer-ownership-and-territory.md`
- `planning-mds/architecture/SOLUTION-PATTERNS.md`
- (additional reads appended as gates consume them)

## Artifacts Created Or Updated

- `evidence-manifest.json` — created (status draft)
- `README.md`, `action-context.md`, `artifact-trace.md`, `gate-decisions.md` — created
- `commands.log`, `lifecycle-gates.log` — created (empty skeleton)
- (gate artifacts appended as produced: `g0-…`, `g1-…`, `g2-…`, etc.)

## Generated Evidence

- `artifacts/diffs/changed-files.txt` — SCM changed-file list (populated at G2/G6)
- `artifacts/test-results/` — backend test output (populated in Step 1 / G2)
- `artifacts/coverage/` — coverage exports (populated at G2)
- (others appended as generated)

## External Or Global Evidence References

- Frontend toolchain validation deferred to CI (recorded in `coverage-report.md`); global frontend lanes may be linked but do not substitute for the feature-level role reports.

## Omissions And Waivers

- Mirrors manifest `omissions[]` / `waivers`. Coverage waiver (frontend-CI-deferred) recorded at G2. Security review omitted as non-required (`security_sensitive_scope=false`; Security not forced per STATUS.md).

## Run Environment (conditional)

- `commands.log` `cwd` recorded as repo-relative `{PRODUCT_ROOT}` where possible. Any absolute `cwd` lines are justified here.
- Absolute cwd: `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm` — PRODUCT_ROOT for this session; commands run from the product repo root.
