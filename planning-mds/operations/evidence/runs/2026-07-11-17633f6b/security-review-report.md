# Security Review Report

Scope: PR #56 — F0037 Hierarchy-Aware Access Scoping & Distribution Rollups (standalone, path-set)
Date: 2026-07-11
Diff range: `e2f78be...27a5162`
Required because: `security_sensitive_scope = true` (authorization / policy enforcement / access-scoping
changed) — Security Reviewer is forced-required per §7.

## Summary

- Assessment: **PASS**
- Vulnerabilities found: 0 critical / 0 high / 0 medium / 1 low
- Risk level: **Low**

F0037's central security property — resolve one distribution scope and apply it as a repository predicate
*before* any rows, counts, facets, drilldowns, or rollup totals are materialized — is implemented soundly
and fails closed. Coarse authorization is enforced at every endpoint (authentication + per-permission
Casbin check + rate limiting) and fine-grained row scoping is enforced in the data layer. External and
Broker users are denied by two independent controls (policy has no rows for them, and the scope service
short-circuits `ExternalRoles` to an empty, `HasScope=false` visibility). The requested-scope path
narrows via set-intersection against the caller's authority and returns a deny-all visibility on any
out-of-authority element. I found no over-permissioning / leak path in the reviewed surface.

Note on the code review's High findings: CR-H1 (AND vs union) and CR-H2 (DB-predicate test depth) are
**under-return / test-adequacy** issues. They do not weaken confidentiality — CR-H1 hides more, not less —
so they do not change this Security verdict, though CR-H2's missing DB-level proof is echoed below as a
detection/assurance gap.

## OWASP Top 10 Assessment

### 1. A01 Broken Access Control
- Status: **PASS**
- Findings: Endpoints use `.RequireAuthorization()` plus explicit per-permission checks —
  `distribution_rollup:read` (`OperationalReportEndpoints.DistributionRollups`), `operational_report:read`
  (`BuildQuery`), `global_search:read` (`SearchEndpoints`), and role-based `territory`/`distribution_node`
  checks (`Territory`/`Distribution` endpoints). Row-level scope is resolved by
  `IDistributionScopeService.ResolveAsync` before materialization in all four read paths (search, workload/
  aging, rollups, broker insights). Deny-by-default: `!visibility.HasScope → q.Where(x => false)` in every
  projection repository. Requested-scope narrowing (`DistributionScopeService.IntersectOrFail`) fails
  closed — requesting a `rootNodeId`/`territoryId`/`producerUserId` outside the caller's authority returns
  an empty `Empty(...)` visibility (`requested_scope_outside_authority`), verified by
  `DistributionScopeServiceTests.ResolveAsync_ManagerRequestOutsideAuthorityFailsClosed` and
  `CanReadBrokerAsync_ReturnsFalseForHiddenSibling`. Direct reads return no-leak `404`
  (`TerritoryEndpoints`/`DistributionEndpoints` return `NotFound` when `CanRead*` is false), matching the
  S0002 "no-leak not-found" requirement.

### 2. A02 Cryptographic Failures
- Status: **PASS** (not in scope) — no secrets, crypto, or transport config changed. The e2e dev token
  (`devToken`, literal `.dev` signature) is test-only fixture code, not a production auth path.

### 3. A03 Injection
- Status: **PASS**
- Findings: All data access is EF Core with parameterized LINQ. Free-text search uses
  `EF.Functions.ILike(d.SearchText, $"%{text}%")` — the interpolated value is passed as a **parameter** by
  the EF `ILike` translator, not concatenated into SQL. Sort is a whitelisted `switch`
  (`"title"`/default), and `groupBy`/`metricFamily` are normalized to fixed allow-lists in both the
  validator (`DistributionRollupQueryValidator`) and `OperationalReportService.Normalize*`. No raw SQL,
  string-built predicates, or dynamic column/table names.

### 4. A04 Insecure Design
- Status: **PASS**
- Findings: Deny-by-default, fail-closed on unknown/out-of-authority scope, single scope-resolution
  chokepoint reused across surfaces (good "no silent risk" / defense-in-depth posture). One design note
  (not a vuln): the same predicate is authored three times (the two projection repos + `ApplyVisibility`);
  centralizing it would reduce the chance of one copy drifting into a leak (ties to CR-H2).

### 5. A05 Security Misconfiguration
- Status: **PASS**
- Findings: The new `distribution_rollup:read` rows land in `planning-mds/security/policies/policy.csv`,
  which `Nebula.Infrastructure.csproj` embeds via `<EmbeddedResource ... Link="Authorization/policy.csv"/>`
  — the same file the Casbin enforcer loads at runtime, so there is **no** split-brain between the edited
  policy and the enforced policy. Grants are least-privilege: `DistributionManager`, `RelationshipManager`,
  `ProgramManager`, `Admin` only; `BrokerUser`/`ExternalUser` have no rows (implicit deny). Verified by
  `CasbinAuthorizationServiceTests.DistributionRollupRead_MatchesF0037PolicyCsv` (8-role allow/deny matrix).

### 6. A06 Vulnerable and Outdated Components
- Status: **PASS** (not in scope) — no dependency/package manifest changes in the reviewed path-set.

### 7. A07 Identification and Authentication Failures
- Status: **PASS** — no authN changes; all new endpoints sit behind `.RequireAuthorization()`.

### 8. A08 Software and Data Integrity Failures
- Status: **PASS** — read-only feature; no deserialization of untrusted data beyond
  `JsonSerializer.Deserialize<List<string>>` of an internally-projected hints column, wrapped in a
  `try/catch (JsonException)` that fails safe to `[]`.

