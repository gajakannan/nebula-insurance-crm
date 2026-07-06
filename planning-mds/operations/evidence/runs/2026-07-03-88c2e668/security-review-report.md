# Security Review Report

Scope: F0028 release-readiness diff
Date: 2026-07-03

## Summary

- Assessment: PASS WITH RECOMMENDATIONS
- Vulnerabilities found: 0 newly introduced F0028 blockers
- Risk level: Medium due inherited dependency advisory and scanner automation gap

## OWASP Top 10 Assessment

### 1. A01 Broken Access Control

- Status: PASS
- Findings: `/carrier-markets` returns expected unauthenticated `401`; Casbin/F0028 authorization tests passed.

### 2. A02 Cryptographic Failures

- Status: PASS
- Findings: No new cryptographic handling introduced by F0028.

### 3. A03 Injection

- Status: PASS
- Findings: API uses typed DTOs, validators, EF repository patterns, and parameterized persistence.

### 4. A04 Insecure Design

- Status: PASS
- Findings: Commercially sensitive carrier/appetite/appointment data remains behind authenticated policy-protected API paths.

### 5. A05 Security Misconfiguration

- Status: PASS WITH RECOMMENDATIONS
- Findings: Runtime local Authentik/API services are healthy; scanner automation remains a CI hardening follow-up.

### 6. A06 Vulnerable and Outdated Components

- Status: PASS WITH RECOMMENDATIONS
- Findings: Existing `Microsoft.OpenApi 2.0.0` NU1903 advisory remains inherited and not introduced by F0028.

### 7. A07 Identification and Authentication Failures

- Status: PASS
- Findings: Unauthenticated F0028 endpoint access is rejected with `401`.

### 8. A08 Software and Data Integrity Failures

- Status: PASS
- Findings: No external carrier synchronization or untrusted package ingestion introduced by F0028.

### 9. A09 Security Logging and Monitoring Failures

- Status: PASS
- Findings: F0028 uses existing audit/timeline conventions; no blocker found.

### 10. A10 Server-Side Request Forgery (SSRF)

- Status: PASS
- Findings: No outbound URL-fetching behavior introduced.

## Recommendations

- [medium] Upgrade or replace inherited `Microsoft.OpenApi 2.0.0` dependency outside F0028 — owner: Security Reviewer; follow-up: dependency maintenance backlog.
- [medium] Add CI scanner automation for dependency, secrets, SAST, and DAST evidence — owner: Security Reviewer; follow-up: security pipeline hardening.

## Result

PASS WITH RECOMMENDATIONS.
