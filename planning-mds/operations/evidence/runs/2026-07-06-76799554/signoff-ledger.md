# Signoff Ledger - F0037-hierarchy-aware-access-scoping-and-distribution-rollups run 2026-07-06-76799554

> Required at G5 per Section 10. Strictly consistent with `STATUS.md` current signoff state, latest row per `(story, role)`. PM-owned.

## Required Role Matrix

| Role | Required |
|------|----------|
| Quality Engineer | Yes |
| Code Reviewer | Yes |
| Security Reviewer | Yes |
| Architect | Yes |
| DevOps | Conditional |

## Current Signoff State

Latest passing row per `(story, role)` derived from STATUS.md provenance.

```text
- F0037-S0001 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0001 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0001 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0001 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
- F0037-S0002 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0002 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0002 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0002 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
- F0037-S0003 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0003 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0003 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0003 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
- F0037-S0004 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0004 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0004 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0004 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
- F0037-S0005 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0005 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0005 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0005 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
- F0037-S0006 / Architect: APPROVED by Architect on 2026-07-06 (g0-assembly-plan-validation.md)
- F0037-S0006 / Quality Engineer: PASS by Quality Engineer on 2026-07-06 (test-execution-report.md)
- F0037-S0006 / Code Reviewer: PASS by Code Reviewer on 2026-07-06 (code-review-report.md)
- F0037-S0006 / Security Reviewer: PASS by Security Reviewer on 2026-07-06 (security-review-report.md)
```

## Recommendation Acceptances

- No role report used `PASS WITH RECOMMENDATIONS` or `APPROVED WITH RECOMMENDATIONS` at G5.
- Security Reviewer residual risk is accepted as non-blocking: dependency vulnerability audit and authenticated DAST remain deferred under recorded waivers.

## Waivers And Omissions

- Omission: `latest-run.json` is omitted until G8 closeout per harness contract.
- Security scan waiver: dependency vulnerability audit was not run because external registry vulnerability checks are network-dependent in this sandbox.
- Security scan waiver: authenticated DAST was not run because it requires a restarted authenticated API/browser environment on this feature branch.
