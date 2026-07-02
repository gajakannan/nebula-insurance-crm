# Security Review Report — F0021-communication-hub-and-activity-capture run 2026-07-01-9cee64f0

**Result:** PASS WITH RECOMMENDATIONS

## Scope

- Feature ID: F0021
- Run ID: 2026-07-01-9cee64f0
- Date: 2026-07-01
- Reviewer: Security Reviewer

This review covers the F0021 communication capture/history API, persistence model, contextual frontend panels, authorization policy surface, audit/timeline behavior, dependency exposure, and scan disposition.

## Reviewed Surfaces

- Auth/authz: `/communications` endpoint group, `CommunicationService.AuthorizeAsync`, `planning-mds/security/policies/policy.csv`, `planning-mds/security/authorization-matrix.md`.
- Input validation: `CommunicationValidators.cs`, endpoint validator calls, pagination clamping.
- Sensitive data flow: communication bodies, participants, email references, corrections, redactions, and timeline payloads.
- Auditability: `ActivityTimelineEvent` emission for capture, correction, redaction, and follow-up task creation.
- Dependency and scanner exposure: artifacts/security/dependency-scan-escalated.txt and the related secrets, SAST, DAST, and fallback artifacts.

## Threat Boundary

| Subject | Resource | Operation | Boundary / Control |
|---|---|---|---|
| Authenticated internal user | Communication events | create/read/correct/create follow-up | API `RequireAuthorization`, rate limiting, Casbin `communication_event` policy checks |
| Admin | Communication events | redact | Admin-only `communication_event redact` policy |
| Authenticated internal user | Linked CRM records | link communication activity | Server-side linked-entity existence checks and allowed entity-type validation |
| Frontend user | Communication body/participant data | view/edit in browser | React text rendering, no raw HTML rendering in F0021 panel |
| Runtime service | Timeline/audit log | emit mutation evidence | Server-side timeline events with actor and payload |

## Auth / Authz

The endpoint group requires authentication and authenticated rate limiting. Each service operation calls the authorization service for the relevant `communication_event` action. Redaction is separately gated by `redact`, which is only granted to Admin in `policy.csv`.

External `BrokerUser` access is explicitly denied by the policy/matrix and not exposed by the frontend panel.

## Validation

API requests are validated with FluentValidation:

- Communication type and direction allowlists.
- Exactly one primary link.
- Linked entity type allowlist and non-empty entity IDs.
- Participant display name and bounded text fields.
- Email reference requires a message ID for `EmailReference`.
- Follow-up task title, priority, assignee, linked entity type, and linked entity ID.
- Correction/redaction action allowlist and required reason/corrected fields.

The repository uses EF LINQ queries, not raw SQL. No injection-prone query construction was found in the reviewed F0021 paths.

## Audit / Logging

Capture, correction, redaction, task creation, and communication follow-up task creation emit timeline events with actor identity and payload references. Redacted communication responses replace summary/body with redacted values, and redaction creates an append-only correction record.

No secrets were found by targeted fallback scanning in F0021 changed files.

## Secrets / Config

F0021 did not add committed secrets or new secret-bearing configuration. The official `gitleaks` wrapper could not run because `gitleaks` is not installed; a targeted `rg` fallback over F0021 changed files found no secret-pattern matches.

## Scan Disposition

| Class | Ran | Result / Finding summary | Artifact or waiver reason |
|-------|-----|--------------------------|---------------------------|
| dependency | Yes | Findings present: frontend audit reported 77 vulnerabilities including 1 critical and 38 high, mostly dev tooling; backend audit reported high `Microsoft.OpenApi 2.0.0` advisory. | artifacts/security/dependency-scan-escalated.txt |
| secrets | Yes | Official scanner unavailable: `gitleaks` not installed. Targeted fallback over F0021 files found no secret-pattern matches. | artifacts/security/secrets-scan.txt and artifacts/security/secrets-rg-fallback.txt |
| sast | Yes | Official scanner unavailable: `semgrep` not installed. Targeted fallback over F0021 files found no risky patterns from the review expression. | artifacts/security/sast-scan.txt and artifacts/security/sast-rg-fallback.txt |
| dast | Yes | OWASP ZAP wrapper attempted Docker path but the image was unavailable; API health fallback passed. | artifacts/security/dast-scan.txt and artifacts/security/dast-health-fallback.txt |

