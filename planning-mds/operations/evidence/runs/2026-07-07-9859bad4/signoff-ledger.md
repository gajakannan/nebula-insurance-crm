# Signoff Ledger - F0025-commission-producer-splits-and-revenue-tracking run 2026-07-07-9859bad4

> Required at G5 per the feature evidence contract. Mirrors the current `STATUS.md` signoff rows for this run.

## Required Role Matrix

| Role | Required |
|------|----------|
| Architect | Yes |
| Quality Engineer | Yes |
| DevOps | Yes |
| Code Reviewer | Yes |
| Security Reviewer | Yes |

## Current Signoff State

- F0025-S0001 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0001 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0001 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0001 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0001 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)
- F0025-S0002 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0002 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0002 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0002 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0002 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)
- F0025-S0003 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0003 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0003 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0003 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0003 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)
- F0025-S0004 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0004 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0004 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0004 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0004 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)
- F0025-S0005 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0005 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0005 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0005 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0005 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)
- F0025-S0006 / Architect: PASS by Architect on 2026-07-07 (g0-assembly-plan-validation.md)
- F0025-S0006 / Quality Engineer: PASS by Quality Engineer on 2026-07-07 (test-execution-report.md)
- F0025-S0006 / DevOps: PASS by DevOps on 2026-07-07 (deployability-check.md)
- F0025-S0006 / Code Reviewer: APPROVED WITH RECOMMENDATIONS by Code Reviewer on 2026-07-07 (code-review-report.md)
- F0025-S0006 / Security Reviewer: PASS WITH RECOMMENDATIONS by Security Reviewer on 2026-07-07 (security-review-report.md)

## Recommendation Acceptances

- Code Reviewer recommendations are non-blocking for G5 and require PM closeout acceptance at G8:
  - Add source-scope regression tests before closeout or record explicit mitigation.
  - Confirm projection granularity with Architect/Product before production release.
  - Re-run full frontend suite once localStorage test environment issue is addressed.
- Security Reviewer recommendations are non-blocking for G5 and require PM closeout acceptance at G8:
  - Rerun dependency, secrets, SAST, and DAST scans in CI/staging before production release.
  - Add or explicitly accept source-scope regression proof.
  - Run staging DAST where feasible.
  - Confirm schedule-list scoping if finer carrier-market boundaries are required.

## Waivers And Omissions

- No manifest omissions are recorded.
- Local G3 security scan waivers are recorded in `evidence-manifest.json` for dependency, secrets, SAST, and DAST scanner classes because the local environment lacked required scanner/network availability.
