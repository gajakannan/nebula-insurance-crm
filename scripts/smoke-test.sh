#!/usr/bin/env bash
# F0014: Automated smoke test for Nebula CRM API.
# Verifies auth, core CRUD endpoints, and timeline event recording
# against a running docker-compose stack.
#
# Usage:
#   ./scripts/smoke-test.sh                  # Run against already-running stack
#   ./scripts/smoke-test.sh --reset          # Tear down, rebuild, then test
#   ./scripts/smoke-test.sh --user lisa.wong  # Test as specific user (default: lisa.wong)
#   ./scripts/smoke-test.sh --all-users      # Test all 4 dev users with role assertions
#
# Prerequisites:
#   - docker compose services running (or use --reset)
#   - curl, python3 available on PATH
#
# Exit codes:
#   0 — all tests passed
#   1 — one or more tests failed
#   2 — setup/infrastructure failure

set -euo pipefail

# ── Configuration ───────────────────────────────────────────────────────
API_BASE="${API_BASE:-http://localhost:8080}"
AUTHENTIK_BASE="${AUTHENTIK_BASE:-http://localhost:9000}"
TOKEN_ENDPOINT="${AUTHENTIK_BASE}/application/o/token/"
CLIENT_ID="${CLIENT_ID:-nebula}"
SCOPES="openid profile email nebula_roles broker_tenant_id"
# All dev users share this app-password token key (seeded by blueprint)
APP_PASSWORD="${APP_PASSWORD:-nebula-dev-token}"
TEST_USER="${TEST_USER:-lisa.wong}"
TOKEN_WAIT_ATTEMPTS="${TOKEN_WAIT_ATTEMPTS:-120}"
TOKEN_WAIT_DELAY_SECONDS="${TOKEN_WAIT_DELAY_SECONDS:-2}"
COMPOSE_PROJECT_DIR="${COMPOSE_PROJECT_DIR:-$(cd "$(dirname "$0")/.." && pwd)}"
ALL_USERS=false

# ── Per-Role Expected Behavior ──────────────────────────────────────────
# Format: username:expected_role:task_crud_allowed
# task_crud_allowed: "full" = all 9 tests, "read_only" = GET succeeds, CUD returns 403
ROLE_MATRIX=(
  "lisa.wong:DistributionUser:full"
  "john.miller:Underwriter:full"
  "broker001:BrokerUser:read_only"
  "akadmin:Admin:full"
)

# ── CLI arg parsing ─────────────────────────────────────────────────────
RESET=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --reset)      RESET=true; shift ;;
    --all-users)  ALL_USERS=true; shift ;;
    --user)       TEST_USER="$2"; shift 2 ;;
    --api)        API_BASE="$2"; shift 2 ;;
    --help|-h)
      sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
      exit 0 ;;
    *) echo "Unknown option: $1"; exit 2 ;;
  esac
done

# ── Counters ────────────────────────────────────────────────────────────
PASSED=0
FAILED=0
TOTAL=0

pass() { PASSED=$((PASSED + 1)); TOTAL=$((TOTAL + 1)); echo "  PASS: $1"; }
fail() { FAILED=$((FAILED + 1)); TOTAL=$((TOTAL + 1)); echo "  FAIL: $1 — $2"; }

# ── Reset (optional) ───────────────────────────────────────────────────
if [[ "$RESET" == "true" ]]; then
  echo "==> Tearing down stack (volumes included)..."
  cd "$COMPOSE_PROJECT_DIR"
  docker compose down -v 2>&1 | tail -3
  echo "==> Rebuilding and starting..."
  docker compose up -d --build 2>&1 | tail -5
  echo ""
fi

# ── Wait for services ──────────────────────────────────────────────────
echo "==> Waiting for services..."