## OWASP Top 10 Coverage

| Category | Status | Notes |
|----------|--------|-------|
| A01 Broken Access Control | OK with recommendation | Server-side auth checks exist for action-level policy. Broader resource-level integration tests are recommended. |
| A02 Cryptographic Failures | N/A | F0021 adds no cryptographic storage or transport changes. |
| A03 Injection | OK | EF LINQ and validator allowlists are used; no raw SQL or HTML injection sink was found in F0021 paths. |
| A04 Insecure Design | OK with recommendation | Redaction/correction audit is append-only. Broader API integration tests should prove policy behavior. |
| A05 Security Misconfiguration | OK with recommendation | DAST tooling was unavailable; local health fallback passed. Full ZAP baseline remains recommended. |
| A06 Vulnerable / Outdated Components | Issue | Dependency audit found existing vulnerable frontend/dev tooling and backend `Microsoft.OpenApi`. |
| A07 Identification & Authentication | OK | Endpoint group requires authentication; no authn mechanism changes. |
| A08 Software & Data Integrity | OK with recommendation | Hand-authored migration and legacy F0019 drift repair should receive final review. |
| A09 Security Logging & Monitoring | OK | Timeline events record mutation/audit evidence with actors. |
| A10 Server-Side Request Forgery | N/A | F0021 adds no outbound URL fetch or connector behavior. |

## Findings

No critical or high F0021 implementation security defects were found in the communication code itself.

Medium:

- [medium] Dependency scan found vulnerable packages, including backend `Microsoft.OpenApi 2.0.0` and frontend/dev-tooling advisories; triage and upgrade or formally accept residual risk before final security signoff - owner: Security Reviewer; follow-up: G5 signoff prerequisite.
- [medium] Official secret/SAST/DAST scanners were unavailable or blocked locally (`gitleaks`, `semgrep`, ZAP image), so fallback scans are not equivalent to full scanner coverage - owner: DevOps; follow-up: provision scanners before final closeout or record explicit waiver.
- [medium] API integration coverage should prove `communication_event` policy behavior for read/create/correct/redact/follow-up actions, including BrokerUser denial and Admin-only redaction - owner: Quality Engineer; follow-up: post-G3 hardening.

Low:

- [low] The frontend Admin-only redaction button mirrors server policy, but role visibility should not be treated as the primary security boundary - owner: Frontend Developer; follow-up: deferred-no-followup.

## Recommendation Disposition

- Dependency vulnerabilities: deferred to G5/security signoff decision because no new exploitable F0021 implementation flaw was found, but the dependency report must not be ignored.
- Scanner availability: deferred to DevOps/security hardening; fallback evidence is acceptable for G3 review only.
- API policy integration tests: deferred to post-G3 hardening before closeout.

## Recommendations

- [medium] Upgrade or explicitly risk-accept vulnerable dependencies before final security signoff, with special attention to `Microsoft.OpenApi` and frontend dev server/tooling advisories - owner: Security Reviewer; follow-up: G5 signoff prerequisite.
- [medium] Install or provide `gitleaks`, `semgrep`, and OWASP ZAP scanner access before closeout so full scanner artifacts can replace local fallback evidence - owner: DevOps; follow-up: before G8 closeout.
- [medium] Add API authorization integration tests for all F0021 communication actions and denied roles - owner: Quality Engineer; follow-up: post-G3 hardening.

## Result

PASS WITH RECOMMENDATIONS
