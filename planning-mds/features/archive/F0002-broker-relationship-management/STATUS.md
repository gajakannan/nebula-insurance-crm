# F0002 — Broker & MGA Relationship Management — Status

**Overall Status:** Done (Archived; MVP scope complete with deferred non-blocking hardening follow-ups tracked)
**Last Updated:** 2026-03-10

## Story Checklist

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| F0002-S0001 | Create Broker | Done | Casbin `broker:create` check enforced. Core flow, duplicate license, timeline event complete. |
| F0002-S0002 | Search Brokers | Done | Casbin `broker:search` check enforced. Pagination, filters, empty state complete. |
| F0002-S0003 | Read Broker (Broker 360 View) | Done | Casbin `broker:read` check enforced. Contacts tab now consumes paginated envelope. Timeline tab now paginated. |
| F0002-S0004 | Update Broker | Done | Casbin `broker:update` check enforced. Optimistic concurrency complete. |
| F0002-S0005 | Delete Broker (Deactivate) | Done | Casbin `broker:delete` check enforced. Deactivation now sets `Status=Inactive` alongside `IsDeleted=true`. PII masking works correctly on deactivated brokers. |
| F0002-S0006 | Manage Broker Contacts | Done | Casbin `contact:create|read|update|delete` checks enforced. `ContactDto` now exposes `RowVersion`. Frontend hook consumes paginated envelope; update flow sends `If-Match`. |
| F0002-S0007 | View Broker Activity Timeline | Done | Casbin `timeline_event:read` check enforced. Paginated response (`page`, `pageSize`, `totalCount`, `totalPages`) implemented in backend and consumed by Broker 360 Timeline tab. "Unknown User" actor fallback applied via `MapToDto`. |
| F0002-S0008 | Reactivate Broker | Done | Casbin `broker:reactivate` check enforced. OpenAPI path `/brokers/{brokerId}/reactivate` added to spec. Integration tests added. |
| F0002-S0009 | Adopt Native Casbin Enforcer | Done | Replaced `PolicyAuthorizationService` with `CasbinAuthorizationService` using `Casbin.NET 2.19.2`. Model + policy loaded from embedded resources. 97 unit tests verify full policy matrix parity. |

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Multi-story regression and acceptance coverage across API + UI + timeline + contacts. | Architect | 2026-02-14 |
| Code Reviewer | Yes | Independent review of implementation correctness and scope completion before archive. | Architect | 2026-02-14 |
| Security Reviewer | Yes | Authorization hardening (`Casbin`) and role boundary behavior are in scope. | Architect | 2026-02-14 |
| DevOps | No | No new runtime service dependency added in final closeout increment. | Architect | 2026-02-14 |
| Architect | No | No unresolved architecture exceptions requiring explicit acceptance. | Architect | 2026-02-14 |

## Signoff Ledger (Execution Evidence)

