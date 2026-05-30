---
template: security-review
version: 2.0
applies_to: security
---

# Security Review Report — F0036-dynamic-product-attribute-form-engine run 2026-05-28-077b7b30

> Required because Security Reviewer is `Required = Yes` in STATUS (risk basis: F0036 consumes the F0035 forced-re-auth path and writes form values into the per-user sessionStorage snapshot, which may transiently include `InternalOnly` fields the user was editing — ADR-024 boundary). `security_sensitive_scope = false` (no `**/Auth*/`, `**/Identity*/`, `**/Security*/`, or `**/Secrets/` path-class change).

## Scope

- Feature ID: F0036
- Run ID: 2026-05-28-077b7b30
- Date: 2026-05-30
- Reviewer: Security Reviewer (feature-action)

## Reviewed Surfaces

- F0035 form-state preservation snapshot path (`useRegisteredForm` → `snapshotDirtyForm`/`consumeFormSnapshot`), the data captured per form, and the per-user namespacing/TTL.
- Forced-re-auth trigger semantics (401 vs 403) — consumed from F0035, unchanged.
- The wired CRUD forms' values flowing into snapshots (PII surface).
- The additive `DirtyFormRegistryContext` export in `session-continuity/index.ts`.
- New dependency exposure (`react-hook-form`, `ajv`, `ajv-formats`, `ajv-errors`).
- No new endpoints, auth code paths, secrets handling, or audit events.

## Threat Boundary

| Subject | Resource | Operation | Boundary |
|---------|----------|-----------|----------|
| Authenticated user | own `sessionStorage` (`nebula.session-restore.v1.<userId>.<formKey>`) | write snapshot on forced re-auth; read on return | Per-user namespaced, browser-local, not sent to any server; 1h TTL; ≤ 256 KB; cleared on sign-out and on different-user sign-in (F0035). |

No new trust boundary is crossed. Snapshots never leave the browser; they are not `localStorage` (per-tab `sessionStorage`).

## Auth / Authz

Unchanged. No new permission keys, role gates, or tenant scoping. Each form's existing route/host authorization governs who can edit; preservation is not an authorization control. Forced re-auth triggers only on `401-auth-failed` (not `403`) per F0035 — F0036 does not alter this. The `DirtyFormRegistryContext` export is additive (no behavior change to F0035).

## Validation

Client AJV (`data-schema.json`) is advisory immediate-feedback only; the backend re-validates and is authoritative on submit (ADR-022). Cross-field rules are backend-authoritative via `lobErrors[]`. CRUD form validation/submit paths are unchanged (registration is render-side only — verified per-form). No validators were relaxed.

## Audit / Logging

No new timeline event classes. Critically, **no mutation is auto-replayed** (F0035 mandate, verified by S0006/S0008 tests): only an explicit user re-save emits the existing entity create/update event. No `mutation-auto-replayed` event exists. No secrets/tokens are logged; snapshots contain `form_values` only (no access/refresh tokens — verified: `FormSnapshotRecord` carries user_id/route/form_key/form_values/dirty_field_paths/timestamp).

## Secrets / Config

No secrets added or handled. Snapshots store form field values, not credentials. No tokens in snapshots (the F0035 record shape excludes them). No `.env`/config change.

## Scan Disposition

`security_sensitive_scope = false`, so the formal four-class scan block is **not mandated** for this run (per the contract, the `security_scans{}` requirement is gated on `security_sensitive_scope = true`). Disclosed explicitly (not silently omitted):

| Class | Ran | Result / Finding summary | Artifact or waiver reason |
|-------|-----|--------------------------|---------------------------|
| dependency | No | Not mandated (`security_sensitive_scope=false`). New deps (`react-hook-form@7.76.1`, `ajv@8.20.0`, `ajv-formats@3.0.1`, `ajv-errors@3.0.0`) are mainstream, widely-audited libraries. | Recommend inclusion in the repo's standard dependency scan (low; below). |
| secrets | No | Not mandated; no secrets added (manual review). | n/a |
| sast | No | Not mandated; manual review of the snapshot path performed. | n/a |
| dast | No | Not mandated (frontend-only; no new server surface). | n/a |

## OWASP Top 10 Coverage

| Category | Status | Notes |
|----------|--------|-------|
| A01 Broken Access Control | OK | No authz change; per-user snapshot isolation; cross-user discard verified. |
| A02 Cryptographic Failures | N/A | No crypto; sessionStorage is plaintext by design (browser-local, per-user, TTL). |
| A03 Injection | OK | AJV schema validation; no eval/SQL; React escaping. |
| A04 Insecure Design | OK | No-auto-replay invariant; fail-closed engine; accepted ADR-024 snapshot boundary. |
| A05 Security Misconfiguration | OK | No config change. |
| A06 Vulnerable / Outdated Components | OK (low) | 4 new mainstream deps; recommend dependency scan (below). |
| A07 Identification & Authentication | OK | Forced re-auth on `401-auth-failed` only; not altered. |
| A08 Software & Data Integrity | OK | No auto-replay; explicit re-save required; lockfile pinned. |
| A09 Security Logging & Monitoring | OK | No secrets in snapshots/logs; existing events only. |
| A10 SSRF | N/A | No server-side request surface. |

## Findings

- [low] The account forms (`CreateAccountPage`, `AccountDetailPage`) snapshot `taxId` (and address PII) to `sessionStorage` without using the `useControlledDirtyTracker` `sensitiveFieldPaths` exclusion hook — owner: Frontend/Security; follow-up: deferred-no-followup. **Within the accepted ADR-024 boundary** (the user is editing data they are already authorized to view; snapshot is per-user, browser-local, 1h TTL, sign-out-cleared), so non-blocking; a defense-in-depth improvement is to pass `sensitiveFieldPaths: ['taxId']` for the account forms.
- [low] Run the 4 new frontend dependencies through the repo's standard dependency scan in CI — owner: DevOps/Security; follow-up: deferred-no-followup.

No `high`/`critical` findings.

## Recommendation Disposition

- taxId/PII in snapshot: **accepted as residual** within the ADR-024 boundary; defense-in-depth (`sensitiveFieldPaths`) deferred as a non-blocking improvement.
- Dependency scan: **deferred** to CI; deps are mainstream and lockfile-pinned.

## Result

PASS
