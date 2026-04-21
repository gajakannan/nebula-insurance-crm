#!/usr/bin/env bash
set -euo pipefail

PACT_DIR="${PACT_DIR:-experience/pacts}"
PACT_FILE="$PACT_DIR/nebula-experience-nebula-api.json"

if [ ! -f "$PACT_FILE" ]; then
  echo "Error: Pact file not found at $PACT_FILE"
  echo "Run consumer tests first: pnpm --dir experience test:contracts"
  exit 1
fi

echo "Running provider verification against $PACT_FILE..."
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj \
  --filter "FullyQualifiedName~BrokerListProviderPactTests" \
  --logger "console;verbosity=normal"

# Optional: publish to Pact Broker if configured
if [ -n "${PACT_BROKER_BASE_URL:-}" ]; then
  echo "Publishing pact to broker at $PACT_BROKER_BASE_URL..."
  pnpm dlx @pact-foundation/pact-cli publish "$PACT_DIR" \
    --broker-base-url "$PACT_BROKER_BASE_URL" \
    --consumer-app-version "$(git rev-parse --short HEAD)" \
    --tag "$(git branch --show-current)"
fi
