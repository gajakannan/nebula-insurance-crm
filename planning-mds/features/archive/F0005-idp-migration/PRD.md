# F0005 — IdP Migration: Keycloak → authentik

**Feature ID:** F0005
**Phase:** Infrastructure Patch (pre-MVP, applied before backend implementation begins)
**Status:** Done (Archived; Foundation Complete, Runtime Enforcement Deferred to F0009)
**Owner:** Architect

---

## Overview

Replace Keycloak with **authentik** as the OpenID Connect identity provider for Nebula CRM. Introduce a stable **internal `UserId`** layer so the app is IdP-agnostic: no business entity stores a raw IdP subject (`sub`).

This is a pre-production patch. No production users, sessions, or data exist. All changes are to planning artifacts, infrastructure config, and backend data model design.

---

## Goals

1. Run authentik in the local Docker dev stack in place of Keycloak.
2. Implement a `ClaimsPrincipalNormalizer` that maps `(iss, sub)` → internal `UserId`.
3. Update all entity principal key fields from string IdP subjects to typed `uuid` (`UserId`).
4. Update the frontend OIDC flow to target authentik endpoints.
5. Keep Casbin policies, the authorization matrix, and the token storage strategy **unchanged**.

---

## Non-Goals

- No production IdP migration (no data exists to migrate).
- No SAML, LDAP, or SSO federation in MVP.
- No changes to Casbin model or policy rows.
- No changes to the authorization matrix.
- No changes to the token storage strategy (ADR-Auth-Token-Storage).

---

## Stories

| Story | Title | Owner |
|-------|-------|-------|
| F0005-S0001 | Replace authentik infrastructure (docker-compose + bootstrap) | DevOps |
| F0005-S0002 | Claims normalization layer + principal key (backend) | Backend Developer |
| F0005-S0003 | Frontend OIDC flow update | Frontend Developer |
| F0005-S0004 | Data model principal key rename | Backend Developer |

---

## Architecture Reference

See [ADR-006: authentik IdP Migration](../../architecture/decisions/ADR-006-authentik-idp-migration.md) for all design decisions, token shape changes, OIDC endpoint updates, and principal key architecture.

---

## Acceptance Criteria (Feature Level)

- [x] `docker-compose up` brings up authentik (server + worker) and the app without Keycloak.
- [x] An authenticated API request returns HTTP 200 with a JWT issued by authentik.
- [x] `UserProfile.UserId` is a stable UUID that does not change across token refreshes.
- [x] All entity `*Subject` / `AssignedTo` string fields are renamed to `*UserId (uuid)`.
- [x] Casbin enforcement passes for all roles defined in the authorization matrix.
- [x] Dev-auth helper (`dev-auth.ts`) successfully fetches a token from authentik.

Feature-level note:
- Runtime login/session enforcement and BrokerUser boundary behavior are carried by F0009.
