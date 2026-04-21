# Nebula API Design Guide

Status: Active
Last Updated: 2026-03-05
Owners: Architect + Backend

This guide operationalizes `api-guidelines-profile.md` for day-to-day design and implementation.

## 1. Route Design

- Use resource routes without `/api` prefix.
- Use plural nouns and path parameters for identity.
- Keep routes shallow; avoid deep parent/child chains.

Examples:
- `GET /brokers`
- `POST /brokers`
- `GET /brokers/{brokerId}`
- `PUT /brokers/{brokerId}`
- `DELETE /brokers/{brokerId}`

## 2. Casing and Naming

- JSON and query parameter names: `camelCase`.
- ProblemDetails `code` values: `snake_case`.
- Keep names stable once released.

## 3. Error Contract

All errors must be RFC Problem Details (`application/problem+json`).

Example:

```json
{
  "type": "https://nebula.local/problems/broker-scope-unresolvable",
  "title": "Broker scope could not be resolved.",
  "status": 403,
  "code": "broker_scope_unresolvable",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## 4. Correlation and Headers

- Accept and propagate `X-Request-Id`.
- Emit `X-Request-Id` in responses for traceability.
- Use `If-Match` for optimistic concurrency preconditions.
- For rate limiting, return `Retry-After` on `429` when possible.

## 5. Status Code Decision Rules

- `400`: malformed request or baseline validation failure.
- `401`: authentication failure.
- `403`: authorized identity failed permission/scope checks.
- `404`: resource not found or hidden.
- `409`: business conflict.
- `412`: failed precondition (`If-Match` mismatch).
- `422`: semantic/domain validation failure.
- `429`: throttled.
- `500`: unexpected error.
- `503`: service/dependency unavailable.

## 6. 403 Subtypes

Always include ProblemDetails `code` for 403 responses:

- `policy_denied`
- `broker_scope_unresolvable`

Do not use empty collections for unresolvable scope. Empty collections are valid only when scope is resolved and data query returns no records.

## 7. OpenAPI Requirements

- Use reusable error response components with `application/problem+json`.
- Reuse `ProblemDetails` schema across all non-2xx responses.
- Document 412 where `If-Match` is required.
- Document 429 and 503 for endpoints under resilience controls.

## 8. Implementation Rules for Agents

- Backend agent must implement exactly per OpenAPI + this guide.
- Architect agent must validate new contracts against this guide.
- Security review must validate 401/403/404/409/412 semantics and ProblemDetails code stability.
