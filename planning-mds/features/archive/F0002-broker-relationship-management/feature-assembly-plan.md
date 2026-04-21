# Feature Assembly Plan — F0002 Pending Items (Completion Sprint)

## Feature
- **Feature Name:** F0002 — Broker & MGA Relationship Management (Completion Sprint)
- **Story File(s):** F0002-S0001 through F0002-S0008
- **Date:** 2026-03-08
- **Owner:** Backend Developer + Frontend Developer + Quality Engineer

---

## Pending Scope (Strict)

Seven gaps remain before F0002 can be declared complete. No other work is in scope.

| # | Gap | Affected Files |
|---|-----|----------------|
| G1 | Casbin authorization checks missing on broker/contact/timeline endpoints | BrokerEndpoints.cs, ContactEndpoints.cs, TimelineEndpoints.cs |
| G2 | Deactivation does not set Status=Inactive | BrokerService.cs |
| G3 | Contact API/UI contract drift — frontend consumes flat array; backend returns paginated envelope; ContactDto missing RowVersion | ContactDto.cs, useBrokerContacts.ts, BrokerContactsTab.tsx, ContactFormModal.tsx |
| G4 | Timeline pagination not implemented (backend limit-only; story requires page/pageSize/totalCount/totalPages) | ITimelineRepository.cs, TimelineRepository.cs, TimelineService.cs, TimelineEndpoints.cs, useBrokerTimeline.ts, BrokerTimelineTab.tsx |
| G5 | OpenAPI path missing for POST /brokers/{brokerId}/reactivate | nebula-api.yaml |
| G6 | Missing integration tests: reactivation endpoint, deactivation status, Casbin rejections, timeline pagination | BrokerEndpointTests.cs + new ContactEndpointTests.cs + TimelineEndpointTests.cs |
| G7 | STATUS.md does not reflect actual completion state | STATUS.md |

**AI scope:** None — no neuron/ changes.
**Migration:** None — no schema changes required (RowVersion already on BaseEntity).

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|-------|---------------|-------|--------|
| Backend (engine/) | G1: Casbin checks on BrokerEndpoints (search/create/read/update/delete), ContactEndpoints (read/create/update/delete), TimelineEndpoints (read); follow HasAccessAsync pattern from DashboardEndpoints.cs | Backend Dev | Pending |
| Backend (engine/) | G2: BrokerService.DeleteAsync — set broker.Status = "Inactive" alongside IsDeleted = true | Backend Dev | Pending |
| Backend (engine/) | G3: Add RowVersion uint to ContactDto record and MapToDto | Backend Dev | Pending |
| Backend (engine/) | G4: Timeline pagination — add ListEventsPagedAsync to ITimelineRepository; implement in TimelineRepository; update TimelineService to return PaginatedResult<TimelineEventDto>; update TimelineEndpoints to accept page/pageSize and return paginated envelope | Backend Dev | Pending |
| Backend (engine/) | G5: Add POST /brokers/{brokerId}/reactivate path to nebula-api.yaml | Backend Dev | Pending |
| Frontend (experience/) | G3-FE: useBrokerContacts.ts types response as PaginatedResponse<ContactDto>; BrokerContactsTab.tsx uses contacts.data; ContactDto type in types.ts gains rowVersion: number; ContactFormModal.tsx passes rowVersion to useUpdateContact | Frontend Dev | Pending |
| Frontend (experience/) | G4-FE: useBrokerTimeline.ts accepts page param and types response as PaginatedResponse<TimelineEventDto>; BrokerTimelineTab.tsx adds pagination controls when totalPages > 1 | Frontend Dev | Pending |
| Quality | G6: Integration tests for reactivation (200/409/404/403), DeleteBroker sets Status=Inactive, authz 403 on broker/contact/timeline, timeline paginated envelope shape | Backend Dev + QE | Pending |
| DevOps/Runtime | No new dependencies — no container/compose/env-var changes | DevOps | N/A |
| Docs | G7: Update STATUS.md to closed state for all stories | All | Pending |

---

## Dependency Order

1. Backend core fixes G1 + G2 + G3 (independent of each other — implement in parallel)
2. Backend timeline pagination G4 (independent; can proceed alongside G1/G2/G3)
3. OpenAPI update G5 (independent)
4. Frontend G3-FE (after backend G3 ContactDto contract stable)
5. Frontend G4-FE (after backend G4 timeline pagination contract stable)
6. Tests G6 (after all backend changes)
7. Docs G7 (last)

---

## Authorization Check Pattern (G1)

Follow the HasAccessAsync helper pattern from DashboardEndpoints.cs:

Endpoint-to-policy mapping (from policy.csv):
- GET /brokers          -> broker:search
- POST /brokers         -> broker:create
- GET /brokers/{id}     -> broker:read
- PUT /brokers/{id}     -> broker:update
- DELETE /brokers/{id}  -> broker:delete
- GET /contacts         -> contact:read
- POST /contacts        -> contact:create
- GET /contacts/{id}    -> contact:read
- PUT /contacts/{id}    -> contact:update
- DELETE /contacts/{id} -> contact:delete
- GET /timeline/events  -> timeline_event:read

BrokerUser paths: skip Casbin check (already scope-isolated by F0009 logic).

---

## Timeline Pagination Contract (G4)

Backend response shape for GET /timeline/events (non-BrokerUser path):
{ "data": [...], "page": 1, "pageSize": 50, "totalCount": 120, "totalPages": 3 }

Default pageSize: 50, max: 100. BrokerUser flat-list path unchanged.

---

## Contact RowVersion Fix (G3)

ContactDto gains RowVersion uint. Frontend ContactDto type gains rowVersion: number.
useBrokerContacts returns PaginatedResponse<ContactDto>. BrokerContactsTab uses contacts.data.
ContactFormModal passes contact.rowVersion to useUpdateContact mutation.

---

## Integration Checkpoints

- [ ] API contract compatibility validated (ContactDto includes rowVersion; timeline returns paginated envelope)
- [ ] Frontend contract compatibility validated (contacts hook returns paginated; timeline hook returns paginated)
- [ ] AI contract compatibility — N/A
- [ ] Test cases mapped to acceptance criteria
- [ ] Run/deploy instructions unchanged

---

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| ContactDto.RowVersion is additive breaking change for consumers expecting DTO without it | Low | Additive field; BrokerUser ContactBrokerUserDto is separate DTO unaffected | Backend Dev |
| Timeline endpoint signature change may affect dashboard feed useTimelineEvents.ts | Medium | Verify useTimelineEvents.ts in features/timeline doesn't break; limit param kept for BrokerUser path | Frontend Dev |
| Casbin check in test context (factory bypasses auth) | Low | CustomWebApplicationFactory uses no-op authz; tests for 403 need a separate client with restricted role user | QE |