wait_for_url() {
  local url="$1" label="$2" max_attempts="${3:-30}"
  for i in $(seq 1 "$max_attempts"); do
    if curl -sf -o /dev/null "$url" 2>/dev/null; then
      echo "  $label ready (attempt $i)"
      return 0
    fi
    sleep 2
  done
  echo "  TIMEOUT: $label not ready after $((max_attempts * 2))s"
  return 1
}

wait_for_url "${API_BASE}/healthz" "API" 45 || exit 2
wait_for_url "${AUTHENTIK_BASE}/-/health/live/" "authentik" 30 || exit 2
echo ""

# ── Helper: HTTP request ────────────────────────────────────────────────
# Returns: body\nhttp_code
http() {
  local method="$1" url="$2"
  shift 2
  curl -s -w "\n%{http_code}" -X "$method" "${API_BASE}${url}" \
    -H "$AUTH" -H "Content-Type: application/json" "$@"
}

http_code() { echo "$1" | tail -1; }
http_body() { echo "$1" | sed '$d'; }
json_field() { echo "$1" | python3 -c "import sys,json; print(json.load(sys.stdin).get('$2',''))" 2>/dev/null; }

# ── Token Acquisition ───────────────────────────────────────────────────
# Sets globals: TOKEN, AUTH, CLAIMS_JSON
acquire_token() {
  local user="$1"
  echo "==> Acquiring JWT for ${user}..."
  local token_resp="" token_code="" token_body="" attempt
  for attempt in $(seq 1 "$TOKEN_WAIT_ATTEMPTS"); do
    token_resp=$(curl -s -w "\n%{http_code}" -X POST "$TOKEN_ENDPOINT" \
      -d "grant_type=password&client_id=${CLIENT_ID}&username=${user}&password=${APP_PASSWORD}&scope=${SCOPES}" 2>/dev/null || true)
    token_code=$(http_code "$token_resp")
    token_body=$(http_body "$token_resp")

    if [[ "$token_code" == "200" ]]; then
      TOKEN=$(echo "$token_body" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")
      echo "  Token acquired (${#TOKEN} chars)"
      AUTH="Authorization: Bearer $TOKEN"

      CLAIMS_JSON=$(python3 -c "
import json, base64, sys
parts = sys.argv[1].split('.')
payload = parts[1] + '=' * (4 - len(parts[1]) % 4)
d = json.loads(base64.urlsafe_b64decode(payload))
print(json.dumps({'sub': d.get('sub'), 'aud': d.get('aud'), 'nebula_roles': d.get('nebula_roles', [])}))" "$TOKEN")
      echo "  Claims: $CLAIMS_JSON"
      echo ""
      return 0
    fi

    if [[ "$attempt" -lt "$TOKEN_WAIT_ATTEMPTS" ]]; then
      if [[ "$attempt" -eq 1 || $(( attempt % 5 )) -eq 0 ]]; then
        echo "  Token endpoint not ready yet (attempt ${attempt}/${TOKEN_WAIT_ATTEMPTS}, HTTP ${token_code:-000})"
      fi
      sleep "$TOKEN_WAIT_DELAY_SECONDS"
    fi
  done

  echo "  FAIL: Could not acquire token for ${user} after ${TOKEN_WAIT_ATTEMPTS} attempts."
  echo "  Last HTTP status: ${token_code:-000}"
  if [[ -n "$token_body" ]]; then
    echo "  Last response body: $token_body"
  else
    echo "  Last response body: <empty>"
  fi
  echo ""
  echo "  Troubleshooting:"
  echo "    1. Is authentik healthy?  curl ${AUTHENTIK_BASE}/-/health/live/"
  echo "    2. Was the blueprint applied? Check: docker compose logs authentik-worker | grep -i blueprint"
  echo "    3. Does the user exist?   docker compose exec authentik-server ak shell -c \"from authentik.core.models import User; print(User.objects.filter(username='${user}').exists())\""
  echo "    4. Does the app-password token exist?  Check blueprint for authentik_core.token entries"
  return 1
}

# ── Claims Verification ────────────────────────────────────────────────
# Returns 0 if claims match expected role, 1 if mismatch.
verify_claims() {
  local expected_role="$1"
  local claims_ok
  claims_ok=$(python3 -c "
import json, sys
c = json.loads(sys.argv[1])
aud = c.get('aud', '')
aud_ok = (aud == 'nebula') if isinstance(aud, str) else ('nebula' in aud if isinstance(aud, list) else False)
roles = c.get('nebula_roles', [])
sub = c.get('sub', '')
ok = 1 if (aud_ok and sys.argv[2] in roles and sub) else 0
print(ok)
" "$CLAIMS_JSON" "$expected_role")
  if [[ "$claims_ok" == "1" ]]; then
    echo "  Claims verified: aud=nebula, role=${expected_role}, sub present"
    return 0
  else
    echo "  FAIL: Claims mismatch — expected role=${expected_role}, aud=nebula"
    echo "  Got: $CLAIMS_JSON"
    return 1
  fi
}

# ── Resolve Internal UserId ─────────────────────────────────────────────
# Sets global: USER_ID. Requires TOKEN/AUTH to be set.
resolve_user_id() {
  local user="$1"
  echo "==> Resolving internal UserId for ${user}..."
  local resp code body
  resp=$(http GET "/my/tasks?limit=1")
  code=$(http_code "$resp")
  body=$(http_body "$resp")

  if [[ "$code" != "200" ]]; then
    echo "  FAIL: GET /my/tasks returned $code — cannot resolve UserId"
    echo "  Body: $body"
    return 1
  fi

  USER_ID=$(docker compose exec -T db psql -U postgres -d nebula -t -A -c \
    "SELECT \"Id\" FROM \"UserProfiles\" WHERE \"IdpSubject\" = '${user}' LIMIT 1;" 2>/dev/null | tr -d '[:space:]')

  if [[ -z "$USER_ID" ]]; then
    echo "  FAIL: UserProfile not found for ${user}"
    return 1
  fi
  echo "  UserId: $USER_ID"
  echo ""
}

# ════════════════════════════════════════════════════════════════════════
#  TEST SUITES
# ════════════════════════════════════════════════════════════════════════

# ── Full CRUD Suite (9 tests) ───────────────────────────────────────────
# Requires: TOKEN, AUTH, USER_ID globals set
run_full_crud_suite() {
  local task_id task2_id task_row_version task2_row_version

  # ── 1. GET /my/tasks ──────────────────────────────────────────────────
  echo "[1/9] GET /my/tasks"
  RESP=$(http GET "/my/tasks?limit=5")
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "200" ]]; then
    TOTAL_COUNT=$(json_field "$BODY" "totalCount")
    pass "200 OK (totalCount=$TOTAL_COUNT)"
  else
    fail "Expected 200, got $CODE" "$BODY"
  fi

  # ── 2. POST /tasks (create) ──────────────────────────────────────────
  echo "[2/9] POST /tasks (create)"
  RESP=$(http POST "/tasks" -d "{\"title\":\"Smoke test task\",\"description\":\"Automated verification\",\"priority\":\"High\",\"dueDate\":\"2026-04-01T00:00:00Z\",\"assignedToUserId\":\"$USER_ID\"}")
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "201" ]]; then
    task_id=$(json_field "$BODY" "id")
    TASK_STATUS=$(json_field "$BODY" "status")
    task_row_version=$(json_field "$BODY" "rowVersion")
    pass "201 Created (id=$task_id, status=$TASK_STATUS, rowVersion=$task_row_version)"
  else
    fail "Expected 201, got $CODE" "$BODY"
    echo "  ABORT: Cannot continue CRUD tests without created task"
    for t in 3 4 5 6 7 8 9; do
      fail "Test $t/9 skipped" "depends on task creation (test 2)"
    done
    return 1
  fi

  # ── 3. GET /tasks/{id} ───────────────────────────────────────────────
  echo "[3/9] GET /tasks/{id}"
  RESP=$(http GET "/tasks/$task_id")
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "200" ]]; then
    TITLE=$(json_field "$BODY" "title")
    task_row_version=$(json_field "$BODY" "rowVersion")
    pass "200 OK (title=$TITLE, rowVersion=$task_row_version)"
  else
    fail "Expected 200, got $CODE" "$BODY"
  fi

  # ── 4. PUT /tasks/{id} — Open → InProgress ──────────────────────────
  echo "[4/9] PUT /tasks/{id} (Open -> InProgress)"
  RESP=$(http PUT "/tasks/$task_id" -H "If-Match: \"$task_row_version\"" -d '{"status":"InProgress"}')
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "200" ]]; then
    NEW_STATUS=$(json_field "$BODY" "status")
    task_row_version=$(json_field "$BODY" "rowVersion")
    [[ "$NEW_STATUS" == "InProgress" ]] && pass "200 OK (status=InProgress, rowVersion=$task_row_version)" || fail "Status mismatch" "expected InProgress, got $NEW_STATUS"
  else
    fail "Expected 200, got $CODE" "$BODY"
  fi

  # ── 5. PUT /tasks/{id} — InProgress → Done ──────────────────────────
  echo "[5/9] PUT /tasks/{id} (InProgress -> Done)"
  RESP=$(http PUT "/tasks/$task_id" -H "If-Match: \"$task_row_version\"" -d '{"status":"Done"}')
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "200" ]]; then
    COMPLETED_AT=$(json_field "$BODY" "completedAt")
    task_row_version=$(json_field "$BODY" "rowVersion")
    [[ -n "$COMPLETED_AT" ]] && pass "200 OK (completedAt=$COMPLETED_AT, rowVersion=$task_row_version)" || fail "completedAt missing" "$BODY"
  else
    fail "Expected 200, got $CODE" "$BODY"
  fi

  # ── 6. PUT — invalid transition (Open → Done = 409) ─────────────────
  echo "[6/9] PUT — invalid transition (Open -> Done)"
  RESP2=$(http POST "/tasks" -d "{\"title\":\"Transition guard test\",\"assignedToUserId\":\"$USER_ID\"}")
  task2_id=$(json_field "$(http_body "$RESP2")" "id")
  task2_row_version=$(json_field "$(http_body "$RESP2")" "rowVersion")
  RESP=$(http PUT "/tasks/$task2_id" -H "If-Match: \"$task2_row_version\"" -d '{"status":"Done"}')
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "409" ]]; then
    ERR_CODE=$(json_field "$BODY" "code")
    pass "409 Conflict (code=$ERR_CODE)"
  else
    fail "Expected 409, got $CODE" "$BODY"
  fi

  # ── 7. DELETE /tasks/{id} ────────────────────────────────────────────
  echo "[7/9] DELETE /tasks/{id}"
  RESP=$(http DELETE "/tasks/$task_id")
  CODE=$(http_code "$RESP")
  if [[ "$CODE" == "204" ]]; then
    pass "204 No Content"
  else
    fail "Expected 204, got $CODE" "$(http_body "$RESP")"
  fi

  # ── 8. GET deleted task (expect 404) ─────────────────────────────────
  echo "[8/9] GET deleted task (expect 404)"
  RESP=$(http GET "/tasks/$task_id")
  CODE=$(http_code "$RESP")
  if [[ "$CODE" == "404" ]]; then
    pass "404 Not Found (soft delete confirmed)"
  else
    fail "Expected 404, got $CODE" "$(http_body "$RESP")"
  fi

  # ── 9. Timeline events verification ─────────────────────────────────
  echo "[9/9] Timeline events for smoke test task"
  EVENTS=$(docker compose exec -T db psql -U postgres -d nebula -t -A -c \
    "SELECT \"EventType\" FROM \"ActivityTimelineEvents\" WHERE \"EntityId\" = '$task_id' ORDER BY \"OccurredAt\";" 2>/dev/null | tr '\n' ',')
  EXPECTED="TaskCreated,TaskUpdated,TaskCompleted,TaskDeleted,"
  if [[ "$EVENTS" == "$EXPECTED" ]]; then
    pass "4 timeline events: TaskCreated,TaskUpdated,TaskCompleted,TaskDeleted"
  else
    fail "Expected $EXPECTED" "got $EVENTS"
  fi

  # ── Cleanup ──────────────────────────────────────────────────────────
  http DELETE "/tasks/$task2_id" > /dev/null 2>&1 || true
}

