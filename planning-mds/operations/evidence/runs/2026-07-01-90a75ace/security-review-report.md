# Security Review Report — F0038 Neuron Day-at-a-Glance Shell

**Run:** `2026-07-01-90a75ace` · **Role:** Security Reviewer · **Stage:** G3 · **Reviewed:** 2026-07-02

**Result:** PASS

F0038 is security-sensitive (on-behalf-of token forwarding, a prompt-injection surface, a new
least-privilege Casbin action, and a telemetry PII boundary). I reviewed each seam against the
scan evidence and the source, and find no security defects. The design correctly keeps the
engine as the sole authorization authority. DAST is waived for this stage (see below).

## Security Surface Reviewed

### 1. On-behalf-of token forwarding (ADR-027)
Neuron forwards the caller's authentik bearer token to the engine and re-implements no
authorization; the engine enforces Casbin ABAC. Verified in `engine_client.py`: the token is
sent only in the `Authorization` header and is **never logged** (error messages carry
exception type / status code only — no token, no body). 401/403 → typed `UpstreamAuthError`;
≥500/transport → `UpstreamUnavailableError`; no 500/stack leak.

### 2. Unverified token decode (accepted design)
`auth.py` decodes the JWT payload **without signature verification** to derive a subject
(thread owner-scoping) and a best-effort persona (telemetry). This is explicitly **not** an
authorization input — verified by inspection: neither `subject_from_token` nor
`persona_from_token` gates data access; the engine (verified token + Casbin) is the sole
authority, and Neuron holds no authoritative business state. A forged `sub` can at most
mis-attribute a Neuron thread bucket; it cannot expose another user's CRM data. **Accepted.**

### 3. Prompt-injection scope guard (S0007)
`scope_guard.py` classifies injection markers **before** any CRM keyword (cannot smuggle
in-scope), fails safe to a redirect on classifier error, and grants **no** authorization —
the engine still authorizes every read. The guard is a UX/scope boundary layered on top of the
real authorization boundary. Sound defense-in-depth.

### 4. Telemetry PII boundary + identity (S0008)
The engine ingest DTO is a **closed shape** (no free-form payload) and the service enforces
`user_id == authenticated sub` (403 on mismatch), preventing cross-user attribution. Confirmed
by the passing `403 mismatch` integration test in the 491-suite. Neuron's emit is
fire-and-forget after the authoritative write and cannot break a user flow.

### 5. No model-generated markup execution (S0002)
The frontend renders only registered component identifiers with AJV-validated props; unknown
ids / invalid props fall back to a safe, non-executable state. No model output is rendered as
markup — closes the injection-to-XSS path.

## Scan Evidence (QE-run, real tooling)

- SAST — `artifacts/security/semgrep-sast.txt` (0 findings, 315 rules, 96 files).
- Secrets — `artifacts/security/secret-scan-gitleaks-history.txt` and `artifacts/security/secret-scan-gitleaks-worktree.txt` (0 leaks).
- Dependency SCA — `artifacts/security/dependency-sca-summary.md` (engine + neuron clean; frontend advisories pre-existing, not F0038-introduced).

## DAST Disposition

DAST is **waived** for this stage (recorded in `evidence-manifest.json` security_scans.dast).
Rationale: Neuron and the engine telemetry endpoint are internal, authenticated services;
F0038 adds no new unauthenticated dynamic attack surface (all `/v1/*` return 401 without a
forwarded token; ingest requires authorization). DAST (OWASP ZAP) belongs at the platform/CI
layer against a deployed environment. I ratify this waiver as Security Reviewer.

## Cross-reference

The uncommitted-source finding raised in `code-review-report.md` is a source-control-hygiene
issue, not a security defect — but the F0038 source must be committed before merge so the
reviewed artifact and the merged artifact are provably identical.

**Result:** PASS
