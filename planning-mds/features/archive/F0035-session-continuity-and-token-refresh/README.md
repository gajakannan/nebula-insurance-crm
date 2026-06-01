# F0035: Session Continuity & Token Refresh

**Status:** Done (Archived 2026-05-24; evidence run `2026-05-24-c92b16b6`)
**Archived:** 2026-05-24
**Priority:** High
**Phase:** Release Enablement / Platform Operations

## Purpose

Stop unexpected login redirects during active Nebula usage while preserving authentication and authorization boundaries. The feature introduces silent token renewal, an idle-warning modal with grace period, route-plus-form-state preservation across forced re-auth, semantic distinction between auth-error classes, and MVP telemetry to measure the reduction in user-visible interruptions.

## Documents

- [PRD](./PRD.md)
- [Status](./STATUS.md)
- [Getting Started](./GETTING-STARTED.md)

## User Stories

- [F0035-S0001 — Silent Token Renewal with Concurrent Request Coalescing](./F0035-S0001-silent-token-renewal.md)
- [F0035-S0002 — Idle Warning Modal with Grace Period](./F0035-S0002-idle-warning-modal.md)
- [F0035-S0003 — Forced Re-Auth with Route and Form State Preservation](./F0035-S0003-forced-reauth-context-restore.md)
- [F0035-S0004 — Auth Error Semantic Distinction (401-expired / 401-failed / 403-denied)](./F0035-S0004-auth-error-semantics.md)
- [F0035-S0005 — Session Continuity Telemetry Events (MVP)](./F0035-S0005-session-telemetry-events.md)

## Dependencies

- F0005 IdP Migration: Keycloak → authentik
- F0009 Authentication + Role-Based Login
- F0018 Policy Lifecycle & Policy 360 (high-API-count validation surface)
- F0020 Document Management & ACORD Intake (high-API-count validation surface)
- F0034 Product Schema Registry and Dynamic LOB Attributes (dynamic panel validation surface)
- F0033 Structured Logging and QE Toolchain Activation (Serilog baseline for telemetry events)

## Architecture

Governed by **ADR-024 — Session Continuity and Token Refresh Architecture** (`planning-mds/architecture/decisions/ADR-024-session-continuity-and-token-refresh.md`).

F0035 introduces no new entities — it is a session-runtime feature. No ERD is required. The most useful architectural diagram is the silent-renewal-with-coalescing sequence; Mermaid + ASCII shown below.

### Silent Renewal Sequence (Happy Path, Coalesced)

```mermaid
sequenceDiagram
    actor User
    participant FE as Frontend (oidc-client-ts + interceptor)
    participant API as Nebula API (Backend)
    participant IDP as authentik

    User->>FE: Open Policy 360 (6 concurrent protected requests)
    par 6 concurrent
        FE->>API: GET /accounts/{id}
        FE->>API: GET /policies/{id}
        FE->>API: GET /policies/{id}/versions
        FE->>API: GET /policies/{id}/endorsements
        FE->>API: GET /timeline/...
        FE->>API: GET /documents?parent.type=policy
    end
    API-->>FE: 401 + WWW-Authenticate: invalid_token + ProblemDetails type=auth/token-expired (x6, near-simultaneous)

    Note over FE: Coalescing semaphore: first 401 wins,<br/>other 5 queue on the same renewal promise
    FE->>IDP: POST /token (refresh_token grant)
    IDP-->>FE: 200 { access_token, refresh_token }
    Note over FE: New token stored;<br/>all 6 queued requests retry once

    par 6 concurrent (retry)
        FE->>API: GET /accounts/{id} (Bearer: new token)
        FE->>API: GET /policies/{id}
        FE->>API: GET /policies/{id}/versions
        FE->>API: GET /policies/{id}/endorsements
        FE->>API: GET /timeline/...
        FE->>API: GET /documents?parent.type=policy
    end
    API-->>FE: 200 (x6)
    FE->>API: POST /internal/telemetry/session-continuity<br/>{ events: [silent-renewal-success<br/>{ coalesced_request_count: 6 }] }
    API-->>FE: 202 Accepted

    Note over User,IDP: User sees the page populate normally.<br/>No redirect. No visible interruption.
```

### Silent Renewal Sequence (ASCII)

```
User              Frontend                      Nebula API           authentik
 |                  |                              |                    |
 |  open page       |                              |                    |
 |----------------->|  6 concurrent protected GETs |                    |
 |                  |----------------------------->|                    |
 |                  |                              |                    |
 |                  |<------- 6x 401 + auth/token-expired --------------|
 |                  |                              |                    |
 |                  | coalescing semaphore:        |                    |
 |                  |  - first 401 triggers renewal|                    |
 |                  |  - other 5 queue on promise  |                    |
 |                  |                              |                    |
 |                  |---- POST /token (refresh) -------------------->   |
 |                  |<--- 200 { new access_token } ------------------   |
 |                  |                              |                    |
 |                  | retry 6 originals (Bearer: new token)              |
 |                  |----------------------------->|                    |
 |                  |<-------- 6x 200 -------------|                    |
 |                  |                              |                    |
 |                  | POST telemetry event:        |                    |
 |                  |  silent-renewal-success      |                    |
 |                  |  coalesced_request_count=6   |                    |
 |                  |----------------------------->|                    |
 |                  |<------ 202 Accepted ---------|                    |
 |  page renders    |                              |                    |
 |<-----------------|                              |                    |
 |                  |                              |                    |
```

For the forced-re-auth-with-form-state-restore sequence, the idle-warning modal flow, and the auth-error classification dispatch matrix, see ADR-024 sections §3, §4, and §1 respectively, and the individual story files.

## Notes

This README is the lightweight index. Authoritative content lives in `PRD.md` (requirements), `ADR-024` (architecture), and the colocated story files. Plan run `2026-05-23-41109356` performed both Phase A enrichment and Phase B architecture; the corresponding base run evidence package is at `planning-mds/operations/evidence/runs/2026-05-23-41109356/`.

Implementation and closeout evidence lives at `planning-mds/operations/evidence/runs/2026-05-24-c92b16b6/`.
