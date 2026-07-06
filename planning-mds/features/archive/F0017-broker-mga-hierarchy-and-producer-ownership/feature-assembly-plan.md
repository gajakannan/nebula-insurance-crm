# Feature Assembly Plan — F0017: Broker/MGA Hierarchy, Producer Ownership & Territory Management

**Created:** 2026-06-07
**Author:** Architect Agent
**Status:** Draft (authored at feature-action G0; governed by ADR-026)
**Run:** 2026-06-07-771a5ef6

> **Purpose:** Implementation execution plan for F0017. Turns the ADR-026 OpenAPI/schema surface (already authored in plan run `2026-06-06-5fb353e9`) into concrete file paths, code signatures, logic flows, and per-endpoint response tables. Route names and payload semantics are fixed by `planning-mds/api/nebula-api.yaml` and `planning-mds/schemas/*` — do not change them without an ADR amendment (ADR-026 Follow-up Actions).

## Overview

F0017 makes the commercial-P&C distribution structure explicit: an arbitrary-depth self-referencing **distribution hierarchy** (S0001–S0002), **effective-dated producer ownership** (S0003), **effective-dated territory management with overlap prevention** (S0004), and **immutable audit/timeline** for every structural mutation (S0005). It introduces four new entities and reuses the F0002 broker patterns (clean architecture, `BaseEntity` audit fields, xmin concurrency, `ActivityTimelineEvent` timeline, Casbin `HasAccessAsync`, `ProblemDetailsHelper`). No economics (F0025), no hierarchy-aware read enforcement or rollups (F0037).

## Build Order

| Step | Scope | Stories | Rationale |
|------|-------|---------|-----------|
| 1 | Domain entities + EF configs + migration (DistributionNode, ProducerOwnership, Territory, TerritoryAssignment) | S0001,S0003,S0004 | Persistence foundation; everything else depends on it |
| 2 | DistributionNode service + reparent (cycle/orphan guards, ancestry recompute) + ancestors/descendants reads | S0001,S0002,S0005 | Hierarchy core + cached ancestry |
| 3 | ProducerOwnership service (effective-dated assign/reassign + as-of read) | S0003,S0005 | Effective-dating engine (shared period logic) |
| 4 | Territory + TerritoryAssignment service (create, assign with overlap rule, as-of reads) | S0004,S0005 | Territory model reuses period logic |
| 5 | API endpoints (9) + Casbin policy rules + DI registration | S0001–S0005 | Expose contract surface |
| 6 | Frontend panels (hierarchy / ownership / territory / timeline) on broker detail | S0002,S0003,S0004 | UI-bearing slice (build validated in CI — see Scope Breakdown) |
| 7 | Tests: unit (services/validators), integration (endpoints), E2E (panels) | all | Evidence for QE/CR signoff |

## Existing Code (Must Be Modified)

> Paths resolved from `code-index.yaml` + `lookup.py F0017`.

