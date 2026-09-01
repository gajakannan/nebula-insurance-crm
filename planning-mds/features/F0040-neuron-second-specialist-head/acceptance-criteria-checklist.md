# F0040 Acceptance Criteria Checklist

Validated against the Product Manager acceptance-criteria contract on 2026-08-31.

## Story Coverage

| Check | S0001 Live zone | S0002 Conversation | S0003 Platform |
|-------|-----------------|--------------------|----------------|
| Specific measurable happy path | Yes — newest 20, exact fields/order/status | Yes — exact domain/action/head and persisted response | Yes — exact live/inactive head set and readiness result |
| Empty/error edge cases | Yes | Yes | Yes |
| Authorization/privacy behavior | Exhaustive internal roles + external denial | Same scope + cross-owner denial | No new grant; sensitive telemetry exclusions |
| Navigation/terminal evidence | Broker 360 route | stored/replayed envelope | independent terminal zone results + telemetry |
| Performance/reliability | p95 < 2 seconds; isolated zone | existing response budget; one terminal message | bidirectional head failure isolation |
| Audit/timeline expectation | N/A — read-only | no broker mutation; existing message/run provenance | N/A — telemetry/provenance only |
| Explicit non-goals | Yes | Yes | Yes |

## Clarity & Testability

- [x] Every acceptance criterion has an observable pass/fail result.
- [x] "Recent" is defined as at most the 20 newest authorized Broker events ordered by `OccurredAt` descending.
- [x] Roles are exhaustive for the existing broker-feed contract, including external denial.
- [x] Empty, missing actor, authentication, authorization filtering, timeout/error, duplicate replay, and invalid registry output are specified.
- [x] User-safe error/empty text is specified for the new live zone.
- [x] Broker 360 navigation and thread replay provide terminal evidence.
- [x] Performance, reliability, security, privacy, and accessibility expectations are stated.
- [x] Every story is explicitly read-only/infrastructure and explains why no CRM audit event is produced.
- [x] Unsupported filter/write requests cannot be falsely fulfilled by the newest-20 list.
- [x] Cross-zone composition, third heads, broker writes, summaries/recommendations, and new auth rules are excluded.

## Requirement Provenance

- `broker_activity` selection — operator G1 approval for plan run `2026-08-30-af27c9c1`.
- Feed rules — F0001-S0004.
- Broker timeline and scope — F0002-S0007.
- Zone assembly and inactive Broker stub — F0038-S0002/S0004.
- Durable thread and fail-closed intent route — F0039.

## Result

**PASS — Phase A acceptance criteria are specific, measurable, role-scoped, and bounded.**
