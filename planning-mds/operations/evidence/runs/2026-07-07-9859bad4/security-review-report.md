# Feature Security Review Report

Feature: F0025 Commission Producer Splits and Revenue Tracking
Run: 2026-07-07-9859bad4
Date: 2026-07-07

## Summary

- Assessment: PASS WITH RECOMMENDATIONS
- Findings: Critical 0, High 0, Medium 3, Low 1
- Risk level: Medium
- Recommendation: APPROVE WITH RECOMMENDATIONS for G4, with scanner/tooling rerun required before production closeout.

## Findings

### Critical

- None.

### High

- None.

### Medium

1. External scanner classes were unavailable or blocked in local execution.
   - Locations: `artifacts/security/dependency-scan.txt`
   - `artifacts/security/secrets-scan.txt`
   - `artifacts/security/sast-scan.txt`
   - `artifacts/security/dast-scan.txt`
   - Exploit scenario: A vulnerable package or secret could remain undetected if CI/staging never runs the missing scanners.
   - Existing control: Raw failed outputs and manifest waivers are recorded; manual code review found no hardcoded F0025 secrets or obvious injection vectors.
   - Remediation: Re-run dependency, secrets, SAST, and DAST scans in CI or a network-enabled staging environment before G6/production.

2. Source-scope authorization is newly patched and needs regression proof.
   - Location: `engine/src/Nebula.Infrastructure/Repositories/CommissionRepository.cs:176`, `engine/src/Nebula.Infrastructure/Repositories/RevenueAttributionRepository.cs:52`
   - Exploit scenario: A future repository change could return cross-policy commission records, adjustments, producer splits, or rollup totals to a caller with only generic `commission:read`.
   - Existing control: F0025 now filters expected commission search/detail/mutation paths and rollups through policy visibility before response materialization.
   - Remediation: Add regression tests for cross-scope denial/empty result behavior.

3. DAST was attempted only against the local frontend target and could not start ZAP.
   - Location: `artifacts/security/dast-scan.txt`
   - Exploit scenario: Browser/API dynamic findings such as missing headers or exposed error pages could be missed.
   - Existing control: API endpoints require authorization and use ProblemDetails; frontend build is static Vite output.
   - Remediation: Run ZAP baseline against the deployed app/API in a staging runtime with authenticated paths where feasible.

### Low

1. Schedule list endpoint remains broad for users with `commission:read`.
   - Location: `engine/src/Nebula.Api/Endpoints/CommissionEndpoints.cs:82`
   - Impact: Internal users with commission read permission can list schedule metadata by carrier market. This appears acceptable for InternalOnly commission users but should be confirmed if finer carrier-market scoping becomes required.
   - Remediation: Consider scoping schedule reads by source policy/carrier assignment in a future hardening story.

## Control Checks

- Authorization coverage complete: PASS WITH RECOMMENDATIONS. Generic Casbin action checks are present on every endpoint, and G3 added policy-source visibility for expected commissions, adjustments, splits, mutations, and rollups.
- Input validation enforced: PASS. FluentValidation covers search pagination/date ranges, schedule basis/rate/source fields, split totals/duplicates, adjustment request/decision fields, and rollup dimensions.
- No secrets in code: PASS WITH WAIVER. `gitleaks` was unavailable; manual review found no F0025 hardcoded secrets and local `.env` values remain ignored/redacted.
- Auditability requirements met: PASS. Schedule, split, calculation, adjustment request, and adjustment decision mutations emit timeline events.

## OWASP Top 10 Assessment

1. A01 Broken Access Control: PASS WITH RECOMMENDATIONS. G3 patched source-policy visibility before row/count/rollup materialization; add regression tests.
2. A02 Cryptographic Failures: PASS. No new cryptography or secret storage was added.
3. A03 Injection: PASS. EF LINQ queries and typed validators are used; no raw SQL or command construction added.
4. A04 Insecure Design: PASS WITH RECOMMENDATIONS. Economic data is InternalOnly and scoped; scanner reruns and projection-granularity confirmation remain.
5. A05 Security Misconfiguration: PASS WITH RECOMMENDATIONS. DAST did not complete locally; staging scan required.
6. A06 Vulnerable Components: PASS WITH WAIVER. Dependency scan was blocked by registry/network and stuck backend vulnerability lookup; rerun in CI/staging.
7. A07 Identification and Authentication Failures: PASS. Routes require authenticated sessions and server-side authorization.
8. A08 Software and Data Integrity Failures: PASS WITH RECOMMENDATIONS. Migration was manually authored after EF tooling hung; G6 migration apply verification required.
9. A09 Security Logging and Monitoring Failures: PASS. Mutations emit audit timeline events with actor/resource context.
10. A10 SSRF: PASS. No outbound HTTP fetch or URL dereference was introduced.

## Scan Verdict

- Dependency: WAIVED for local G3. Artifact: `artifacts/security/dependency-scan.txt`
  Reason: npm advisory endpoint failed under restricted network and backend `dotnet list package --vulnerable` hung; process was terminated and must rerun in CI/staging.
- Secrets: WAIVED for local G3. Artifact: `artifacts/security/secrets-scan.txt`
  Reason: `gitleaks` is not installed.
- SAST: WAIVED for local G3. Artifact: `artifacts/security/sast-scan.txt`
  Reason: `semgrep` is not installed.
- DAST: WAIVED for local G3. Artifact: `artifacts/security/dast-scan.txt`
  Reason: OWASP ZAP docker image was unavailable/pull denied.

## Recommendation

APPROVE WITH RECOMMENDATIONS for the G4 approval gate. Do not treat this as production security clearance until waived scanner classes are rerun in CI/staging and source-scope regression tests are added or explicitly accepted.