| File | Current State | F0017 Change |
|------|---------------|----------------|
| `engine/src/Nebula.Infrastructure/Persistence/NebulaDbContext.cs` | DbSets for existing entities | **Expand** — add `DbSet<DistributionNode>`, `DbSet<ProducerOwnership>`, `DbSet<Territory>`, `DbSet<TerritoryAssignment>` + apply new configurations |
| `engine/src/Nebula.Api/Program.cs` (or `DependencyInjection.cs`) | endpoint mapping + DI | **Expand** — `MapDistributionEndpoints()`, `MapProducerOwnershipEndpoints()`, `MapTerritoryEndpoints()`; register 3 services + 4 repositories |
| `engine/src/Nebula.Infrastructure/Authorization/policy.csv` (+ embedded copy) | Casbin rules | **Expand** — add `distribution_node:read/update`, `producer_ownership:read/assign`, `territory:read/create/assign` for authorized roles |
| `experience/src/pages/BrokerDetailPage.tsx` | broker detail tabs | **Expand** — add Hierarchy / Ownership / Territories / Timeline panels (per PRD ASCII layouts) |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/DistributionNode.cs` | Domain | Self-referencing hierarchy node |
| `engine/src/Nebula.Domain/Entities/ProducerOwnership.cs` | Domain | Effective-dated ownership period |
| `engine/src/Nebula.Domain/Entities/Territory.cs` | Domain | Territory definition |
| `engine/src/Nebula.Domain/Entities/TerritoryAssignment.cs` | Domain | Effective-dated territory membership |
| `engine/src/Nebula.Application/DTOs/DistributionNodeDto.cs` (+ `DistributionNodeParentRequestDto`, `DistributionNodeAncestorsResponseDto`) | Application | Hierarchy DTOs |
| `engine/src/Nebula.Application/DTOs/ProducerOwnershipDto.cs` (+ request/lookup-response) | Application | Ownership DTOs |
| `engine/src/Nebula.Application/DTOs/TerritoryDto.cs`, `TerritoryAssignmentDto.cs` (+ request/lookup-response) | Application | Territory DTOs |
| `engine/src/Nebula.Application/Interfaces/IDistributionNodeRepository.cs`, `IProducerOwnershipRepository.cs`, `ITerritoryRepository.cs`, `ITerritoryAssignmentRepository.cs` | Application | Repo contracts |
| `engine/src/Nebula.Application/Services/DistributionNodeService.cs`, `ProducerOwnershipService.cs`, `TerritoryService.cs` | Application | Business logic |
| `engine/src/Nebula.Application/Validators/*` (parent-request, ownership-assignment, territory-create, territory-member-assignment) | Application | FluentValidation |
| `engine/src/Nebula.Application/Common/DistributionDescriptionTemplates.cs` | Application | Timeline description templates |
| `engine/src/Nebula.Infrastructure/Repositories/DistributionNodeRepository.cs`, `ProducerOwnershipRepository.cs`, `TerritoryRepository.cs`, `TerritoryAssignmentRepository.cs` | Infrastructure | EF Core repos |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/DistributionNodeConfiguration.cs`, `ProducerOwnershipConfiguration.cs`, `TerritoryConfiguration.cs`, `TerritoryAssignmentConfiguration.cs` | Infrastructure | EF configs |
| `engine/src/Nebula.Infrastructure/Persistence/Migrations/<ts>_F0017_DistributionHierarchyOwnershipTerritory.cs` | Infrastructure | Schema migration |
| `engine/src/Nebula.Api/Endpoints/DistributionEndpoints.cs`, `ProducerOwnershipEndpoints.cs`, `TerritoryEndpoints.cs` | Api | Route handlers |
| `experience/src/features/distribution/**` (components, hooks, api, types, tests) | Frontend | Hierarchy/ownership/territory panels (vertical slice) |
| `engine/tests/Nebula.Tests/Unit/Distribution*/**`, `Integration/Distribution*.cs`, `Integration/ProducerOwnership*.cs`, `Integration/Territory*.cs` | Tests | Backend tests |

---

## Step 1 — Domain entities, EF configs, migration (S0001, S0003, S0004)

### Entity / DTO / Code

```csharp
// engine/src/Nebula.Domain/Entities/DistributionNode.cs
namespace Nebula.Domain.Entities;

public class DistributionNode : BaseEntity
{
    public string NodeType { get; set; } = default!;      // MGA | Broker | SubBroker | Producer (advisory order, not enforced)
    public string DisplayName { get; set; } = default!;
    public Guid? ParentId { get; set; }
    // Materialized ancestry path, root-first, ancestors only (excludes self), '/'-delimited GUIDs: "/{root}/{...}/{parent}".
    // Empty string ("") for a root node. Parsed to ancestryPath[] (root->node, excluding self) for the API.
    public string AncestryPath { get; set; } = "";
    public int Depth { get; set; }                         // = ancestor count = AncestryPath segment count
    public int ChildCount { get; set; }                    // maintained on reparent of any child
    public bool IsActive { get; set; } = true;