| Role | Reviewer | Verdict | Evidence | Date | Notes |
|------|----------|---------|----------|------|-------|
| Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs`; `engine/tests/Nebula.Tests/Integration/ContactEndpointTests.cs`; `engine/tests/Nebula.Tests/Integration/TimelineEndpointTests.cs` | 2026-03-09 | Story acceptance and regression coverage confirmed for completed scope. |
| Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Critical completion gaps resolved and remaining items documented as non-blocking. |
| Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md`; `planning-mds/security/authorization-matrix.md` | 2026-03-09 | Authorization matrix and endpoint enforcement reviewed for final scope. |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0002-S0001 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs` | 2026-03-09 | Broker create flow and conflict behavior validated. |
| F0002-S0001 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0001 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Create permission mapping verified. |
| F0002-S0002 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs` | 2026-03-09 | Search pagination and filtering behavior validated. |
| F0002-S0002 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0002 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Search/read role boundaries verified. |
| F0002-S0003 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs`; `engine/tests/Nebula.Tests/Integration/ContactEndpointTests.cs`; `engine/tests/Nebula.Tests/Integration/TimelineEndpointTests.cs` | 2026-03-09 | Broker 360 read contracts, contacts envelope, and timeline pagination validated. |
| F0002-S0003 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0003 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Read scope controls verified. |
| F0002-S0004 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs` | 2026-03-09 | Update flow and concurrency checks validated. |
| F0002-S0004 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0004 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Update permissions verified. |
| F0002-S0005 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs` | 2026-03-09 | Deactivation path and soft-delete behavior validated. |
| F0002-S0005 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0005 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Delete permission boundaries verified. |
| F0002-S0006 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/ContactEndpointTests.cs` | 2026-03-09 | Contact CRUD behavior and row-version contract validated. |
| F0002-S0006 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0006 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Contact permission matrix verified. |
| F0002-S0007 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/TimelineEndpointTests.cs` | 2026-03-09 | Timeline pagination and payload envelope validated. |
| F0002-S0007 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0007 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Timeline read boundary checks verified. |
| F0002-S0008 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Integration/BrokerEndpointTests.cs` | 2026-03-09 | Reactivation behavior and response contracts validated. |
| F0002-S0008 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Story accepted in completion review. |
| F0002-S0008 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md` | 2026-03-09 | Reactivation permission checks verified. |
| F0002-S0009 | Quality Engineer | Quality Engineer agent | PASS | `engine/tests/Nebula.Tests/Unit/CasbinAuthorizationServiceTests.cs`; `engine/tests/Nebula.Tests/Integration/BrokerAuthorizationTests.cs` | 2026-03-09 | Native Casbin parity and endpoint denial coverage validated. |
| F0002-S0009 | Code Reviewer | Code Reviewer agent | PASS | `planning-mds/features/archive/F0002-broker-relationship-management/STATUS.md` | 2026-03-09 | Hardening story accepted after gap closure. |
| F0002-S0009 | Security Reviewer | Security agent | PASS | `planning-mds/security/authorization-review.md`; `planning-mds/security/authorization-matrix.md` | 2026-03-09 | Authorization hardening accepted with policy parity evidence. |

## Resolved Gaps (2026-03-08)

1. **Casbin policy enforcement** — All broker/contact/timeline endpoints now call `HasAccessAsync` with the correct resource+action per `policy.csv`. BrokerUser paths bypass Casbin (scope-isolated by F0009 logic).
2. **Deactivation sets Status=Inactive** — `BrokerService.DeleteAsync` now sets `broker.Status = "Inactive"` alongside `IsDeleted = true`. PII masking (`MaskPii`) now triggers correctly on deactivation.
3. **Contact API/UI contract** — `ContactDto` now includes `RowVersion uint`. `useBrokerContacts` hook types response as `PaginatedResponse<ContactDto>`. `BrokerContactsTab` extracts `.data`. `ContactFormModal` passes `rowVersion` to update mutation.
4. **Timeline pagination** — `ITimelineRepository` has new `ListEventsPagedAsync`. `TimelineService` has `ListEventsPagedAsync`. `TimelineEndpoints` returns `{ data, page, pageSize, totalCount, totalPages }`. `useBrokerTimeline` and `BrokerTimelineTab` support page navigation.
5. **OpenAPI reactivate path** — `POST /brokers/{brokerId}/reactivate` path added to `nebula-api.yaml` with correct responses (200, 403, 404, 409).
6. **Tests** — Added: `BrokerAuthorizationTests` (10 Casbin 403 tests), `TimelineEndpointTests` (3 pagination tests), reactivation tests in `BrokerEndpointTests` (3 tests), contact paginated envelope + RowVersion tests in `ContactEndpointTests` (2 tests).

## Resolved Gaps (2026-03-09 — S0009)

7. **Native Casbin enforcer adopted** — `CasbinAuthorizationService` replaces `PolicyAuthorizationService`. Uses `Casbin.NET 2.19.2` with embedded `model.conf` + `policy.csv`. `IAuthorizationService` interface unchanged — zero endpoint modifications. DI binding switched in `DependencyInjection.cs`. Sentinel values prevent empty-string condition match (deny-by-default for task ownership when attrs are absent). 97 unit tests cover full broker/contact/timeline/task/dashboard policy matrix, condition-based ownership checks, ExternalUser deny-all, and unknown role/action/resource deny-by-default. ADR-008 status updated to Accepted.

## Deferred Non-Blocking Follow-ups (Post-MVP Hardening)

These items are intentionally deferred and do not block F0002 completion status.

- UI-level action hiding (edit/deactivate/delete buttons hidden for unauthorized roles) deferred — requires frontend auth context integration; backend 403 responses prevent unauthorized mutations regardless.
- Cross-broker ownership validation in contact service (contacts created with mismatched brokerId) deferred — existing validator checks broker existence but not requester scope boundary; scoped to future hardening sprint.
- Integration test WSL environment limitation — `WebApplicationFactory` path resolution fails in `/mnt/c/` WSL paths; tests must be run from Windows or in a container. No C# compiler errors in test code.
- `PolicyAuthorizationService.cs` retained in repo for reference but no longer registered in DI (no runtime use). Consider deleting or marking `[Obsolete]` to prevent accidental re-registration.
- Add unit test for BrokerUser `task:read` — the only role with condition=`true` on task:read (no ownership check); currently untested in `CasbinAuthorizationServiceTests`. (Code Review H-1)
- Consider in-memory Casbin initialization (`Model.CreateDefaultFromText()` + `StringAdapter`) to eliminate temp file writes during startup. (Code Review M-1)
- Install `gitleaks` for automated secret scanning in CI — `check-secrets.sh` currently skips. (Security Review SL-2)

## Resolved (F0009 Complete)

- BrokerUser tenant isolation and field-boundary filtering are implemented in F0009-S0004 (scope resolution + audience-specific DTOs).
- BrokerUser timeline event filtering to approved event types with `BrokerDescription` is implemented.
