# Nebula API Guidelines Profile

Status: Accepted
Last Updated: 2026-03-05
Owners: Architect + Backend

Purpose: define Nebula's API standards profile based on industry practices (including Zalando RESTful API Guidelines), adapted to Nebula constraints.

## 1. URI and Resource Conventions

- Do not use an `/api` base path in endpoint routes.
- Use plural resource nouns with lowercase, kebab-case segments when multi-word.
- Prefer shallow resources and avoid deep nesting.
- Use query parameters for filter/search concerns.

Examples:
- `GET /brokers`
- `GET /brokers/{brokerId}`
- `GET /contacts?brokerId={brokerId}`

## 2. Naming and Payload Casing

- JSON field names and query parameter names must use `camelCase`.
- Error `code` values remain stable machine-readable identifiers in `snake_case`.
- Header names follow HTTP conventions (`X-Request-Id`, `If-Match`).

## 3. Error Format Standard

- All non-2xx error responses must use RFC Problem Details.
- Response media type for errors must be `application/problem+json`.
- Response body must conform to `planning-mds/schemas/problem-details.schema.json`.

Required fields:
- `type`
- `title`
- `status`
- `code`
- `traceId`

`code` values are governed by `planning-mds/architecture/error-codes.md`.

## 4. Request Correlation and Tracing

- Primary correlation header: `X-Request-Id`.
- If provided by caller, propagate `X-Request-Id`; otherwise generate one.
- Include correlation id in structured logs and in ProblemDetails `traceId`.
- `traceparent` may be supported for distributed tracing compatibility but is optional for MVP.

## 5. HTTP Status Code Policy

Use these codes consistently:

- `400 Bad Request`: malformed request or schema validation input shape issues.
- `401 Unauthorized`: missing/invalid/expired authentication token.
- `403 Forbidden`: authenticated identity lacks permission or valid scope.
- `404 Not Found`: resource absent (or intentionally hidden by visibility policy).
- `409 Conflict`: business/workflow conflict (state rule violations, duplicates).
- `412 Precondition Failed`: precondition headers fail (e.g., `If-Match` concurrency token mismatch).
- `422 Unprocessable Content`: semantically valid JSON but domain validation failed.
- `429 Too Many Requests`: rate limit exceeded (include `Retry-After` when possible).
- `500 Internal Server Error`: unexpected server failure.
- `503 Service Unavailable`: dependency/service outage or maintenance window.

## 6. Forbidden (403) Subtype Guidance

Use stable ProblemDetails `code` values to disambiguate 403 outcomes:

- `policy_denied`: ABAC/policy denies authenticated user action.
- `broker_scope_unresolvable`: broker_tenant_id scope resolution failed.

Rule:
- unresolvable scope => 403 with subtype code.
- valid resolved scope + no matching records => 200 with empty collection.

## 7. Contract Governance

- OpenAPI contract (`planning-mds/api/nebula-api.yaml`) is authoritative for endpoints and status responses.
- Architecture patterns must reference this profile, not duplicate conflicting guidance.
- New deviations require an ADR.

## 8. Change Management

- `/api` removal and any casing changes are contract-breaking for existing clients.
- Any runtime rollout must use migration notes and client coordination.
- Planning artifacts in this repository now use the no-`/api` convention.
