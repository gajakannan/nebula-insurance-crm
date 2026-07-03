# F0035 Artifact Trace

## Artifacts Read

- Prior evidence package: `planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/`
- Feature tracker: `planning-mds/features/archive/F0035-session-continuity-and-token-refresh/STATUS.md`
- Historical changed path list copied into `artifacts/diffs/changed-files.txt`

## Artifacts Created Or Updated

- Backend build: artifacts/test-results/backend-build.txt
- Backend session/auth test attempt: artifacts/test-results/backend-session-auth-tests.txt
- Backend coverage attachment: artifacts/coverage/backend-session-auth-coverage.cobertura.xml
- Frontend direct build: artifacts/test-results/frontend-build-direct-binaries.txt
- Frontend session tests passing rerun: artifacts/test-results/frontend-session-continuity-tests-direct-rerun.txt
- Frontend coverage: artifacts/coverage/frontend-session-continuity/coverage-summary.json
- Dependency scans: artifacts/security/dependency-dotnet-vulnerable-escalated.txt and artifacts/security/dependency-pnpm-audit-escalated.txt
- Secrets keyword review: artifacts/security/secrets-keyword-review.txt

## Generated Evidence

Generated evidence is stored under `planning-mds/operations/evidence/runs/2026-06-30-bac66bac/`.

## External Or Global Evidence References

No durable external or global evidence references are used.

## Omissions And Waivers

SAST and DAST scanner classes are explicitly waived in `evidence-manifest.json`. Docker/Testcontainers unavailability is documented in `test-execution-report.md`.
