# Feature Assembly Plan — F0014: DevOps Smoke Test Automation

**Created:** 2026-03-27
**Author:** Architect Agent
**Status:** Approved

> **Purpose:** Implementation execution plan for F0014 stories S0002 (multi-role smoke test verification) and S0003 (CI smoke test integration). F0014 is an infrastructure/DevOps tooling feature — no application entities, API endpoints, workflow state machines, or Casbin policy changes. All work is in shell scripts and CI workflow configuration.

## Overview

F0014 automates DevOps verification of the Nebula stack. S0001 (Done) delivered the foundational `smoke-test.sh` (9-test suite) and `dev-reset.sh` (clean teardown workflow). This plan covers the remaining stories:

- **S0002** — Extend `smoke-test.sh` with `--all-users` mode that runs role-appropriate assertions for all four seeded dev users, verifying ABAC boundaries end-to-end.
- **S0003** (Future) — Create a GitHub Actions workflow that starts the full docker compose stack in CI and runs the multi-role smoke test as a merge gate.

## Architecture Review Findings

### CRITICAL: S0002 BrokerUser Access Expectation is Incorrect

The S0002 acceptance criteria states:

> **Given** a JWT for broker001 (BrokerUser / ExternalUser)
> **When** the script attempts `POST /tasks` and `GET /my/tasks`
> **Then** the API returns 403 Forbidden for both requests (ExternalUser deny-all Casbin policy)

**This is wrong.** BrokerUser is NOT ExternalUser. BrokerUser has explicit ALLOW policies in `policy.csv` (§2.10):

```
p, BrokerUser, broker,            read,   true
p, BrokerUser, broker,            search, true
p, BrokerUser, contact,           read,   true
p, BrokerUser, dashboard_nudge,   read,   true
p, BrokerUser, timeline_event,    read,   true
p, BrokerUser, task,              read,   true    ← ALLOWS task read
```

**Correct expected behavior for broker001 (BrokerUser):**

| Operation | Expected | Reason |
|-----------|----------|--------|
| `GET /my/tasks` | **200 OK** | `task:read` ALLOW (policy.csv line 382) |
| `POST /tasks` | **403 Forbidden** | No `task:create` policy for BrokerUser |
| `PUT /tasks/{id}` | **403 Forbidden** | No `task:update` policy for BrokerUser |
| `DELETE /tasks/{id}` | **403 Forbidden** | No `task:delete` policy for BrokerUser |

The S0002 story conflates BrokerUser (which has limited read access) with ExternalUser (which has no policy lines and thus implicit deny-all). The implementation must use the corrected expectations above.

### No New Architecture Artifacts Required

F0014 does not introduce:
- New entities or data model changes → No ERD update
- New API endpoints → No OpenAPI spec changes
- New workflow state machines → No state machine diagrams
- New Casbin policies → No policy.csv changes
- New JSON schemas → No schema files

All architecture artifacts (C4 diagrams, ERD, OpenAPI, Casbin policy) remain unchanged.

---

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Script enhancement: `--all-users` mode | S0002 | Extends existing `smoke-test.sh` with multi-role iteration and per-role assertion matrix. Must be done before CI integration. |
| 2 | CI workflow: GitHub Actions smoke test | S0003 (Future) | Depends on S0002 `--all-users` mode. Creates merge gate workflow. |

## Existing Code (Must Be Modified)

| File | Current State | F0014 Change |
|------|---------------|--------------|
| `scripts/smoke-test.sh` | 9-test single-user suite, 271 lines. CLI: `--reset`, `--user`, `--api`, `--help`. | **Expand** — Add `--all-users` flag, per-role expected behavior matrix, role-appropriate assertion routing, unified multi-user summary. |
| `planning-mds/features/F0014-devops-smoke-test-automation/README.md` | Feature overview, story table. | **Expand** — Add architecture notes section. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `.github/workflows/smoke-test.yml` | CI/CD | GitHub Actions workflow for S0003 — starts docker compose stack, runs `--all-users` smoke test, reports as GitHub check. |

---

## Step 1 — Multi-Role Smoke Test Verification (S0002)

### Modified Files

| File | Change |
|------|--------|
| `scripts/smoke-test.sh` | Add `--all-users` flag, role expectation matrix, per-role test routing, continue-on-failure per user, unified summary. |
| `planning-mds/features/F0014-devops-smoke-test-automation/README.md` | Add architecture notes and `--all-users` usage. |

### Implementation Specification

#### 1. CLI Argument Addition

Add to the existing `while` loop in arg parsing (after line 43):

