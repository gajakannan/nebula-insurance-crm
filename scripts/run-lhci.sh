#!/usr/bin/env bash
set -euo pipefail

# Lighthouse uses the Vite dev server (not a production build) so that
# VITE_AUTH_MODE=dev can bypass OIDC for perf-only auditing without
# weakening the production build guard (nebula-auth-mode-guard plugin).
export VITE_AUTH_MODE="${VITE_AUTH_MODE:-dev}"
export NEBULA_API_PROXY_TARGET="${NEBULA_API_PROXY_TARGET:-http://localhost:8080}"

# WSL fix: chrome-launcher detects WSL and uses Windows temp paths.
# Setting LOCALAPPDATA to a Linux path makes its temp dir creation work.
export LOCALAPPDATA="${LOCALAPPDATA:-/tmp}"

# Auto-detect Chrome in WSL if not set
if [ -z "${CHROME_PATH:-}" ]; then
  CHROME_PATH="$(which chromium-browser 2>/dev/null || which chromium 2>/dev/null || which google-chrome 2>/dev/null || true)"
  if [ -n "$CHROME_PATH" ]; then
    export CHROME_PATH
  fi
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

pnpm --dir "$REPO_ROOT/experience" install --frozen-lockfile

# Pick a port: use LHCI_PORT if set, otherwise find a free one
LHCI_PORT="${LHCI_PORT:-5173}"

# Start dev server in the background
VITE_AUTH_MODE=dev pnpm --dir "$REPO_ROOT/experience" dev --port "$LHCI_PORT" --strictPort &
DEV_PID=$!
trap "kill $DEV_PID 2>/dev/null; wait $DEV_PID 2>/dev/null" EXIT

# Wait for dev server — detect actual port from output
ACTUAL_PORT="$LHCI_PORT"
for i in $(seq 1 30); do
  if curl -sf "http://127.0.0.1:$ACTUAL_PORT" > /dev/null 2>&1; then
    echo "Dev server is ready on port $ACTUAL_PORT"
    break
  fi
  sleep 2
done

# Generate lighthouserc with the actual port
LHCI_CONFIG=$(mktemp)
sed "s/127\.0\.0\.1:5173/127.0.0.1:$ACTUAL_PORT/g" "$REPO_ROOT/experience/lighthouserc.json" > "$LHCI_CONFIG"

cd "$REPO_ROOT/experience"
# WSL fix: chrome-launcher detects WSL and calls makeWin32TmpDir which needs
# LOCALAPPDATA or USERPROFILE. Inline env ensures it reaches the subprocess.
LOCALAPPDATA=/tmp USERPROFILE=/tmp pnpm exec lhci autorun --config="$LHCI_CONFIG"
