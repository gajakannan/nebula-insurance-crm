#!/usr/bin/env bash
set -euo pipefail

SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:9002}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-nebula-crm}"

docker compose -f docker-compose.yml -f docker-compose.qe.yml up -d sonarqube

dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --collect:"XPlat Code Coverage"
pnpm --dir experience test:coverage

dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.cs.vstest.reportsPaths="**/TestResults/*.trx" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
  /d:sonar.javascript.lcov.reportPaths="experience/coverage/lcov.info" \
  ${SONAR_TOKEN:+/d:sonar.token="$SONAR_TOKEN"}

dotnet build engine/Nebula.slnx

dotnet sonarscanner end ${SONAR_TOKEN:+/d:sonar.token="$SONAR_TOKEN"}
