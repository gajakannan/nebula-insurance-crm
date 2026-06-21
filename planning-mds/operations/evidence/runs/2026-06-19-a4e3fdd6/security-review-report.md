# Security Review Report — F0023 run 2026-06-19-a4e3fdd6

**Role:** Security Reviewer · **Date:** 2026-06-20 · **Assessment:** PASS

## Scope

F0023 read-side SearchReporting: global search, personal/team saved views, operational reports. Security-sensitive because search/reporting cross object-visibility boundaries; counts, facets, snippets, suggestions, saved-view metadata, and reports must not leak unauthorized records.

## Reviewed Surfaces

`/search-results`, `/saved-views` (CRUD + default), `/operational-reports/{workload,workflow-aging}`; `SearchService`, `SavedViewService`, `OperationalReportService`, repositories + visibility predicate; Casbin policy rows (`global_search`, `saved_view`, `operational_report`).

## Threat Boundary

Source modules remain authoritative for record access. Projections store denormalized owner/region keys only; saved views store criteria only (never grant source access). The query layer applies the source-visibility predicate before any row/count/facet/drilldown materializes.

## Auth / Authz

- Endpoints enforce Casbin (`global_search:read`, `saved_view:read|manage|default`, `operational_report:read`) over caller roles → 401/403 verified by smoke.
- `SavedViewService` adds fine-grained checks: personal = owner match; team = manager/admin + administered `teamScopeType/teamScopeKey`; non-owner personal read returns 404 (no existence leak). Unit-verified.
- Search/report visibility: broad roles see all; scoped roles restricted to owner-matched or in-region rows (`ProjectionVisibilityResolver`). Counts/facets computed post-filter (Critical-risk mitigation). Unit-verified.

## Validation

Server-side: query length ≥2, bounded filters/pagination (FluentValidation); saved-view criteria must be a JSON object (422 on violation); team scope required/validated. `If-Match`/RowVersion concurrency (412 on stale).

## Audit / Logging

Every saved-view mutation writes an immutable `SavedViewAuditEvent` (Created/Updated/DefaultChanged/Archived) with actor + before/after, in the same transaction.

## Secrets / Config

No hardcoded secrets in F0023 code (manual diff review + commands.log secret-pattern validator). Casbin runtime policy is the existing embedded resource; F0023 rows present.

## Scan Disposition

- **Dependency:** RAN. Backend `dotnet list package --vulnerable` → 0 vulnerable (artifacts/security/dependency-dotnet.txt). Frontend `pnpm audit` → 9 advisories (artifacts/security/dependency-pnpm.txt). All 9 are in PRE-EXISTING platform dependencies (react-router, fast-uri); F0023 added no new dependencies and none sit in F0023-authored code paths. Disposition: deferred platform follow-up (upgrade react-router ≥7.15.1) — accepted by PM at G8, not an F0023 finding.
- **Secrets / SAST / DAST:** NOT RUN — gitleaks / semgrep / OWASP ZAP unavailable in this environment (waivers recorded in manifest `security_scans{}`). Compensating: manual diff review (no secrets), .NET nullable + analyzers clean, manual auth/validation review above. (Contract effective 2026-05-19: scan rules advisory.)

## OWASP Top 10 Coverage

| Risk | Disposition |
|------|-------------|
| A01 Broken Access Control | Casbin + owner/team-scope checks + visibility predicate before counts; 404 on non-owner | 
| A02 Cryptographic Failures | No new secrets/crypto surface |
| A03 Injection | EF parameterized queries; ILIKE via `EF.Functions.ILike` (no string SQL); criteria parsed as JSON |
| A04 Insecure Design | Read-side projection; source remains authoritative; audited mutations |
| A05 Security Misconfiguration | No new config/exposed endpoints; auth required on all routes |
| A07 Identity/Auth Failures | Existing authentik/JWT; endpoints `RequireAuthorization` |
| A08 Data Integrity | RowVersion optimistic concurrency + audit trail |
| A09 Logging | SavedViewAuditEvent + projection metadata (`indexedAt`/`generatedAt`) |
| A10 SSRF | N/A (no outbound fetch from user input) |

## Findings

- **Critical (F0023):** none.
- **High (F0023):** none.
- **Medium/Low (F0023):** none introduced. Pre-existing platform dependency advisories noted above (out of F0023 scope).

## Recommendation Disposition

No blocking recommendations. Pre-existing platform dependency upgrades and deferred DAST (in a deployed authenticated env) are tracked as deferred follow-ups in pm-closeout, accepted by PM at G8.

## Result

`PASS` — F0023 introduces no critical/high security findings; access boundaries enforced and audited. Pre-existing platform dependency advisories accepted with a tracked follow-up.
