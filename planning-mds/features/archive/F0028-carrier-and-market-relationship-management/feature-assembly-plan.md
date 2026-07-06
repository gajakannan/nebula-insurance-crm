# Feature Assembly Plan — F0028: Carrier & Market Relationship Management

**Created:** 2026-07-02
**Author:** Architect Agent
**Status:** G0 PASS

## Overview

F0028 adds an internal-only carrier market relationship module with backend persistence/API, timeline events, search projection participation, and a frontend directory/detail workspace. The implementation follows the approved Phase B contract: no carrier API sync, no rating/pricing, no recommendation engine, and no external broker visibility.

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Backend domain, EF config, repository, service, validators, endpoints | S0001-S0006 | Establish source-of-truth API, concurrency, authorization, timeline, and projection behavior. |
| 2 | Backend tests | S0001-S0006 | Lock authorization, validation, persistence, timeline, and related-work behavior before UI wiring. |
| 3 | Frontend feature slice, routes, mocks, tests | S0001-S0006 | Build directory/detail workflows against stable API contract. |
| 4 | QE, review, security, deployability evidence | S0001-S0006 | Satisfy harness gates G2-G6. |
| 5 | Architect KG reconciliation + PM closeout | S0001-S0006 | Bind as-built source paths, validate drift, archive and publish evidence. |

## Existing Code (Must Be Modified)

| File | Current State | F0028 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | Existing DbSets and `ApplyConfigurationsFromAssembly` | Add DbSets for `CarrierMarket`, `CarrierMarketContact`, `CarrierAppetiteNote`, `CarrierAppointment`, `CarrierMarketActivityLink`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories/services and `ISearchProjectionService` | Register `ICarrierMarketRepository`, `CarrierMarketRepository`, and `CarrierMarketService`. |
| `engine/src/Nebula.Api/Program.cs` | Maps endpoint groups | Add `app.MapCarrierMarketEndpoints()`. |
| `engine/src/Nebula.Infrastructure/Services/SearchProjectionService.cs` | Backfills and refreshes source records into `SearchDocument` | Add carrier market projection rows and internal-only visibility. |
| `experience/src/services/api.ts` | Shared API client and exported service helpers | Add carrier market request helpers. |
| `experience/src/App.tsx` | Protected routes | Add `/carrier-markets` and `/carrier-markets/:carrierMarketId`. |
| `experience/src/mocks/handlers.ts` | MSW handlers | Add carrier market handlers for frontend tests. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/CarrierMarket*.cs` | Domain | Carrier market aggregate and child records. |
| `engine/src/Nebula.Application/DTOs/CarrierMarketDtos.cs` | Application | Request/response DTOs. |
| `engine/src/Nebula.Application/Interfaces/ICarrierMarketRepository.cs` | Application | Persistence contract. |
| `engine/src/Nebula.Application/Services/CarrierMarketService.cs` | Application | Use cases, validation handoff, timeline, projection refresh. |
| `engine/src/Nebula.Application/Validators/CarrierMarketValidators.cs` | Application | FluentValidation rules. |
| `engine/src/Nebula.Infrastructure/Repositories/CarrierMarketRepository.cs` | Infrastructure | EF repository. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/CarrierMarket*.cs` | Infrastructure | EF table/index/concurrency mapping. |
| `engine/src/Nebula.Api/Endpoints/CarrierMarketEndpoints.cs` | API | Minimal API routes. |
| `engine/tests/Nebula.Tests/Integration/CarrierMarketEndpointTests.cs` | Tests | API and authorization regression coverage. |
| `experience/src/features/carrierMarkets/*` | Frontend | Types, hooks, list/detail components. |
| `experience/src/pages/CarrierMarketsPage.tsx` | Frontend | Directory/search page. |
| `experience/src/pages/CarrierMarketDetailPage.tsx` | Frontend | Relationship workspace. |
| `experience/src/pages/tests/CarrierMarketsPage.integration.test.tsx` | Tests | UI workflow coverage. |

## Backend Contract

Create these domain types with `BaseEntity` audit/concurrency fields:

```csharp
public class CarrierMarket : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NaicCode { get; set; }
    public string? AmBestRating { get; set; }
    public string Status { get; set; } = "Active";
    public string MarketType { get; set; } = "Admitted";
    public Guid? RelationshipOwnerUserId { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GeneralEmail { get; set; }
    public string? MainPhone { get; set; }
    public string? Notes { get; set; }
    public ICollection<CarrierMarketContact> Contacts { get; set; } = [];
    public ICollection<CarrierAppetiteNote> AppetiteNotes { get; set; } = [];
    public ICollection<CarrierAppointment> Appointments { get; set; } = [];
    public ICollection<CarrierMarketActivityLink> ActivityLinks { get; set; } = [];
}
```

```csharp
public class CarrierMarketContact : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public CarrierMarket? CarrierMarket { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string RolesJson { get; set; } = "[]";
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }
}
```

```csharp
public class CarrierAppetiteNote : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public CarrierMarket? CarrierMarket { get; set; }
    public string? LineOfBusiness { get; set; }
    public string? Region { get; set; }
    public string AppetiteLevel { get; set; } = "Open";
    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Source { get; set; }
}
```

