# F0005 — Status

**Last Updated:** 2026-03-10
**Overall Status:** Done (Archived; Foundation Complete, Runtime Enforcement Deferred to F0009)

## Stories

| Story | Status | Blocker |
|-------|--------|---------|
| F0005-S0001 — Replace authentik infrastructure | ✅ Done | — |
| F0005-S0002 — Claims normalization + principal key | ✅ Done | — |
| F0005-S0003 — Frontend OIDC flow | ⚠️ Foundation Done; runtime token wiring deferred to F0009 | F0009 owns `/login`, `/auth/callback`, and OIDC token-first API flow |
| F0005-S0004 — Data model principal key rename | ✅ Done | — |

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Foundation migration required verification across identity, claims, and schema behavior. | Architect | 2026-02-14 |
| Code Reviewer | Yes | Independent review required for infra/auth migration baseline safety. | Architect | 2026-02-14 |
| Security Reviewer | Yes | IdP migration and claims normalization affect authentication and identity trust boundaries. | Architect | 2026-02-14 |
| DevOps | Yes | Runtime/infrastructure composition changed (`docker-compose`, authentik, env contracts). | Architect | 2026-02-14 |
| Architect | No | No unresolved architecture exceptions requiring explicit acceptance. | Architect | 2026-02-14 |

## Signoff Ledger (Execution Evidence)

| Role | Reviewer | Verdict | Evidence | Date | Notes |
|------|----------|---------|----------|------|-------|
| Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/JwtAudienceValidationTests.cs`; `engine/tests/Nebula.Tests/Integration/AuthEndpointTests.cs` | 2026-03-05 | Authentication baseline tests validated post-migration behavior. |
| Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0005-idp-migration/STATUS.md` | 2026-03-05 | Migration scope reviewed and blocking defects resolved before downstream feature work. |
| Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md`; `planning-mds/security/implementation-security-review.md` | 2026-03-05 | Identity/authorization controls reviewed against migration outputs. |
| DevOps | DevOps agent | PASS | `docker-compose.yml`; `docker/authentik/blueprints/nebula-dev.yaml`; `planning-mds/features/archive/F0005-idp-migration/GETTING-STARTED.md` | 2026-03-05 | Runtime stack changes and env contract verified for local deployment. |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0005-S0001 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/AuthEndpointTests.cs` | 2026-03-05 | Authentication stack baseline behavior validated after infra replacement. |
| F0005-S0001 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0005-idp-migration/STATUS.md` | 2026-03-05 | Story accepted in migration completion review. |
| F0005-S0001 | Security Reviewer | Security agent | PASS | `planning-mds/security/implementation-security-review.md` | 2026-03-05 | IdP replacement boundary controls reviewed. |
| F0005-S0001 | DevOps | DevOps agent | PASS | `docker-compose.yml`; `docker/authentik/blueprints/nebula-dev.yaml` | 2026-03-05 | Runtime services and bootstrap paths validated. |
| F0005-S0002 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/JwtAudienceValidationTests.cs` | 2026-03-05 | Claims normalization and audience validation behavior verified. |
| F0005-S0002 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0005-idp-migration/STATUS.md` | 2026-03-05 | Story accepted in migration completion review. |
| F0005-S0002 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-05 | Claims and authorization implications reviewed. |
| F0005-S0002 | DevOps | DevOps agent | PASS | `docker-compose.yml`; `planning-mds/features/archive/F0005-idp-migration/GETTING-STARTED.md` | 2026-03-05 | Environment contract updates validated for runtime startup. |
| F0005-S0003 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/AuthEndpointTests.cs` | 2026-03-05 | Frontend token-source compatibility verified against backend auth path. |
| F0005-S0003 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0005-idp-migration/STATUS.md` | 2026-03-05 | Story accepted in migration completion review. |
| F0005-S0003 | Security Reviewer | Security agent | PASS | `planning-mds/security/implementation-security-review.md` | 2026-03-05 | Dev-auth and OIDC migration security constraints reviewed. |
| F0005-S0003 | DevOps | DevOps agent | PASS | `docker-compose.yml`; `planning-mds/features/archive/F0005-idp-migration/GETTING-STARTED.md` | 2026-03-05 | Frontend runtime configuration guidance validated. |
| F0005-S0004 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/JwtAudienceValidationTests.cs` | 2026-03-05 | Principal key migration behavior validated against auth flows. |
| F0005-S0004 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0005-idp-migration/STATUS.md` | 2026-03-05 | Story accepted in migration completion review. |
| F0005-S0004 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-05 | Identity mapping implications reviewed for access controls. |
| F0005-S0004 | DevOps | DevOps agent | PASS | `docker-compose.yml`; `planning-mds/features/archive/F0005-idp-migration/GETTING-STARTED.md` | 2026-03-05 | Migration/deployment sequencing verified for runtime readiness. |

## Architecture Gate

| Check | Status |
|-------|--------|
| ADR-006 written and accepted | ✅ Done |
| BLUEPRINT.md updated | ✅ Done |
| SOLUTION-PATTERNS.md updated | ✅ Done |
| data-model.md updated | ✅ Done |
| docker-compose.yml updated | ✅ Done (authentik-server + authentik-worker; Redis removed) |
| dev-auth.ts updated | ✅ Done |
| engine/ domain entities updated | ✅ Done (F0005-S0004) |
| engine/ EF configurations updated | ✅ Done |
| engine/ application services updated | ✅ Done |
| engine/ HttpCurrentUserService updated | ✅ Done (nebula_roles claim, UserId DB lookup) |
| engine/ appsettings.Development.json updated | ✅ Done |
| EF migration created | ✅ Done (20260301000000_F0005_IdpPrincipalRefactor) |
| AppDbContextModelSnapshot.cs regenerated | ⚠️ Pending — run `dotnet ef migrations add` to regenerate |
| authentik blueprint fully working | ✅ Done (2026-03-02) — fixed invalidation_flow + redirect_uris for authentik 2026.2 |

## Notes

F0005 is a prerequisite for all backend implementation. All F0001/F0002 backend stories (Phase C) must not begin until F0005-S0004 (principal key rename) is complete. The production auth/session behavior and BrokerUser boundary enforcement remain explicitly in F0009 scope.

**Blueprint root-cause (2026-03-02):** authentik 2026.2.0 added two required fields to `OAuth2Provider` that older blueprints omit: `invalidation_flow` (FK to an invalidation flow) and `redirect_uris` (now a list of `{url, matching_mode}` objects). The blueprint importer's `apply()` returns False silently in this case — the error only surfaces via `capture_logs()` inside the worker task.
