#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

BRUNO_ENV="${BRUNO_ENV:-local}"
REPORT_DIR="${REPORT_DIR:-$REPO_ROOT/planning-mds/operations/evidence/f0033/artifacts/bruno}"
mkdir -p "$REPORT_DIR"

cd "$REPO_ROOT/bruno/nebula"

pnpm dlx @usebruno/cli run \
  --env "$BRUNO_ENV" \
  --reporter-junit "$REPORT_DIR/bruno-junit.xml" \
  --reporter-json "$REPORT_DIR/bruno-report.json"