```csharp
public class CarrierAppointment : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public CarrierMarket? CarrierMarket { get; set; }
    public string AppointmentStatus { get; set; } = "NotAppointed";
    public string StatesJson { get; set; } = "[]";
    public string? LineOfBusiness { get; set; }
    public string? AppointmentNumber { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string? Notes { get; set; }
}
```

```csharp
public class CarrierMarketActivityLink : BaseEntity
{
    public Guid CarrierMarketId { get; set; }
    public CarrierMarket? CarrierMarket { get; set; }
    public string RelatedEntityType { get; set; } = "Submission";
    public Guid RelatedEntityId { get; set; }
    public string RelationshipKind { get; set; } = "GeneralReference";
    public string? Note { get; set; }
}
```

Repository methods:

```csharp
Task<PaginatedResult<CarrierMarket>> ListAsync(string? q, string? status, string? marketType, int page, int pageSize, CancellationToken ct);
Task<CarrierMarket?> GetByIdAsync(Guid id, CancellationToken ct);
Task<CarrierMarket?> GetByCodeAsync(string code, CancellationToken ct);
Task AddAsync(CarrierMarket market, CancellationToken ct);
Task AddContactAsync(CarrierMarketContact contact, CancellationToken ct);
Task AddAppetiteNoteAsync(CarrierAppetiteNote note, CancellationToken ct);
Task AddAppointmentAsync(CarrierAppointment appointment, CancellationToken ct);
Task AddActivityLinkAsync(CarrierMarketActivityLink link, CancellationToken ct);
Task<CarrierMarketContact?> GetContactAsync(Guid marketId, Guid contactId, CancellationToken ct);
Task<CarrierAppetiteNote?> GetAppetiteNoteAsync(Guid marketId, Guid noteId, CancellationToken ct);
Task<CarrierAppointment?> GetAppointmentAsync(Guid marketId, Guid appointmentId, CancellationToken ct);
Task<IReadOnlyList<CarrierMarketContact>> ListContactsAsync(Guid marketId, CancellationToken ct);
Task<IReadOnlyList<CarrierAppetiteNote>> ListAppetiteNotesAsync(Guid marketId, CancellationToken ct);
Task<IReadOnlyList<CarrierAppointment>> ListAppointmentsAsync(Guid marketId, CancellationToken ct);
Task<IReadOnlyList<CarrierMarketActivityLink>> ListActivityLinksAsync(Guid marketId, CancellationToken ct);
```

## Endpoint And Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|----------------------|-------------|----------|----------------|------------------|---------------|-------------|--------------------|------------------|------------------|
| Market Directory | Search/open | `GET /carrier-markets` | `ListAsync` | `CarrierMarket` | `carrier_market:read` and `carrier_market:search` | N/A | 400 validation | N/A | Results scoped and page survives reload. |
| Market Directory | New Market Save | `POST /carrier-markets` | `CreateAsync` | `CarrierMarket` | `carrier_market:create` | N/A | 400/409/422 | `CarrierMarketCreated` | Created row opens and persists after reload. |
| Market Workspace | Edit profile Save | `PUT /carrier-markets/{id}` | `UpdateAsync` | `CarrierMarket` | `carrier_market:update` | `If-Match` required | 400/409/412/422 | `CarrierMarketUpdated` | Updated fields/timeline visible after reload. |
| Contacts tab | Add contact | `POST /carrier-markets/{id}/contacts` | `CreateContactAsync` | `CarrierMarketContact` | `carrier_market:manage_contact` | N/A | 400/409/422 | `CarrierMarketContactCreated` | Contact row persists. |
| Contacts tab | Edit contact | `PUT /carrier-markets/{id}/contacts/{contactId}` | `UpdateContactAsync` | `CarrierMarketContact` | `carrier_market:manage_contact` | `If-Match` required | 400/409/412/422 | `CarrierMarketContactUpdated` | Updated contact persists. |
| Appetite tab | Add note | `POST /carrier-markets/{id}/appetite-notes` | `CreateAppetiteNoteAsync` | `CarrierAppetiteNote` | `carrier_market:manage_appetite` | N/A | 400/422 | `CarrierAppetiteNoteCreated` | Note and freshness state persist. |
| Appetite tab | Edit note | `PUT /carrier-markets/{id}/appetite-notes/{noteId}` | `UpdateAppetiteNoteAsync` | `CarrierAppetiteNote` | `carrier_market:manage_appetite` | `If-Match` required | 400/412/422 | `CarrierAppetiteNoteUpdated` | Updated note persists. |
| Appointments tab | Add appointment | `POST /carrier-markets/{id}/appointments` | `CreateAppointmentAsync` | `CarrierAppointment` | `carrier_market:manage_appointment` | N/A | 400/422 | `CarrierAppointmentCreated` | Appointment persists. |
| Appointments tab | Edit appointment | `PUT /carrier-markets/{id}/appointments/{appointmentId}` | `UpdateAppointmentAsync` | `CarrierAppointment` | `carrier_market:manage_appointment` | `If-Match` required | 400/412/422 | `CarrierAppointmentUpdated` | Updated appointment persists. |
| Related Work tab | Link submission/policy | `POST /carrier-markets/{id}/activity-links` | `CreateActivityLinkAsync` | `CarrierMarketActivityLink` | `carrier_market:link_activity` plus related read check | N/A | 400/403/404/422 | `CarrierMarketActivityLinked` | Link appears on market workspace. |

