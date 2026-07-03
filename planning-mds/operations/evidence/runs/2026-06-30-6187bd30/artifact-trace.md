# F0019 Artifact Trace

## Artifacts Read

- Prior evidence package: `planning-mds/operations/evidence/runs/2026-06-03-7e8e0ddc/`
- Feature tracker: `planning-mds/features/archive/F0019-submission-quoting-proposal-and-approval/STATUS.md`
- Historical changed path list copied into `artifacts/diffs/changed-files.txt`

## Artifacts Created Or Updated

- Backend build: artifacts/test-results/backend-build.txt
- Backend workflow tests: artifacts/test-results/backend-workflow-tests.txt
- Backend coverage: artifacts/coverage/backend-workflow-coverage.cobertura.xml
- Frontend install: artifacts/test-results/frontend-pnpm-install-global-tmp-home-escalated.txt
- Frontend direct build: artifacts/test-results/frontend-build-direct-binaries.txt
- Frontend submissions tests: artifacts/test-results/frontend-submissions-tests-direct.txt
- Frontend coverage: artifacts/coverage/frontend-submissions/coverage-summary.json
- Dependency scans: artifacts/security/dependency-dotnet-vulnerable-escalated.txt and artifacts/security/dependency-pnpm-audit-escalated.txt
- Secrets keyword review: artifacts/security/secrets-keyword-review.txt

## Generated Evidence

Generated evidence is stored under `planning-mds/operations/evidence/runs/2026-06-30-6187bd30/`.

## External Or Global Evidence References

No durable external or global evidence references are used.

## Omissions And Waivers

SAST and DAST scanner classes are explicitly waived in `evidence-manifest.json` because Semgrep was unavailable and no deployed DAST target existed.

## Notes

Several early command attempts captured sandbox, pnpm home, and pnpm command-shape failures before the successful direct-binary frontend path was used. Those attempts remain in `commands.log`; successful artifacts above are the evidence-bearing outputs.

## Durable Path Policy

Reports and manifest entries use run-local, repo-relative artifact paths. Scratch `/tmp` paths appear only as command environment for package-manager caches.
