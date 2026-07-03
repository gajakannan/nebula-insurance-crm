# F0035 Coverage Report

## Result

PASS WITH ENVIRONMENT LIMITATION

## Backend Coverage

The failed Testcontainers backend run still emitted a coverage attachment:

artifacts/coverage/backend-session-auth-coverage.cobertura.xml

Because Docker was unavailable, this backend coverage does not prove backend assertions passed.

## Frontend Coverage

Vitest coverage was captured at:

artifacts/coverage/frontend-session-continuity/coverage-summary.json

artifacts/coverage/frontend-session-continuity/lcov.info

## Coverage Interpretation

Frontend coverage is focused on F0035 session/auth surfaces. Backend integration coverage is retained as an attempted-run artifact with the Docker limitation disclosed.