    public DistributionNode? Parent { get; set; }
    public ICollection<DistributionNode> Children { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/ProducerOwnership.cs
public class ProducerOwnership : BaseEntity
{
    public string ScopeType { get; set; } = default!;      // Account | BrokerRelationship
    public Guid ScopeId { get; set; }
    public Guid ProducerNodeId { get; set; }               // FK -> DistributionNode (Producer)
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }             // null = open period
    public string? AssignmentReason { get; set; }
    public DistributionNode? ProducerNode { get; set; }
}

// engine/src/Nebula.Domain/Entities/Territory.cs
public class Territory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string CriteriaJson { get; set; } = "{}";       // criteria object (string->string) stored as JSON text
    public bool IsActive { get; set; } = true;
    public ICollection<TerritoryAssignment> Assignments { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/TerritoryAssignment.cs
public class TerritoryAssignment : BaseEntity
{
    public Guid TerritoryId { get; set; }
    public string MemberType { get; set; } = default!;     // Broker | Producer
    public Guid MemberId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? AssignmentReason { get; set; }
    public Territory? Territory { get; set; }
}
```

### EF Configuration (key rules — mirror BrokerConfiguration)

- All four: `HasKey(Id)`, `RowVersion` → `xmin`/`xid` concurrency token, `HasQueryFilter(e => !e.IsDeleted)`, audit columns required.
- **DistributionNode:** `NodeType` maxlen 20 required; `DisplayName` maxlen 255 required; `AncestryPath` text (no maxlen); self-ref `HasOne(Parent).WithMany(Children).HasForeignKey(ParentId).OnDelete(Restrict)`. Indexes: `IX_DistributionNodes_ParentId`, `IX_DistributionNodes_AncestryPath` (prefix LIKE for descendant queries), `IX_DistributionNodes_NodeType_IsActive`.
- **ProducerOwnership:** `ScopeType` maxlen 20; `EffectiveFrom`/`EffectiveTo` as `date`; `AssignmentReason` maxlen 500; FK `ProducerNodeId` → DistributionNode (Restrict). **Filtered unique index** for the single-open-period invariant — raw SQL below.
- **Territory:** `Name` maxlen 150 required; `Description` maxlen 500; `CriteriaJson` text. **Filtered unique index** on active name — raw SQL below.
- **TerritoryAssignment:** `MemberType` maxlen 20; dates; FK `TerritoryId` → Territory (Restrict). Index `IX_TerritoryAssignments_Member_AsOf` on `(MemberType, MemberId, EffectiveFrom)`.

### Migration SQL (expression/filtered indexes — not expressible via fluent API)

```sql
-- One OPEN ownership period per (scopeType, scopeId)
CREATE UNIQUE INDEX "IX_ProducerOwnership_OpenPeriod"
  ON "ProducerOwnership" ("ScopeType", "ScopeId")
  WHERE "EffectiveTo" IS NULL AND "IsDeleted" = false;

-- Unique active territory name (case-insensitive)
CREATE UNIQUE INDEX "IX_Territories_ActiveName"
  ON "Territories" (LOWER("Name"))
  WHERE "IsActive" = true AND "IsDeleted" = false;

-- One OPEN territory assignment per (territoryId, memberType, memberId)
CREATE UNIQUE INDEX "IX_TerritoryAssignments_OpenPeriod"
  ON "TerritoryAssignments" ("TerritoryId", "MemberType", "MemberId")
  WHERE "EffectiveTo" IS NULL AND "IsDeleted" = false;
```

### Integration Checkpoint (after Step 1)
- [ ] `dotnet ef migrations add` produces a migration that applies cleanly against the `nebula-db` container; `dotnet ef database update` succeeds.
- [ ] All four tables + three filtered indexes exist (verify via `\d+` in psql).

---

## Step 2 — DistributionNodeService: reparent + ancestry, ancestors/descendants reads (S0001, S0002, S0005)

### Logic Flow — `SetParentAsync(Guid nodeId, Guid? parentId, uint rowVersion, ICurrentUserService user, ct) → (DistributionNodeDto?, string? error)`

1. Load node (incl. deactivated). Null → `not_found` (404).
2. If `parentId == nodeId` → `distribution_node_self_parent` (422).
3. If `parentId` set: load parent. Null/inactive/soft-deleted → `invalid_distribution_parent` (422).
4. **Cycle guard:** if `parentId` is `nodeId` or appears in the *subtree* of `nodeId` (i.e. proposed parent's `AncestryPath` contains `nodeId`, or proposed parent == a descendant) → `distribution_node_cycle` (409). Check: `parent.AncestryPath.Contains("/" + nodeId + "/")` OR `parent.Id == nodeId`.
5. Set `node.RowVersion = rowVersion` (optimistic concurrency → `DbUpdateConcurrencyException` → 412 `precondition_failed`).
6. Compute `newAncestry = parent is null ? "" : parent.AncestryPath + "/" + parent.Id` (normalized leading `/`); `newDepth = segments(newAncestry)`.
7. Update node: `ParentId`, `AncestryPath`, `Depth`, audit fields.
8. **Recompute descendants:** load all nodes whose `AncestryPath` LIKE `oldNodePrefix%` where `oldNodePrefix = node.AncestryPath + "/" + node.Id`; for each, replace the prefix up-to-and-including `node.Id` with `newAncestry + "/" + node.Id`, recompute `Depth`. Bulk-update.
9. Maintain `ChildCount`: decrement old parent, increment new parent.
10. Emit timeline events: one `DistributionNodeReparented` for the node; for bulk descendant recompute, one event per affected node under a shared `CorrelationId` (ADR-026 §5).
11. `unitOfWork.CommitAsync(ct)`. Return mapped DTO.

### Logic Flow — `GetAncestorsAsync(nodeId, ct) → DistributionNodeAncestorsResponseDto?`
1. Load node. Null → 404. 2. Parse `AncestryPath` → ancestor id list (root→parent). 3. Batch-load ancestor nodes preserving order. 4. Return `{ node: DistributionNodeDto, ancestors: DistributionNodeDto[] }`.

### Logic Flow — `ListDescendantsAsync(nodeId, depth=2, page, pageSize, ct) → PaginatedResult<DistributionNodeDto>`
1. Load node. Null → 404. 2. `depth = Math.Clamp(depth ?? 2, 1, 5)` (cap). 3. Query nodes where `AncestryPath LIKE node.{path}/{id}%` AND `Depth <= node.Depth + depth`; order by `Depth, DisplayName`; paginate (pageSize ≤ 100).

### Mutation Traceability

| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Broker detail → Hierarchy panel → Set/Change Parent | Save parent | `PUT /distribution-nodes/{nodeId}/parent` | `DistributionNodeService.SetParentAsync` | `DistributionNode.ParentId/AncestryPath/Depth` | `distribution_node:update` | `If-Match` → `rowVersion`; 412 on stale | self-parent 422, cycle 409, invalid parent 422 | `DistributionNodeReparented` (+ per-descendant under correlationId) | Integration: reparent persists new ancestry after reload; cycle attempt → 409; self → 422 |

### Casbin Enforcement
- Resource `distribution_node`, actions `read` (ancestors/descendants), `update` (parent). Pattern: `HasAccessAsync(user, authz, "distribution_node", action)` looping roles (mirror BrokerEndpoints).

### Timeline Event
- EventType `DistributionNodeReparented`; EntityType `DistributionNode`; EntityId `node.Id`; EventDescription `$"{displayName} re-parented to {newParentName ?? "(root)"}"`; EventPayloadJson `{ id, nodeType, oldParentId, newParentId, oldDepth, newDepth, correlationId }` per `activity-event-payloads.schema.json`.

### HTTP Responses
| Status | Body | Condition |
|--------|------|-----------|
| 200 | DistributionNode | Reparent OK |
| 403 | ProblemDetails (`policy_denied`) | Casbin deny |
| 404 | ProblemDetails (`not_found`) | Node missing |
| 409 | ProblemDetails (`distribution_node_cycle`) | Cycle-producing move |
| 412 | ProblemDetails (`precondition_failed`) | Stale rowVersion |
| 422 | ProblemDetails (`distribution_node_self_parent` / `invalid_distribution_parent`) | Self-parent / invalid parent |

---

## Step 3 — ProducerOwnershipService: effective-dated assign + as-of read (S0003, S0005)

### Shared effective-dating rule (reused in Step 4)
On assign with `effectiveFrom = D`:
1. Validate `effectiveTo` (if provided) `> effectiveFrom` else `*_period_invalid` (422).
2. Load current **open** period for the scope (`EffectiveTo IS NULL`).
3. If new `D` ≤ open.`EffectiveFrom` → backdating before existing period → `*_period_invalid` (422) (correction path, not silent insert).
4. Close open period: `open.EffectiveTo = D` (one day before, or `D` per ADR — use `D` as exclusive end). Open new row `EffectiveFrom = D, EffectiveTo = null`.
5. Overlap check against any active period covering `[D, ∞)` for the scope → `ownership_period_overlap` (409).
6. Both writes + timeline events committed in one `unitOfWork.CommitAsync`.

### `AssignAsync(ProducerOwnershipAssignmentRequest req, uint rowVersion, user, ct) → (ProducerOwnershipDto?, error)`
- Validates producer node exists & is `Producer` type; scope rowVersion via If-Match (412); applies shared rule; emits `ProducerOwnershipAssigned`/`ProducerOwnershipReassigned` timeline events.

### `GetAsOfAsync(scopeType, scopeId, asOf?, ct) → ProducerOwnershipLookupResponseDto`
- `asOf ?? today`; return the period where `EffectiveFrom <= asOf AND (EffectiveTo IS NULL OR EffectiveTo > asOf)`; include history list.

### Mutation Traceability
| Screen / Entry Point | User Action | Endpoint | Service Method | Entity / Carrier | Authorization | Concurrency | Validation Failure | Audit / Timeline | Test Expectation |
|---|---|---|---|---|---|---|---|---|---|
| Ownership panel → Assign/Reassign | Save owner | `POST /producer-ownership` | `ProducerOwnershipService.AssignAsync` | `ProducerOwnership.*` | `producer_ownership:assign` | `If-Match` scope rowVersion; 412 | overlap 409, bad order/backdate 422 | `ProducerOwnershipAssigned`/`Reassigned` | Integration: reassign closes prior + opens new; as-of read returns prior owner |

### HTTP Responses
| 201 ProducerOwnership | success | · 403 policy_denied · 404 not_found · 409 ownership_period_overlap · 412 precondition_failed · 422 ownership_period_invalid |

---

## Step 4 — Territory + TerritoryAssignment service (S0004, S0005)

### `CreateTerritoryAsync(TerritoryCreateRequest req, user, ct) → (TerritoryDto?, error)`
1. Normalize name; if active territory with same LOWER(name) exists → `territory_duplicate_name` (409). 2. Persist (criteria object → `CriteriaJson`). 3. Emit `TerritoryCreated`. 4. Commit.

### `AssignMemberAsync(territoryId, TerritoryMemberAssignmentRequest req, uint rowVersion, user, ct) → (TerritoryAssignmentDto?, error)`
- Load territory (404). Validate member exists. Apply **shared effective-dating rule** keyed by `(territoryId, memberType, memberId)`. Overlap (a member already holding a conflicting active assignment for the period) → `territory_assignment_overlap` (409); bad order → `territory_assignment_period_invalid` (422); stale territory rowVersion → 412. Emit `TerritoryMemberAssigned`. Commit.

### Reads
- `ListMembersAsOfAsync(territoryId, asOf?, page, pageSize)` → `PaginatedTerritoryAssignmentList`.
- `GetAssignmentForMemberAsOfAsync(memberType, memberId, asOf?)` → `TerritoryAssignmentLookupResponse`.

### Mutation Traceability
| Territory panel → Create | Create | `POST /territories` | `TerritoryService.CreateTerritoryAsync` | `Territory.*` | `territory:create` | N/A (create) | duplicate name 409 | `TerritoryCreated` | Integration: duplicate active name → 409 |
| Territory panel → Assign Member | Assign | `POST /territories/{id}/members` | `TerritoryService.AssignMemberAsync` | `TerritoryAssignment.*` | `territory:assign` | If-Match territory rowVersion; 412 | overlap 409, bad order 422 | `TerritoryMemberAssigned` | Integration: overlapping active assign → 409; as-of read returns member set |

---

## Step 5 — API endpoints + Casbin + DI

- Three endpoint classes mirroring `BrokerEndpoints` (static `Map…Endpoints`, `RequireAuthorization()`, `HasAccessAsync` Casbin loop, FluentValidation → `ProblemDetailsHelper.ValidationError`, If-Match parse → 428 if missing/unparseable, `DbUpdateConcurrencyException` → `ProblemDetailsHelper.ConcurrencyConflict()` 412). Map error codes to the precise ProblemDetails codes in the response tables above (extend `ProblemDetailsHelper` with `Conflict(code)` and `UnprocessableContent(code)` helpers if not present).
- **Casbin policy.csv** additions (per authorized role per ADR-026 §6 — `DistributionManager`, `DistributionUser`, `Admin` for mutations; broad authenticated read): `p, <role>, distribution_node, read` / `update`; `producer_ownership, read`/`assign`; `territory, read`/`create`/`assign`. Copy updated `policy.csv` to the embedded resource location (see Casbin Policy Sync).
- **DI:** register `DistributionNodeService`, `ProducerOwnershipService`, `TerritoryService` (scoped) + four repositories against their interfaces; map the three endpoint groups in `Program.cs`.

---

## Step 6 — Frontend (experience/) — vertical slice `experience/src/features/distribution/`

- Panels on `BrokerDetailPage`: **Hierarchy** (tree with Set/Change Parent, inline cycle/self rejection), **Ownership** (current owner + history + "as of" date picker), **Territories** (member list + Create/Assign, 409 overlap surfaced), **Timeline** (reuse existing timeline component filtered to the node).
- TanStack Query hooks per endpoint; RHF + AJV using the shared schemas; semantic theme tokens only (no raw palette classes); light/dark verified.
- Co-locate components/hooks/api/types/tests under `features/distribution/`.
- **NOTE (runtime constraint):** the experience toolchain (vitest/lint/build) cannot run on the local `/mnt/c` WSL mount; frontend validation is deferred to CI and recorded as a coverage waiver in `coverage-report.md` for run `2026-06-07-771a5ef6`.

---

## Step 7 — Tests

- **Unit (xUnit):** cycle/self-parent/orphan guards; ancestry recompute correctness (multi-level reparent); effective-dating period close/open; overlap rejection; duplicate territory name; validators.
- **Integration:** each of the 9 endpoints — happy path + each error code (403/404/409/412/422); timeline event emitted on success / absent on rejection; as-of reads.
- **E2E (Playwright, CI):** reparent in hierarchy panel; assign/reassign owner with as-of read; create territory + assign member + overlap 409.

---

## Scope Breakdown

| Layer | Required Work | Owner | Status |
|------|----------------|-------|--------|
| Backend (`engine/`) | 4 entities, 4 configs, migration, 3 services, 4 repos, validators, 9 endpoints, Casbin rules, DI | Backend Developer | Pending |
| Frontend (`experience/`) | distribution feature slice + 4 panels on broker detail | Frontend Developer | Pending (build validated in CI) |
| AI (`neuron/`) | None | — | N/A (no AI scope) |
| Quality | test-plan, unit/integration/E2E, coverage, AC mapping | Quality Engineer | Pending |
| DevOps/Runtime | EF migration runs in container; no new topology | DevOps | Pending (deployability check) |

## Dependency Order

```
Step 0 (Architect):  this plan + STATUS signoff matrix
Step 1 (Backend):    entities + configs + migration
  ──── checkpoint: migration applies in nebula-db container ────
Step 2-4 (Backend):  services (hierarchy, ownership, territory) + validators + repos
Step 5 (Backend):    endpoints + Casbin + DI
  ──── checkpoint: 9 endpoints return contract-correct statuses (integration green) ────
Step 6 (Frontend):   distribution panels (build in CI)
Step 7 (QE):         E2E + coverage
```

## Integration Checkpoints

### After Step 1 (Persistence)
- [ ] Migration applies cleanly in `nebula-db`; 4 tables + 3 filtered indexes present.

### After Step 5 (API)
- [ ] All 9 endpoints registered; integration tests assert each documented status code.
- [ ] Casbin: `distribution_node:update`, `producer_ownership:assign`, `territory:create`, `territory:assign` enforced (403 for unauthorized role; ExternalUser denied).
- [ ] Timeline events emitted on every successful mutation; none on rejection.

### Cross-Story Verification
- [ ] Full lifecycle: create node tree → reparent (cycle rejected) → assign owner (reassign closes prior) → create territory + assign member (overlap rejected) → timeline reflects all.
- [ ] ProblemDetails format consistent (code + traceId) with existing endpoints.
- [ ] As-of reads return point-in-time correct results for ownership and territory.

## Integration Checklist

- [x] API contract compatibility validated (routes/schemas fixed by ADR-026 / nebula-api.yaml — plan follows verbatim)
- [x] Frontend contract compatibility validated (shared schemas reused)
- [ ] Test cases mapped to acceptance criteria (QE at G2)
- [x] Developer-owned fast-test responsibilities identified by layer
- [x] Required runtime evidence artifacts identified (coverage/report/log paths under run folder)
- [x] Framework vs solution boundary reviewed (all work under `{PRODUCT_ROOT}/engine`,`/experience`)
- [x] Mutation traceability tables completed for every assign/update/create path
- [x] Render-only tests not used to close mutation stories
- [ ] Run/deploy instructions updated (GETTING-STARTED at Step 1)

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Deep-tree reparent recompute cost | medium | Materialized path + prefix index; async recompute deferred to F0037 | Backend |
| Effective-dating edge cases (backdate/overlap) | medium | Shared period rule + filtered unique open-period index as DB backstop | Backend |
| Frontend cannot validate locally (WSL `/mnt/c`) | low | Defer experience toolchain to CI; documented coverage waiver | Frontend/QE |
| Casbin policy embedded-copy drift | low | Casbin Policy Sync step copies policy.csv to embedded resource | Backend |

## JSON Serialization Convention

- `rowVersion` is PostgreSQL `xmin` (`uint`) serialized as a **string** per schema (`{ "rowVersion": { "type": "string" } }`). Map `BaseEntity.RowVersion` (uint) → string in DTOs.
- `ancestryPath` exposed as `string[]` (UUIDs root→node, excluding self); stored as `'/'`-delimited text. Parse on read, build on write.
- Dates (`effectiveFrom`/`effectiveTo`) are `DateOnly` ↔ JSON `"date"` (`YYYY-MM-DD`). `changedAt` is `date-time`.
- `criteria` is `object<string,string>` ↔ `CriteriaJson` text.
- camelCase property naming (existing global `JsonSerializerOptions`).

## DI Registration Changes

Register (scoped): `DistributionNodeService`, `ProducerOwnershipService`, `TerritoryService`; `IDistributionNodeRepository→DistributionNodeRepository`, `IProducerOwnershipRepository→ProducerOwnershipRepository`, `ITerritoryRepository→TerritoryRepository`, `ITerritoryAssignmentRepository→TerritoryAssignmentRepository`. Map endpoint groups in `Program.cs`.

## Casbin Policy Sync

After editing `policy.csv`, copy it to the embedded resource location used at runtime (same path the build embeds for `BrokerEndpoints` authorization) so the container picks up the new `distribution_node`/`producer_ownership`/`territory` rules. Verify via an integration test that an unauthorized role receives 403.

## Knowledge-Graph Binding Plan (baseline for G7 reconciliation)

**Prediction** (G7 diffs as-built against this):

- **New canonical nodes expected:** `entity:distribution-node`, `entity:producer-ownership`, `entity:territory`, `entity:territory-assignment` (entities already declared in `feature-mappings`/`lookup`; confirm bindings at G7). Capabilities `distribution-hierarchy-management`, `producer-ownership-management`, `territory-management` already exist — affirm, do not duplicate.
- **Anticipated `code-index.yaml` glob bindings:**
  - `entity:distribution-node` → `engine/src/Nebula.Domain/Entities/DistributionNode.cs`, `engine/src/Nebula.Application/{DTOs,Services,Interfaces,Validators}/Distribution*.cs`, `engine/src/Nebula.Infrastructure/Repositories/DistributionNodeRepository.cs`, `engine/src/Nebula.Infrastructure/Persistence/Configurations/DistributionNodeConfiguration.cs`, `engine/src/Nebula.Api/Endpoints/DistributionEndpoints.cs`, `engine/tests/Nebula.Tests/**/Distribution*`, `experience/src/features/distribution/**`
  - `entity:producer-ownership` → `**/ProducerOwnership*.cs`, ownership endpoint/tests
  - `entity:territory` / `entity:territory-assignment` → `**/Territory*.cs`, `**/TerritoryAssignment*.cs`, territory endpoint/tests
- **Endpoints/schemas/policy_rules** already bound in the plan run; confirm coverage at G7.

"No new shared canonical *semantics* beyond these feature entities; reuses existing timeline/Casbin/ProblemDetails patterns."
