# F0014 — DevOps Smoke Test Automation — PRD

**Feature ID:** F0014
**Feature Name:** DevOps Smoke Test Automation
**Priority:** High
**Phase:** Infrastructure
**Status:** In Progress

## Feature Statement

**As a** DevOps engineer or developer
**I want** automated smoke tests and a one-command environment reset workflow
**So that** I can verify that the Nebula stack (API, auth, database, blueprints) works correctly from scratch without manually crafting curl commands or remembering multi-step procedures

## Business Objective

- **Goal:** Reduce DevOps verification time from 30+ minutes of manual curl/inspect work to < 5 minutes of automated execution
- **Metric:** Time-to-confidence after a clean deploy or feature merge
- **Baseline:** Manual curl commands, visual JSON inspection, ad-hoc `docker exec` debugging (discovered during F0003 verification)
- **Target:** Single-command verification (`./scripts/dev-reset.sh`) with pass/fail exit codes and structured output

## Problem Statement

- **Current State:** During F0003 (Task Center API) verification, four distinct pain points emerged:
  1. authentik blueprint gap — `OAuth2Provider` missing `authentication_flow` caused silent `invalid_grant` failures on ROPC
  2. authentik 2026.2 ROPC requires `Token` with `intent=app_password`, not login passwords — blueprint had no token entries
  3. No automated smoke test — each verification required hand-crafted curl commands and manual JSON inspection
  4. No clean-reset workflow — verifying from-scratch deployments required memorizing exact docker compose sequences and health-check polling
- **Desired State:** Automated, repeatable, scriptable verification of auth + API + data integrity after any environment change
- **Impact:** Each manual verification cycle burned 30–60 minutes of DevOps agent time; F0003 verification alone took multiple debugging sessions due to undocumented authentik ROPC requirements

## Scope & Boundaries

**In Scope (F0014 owns):**
- authentik blueprint corrections for dev ROPC authentication (authentication_flow, app-password tokens)
- `scripts/smoke-test.sh` — automated API smoke test suite with structured pass/fail output
- `scripts/dev-reset.sh` — clean teardown → rebuild → health-wait → smoke-test workflow
- Dev user credential documentation and ROPC usage patterns
- CLI argument handling (`--user`, `--reset`, `--skip-smoke`, `--api`)
- Service health-check polling (API `/healthz`, authentik `/-/health/live/`)
- Exit code contract (0 = pass, 1 = test failure, 2 = infra failure)

**Out of Scope (delegated):**
- authentik IdP configuration ownership → F0005 (IdP Migration)
- Task entity API behavior and business logic → F0003 (Task Center API)
- Task Center UI → F0004 (Task Center UI + Assignment)
- Frontend test infrastructure → F0015 (Frontend Quality Gates)
- Submission/Renewal API smoke coverage → Future (depends on F0006/F0007 implementation)
- CI/CD pipeline configuration and orchestration → Future
- Production deployment verification → Future (different credential and network model)
- Performance benchmarking or load testing → Out of scope (k6 per BLUEPRINT Section 2)
- Browser-based E2E testing → Out of scope (Playwright per F0015)

## Acceptance Criteria Overview

- [ ] `./scripts/smoke-test.sh` executes 9 API smoke tests against a running stack and reports pass/fail with structured output
- [ ] `./scripts/dev-reset.sh` performs clean teardown (volumes removed), rebuild, health-wait, and optional smoke test in one command
- [ ] authentik blueprint provisions `authentication_flow` on `OAuth2Provider` and `app_password` tokens for all dev users
- [ ] Smoke tests verify: JWT acquisition, Task CRUD, workflow transitions (valid + invalid), soft delete, and timeline event recording
- [ ] Scripts support `--user` flag to test as any seeded dev user (lisa.wong, john.miller, broker001, akadmin)
- [ ] Exit codes follow contract: 0 (pass), 1 (test failure), 2 (infra failure)
- [ ] Multi-role execution confirms role-specific access boundaries work correctly

## UX / Screens

F0014 is an infrastructure feature with no UI screens. User interaction is entirely CLI-based.

| Interface | Purpose | Key Actions |
|-----------|---------|-------------|
| Terminal (smoke-test.sh) | Run API smoke tests | `./scripts/smoke-test.sh [--user USER] [--reset] [--api URL]` |
| Terminal (dev-reset.sh) | Full environment reset | `./scripts/dev-reset.sh [--skip-smoke] [--user USER]` |
| Terminal output | Structured results | Pass/fail per test, summary line, exit code |

