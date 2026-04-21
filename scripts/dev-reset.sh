#!/usr/bin/env bash
# F0014: Clean teardown + rebuild + smoke test in one command.
# This is the canonical "prove it works from scratch" workflow
# for DevOps verification of any feature.
#
# Usage:
#   ./scripts/dev-reset.sh                   # Full reset + smoke test
#   ./scripts/dev-reset.sh --skip-smoke      # Reset only, no smoke test
#   ./scripts/dev-reset.sh --user john.miller # Reset + smoke test as specific user
#
# What it does:
#   1. docker compose down -v  (removes containers AND volumes — clean DB)
#   2. docker compose up -d --build  (rebuild images, start fresh)
#   3. Wait for all services healthy
#   4. Run smoke-test.sh (unless --skip-smoke)
#
# Exit codes:
#   0 — stack healthy + smoke tests passed
#   1 — smoke test failure
#   2 — infrastructure failure (services didn't come up)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SKIP_SMOKE=false
TEST_USER="${TEST_USER:-lisa.wong}"
AUTHENTIK_BASE="${AUTHENTIK_BASE:-http://localhost:9000}"
TOKEN_ENDPOINT="${AUTHENTIK_BASE}/application/o/token/"
CLIENT_ID="${CLIENT_ID:-nebula}"
SCOPES="${SCOPES:-openid profile email nebula_roles broker_tenant_id}"
APP_PASSWORD="${APP_PASSWORD:-nebula-dev-token}"
TOKEN_WAIT_ATTEMPTS="${TOKEN_WAIT_ATTEMPTS:-120}"
TOKEN_WAIT_DELAY_SECONDS="${TOKEN_WAIT_DELAY_SECONDS:-2}"
SMOKE_ARGS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-smoke)  SKIP_SMOKE=true; shift ;;
    --user)        TEST_USER="$2"; SMOKE_ARGS+=(--user "$2"); shift 2 ;;
    --help|-h)
      sed -n '2,/^$/p' "$0" | sed 's/^# \?//'
      exit 0 ;;
    *) echo "Unknown option: $1"; exit 2 ;;
  esac
done

cd "$PROJECT_DIR"

echo "════════════════════════════════════════════════════════════════"
echo "  NEBULA DEV RESET — $(date -u +%Y-%m-%dT%H:%M:%SZ)"
echo "════════════════════════════════════════════════════════════════"
echo ""

# ── Step 1: Tear down ──────────────────────────────────────────────────
echo "==> Step 1: Tearing down stack (containers + volumes)..."
docker compose down -v 2>&1 | tail -5
echo ""

# ── Step 2: Rebuild + start ────────────────────────────────────────────
echo "==> Step 2: Building images and starting services..."
docker compose up -d --build 2>&1 | tail -10
echo ""

# ── Step 3: Wait for health ────────────────────────────────────────────
echo "==> Step 3: Waiting for services to become healthy..."

wait_healthy() {
  local service="$1" max_attempts="${2:-60}"
  for i in $(seq 1 "$max_attempts"); do
    local status
    status=$(docker compose ps --format '{{.Status}}' "$service" 2>/dev/null || echo "")
    if [[ "$status" == *"healthy"* ]] || [[ "$status" == "Up"* && "$service" == "api" ]]; then
      echo "  $service: healthy (attempt $i)"
      return 0
    fi
    sleep 2
  done
  echo "  TIMEOUT: $service not healthy after $((max_attempts * 2))s"
  echo "  Last status: $status"
  echo "  Logs:"
  docker compose logs "$service" --tail 15 2>&1 | sed 's/^/    /'
  return 1
}

wait_healthy db 30 || exit 2
wait_healthy authentik-server 60 || exit 2
wait_healthy authentik-worker 60 || exit 2

http_code() { echo "$1" | tail -1; }
http_body() { echo "$1" | sed '$d'; }

wait_for_token_ready() {
  local user="$1" max_attempts="${2:-$TOKEN_WAIT_ATTEMPTS}"
  local resp code body=""
  for i in $(seq 1 "$max_attempts"); do
    resp=$(curl -s -w "\n%{http_code}" -X POST "$TOKEN_ENDPOINT" \
      -d "grant_type=password&client_id=${CLIENT_ID}&username=${user}&password=${APP_PASSWORD}&scope=${SCOPES}" 2>/dev/null || true)
    code=$(http_code "$resp")
    body=$(http_body "$resp")
    if [[ "$code" == "200" ]]; then
      echo "  authentik blueprint/token ready (attempt $i)"
      return 0
    fi
    if [[ "$i" -eq 1 || $(( i % 5 )) -eq 0 ]]; then
      echo "  Waiting for blueprint/token readiness (attempt $i/$max_attempts, HTTP ${code:-000})..."
    fi
    sleep "$TOKEN_WAIT_DELAY_SECONDS"
  done

  echo "  TIMEOUT: authentik blueprint/token not ready after $((max_attempts * TOKEN_WAIT_DELAY_SECONDS))s"
  echo "  Last HTTP status: ${code:-000}"
  if [[ -n "$body" ]]; then
    echo "  Last response body: $body"
  else
    echo "  Last response body: <empty>"
  fi
  echo "  Logs:"
  docker compose logs authentik-worker --tail 20 2>&1 | sed 's/^/    /'
  return 1
}

# API doesn't have a healthcheck in compose, so poll the endpoint
echo "  Waiting for API healthz..."
for i in $(seq 1 45); do
  if curl -sf -o /dev/null "http://localhost:8080/healthz" 2>/dev/null; then
    echo "  api: healthy (attempt $i)"
    break
  fi
  if [[ $i -eq 45 ]]; then
    echo "  TIMEOUT: API not ready"
    docker compose logs api --tail 15 2>&1 | sed 's/^/    /'
    exit 2
  fi
  sleep 2
done
echo ""

echo "  Waiting for blueprint/token readiness..."
wait_for_token_ready "$TEST_USER" "$TOKEN_WAIT_ATTEMPTS" || exit 2

# ── Step 4: Service summary ────────────────────────────────────────────
echo "==> Service status:"
docker compose ps --format "table {{.Name}}\t{{.Status}}" 2>&1
echo ""

# ── Step 5: Smoke test ─────────────────────────────────────────────────
if [[ "$SKIP_SMOKE" == "true" ]]; then
  echo "==> Smoke test skipped (--skip-smoke)"
  echo ""
  echo "  Stack is ready. Run smoke tests manually:"
  echo "    ./scripts/smoke-test.sh"
  exit 0
fi

echo "==> Step 4: Running smoke tests..."
echo ""
exec "$SCRIPT_DIR/smoke-test.sh" "${SMOKE_ARGS[@]}"
