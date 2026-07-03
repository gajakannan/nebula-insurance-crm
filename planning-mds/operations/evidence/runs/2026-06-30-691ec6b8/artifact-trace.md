# F0023 Artifact Trace

## Artifacts Read

- Prior evidence package: `planning-mds/operations/evidence/runs/2026-06-19-a4e3fdd6/`
- Feature tracker: `planning-mds/features/archive/F0023-global-search-saved-views-and-operational-reporting/STATUS.md`
- Historical changed path list copied into `artifacts/diffs/changed-files.txt`

## Artifacts Created Or Updated

- Backend build: artifacts/test-results/backend-build.txt
- Backend SearchReporting tests: artifacts/test-results/backend-search-reporting-tests.txt
- Backend coverage: artifacts/coverage/backend-search-reporting-coverage.cobertura.xml
- Frontend direct build: artifacts/test-results/frontend-build-direct-binaries.txt
- Frontend search tests: artifacts/test-results/frontend-search-tests-direct.txt
- Frontend coverage: artifacts/coverage/frontend-search/coverage-summary.json
- Dependency scans: artifacts/security/dependency-dotnet-vulnerable-escalated.txt and artifacts/security/dependency-pnpm-audit-escalated.txt
- Secrets keyword review: artifacts/security/secrets-keyword-review.txt

## Generated Evidence

Generated evidence is stored under `planning-mds/operations/evidence/runs/2026-06-30-691ec6b8/`.

## External Or Global Evidence References

No durable external or global evidence references are used.

## Omissions And Waivers

SAST and DAST scanner classes are explicitly waived in `evidence-manifest.json` because Semgrep was unavailable and no deployed DAST target existed.