```bash
--all-users)  ALL_USERS=true; shift ;;
```

Initialize before the loop:
```bash
ALL_USERS=false
```

When `--all-users` is true, `--user` is ignored (precedence rule from S0002 AC).

#### 2. Role Expectation Matrix

Define as an associative array (bash 4+) or parallel arrays near the configuration block:

```bash
# ── Per-Role Expected Behavior ──────────────────────────────────────────
# Format: username:expected_role:task_crud_allowed
# task_crud_allowed: "full" = all 9 tests, "read_only" = GET succeeds, CUD returns 403
ROLE_MATRIX=(
  "lisa.wong:DistributionUser:full"
  "john.miller:Underwriter:full"
  "broker001:BrokerUser:read_only"
  "akadmin:Admin:full"
)
```

#### 3. Per-User JWT Claims Verification

For each user, after acquiring the JWT, verify:
1. `aud` equals `"nebula"` — fail if mismatch
2. `nebula_roles` contains the expected role — fail if missing
3. `sub` is a non-empty string — fail if empty

```bash
verify_claims() {
  local token="$1" expected_role="$2"
  local claims
  claims=$(python3 -c "
import json, base64, sys
parts = sys.argv[1].split('.')
payload = parts[1] + '=' * (4 - len(parts[1]) % 4)
d = json.loads(base64.urlsafe_b64decode(payload))
aud = d.get('aud', '')
roles = d.get('nebula_roles', [])
sub = d.get('sub', '')
ok = 1 if (aud == 'nebula' and '$expected_role' in roles and sub) else 0
print(json.dumps({'aud': aud, 'roles': roles, 'sub': sub, 'ok': ok}))
" "$token")
  echo "$claims"
}
```

#### 4. Conditional Test Routing

For users with `task_crud_allowed=full`: run the existing 9-test suite as-is (refactored into a function `run_full_crud_suite`).

For users with `task_crud_allowed=read_only` (broker001/BrokerUser): run a restricted assertion set:

```bash
run_read_only_suite() {
  # Test 1: GET /my/tasks → expect 200
  # Test 2: POST /tasks → expect 403
  # Test 3: PUT /tasks/{any-id} → expect 403 (use a placeholder or skip if no task exists)
  # Test 4: DELETE /tasks/{any-id} → expect 403
}
```

**BrokerUser read-only test details:**

| # | Operation | Expected Status | Assertion |
|---|-----------|-----------------|-----------|
| 1 | `GET /my/tasks?limit=1` | 200 OK | `totalCount` is integer (may be 0) |
| 2 | `POST /tasks` (create) | 403 Forbidden | Body contains ProblemDetails with `code` |
| 3 | `PUT /tasks/{fake-uuid}` (update) | 403 Forbidden | 403 before 404 — Casbin denies before entity lookup |
| 4 | `DELETE /tasks/{fake-uuid}` | 403 Forbidden | Same rationale |

For the 403 tests, use a well-formed but nonexistent UUID (`00000000-0000-0000-0000-000000000000`). The Casbin enforcer evaluates policy before entity lookup, so the response is 403, not 404.

#### 5. Continue-on-Failure Semantics

Each user's test run is independent. If one user fails:
- Record the failure (user name + which tests failed)
- Continue with the next user
- Do **not** abort early (unlike `set -e` behavior for infra failures)

Implementation: wrap each user's test run in a subshell or use `|| true` with explicit tracking:

```bash
USER_RESULTS=()  # "username:passed:failed"

for entry in "${ROLE_MATRIX[@]}"; do
  IFS=: read -r username expected_role crud_mode <<< "$entry"
  # ... acquire token, verify claims, run suite ...
  USER_RESULTS+=("$username:$user_passed:$user_failed")
done
```

#### 6. Unified Summary Output

After all users complete:

```
════════════════════════════════════════════════════════════════
  MULTI-ROLE SMOKE TEST RESULTS
  ────────────────────────────────────────────────────────────
  lisa.wong (DistributionUser):  9/9 passed
  john.miller (Underwriter):    9/9 passed
  broker001 (BrokerUser):       4/4 passed
  akadmin (Admin):              9/9 passed
  ────────────────────────────────────────────────────────────
  TOTAL: 31/31 passed, 0 failed
  $(date -u +%Y-%m-%dT%H:%M:%SZ)
════════════════════════════════════════════════════════════════
```

Exit code: 0 only if ALL users pass ALL role-appropriate assertions. Otherwise 1.

#### 7. Refactoring Existing Code

