# Signoff Ledger — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Required Role Matrix

| Role | Required | Evidence |
|------|----------|----------|
| Quality Engineer | Yes | `test-plan.md`, `test-execution-report.md`, `coverage-report.md` |
| Code Reviewer | Yes | `code-review-report.md` |
| Security Reviewer | Yes | `security-review-report.md` |
| DevOps | Yes | `deployability-check.md` |
| Architect | Yes | `g0-assembly-plan-validation.md`; final KG reconciliation remains G7 |

## Current Signoff State

- F0027-S0001 / Quality Engineer: PASS WITH RECOMMENDATIONS by Quality Engineer on 2026-07-03 (test-execution-report.md)
- F0027-S0001 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-03 (code-review-report.md)
- F0027-S0001 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-03 (security-review-report.md)
- F0027-S0001 / DevOps: PASS WITH RECOMMENDATIONS by DevOps on 2026-07-03 (deployability-check.md)
- F0027-S0001 / Architect: PASS WITH RECOMMENDATIONS by Architect on 2026-07-03 (g0-assembly-plan-validation.md)
- F0027-S0002 / Quality Engineer: PASS WITH RECOMMENDATIONS by Quality Engineer on 2026-07-03 (test-execution-report.md)
- F0027-S0002 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-03 (code-review-report.md)
- F0027-S0002 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-03 (security-review-report.md)
- F0027-S0002 / DevOps: PASS WITH RECOMMENDATIONS by DevOps on 2026-07-03 (deployability-check.md)
- F0027-S0002 / Architect: PASS WITH RECOMMENDATIONS by Architect on 2026-07-03 (g0-assembly-plan-validation.md)
- F0027-S0003 / Quality Engineer: PASS WITH RECOMMENDATIONS by Quality Engineer on 2026-07-03 (test-execution-report.md)
- F0027-S0003 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-03 (code-review-report.md)
- F0027-S0003 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-03 (security-review-report.md)
- F0027-S0003 / DevOps: PASS WITH RECOMMENDATIONS by DevOps on 2026-07-03 (deployability-check.md)
- F0027-S0003 / Architect: PASS WITH RECOMMENDATIONS by Architect on 2026-07-03 (g0-assembly-plan-validation.md)
- F0027-S0004 / Quality Engineer: PASS WITH RECOMMENDATIONS by Quality Engineer on 2026-07-03 (test-execution-report.md)
- F0027-S0004 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-03 (code-review-report.md)
- F0027-S0004 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-03 (security-review-report.md)
- F0027-S0004 / DevOps: PASS WITH RECOMMENDATIONS by DevOps on 2026-07-03 (deployability-check.md)
- F0027-S0004 / Architect: PASS WITH RECOMMENDATIONS by Architect on 2026-07-03 (g0-assembly-plan-validation.md)
- F0027-S0005 / Quality Engineer: PASS WITH RECOMMENDATIONS by Quality Engineer on 2026-07-03 (test-execution-report.md)
- F0027-S0005 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-03 (code-review-report.md)
- F0027-S0005 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-03 (security-review-report.md)
- F0027-S0005 / DevOps: PASS WITH RECOMMENDATIONS by DevOps on 2026-07-03 (deployability-check.md)
- F0027-S0005 / Architect: PASS WITH RECOMMENDATIONS by Architect on 2026-07-03 (g0-assembly-plan-validation.md)

## Recommendation Acceptances

- F0027-G6-regenerate-tests: accepted. Add focused regenerate/retrieve coverage before G6 candidate evidence validation.
- F0027-G6-api-tests: accepted. Add API-level preview/issue/regenerate validation before G6 candidate evidence validation where runtime constraints allow.
- F0027-renderer-hardening: accepted. Keep the simple renderer in v1, with production renderer hardening tracked as a nonblocking follow-up.
- F0027-openapi-sourcegen-compatible-upgrade: accepted with waiver. `Microsoft.OpenApi` remains pinned to the compatible 2.3.x line until the ASP.NET OpenAPI source generator supports a patched 3.x line in this project.
- F0027-security-tooling: accepted with waiver. Dependency scan ran; gitleaks, semgrep, and ZAP are unavailable in this environment and remain explicitly waived for this run.

## Waivers And Omissions

- Dependency scan waiver: frontend high/critical dependency findings were remediated. `Microsoft.OpenApi` remains suppressed because the patched 3.x line breaks the current ASP.NET OpenAPI source generator. Owner: Security Reviewer. Approved on: 2026-07-03.
- Secrets scan waiver: gitleaks is not installed in PATH and no `SECRET_SCAN_CMD` override is configured. Owner: Security Reviewer. Approved on: 2026-07-02.
- SAST waiver: semgrep is not installed in PATH and no `SAST_SCAN_CMD` override is configured. Owner: Security Reviewer. Approved on: 2026-07-02.
- DAST waiver: OWASP ZAP docker image could not be run or pulled in this environment after Docker escalation. Owner: Security Reviewer. Approved on: 2026-07-02.
- Omissions: none.