# ── Read-Only Suite (4 tests) ───────────────────────────────────────────
# For users with task:read but no task:create/update/delete (e.g., BrokerUser).
# Update/delete on existing tasks are intentionally normalized to 404 to prevent IDOR.
run_read_only_suite() {
  local existing_task existing_task_id existing_task_row_version
  existing_task=$(docker compose exec -T db psql -U postgres -d nebula -t -A -F '|' -c \
    "SELECT \"Id\", xmin::text FROM \"Tasks\" WHERE \"IsDeleted\" = false ORDER BY \"CreatedAt\" DESC LIMIT 1;" 2>/dev/null | tail -1 | tr -d '[:space:]')

  if [[ -z "$existing_task" ]]; then
    fail "Read-only fixture missing" "no active task found for BrokerUser authorization checks"
    fail "Test 3/4 skipped" "depends on read-only fixture task"
    fail "Test 4/4 skipped" "depends on read-only fixture task"
    return 1
  fi

  IFS='|' read -r existing_task_id existing_task_row_version <<< "$existing_task"

  # ── 1. GET /my/tasks → 200 OK ───────────────────────────────────────
  echo "[1/4] GET /my/tasks (read-only)"
  RESP=$(http GET "/my/tasks?limit=1")
  CODE=$(http_code "$RESP"); BODY=$(http_body "$RESP")
  if [[ "$CODE" == "200" ]]; then
    TOTAL_COUNT=$(json_field "$BODY" "totalCount")
    pass "200 OK (totalCount=$TOTAL_COUNT)"
  else
    fail "Expected 200, got $CODE" "$BODY"
  fi

  # ── 2. POST /tasks → 403 Forbidden ──────────────────────────────────
  echo "[2/4] POST /tasks (expect 403)"
  RESP=$(http POST "/tasks" -d '{"title":"Should be denied","priority":"High"}')
  CODE=$(http_code "$RESP")
  if [[ "$CODE" == "403" ]]; then
    pass "403 Forbidden (create denied)"
  else
    fail "Expected 403, got $CODE" "$(http_body "$RESP")"
  fi

  # ── 3. PUT /tasks/{id} → 404 Not Found (IDOR normalized) ─────────────
  echo "[3/4] PUT /tasks/{id} (expect 404)"
  RESP=$(http PUT "/tasks/$existing_task_id" -H "If-Match: \"$existing_task_row_version\"" -d '{"status":"InProgress"}')
  CODE=$(http_code "$RESP")
  if [[ "$CODE" == "404" ]]; then
    pass "404 Not Found (update denied via IDOR normalization)"
  else
    fail "Expected 404, got $CODE" "$(http_body "$RESP")"
  fi

  # ── 4. DELETE /tasks/{id} → 404 Not Found (IDOR normalized) ──────────
  echo "[4/4] DELETE /tasks/{id} (expect 404)"
  RESP=$(http DELETE "/tasks/$existing_task_id")
  CODE=$(http_code "$RESP")
  if [[ "$CODE" == "404" ]]; then
    pass "404 Not Found (delete denied via IDOR normalization)"
  else
    fail "Expected 404, got $CODE" "$(http_body "$RESP")"
  fi
}

