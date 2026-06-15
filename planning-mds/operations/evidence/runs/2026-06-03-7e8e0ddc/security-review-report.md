---
template: security-review
version: 2.0
applies_to: security
---

# Security Review Report - F0019-submission-quoting-proposal-and-approval run 2026-06-03-7e8e0ddc

## Scope

- Feature ID: F0019
- Run ID: 2026-06-03-7e8e0ddc
- Date: 2026-06-03
- Reviewer: Security Reviewer Agent

## Reviewed Surfaces

- Permission-gated submission endpoints for quote packet update, approval, bind request/confirmation, archive, and reactivate.
- SubmissionService role and state guards.
- Archive/reactivate audit reason handling.
- Timeline event payloads for packet update, approval granted/declined, terminal transitions, archive, and reactivate.
- OpenAPI contract for If-Match, Idempotency-Key, and downstream mutation payloads.
- EF migration for new F0019 audit-bearing tables.
- Frontend controls that expose downstream actions by role/status/archive state.

## Threat Boundary

| Subject | Resource | Operation | Boundary |
|---------|----------|-----------|----------|
| Authenticated user with submission:read | Submission quote packet | Read packet | ABAC-scoped submission read |
| Underwriter/Admin with submission:update or submission:transition | Submission quote packet | Update/mark ready | Endpoint permission plus service role/status guard |
| Underwriter/Admin with submission:approve | Approval decision | Grant/decline | Endpoint permission plus service role/status guard |
| Underwriter/Admin with submission:transition | Bind handoff | Request/confirm bind | Endpoint permission plus service role/status/idempotency guard |
| Underwriter/Admin with submission:archive | Terminal submission | Archive/reactivate | Endpoint permission plus service terminal/archive guard |

## Auth / Authz

- Read path requires submission:read.
- Quote packet mutation requires submission:update or submission:transition at the endpoint, and service-level downstream management role Admin or Underwriter.
- Approval requires submission:approve and service-level Admin or Underwriter.
- Bind request/confirmation requires submission:transition and service-level Admin or Underwriter.
- Archive/reactivate requires submission:archive and service-level Admin or Underwriter.
- Mutations require If-Match rowVersion and return precondition_failed on stale versions.
- Repository list scope remains constrained by assignment, region, broker/program manager role, or Admin.

## Validation

- WorkflowTransitionRequestValidator requires reasonCode for Declined/Withdrawn and reasonDetail when reasonCode=Other.
- Approval decisions normalize to Granted or Declined and require reason.
- Archive/reactivate now reject blank reason before state changes or audit events.
- Bind transitions cannot be driven through the generic transition endpoint; callers must use bind handoff endpoints.
- Quote packet readiness requires linked documents, recorded premium, limits, deductibles, effective date, and carrier market before Quoted.

## Audit / Logging

- SubmissionPacketUpdated, SubmissionApprovalGranted, SubmissionApprovalDeclined, SubmissionArchived, and SubmissionReactivated events are persisted in ActivityTimelineEvents.
- SubmissionTransitioned payloads now include terminal reasonCode and reasonDetail for Declined/Withdrawn.
- Approval decision records preserve approver, reason, blocking conditions, and authority context.
- Bind handoff records preserve idempotency key, correlation ID, requested/completed timestamps, and payload snapshot.
- No secrets are written to timeline payloads or command artifacts.

## Secrets / Config

- No new secrets.
- No new environment variables.
- No new deployment config keys.
- Idempotency-Key is a client coordination token, not a credential.

## Scan Disposition

| Class | Ran | Result / Finding summary | Artifact or waiver reason |
|-------|-----|--------------------------|---------------------------|
| dependency | No | No dependency surface changed | artifacts/security/dependency-scan.txt |
| secrets | Yes | No secrets found; token matches were false positives | artifacts/security/secrets-scan.txt |
| sast | Yes | Manual authz/SAST review passed after two G3 fixes | artifacts/security/sast-authz-review.txt |
| dast | No | No deployed preview/live DAST target in this run | artifacts/security/dast-disposition.txt |

## OWASP Top 10 Coverage

| Category | Status | Notes |
|----------|--------|-------|
| A01 Broken Access Control | OK | Endpoint permissions plus service role/status guards reviewed. |
| A02 Cryptographic Failures | N/A | No cryptographic storage or transport changes. |
| A03 Injection | OK | EF LINQ queries and typed DTOs used; no raw SQL in runtime code. |
| A04 Insecure Design | OK | Bind and approval require explicit workflow checkpoints and audit records. |
| A05 Security Misconfiguration | OK | No new env/config settings. |
| A06 Vulnerable / Outdated Components | OK | No dependency changes. |
| A07 Identification & Authentication | OK | No authentication flow changes; existing current-user boundary reused. |
| A08 Software & Data Integrity | OK | Idempotency and optimistic concurrency guard state-changing operations. |
| A09 Security Logging & Monitoring | OK | Audit timeline events and approval/bind records added. |
| A10 Server-Side Request Forgery | N/A | No outbound network fetch or URL handling added. |

## Findings

- [medium] Archive/reactivate accepted blank audit reasons. Fixed before PASS by adding missing_reason validation and a focused unit test.
- [medium] approvalPending filter over-included non-pending quoted submissions. Fixed before PASS by requiring ReadyForApproval packet and no approval decisions.

## Recommendation Disposition

- No deferred recommendations.

## Result

Result: PASS