The existing tests (lines 156–257) must be extracted into a `run_full_crud_suite()` function that:
- Accepts `$TOKEN`, `$AUTH`, `$USER_ID`, `$TEST_USER` as parameters or globals
- Returns pass/fail counts via global counters (already `PASSED`/`FAILED`/`TOTAL`)
- Resets counters per-user in `--all-users` mode, accumulates into global totals

The single-user path (`--all-users=false`) continues to work exactly as before — no behavioral change for existing usage.

### NFRs

- **Performance:** Multi-role suite completes in < 2 minutes against a warm stack (4 users × ~25s each)
- **Portability:** No new dependencies — still just `bash`, `curl`, `python3`, `docker compose`
- **Backward compatibility:** Running `smoke-test.sh` without `--all-users` behaves identically to S0001

### Integration Checkpoint — After Step 1

- [ ] `./scripts/smoke-test.sh --all-users` runs all 4 users and prints unified summary
- [ ] `./scripts/smoke-test.sh` (no flags) still works as single-user mode — no regression
- [ ] `./scripts/smoke-test.sh --user john.miller` still works — no regression
- [ ] broker001 gets 200 on `GET /my/tasks` and 403 on POST/PUT/DELETE
- [ ] Claims verification catches `aud`, `nebula_roles`, and `sub` for each user
- [ ] One user failure does not abort remaining users
- [ ] Exit code is 0 only when all users pass all assertions
- [ ] `./scripts/dev-reset.sh` still works (does not use `--all-users` by default)

---

## Step 2 — CI Smoke Test Integration (S0003 — Future)

### New Files

| File | Layer |
|------|-------|
| `.github/workflows/smoke-test.yml` | CI/CD |

### Implementation Specification

#### 1. Workflow File Structure

```yaml
name: Smoke Test (Full Stack)

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  workflow_dispatch:

# Cancel older runs on same PR/branch (same pattern as dotnet-test.yml)
concurrency:
  group: ${{ github.workflow }}-${{ github.event.pull_request.number || github.ref }}
  cancel-in-progress: true

jobs:
  smoke-test:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - uses: actions/checkout@v4

      - name: Start Nebula stack
        run: |
          docker compose up -d --build
        env:
          COMPOSE_PROJECT_NAME: nebula-ci-${{ github.run_id }}

      - name: Wait for services
        run: |
          # Reuse the health-check polling from smoke-test.sh
          # API healthz + authentik health + blueprint application delay
          ./scripts/smoke-test.sh --help > /dev/null  # validate script exists
          echo "Waiting for API..."
          for i in $(seq 1 45); do
            curl -sf -o /dev/null http://localhost:8080/healthz 2>/dev/null && break
            sleep 2
          done
          echo "Waiting for authentik..."
          for i in $(seq 1 30); do
            curl -sf -o /dev/null http://localhost:9000/-/health/live/ 2>/dev/null && break
            sleep 2
          done
          # Blueprint application delay
          sleep 5

      - name: Run multi-role smoke tests
        run: ./scripts/smoke-test.sh --all-users

      - name: Collect service logs on failure
        if: failure()
        run: |
          docker compose logs --tail 50 2>&1 > /tmp/compose-logs.txt
        env:
          COMPOSE_PROJECT_NAME: nebula-ci-${{ github.run_id }}

      - name: Upload failure logs
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: smoke-test-failure-logs
          path: /tmp/compose-logs.txt

      - name: Tear down stack
        if: always()
        run: docker compose down -v
        env:
          COMPOSE_PROJECT_NAME: nebula-ci-${{ github.run_id }}
```

#### 2. CI Runner Requirements

| Resource | Requirement | Rationale |
|----------|-------------|-----------|
| RAM | >= 8 GB | PostgreSQL + authentik (Django) + .NET API + Temporal running concurrently |
| Disk | >= 10 GB free | Docker images (postgres:16, authentik:2026.2.0, temporalio, .NET build) |
| Docker Compose | v2 (bundled with Docker Engine on ubuntu-latest) | `docker compose` (not `docker-compose`) |
| Ports | 5432, 8080, 9000 | Default ports; no conflicts on fresh CI runner |

GitHub Actions `ubuntu-latest` runners have 7 GB RAM and ~14 GB free disk. This is marginal for 5 concurrent services. Mitigations:

- Skip `temporal` and `temporal-ui` in CI if not needed by smoke tests (they are not — smoke tests only exercise API + auth + DB). This can be done via a CI-specific compose profile or by passing `docker compose up -d db authentik-server authentik-worker api`.
- If memory is tight, the workflow should start services selectively.

