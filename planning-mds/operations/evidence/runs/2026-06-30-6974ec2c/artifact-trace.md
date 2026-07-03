# F0036 Artifact Trace

## Run Environment

Commands were executed from the product repository using repo-relative durable artifacts under `planning-mds/operations/evidence/runs/2026-06-30-6974ec2c/`. No `/tmp` or machine-specific absolute path is used as a durable artifact reference.

## Artifacts Read

- `planning-mds/features/archive/F0036-dynamic-product-attribute-form-engine/STATUS.md`
- `planning-mds/operations/evidence/features/F0036-dynamic-product-attribute-form-engine/latest-run.json`
- `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/evidence-manifest.json`
- `experience/package.json`
- `experience/src/features/lob-attributes/**`
- `experience/src/features/forms/**`

## Artifacts Created Or Updated

- `planning-mds/operations/evidence/runs/2026-06-30-6974ec2c/**`
- `planning-mds/operations/evidence/features/F0036-dynamic-product-attribute-form-engine/latest-run.json`
- `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/evidence-manifest.json` status will be patched to `superseded` by the framework helper after this manifest exists.

## Generated Evidence

- `artifacts/test-results/pnpm-global-version.txt`
- `artifacts/test-results/frontend-build-pnpm.txt`
- `artifacts/test-results/frontend-lob-attributes-tests-pnpm.txt`
- `artifacts/coverage/frontend-lob-attributes/coverage-summary.json`
- `artifacts/coverage/frontend-lob-attributes/lcov.info`
- `artifacts/test-results/frontend-lint-pnpm.txt`
- `artifacts/test-results/frontend-lint-theme-pnpm.txt`
- `artifacts/test-results/frontend-lint-effects-pnpm.txt`
- `artifacts/security/dependency-pnpm-audit.txt`
- `artifacts/security/dependency-pnpm-audit-escalated.txt`
- `artifacts/security/secrets-sensitive-keyword-review.txt`
- `artifacts/test-results/kg-validate-correct-path.txt`
- `artifacts/test-results/kg-validate-check-drift-correct-path.txt`

## External Or Global Evidence References

The old run `planning-mds/operations/evidence/runs/2026-05-28-077b7b30/` is referenced only as historical context. The current authoritative artifacts are local to this run folder.

## Omissions And Waivers

SAST and DAST scans are waived because this remediation is frontend-only evidence repair with no running target or configured SAST tool in the current contract lane. Dependency and sensitive-term review artifacts were captured under `artifacts/security/`.