# ════════════════════════════════════════════════════════════════════════
#  MAIN EXECUTION
# ════════════════════════════════════════════════════════════════════════

if [[ "$ALL_USERS" == "true" ]]; then
  # ── Multi-user mode ──────────────────────────────────────────────────
  GRAND_PASSED=0
  GRAND_FAILED=0
  USER_RESULTS=()

  for entry in "${ROLE_MATRIX[@]}"; do
    IFS=: read -r username expected_role crud_mode <<< "$entry"

    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║  Testing: $username ($expected_role) — mode: $crud_mode"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""

    # Reset per-user counters
    PASSED=0
    FAILED=0
    TOTAL=0

    # Acquire token
    if ! acquire_token "$username"; then
      fail "Token acquisition failed" "$username"
      USER_RESULTS+=("$username:$expected_role:$PASSED:$FAILED")
      GRAND_PASSED=$((GRAND_PASSED + PASSED))
      GRAND_FAILED=$((GRAND_FAILED + FAILED))
      echo ""
      continue
    fi

    # Verify claims
    if ! verify_claims "$expected_role"; then
      fail "Claims verification failed" "$username"
      USER_RESULTS+=("$username:$expected_role:$PASSED:$FAILED")
      GRAND_PASSED=$((GRAND_PASSED + PASSED))
      GRAND_FAILED=$((GRAND_FAILED + FAILED))
      echo ""
      continue
    fi

    echo ""
    echo "==> Running smoke tests for $username..."
    echo ""

    if [[ "$crud_mode" == "full" ]]; then
      # Resolve UserId (needed for task creation)
      if ! resolve_user_id "$username"; then
        fail "UserId resolution failed" "$username"
        USER_RESULTS+=("$username:$expected_role:$PASSED:$FAILED")
        GRAND_PASSED=$((GRAND_PASSED + PASSED))
        GRAND_FAILED=$((GRAND_FAILED + FAILED))
        echo ""
        continue
      fi
      run_full_crud_suite || true
    else
      run_read_only_suite || true
    fi

    USER_RESULTS+=("$username:$expected_role:$PASSED:$FAILED")
    GRAND_PASSED=$((GRAND_PASSED + PASSED))
    GRAND_FAILED=$((GRAND_FAILED + FAILED))
    echo ""
  done

  # ── Unified Summary ──────────────────────────────────────────────────
  echo "════════════════════════════════════════════════════════════════"
  echo "  MULTI-ROLE SMOKE TEST RESULTS"
  echo "  ────────────────────────────────────────────────────────────"
  for result in "${USER_RESULTS[@]}"; do
    IFS=: read -r r_user r_role r_passed r_failed <<< "$result"
    r_total=$((r_passed + r_failed))
    printf "  %-25s %s/%s passed\n" "$r_user ($r_role):" "$r_passed" "$r_total"
  done
  echo "  ────────────────────────────────────────────────────────────"
  GRAND_TOTAL=$((GRAND_PASSED + GRAND_FAILED))
  echo "  TOTAL: $GRAND_PASSED/$GRAND_TOTAL passed, $GRAND_FAILED failed"
  echo "  $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "════════════════════════════════════════════════════════════════"

  [[ "$GRAND_FAILED" -eq 0 ]] && exit 0 || exit 1

else
  # ── Single-user mode (original behavior) ─────────────────────────────
  acquire_token "$TEST_USER" || exit 2
  resolve_user_id "$TEST_USER" || exit 2

  echo "==> Running smoke tests..."
  echo ""

  run_full_crud_suite

  # ── Summary ──────────────────────────────────────────────────────────
  echo ""
  echo "════════════════════════════════════════════════════════════════"
  echo "  SMOKE TEST RESULTS: $PASSED/$TOTAL passed, $FAILED failed"
  echo "  User: $TEST_USER | API: $API_BASE | $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "════════════════════════════════════════════════════════════════"

  [[ "$FAILED" -eq 0 ]] && exit 0 || exit 1
fi