#### 3. Isolation Strategy

Use `COMPOSE_PROJECT_NAME=nebula-ci-${{ github.run_id }}` to isolate concurrent CI runs. This creates unique container and volume names per run, preventing port conflicts (though on a single runner, only one job runs at a time — the isolation protects against stale containers from cancelled runs).

#### 4. Exit Code Propagation

`smoke-test.sh --all-users` exit codes map directly to GitHub check status:
- Exit 0 → check passes
- Exit 1 → check fails (test assertion failure)
- Exit 2 → check fails (infrastructure failure)

No translation needed — GitHub Actions treats any non-zero exit as failure.

#### 5. Branch Protection

After the workflow is validated, add `smoke-test` to the branch protection rules for `main` as a required status check. This is a manual GitHub UI step, not automatable in the workflow file.

### Integration Checkpoint — After Step 2

- [ ] `smoke-test.yml` workflow triggers on PR and push to main
- [ ] Stack starts successfully on `ubuntu-latest` runner
- [ ] `./scripts/smoke-test.sh --all-users` runs and reports results in CI log
- [ ] Failure produces uploaded artifact with compose logs
- [ ] Stack is torn down on success and failure (`if: always()`)
- [ ] Concurrent runs use isolated project names
- [ ] Workflow completes in < 10 minutes

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|-------|---------------|-------|--------|
| Backend (`engine/`) | None — no application code changes | N/A | N/A |
| Frontend (`experience/`) | None — no UI changes | N/A | N/A |
| AI (`neuron/`) | None | N/A | N/A |
| DevOps/Scripts | S0002: Extend `smoke-test.sh` with `--all-users` | DevOps Agent | Not Started |
| CI/CD | S0003: Create `smoke-test.yml` workflow | DevOps Agent | Future |
| Quality | S0002 is self-verifying; S0003 is the CI gate itself | N/A | N/A |

## Dependency Order

```
Step 0 (Architect):   Architecture review + assembly plan ← YOU ARE HERE
Step 1 (DevOps):      S0002 — Multi-role smoke test enhancement
  ──── Checkpoint: --all-users runs 4 users, correct assertions, unified summary ────
Step 2 (DevOps):      S0003 — CI smoke test workflow (Future)
  ──── Checkpoint: Workflow passes on ubuntu-latest, logs uploaded on failure ────
```

## Cross-Story Verification

- [ ] `--all-users` runs all 4 dev users with correct role-appropriate assertions
- [ ] broker001 gets 200 on `GET /my/tasks` (not 403 — corrected from S0002 AC)
- [ ] broker001 gets 403 on `POST /tasks`, `PUT /tasks/{id}`, `DELETE /tasks/{id}`
- [ ] Single-user mode (`--user`) is not regressed
- [ ] `dev-reset.sh` is not regressed
- [ ] Exit code contract: 0 = all pass, 1 = test failure, 2 = infra failure
- [ ] CI workflow starts only necessary services (consider skipping temporal/temporal-ui)

## Integration Checklist

- [x] API contract compatibility validated — no API changes (scripts consume existing endpoints)
- [x] Frontend contract compatibility validated — no frontend changes
- [x] AI contract compatibility validated — no AI scope
- [x] Test cases mapped to acceptance criteria — S0002 test matrix defined above
- [x] Developer-owned fast-test responsibilities identified — DevOps agent runs `--all-users` against warm stack
- [x] Framework vs solution boundary reviewed — this is solution tooling only
- [x] Run/deploy instructions — README.md and GETTING-STARTED.md to be updated with `--all-users`

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| S0002: BrokerUser 403 expectation is wrong in PM story | Medium | Corrected in this plan — implementation uses actual Casbin policy. PM story needs amendment. | Architect → PM |
| S0003: CI runner RAM (7 GB) may be insufficient for full stack | Medium | Start only required services (`db`, `authentik-server`, `authentik-worker`, `api`); skip `temporal`, `temporal-ui`. | DevOps |
| S0003: authentik image pull time on CI | Low | Docker layer caching via `actions/cache` or accept cold-pull penalty (~60s). | DevOps |
| S0003: Blueprint application timing | Low | Already mitigated by `sleep 5` + health polling in smoke-test.sh. | N/A |

## JSON Serialization Convention

N/A — no application-layer serialization changes.

## DI Registration Changes

None — no application services added or modified.

## Casbin Policy Sync

No Casbin policy changes. Scripts validate existing policies as a consumer, not a producer.
