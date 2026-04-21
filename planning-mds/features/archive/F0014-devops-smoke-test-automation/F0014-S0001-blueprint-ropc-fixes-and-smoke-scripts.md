# F0014-S0001 — Blueprint ROPC Fixes and Smoke Test Scripts

**Story ID:** F0014-S0001
**Feature:** F0014 — DevOps Smoke Test Automation
**Title:** Blueprint ROPC fixes and smoke test scripts
**Priority:** Critical
**Phase:** Infrastructure

## User Story

**As a** DevOps engineer verifying a Nebula deployment
**I want** a corrected authentik blueprint and automated smoke test scripts
**So that** I can verify auth, API, and data integrity in one command instead of manually debugging ROPC failures and hand-crafting curl sequences

## Context & Background

During F0003 (Task Center API) DevOps verification, two authentik blueprint gaps were discovered:

1. The `OAuth2Provider` was missing `authentication_flow`, causing ROPC `password` grant to silently return `invalid_grant` — even with valid credentials. This was an F0005 oversight.
2. authentik 2026.2 ROPC requires `Token` objects with `intent=app_password`. The user's login password does not work for password grant. The blueprint had no token entries.

These issues made automated token acquisition impossible. Combined with the lack of any scripted smoke tests, each DevOps verification cycle took 30–60 minutes of manual work.

This story delivers three artifacts:
- **Blueprint fixes** to `docker/authentik/blueprints/nebula-dev.yaml`
- **`scripts/smoke-test.sh`** — 9-test API smoke suite
- **`scripts/dev-reset.sh`** — clean teardown + rebuild + smoke test workflow

## Acceptance Criteria

**Happy Path — Smoke Test:**
- **Given** the Nebula docker compose stack is running with a clean database
- **When** I run `./scripts/smoke-test.sh`
- **Then** the script acquires a JWT via ROPC for `lisa.wong`, runs 9 tests, and exits with code 0 if all pass

**Happy Path — Dev Reset:**
- **Given** the Nebula stack may or may not be running
- **When** I run `./scripts/dev-reset.sh`
- **Then** the script tears down all containers and volumes, rebuilds, waits for health, runs smoke tests, and exits with code 0 if all pass

**Happy Path — Custom User:**
- **Given** a running stack with seeded dev users
- **When** I run `./scripts/smoke-test.sh --user john.miller`
- **Then** the script acquires a JWT for `john.miller` (Underwriter) and runs the smoke suite as that user

**Alternative Flows / Edge Cases:**
- Service not ready within timeout → script exits with code 2 and prints timeout diagnostic
- ROPC token acquisition fails → script exits with code 2 with troubleshooting steps (blueprint check, user existence, token existence)
- Invalid workflow transition (Open → Done) → test expects HTTP 409 Conflict
- Deleted task GET → test expects HTTP 404

**Checklist:**
- [x] `OAuth2Provider` in blueprint includes `authentication_flow` reference
- [x] Blueprint provisions `authentik_core.token` entries with `intent: app_password` for lisa.wong, john.miller, broker001, akadmin
- [x] All dev tokens share key `nebula-dev-token`
- [x] `smoke-test.sh` tests: GET /my/tasks, POST /tasks, GET /tasks/{id}, PUT (Open→InProgress), PUT (InProgress→Done), PUT (Open→Done = 409), DELETE, GET deleted (404), timeline events (4 events)
- [x] `dev-reset.sh` performs: docker compose down -v, docker compose up -d --build, health polling, optional smoke test
- [x] Exit codes: 0 (pass), 1 (test failure), 2 (infra failure)
- [x] `--user`, `--reset`, `--skip-smoke`, `--api` flags work correctly

## Data Requirements

**Required Fields:**
- Task: `title` (string, required for POST /tasks), `assignedToUserId` (uuid, self-assignment)
- Token: ROPC `grant_type=password`, `client_id=nebula`, `username`, `password` (app-password key), `scope`

**Validation Rules:**
- ROPC `password` must be an app-password token key, not the user's login password
- JWT `aud` claim must equal `nebula` (matching `Authentication__Audience`)
- JWT must contain `nebula_roles` array with at least one role

## Role-Based Visibility

**Roles that can execute smoke tests:**
- Any developer or DevOps agent with shell access to the project directory

**Data Visibility:**
- Scripts access the database directly via `docker compose exec -T db psql` for timeline verification
- JWT claims are decoded and printed for diagnostic purposes (dev-only — no production credentials)

## Non-Functional Expectations

- Performance: Full smoke test suite completes in < 30 seconds against a warm stack
- Performance: Dev-reset (including rebuild) completes in < 5 minutes on a standard dev machine
- Reliability: Scripts use `set -euo pipefail` for fail-fast behavior
- Portability: Scripts require only `bash`, `curl`, `python3`, and `docker compose`

## Dependencies

**Depends On:**
- F0005 — authentik IdP infrastructure and blueprint format
- F0003 — Task entity CRUD endpoints (`/tasks`, `/my/tasks`)

**Related Stories:**
- F0014-S0002 — Multi-role smoke test verification (extends this story's single-user coverage)

## Out of Scope

- Multi-entity smoke coverage beyond Tasks (Brokers, Submissions, Renewals)
- CI/CD pipeline integration
- Production deployment verification
- Frontend smoke testing

## Questions & Assumptions

**Assumptions (validated):**
- Docker compose is the canonical local dev environment
- All dev users share `nebula-dev-token` as their app-password key
- PostgreSQL direct access is available for timeline event verification
- authentik health endpoint is `/-/health/live/` and API health endpoint is `/healthz`

## Business Rules

1. **ROPC app-password requirement:** authentik 2026.2+ password grant requires `Token` objects with `intent=app_password`. The `password` field must match the token's `key` field.
2. **Blueprint authentication_flow:** `OAuth2Provider` must reference `authentication_flow` for ROPC to produce tokens. Without it, `invalid_grant` is returned silently.
3. **Exit code contract:** 0 = pass, 1 = test failure, 2 = infra failure. `dev-reset.sh` propagates `smoke-test.sh` exit code.
4. **Timeline event integrity:** Every Task mutation produces an `ActivityTimelineEvent`. The smoke test verifies 4 events: `TaskCreated`, `TaskUpdated`, `TaskCompleted`, `TaskDeleted`.
5. **Soft delete semantics:** Deleted tasks return 404 on subsequent GET requests.

## Definition of Done

- [x] Acceptance criteria met
- [x] Edge cases handled
- [x] Permissions enforced (N/A — CLI tooling, no server-side auth changes)
- [x] Audit/timeline logged (verified by smoke test #9)
- [x] Tests pass (smoke-test.sh is the test)
- [x] Documentation updated (README.md documents usage)
- [x] Story filename matches `Story ID` prefix (`F0014-S0001-...`)
- [x] Story index regenerated if story file was added/renamed/moved
