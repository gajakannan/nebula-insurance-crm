# Security Review Report — F0024 Claims And Service Case Tracking

**Result:** PASS WITH RECOMMENDATIONS

## Scope

Reviewed F0024 service-case and claim-reference implementation, including API authorization checks, claim-reference data handling, frontend routes/forms, migration, policy rows, and scanner artifacts under artifacts/security.

## Threat Model

Assets:
- Service case records and claim-reference context.
- Account/policy linkage and timeline audit events.
- Follow-up tasks and communication links.

Actors:
- Internal DistributionUser, DistributionManager, Underwriter, RelationshipManager, ProgramManager, Admin.
- BrokerUser and ExternalUser are denied for MVP service-case access by policy.

Trust boundaries:
- Browser to API.
- API authorization to application service.
- Application service to EF/PostgreSQL.
- Timeline projections to account/policy activity reads.

## Security Findings

### Medium — Existing vulnerable dependency advisory remains

Microsoft.OpenApi 2.0.0 reports high severity advisory GHSA-v5pm-xwqc-g5wc during .NET build/test.

Impact: dependency risk remains in the API project independent of F0024.

Required follow-up: upgrade or formally accept the advisory before final security signoff.

### Medium — Dependency scanner did not complete in local environment

dotnet list package --vulnerable and the dependency wrapper hung and were terminated. Frontend wrapper used npm audit and failed because this repo uses pnpm-lock.yaml.

Impact: current dependency verdict relies on observed build/test advisory output rather than a complete vulnerability inventory.

Required follow-up: rerun dependency audit in CI or a working local environment before closeout.

### Low — Secrets wrapper unavailable; fallback scan found test tokens only

gitleaks was not installed. Fallback rg scan returned test token literals in test files and one archived planning shell snippet.

Impact: no production secret was identified in F0024 code, but scanner coverage is weaker than preferred.

Required follow-up: run gitleaks or equivalent before final signoff.

## Control Assessment

- Authorization: PASS — service-case endpoints route through service-layer `service_case:*` checks; communication links also require `communication_event:read`; follow-up tasks also require `task:create`.
- Input validation: PASS — FluentValidation covers type, status, priority, required IDs, and length limits.
- Data minimization: PASS — claim-reference model excludes reserves, payments, coverage decisions, adjudication state, and carrier credentials.
- Auditability: PASS — create/update/transition/claim-reference/communication/task actions emit timeline events.
- Error disclosure: PASS — endpoints use ProblemDetails-style responses and avoid leaking internal exception details.
- Abuse resistance: PASS — endpoint group requires auth and authenticated rate limiting.

## Scan Artifacts

- artifacts/security/planning-security-audit.txt — PASS.
- artifacts/security/check-secrets-wrapper.txt — tool unavailable.
- artifacts/security/secrets-scan.txt — fallback findings are test-token literals and archived planning text.
- artifacts/security/dependency-scan-summary.txt — findings present; dependency wrapper incomplete.
- artifacts/security/sast-scan.txt — semgrep unavailable.
- artifacts/security/dast-scan.txt — target URL not supplied for DAST wrapper.

## Recommendations

- [medium] Resolve or formally accept Microsoft.OpenApi 2.0.0 advisory GHSA-v5pm-xwqc-g5wc before security signoff — owner: Security Reviewer; follow-up: required-before-closeout.
- [medium] Re-run dependency, gitleaks, SAST, and DAST scans in CI or with required tools installed before closeout — owner: Security Reviewer; follow-up: required-before-closeout.
- [low] Add F0024 authorization integration tests for denied roles and cross-resource checks — owner: Quality Engineer; follow-up: required-before-closeout.