## Casbin Enforcement

- Every endpoint loops through `ICurrentUserService.Roles` and calls `IAuthorizationService.AuthorizeAsync(role, "carrier_market", action)`.
- `BrokerUser` and `ExternalUser` receive no policy rows and must be denied.
- Activity links must also verify the related submission/policy exists and is readable before saving.

## Timeline Events

All successful mutations create `ActivityTimelineEvent` rows with `EntityType = "CarrierMarket"`, `EntityId = carrierMarketId`, actor fields from `ICurrentUserService`, `OccurredAt = now`, pre-rendered description, and JSON payload with changed record identifiers.

## Search Projection

Extend `SearchProjectionService` to emit `SearchDocument.ObjectType = "CarrierMarket"` with title `Name`, status, owner, region/LOB from related notes/appointments where available, target URL `/carrier-markets/{id}`, and internal roles only.

## Frontend Contract

- Add `experience/src/features/carrierMarkets/types.ts` mirroring OpenAPI camelCase fields.
- Add React Query hooks for list/detail and mutations with `If-Match` headers using `rowVersion`.
- Add `CarrierMarketsPage` for searchable directory and `CarrierMarketDetailPage` for tabs: Overview, Contacts, Appetite, Appointments, Related Work.
- Use existing UI components and route layout patterns; no marketing/landing page.

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|---------------|-------|--------|
| Backend (`engine/`) | Entities, EF configs, migration, repository, service, validators, endpoints, tests | Backend Developer | Pending |
| Frontend (`experience/`) | Feature slice, routes, mocks, pages, tests | Frontend Developer | Pending |
| AI (`neuron/`) | None | N/A | Not in scope |
| Quality | Acceptance criteria evidence, API/UI test execution | QE | Pending |
| DevOps/Runtime | Runtime preflight, migration/deployability check | DevOps | Pending |
| Reviews | Code and security reports | Reviewers | Pending |

## Dependency Order

```
Step 0 (Architect): assembly plan + evidence validation
Step 1 (Backend): domain/EF/repository/service/validators/endpoints
Step 2 (Backend): integration/unit tests
Step 3 (Frontend): API helpers, hooks, routes, pages, mocks
Step 4 (Frontend): UI tests
Step 5 (QE/Security/Review): evidence and signoff
Step 6 (Architect/PM): KG reconciliation and closeout
```

## Integration Checkpoints

- Backend checkpoint: all F0028 endpoints return RFC7807 errors, enforce Casbin, persist data, and emit timeline events.
- Frontend checkpoint: users can search/open market records, create/update profile/contact/appetite/appointment/link records, and reload to see persisted data.
- Security checkpoint: external roles denied; appetite/appointment data never appears in external-facing payloads.
- Deployability checkpoint: migrations apply locally and runtime preflight records healthy API/frontend surfaces.

## Integration Checklist

- [x] API contract compatibility identified
- [x] Frontend contract compatibility identified
- [x] AI contract compatibility marked not in scope
- [x] Test cases mapped to acceptance criteria
- [x] Developer-owned fast-test responsibilities identified by layer
- [x] Required runtime evidence artifacts identified
- [x] Framework vs solution boundary reviewed
- [x] Mutation traceability tables completed
- [x] Render-only tests are not used to close mutation stories
- [x] Run/deploy instructions require runtime preflight

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Sensitive appetite/appointment data exposure | High | Security review plus internal-only policy tests | Security Reviewer |
| Migration/runtime drift | Medium | DevOps preflight and deployability evidence | DevOps |
| Scope creep into integrations/rating | Medium | Keep all endpoints manual CRM records only | Architect |

## JSON Serialization Convention

Use camelCase JSON through existing ASP.NET minimal API defaults and frontend TypeScript types. Dates use ISO strings; `DateOnly` values serialize as `YYYY-MM-DD`. `rowVersion` is returned as integer and passed back in `If-Match`.

## DI Registration Changes

Register `ICarrierMarketRepository`, `CarrierMarketRepository`, and `CarrierMarketService`. Validators are discovered by `AddValidatorsFromAssemblyContaining<BrokerCreateValidator>()`.

## Casbin Policy Sync

F0028 policy rows are already added to `planning-mds/security/policies/policy.csv`. Implementation must verify runtime Casbin loads the updated policy source and tests exercise `carrier_market` actions.

## Knowledge-Graph Binding Plan

G7 must bind as-built paths for `entity:carrier-market`, child entities, endpoints, schemas, and policy rules into `code-index.yaml`, regenerate symbols if needed, and run `scripts/kg/validate.py --check-symbols` plus `--check-drift`.