**Key Workflows:**
1. **Clean verification** — `./scripts/dev-reset.sh` → tears down stack → rebuilds → waits for health → runs smoke tests → reports results
2. **Quick smoke** — `./scripts/smoke-test.sh` → acquires JWT → runs 9 tests against running stack → reports results
3. **Role-specific verification** — `./scripts/smoke-test.sh --user john.miller` → tests as Underwriter role

## Data Requirements

**Core Entities (consumed, not owned):**
- Task: CRUD + workflow transitions (Open → InProgress → Done) — owned by F0003
- UserProfile: JWT-to-UserId resolution via `IdpSubject` lookup — owned by F0005
- ActivityTimelineEvent: Append-only audit trail — owned by core platform

**Validation Rules (F0014-specific):**
- JWT must contain `aud: "nebula"` claim matching `Authentication__Audience`
- JWT must contain `nebula_roles` claim with at least one role
- ROPC `password` field must be an app-password token key (`nebula-dev-token`), NOT the user's login password
- authentik `OAuth2Provider` must have `authentication_flow` set for ROPC grant to succeed

**Data Relationships:**
- Smoke test → authentik blueprint: depends on correct `OAuth2Provider` + `Token` provisioning
- Smoke test → Task API: depends on `/tasks` and `/my/tasks` endpoints being available
- Smoke test → UserProfile: depends on UserProfile upsert triggered by first authenticated request

## Role-Based Access

F0014 scripts run locally or in CI — no server-side authorization changes. The scripts test existing ABAC boundaries by executing as different users.

| Dev User | Casbin Role | Smoke Test Purpose |
|----------|-------------|-------------------|
| lisa.wong | DistributionUser | Primary smoke test user — full Task CRUD |
| john.miller | Underwriter | Verify Underwriter-scoped access |
| broker001 | BrokerUser | Verify external user boundaries (ExternalUser deny-all) |
| akadmin | Admin | Verify Admin-level access |

## Success Criteria

- DevOps verification of a clean environment takes < 5 minutes (single command)
- Smoke test failures produce actionable output (which test failed, HTTP status, response body)
- New developers can run `./scripts/dev-reset.sh` to verify their local setup works
- Smoke tests catch auth-layer regressions (blueprint drift, ROPC misconfiguration) before manual testing begins

## Risks & Assumptions

- **Risk:** Smoke tests are tightly coupled to Task API shape; changes to `/tasks` endpoints will break tests → Mitigation: smoke tests serve as regression canaries — breakage is the signal, not a bug
- **Risk:** authentik blueprint format changes across versions → Mitigation: blueprint is version-pinned in docker-compose
- **Assumption:** Docker compose is the canonical local dev environment
- **Assumption:** All dev users share the same app-password token key (`nebula-dev-token`) — acceptable for dev, not for production
- **Assumption:** PostgreSQL direct access (`docker compose exec -T db psql`) is available for timeline event verification

## Dependencies

- **F0005** (IdP Migration) — authentik infrastructure and blueprint format
- **F0003** (Task Center API) — Task entity endpoints exercised by smoke tests
- **Docker Compose** — all scripts assume `docker compose` CLI availability

## Related Stories

Stories are colocated in this feature folder as `F0014-S{NNNN}-{slug}.md`.

- F0014-S0001 — Blueprint ROPC fixes and smoke test scripts (MVP — Done)
- F0014-S0002 — Multi-role smoke test verification (MVP)
- F0014-S0003 — CI smoke test integration (Future)

## Business Rules

1. **ROPC app-password requirement:** authentik 2026.2+ password grant requires `Token` objects with `intent=app_password`. The `password` field in the ROPC request must match the token's `key`, not the user's login password. This is an authentik-specific behavior discovered during F0003 verification.
2. **Blueprint provisioning dependency:** `OAuth2Provider` must include `authentication_flow` (FK to the default authentication flow) for ROPC to succeed. Without it, token requests silently return `invalid_grant`.
3. **Exit code contract:** Scripts must follow `0 = all tests passed`, `1 = test failure`, `2 = infra/setup failure`. Callers (CI, dev-reset) depend on this contract.
4. **Timeline event integrity:** Every Task mutation must produce a corresponding `ActivityTimelineEvent`. Smoke tests verify this by querying the database directly after the CRUD sequence.
5. **Soft delete semantics:** Deleted tasks return 404 on subsequent GET. Smoke tests verify this behavior explicitly.
