# Feature Security Review Report

Feature: F0012 — Dashboard Storytelling Infographic Canvas

## Summary

- Assessment: PASS WITH RECOMMENDATIONS
- Findings:
  - Critical: 0
  - High: 1
  - Medium: 3
  - Low: 0

## Findings

### Critical

- None.

### High

1. **H-SEC-01: Dependency vulnerability scan evidence is incomplete**
   - **Location:** `agents/security/scripts/scan-dependencies.sh` execution log (2026-03-14), `planning-mds/features/archive/F0012-dashboard-storytelling-infographic-canvas/DEPLOYABILITY-CHECK.md:36`
   - **What:** Automated dependency scan could not complete end-to-end (frontend audit network resolution failure; backend dependency scan restore failure).
   - **Why it matters:** Known vulnerable dependencies cannot be ruled out for this release slice without a completed scan in the target runtime.
   - **Exploit scenario:** A vulnerable package remains in the dependency graph and is shipped without detection, enabling a known public exploit path.
   - **Remediation:** Re-run dependency scanning in healthy runtime/CI containers with network access and successful restore, then attach scan artifacts to F0012 evidence.

### Medium

1. **M-SEC-01: Secret scanning tool unavailable in runtime**
   - **Location:** `agents/security/scripts/check-secrets.sh` output (`gitleaks not found`)
   - **What:** Repository-wide secret scanning was not executed by the required toolchain in this run.
   - **Why it matters:** Hardcoded credential leakage detection is incomplete.
   - **Remediation:** Install/configure `gitleaks` in runtime image/CI job and rerun scan.

2. **M-SEC-02: SAST scanner unavailable in runtime**
   - **Location:** `agents/security/scripts/run-sast-scan.sh` output (`semgrep not found`)
   - **What:** Static security analysis was not executed.
   - **Why it matters:** Code-level vulnerability patterns (injection, unsafe APIs, insecure config usage) may go undetected.
   - **Remediation:** Install/configure `semgrep` (or configured `SAST_SCAN_CMD`) and rerun.

3. **M-SEC-03: DAST scan target not configured**
   - **Location:** `agents/security/scripts/run-dast-scan.sh` output (`target URL is required`)
   - **What:** Dynamic application scan did not run due missing target configuration.
   - **Why it matters:** Runtime route/headers/error-surface weaknesses are not validated.
   - **Remediation:** Provide `--target` or `DAST_TARGET_URL` for the dashboard runtime and run DAST in isolated test environment.

### Low

- None.

## Control Checks

- [x] Authorization coverage complete for dashboard read endpoints (`/dashboard/*` read paths remain protected by authorization checks)
- [x] Input validation enforced (entityType/outcome key validation + `periodDays` normalization bounds in repository)
- [x] No hardcoded secrets introduced in F0012 feature code changes
- [x] Auditability requirements met for this scope (read-path changes only; no new mutation endpoints)

## Recommendation

**FIX HIGH** — resolve H-SEC-01 before release approval; track M-SEC-01..03 as required hardening/CI capability work if not already provisioned.