### 9. A09 Security Logging and Monitoring Failures
- Status: **PASS (with a low hardening note)** — see SEC-L1. Reads are not audited (consistent with the
  rest of the platform), and internal `ExplanationCodes` are **not** exposed in any response DTO (the
  rollup DTO echoes only the requested `rootNodeId`/`territoryId`/`producerUserId`), so scope diagnostics
  do not leak to clients — good.

### 10. A10 Server-Side Request Forgery (SSRF)
- Status: **PASS** (not applicable) — no outbound requests constructed from user input.

## Vulnerability Findings

### Critical / High / Medium
None.

### Low (best-practice recommendations)

**SEC-L1 — No security signal on scope-denied access to sensitive distribution reports.**
- Location: scope-denial paths (`DistributionScopeService.Empty(...)`; `!HasScope` branches in the projection
  repositories; `CanRead*` → `404` in Territory/Distribution endpoints).
- What: A user who repeatedly requests `rootNodeId`/`territoryId`/`producerUserId` values outside their
  authority is silently returned empty/404 with no audit or monitoring event.
- Why it matters: This is correct for confidentiality, but it removes a useful detection signal for scope
  probing / enumeration attempts against channel data.
- Remediation (optional): Emit a structured, non-PII security event (role, resource, `requested_scope_outside_authority`)
  on scope denial for the sensitive report/search surfaces, without exposing hidden-record existence to the caller.
  Owner: backend/security; target: backlog.

## Authorization Review
- [x] ABAC implementation correct (Casbin enforcer; policy embedded from the single source of truth).
- [x] All new endpoints protected (auth + per-permission check).
- [x] Per-endpoint authorization enforced (coarse permission) **and** per-row scope enforced (fine-grained).
- [x] Server-side enforcement only (predicates run in repositories, before pagination/counts/facets).
- [x] No client-side trust (frontend passes filter ids; server intersects them against authority).

## Audit & Compliance
- [x] Sensitive data protected — scope applied before materialization; hidden records excluded from rows/
      counts/facets/drilldowns/totals.
- [x] Internal scope reasoning not leaked to clients.
- [~] Security event on scope denial — not emitted (SEC-L1, optional).

## Secrets Management
- [x] No hardcoded secrets introduced.
- [x] No secrets in the reviewed diff or in `policy.csv`.

## Assurance gap (echo of CR-H2)
The no-leak predicate is enforced by EF `.Where` chains in `DistributionScopeRepository` and the three
projection repositories, but no test exercises those predicates against a real database (unit tests mock
the repos; the e2e proves only the guaranteed-empty case). Confidentiality is sound **as written**, but a
future regression in a repository predicate would not be caught by the current suite. Recommend the CR-H2
integration test as the durable guard for this security property.

## Recommendation

**PASS.** No critical, high, or medium security findings. One low, optional hardening item (SEC-L1). The
access-control design is deny-by-default and fails closed across the reviewed read paths. Recommend
adopting the CR-H2 DB-level integration test so the no-leak property is regression-protected, and
tracking SEC-L1 in the backlog.

## Remediation Plan
1. [low] Add a scope-denial security event for sensitive report/search surfaces (SEC-L1). — owner: backend/security; target: backlog.
2. [assurance] Land the CR-H2 integration test to regression-guard the no-leak predicates. — owner: QE/backend; target: F0037 fast-follow.

---

## Re-Review Addendum — Cycle 2 (post-fix)

Date: 2026-07-11 · Trigger: R2 "fix all high"

The CR-H1 fix changes the default-view predicate from intersection to an **authority union**
(`owner OR region OR authorized-broker OR authorized-producer`). Reviewed for leak-safety:

- The change is a **strict superset within authority** of the previous predicate: it only adds rows whose
  `BrokerId` is in the caller's authorized broker set (managed / owned / region-assigned brokers) or whose
  owner is the caller. Every added row was already authorized; no cross-scope row becomes visible.
- **Territory was intentionally excluded** from the union. Because `TerritoryIds` are derived from broker
  authority, OR-ing them would have exposed sibling brokers that merely share a territory — that specific
  leak was identified and avoided. Territory continues to only *narrow* explicit requests.
- Deny-by-default (`!HasScope → Where(false)`), external-role short-circuit, and the fail-closed
  requested-scope intersection are unchanged. The new `ExplicitScopeReadScopingTests` (CI) assert
  sibling-exclusion and fail-closed behavior against the real predicates.

**Security verdict unchanged: PASS.** The assurance gap (echo of CR-H2) is now backed by an authored
DB-level integration test (executes in CI). SEC-L1 remains an optional Low.

---

## Re-Review Addendum — Cycle 3 (post-approval low cleanup)

Date: 2026-07-11 · Trigger: R2 "approve + fix issues anyway"

- **SEC-L1 — FIXED.** `DistributionScopeService` now emits a structured `LogWarning` when an explicit
  request resolves outside the caller's authority (`requested_scope_outside_authority`), giving a
  server-side detection signal for scope probing. The log records `UserId`, roles, and which dimensions were
  requested (booleans) — it does **not** disclose hidden-record existence to the caller (the response is
  still a no-leak empty scope). Reviewed: no sensitive record identifiers or PII are logged.
- **CR-L3 fix reviewed for leak-safety.** Separating authority-union from the requested-narrowing sets does
  not broaden confidentiality: the authority union is unchanged (owner/region/authorized-broker/authorized-
  producer, territory still excluded), and `Requested*` only ANDs a narrowing filter clamped to authority by
  the unchanged fail-closed `IntersectOrFail`. Requesting outside authority still fails closed.

**Security verdict: PASS.** No new findings; SEC-L1 closed.
