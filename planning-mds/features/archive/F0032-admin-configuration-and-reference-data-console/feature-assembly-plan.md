# F0032 Feature Assembly Plan - Admin Configuration & Reference Data Console

**Owner:** Architect  
**Status:** G0 approved for feature action  
**Run ID:** `2026-07-06-f0ef8526`  
**Last Updated:** 2026-07-06

## Goal

Implement the first governed runtime configuration console as a single vertical slice: persisted configuration domains, draft/validate/compare/publish/rollback/audit lifecycle, admin-only API surface, and an internal frontend workspace. The slice must not replace module-owned execution paths for queues, saved views, reports, document generation, or product-schema authoring.

## Source Contracts

| Contract | Path | Implementation Use |
|----------|------|--------------------|
| Feature PRD | `planning-mds/features/F0032-admin-configuration-and-reference-data-console/PRD.md` | User workflows, boundaries, stories, success criteria |
| Architecture | `planning-mds/features/F0032-admin-configuration-and-reference-data-console/ARCHITECTURE.md` | Service boundaries, lifecycle rules, NFRs |
| ADR | `planning-mds/architecture/decisions/ADR-032-admin-configuration-console-contract.md` | Governed configuration facade and publish/rollback contract |
| Data model | `planning-mds/architecture/data-model.md#13-admin-configuration-and-reference-data-console-f0032` | Tables, indexes, migration order |
| API contract | `planning-mds/api/nebula-api.yaml` tag `AdminConfiguration` | Endpoint paths, DTO shapes, response codes |
| Security policy | `planning-mds/security/policies/policy.csv` section `§6 Admin configuration console (F0032)` | Casbin resource/action rows |
| JSON schemas | `planning-mds/schemas/admin-configuration-*.schema.json` | Request/response parity checks |
| KG lookup | `python3 scripts/kg/lookup.py F0032` | Canonical capability, endpoint, policy, schema, and entity bindings |

## Slice Order

1. Backend domain and persistence foundation.
2. Application DTOs, repository, validation adapters, and lifecycle service.
3. Minimal API endpoints, authorization, ProblemDetails, and OpenAPI/schema parity.
4. Frontend admin-configuration workspace, route, API hooks, and guarded mutation flows.
5. Focused backend/frontend tests, security/deployability evidence, signoff, KG reconciliation, and PM closeout.

## Backend Implementation Plan

### Domain Entities

Create the following files under `engine/src/Nebula.Domain/Entities/`:

| File | Public Type | Required Members |
|------|-------------|------------------|
| `ConfigurationDomain.cs` | `public class ConfigurationDomain : BaseEntity` | `string DomainKey`, `string DisplayName`, `string OwningModule`, `string Status`, `string EditableSchemaRef`, `bool SupportsRollback`, navigation collections |
| `ConfigurationDraft.cs` | `public class ConfigurationDraft : BaseEntity` | `Guid Id`, `string DomainKey`, `int BasePublishedVersion`, `int DraftVersion`, `string Status`, `string PayloadJson`, `string PayloadHash`, `Guid CreatedByUserId`, `byte[] RowVersion` |
| `ConfigurationValidationResult.cs` | `public class ConfigurationValidationResult : BaseEntity` | `Guid Id`, `Guid DraftId`, `string Status`, `string DraftPayloadHash`, `string BlockingErrorsJson`, `string WarningsJson`, `string CompareSummaryJson` |
| `PublishedOperationalConfigurationSet.cs` | `public class PublishedOperationalConfigurationSet : BaseEntity` | `Guid Id`, `string DomainKey`, `int PublishedVersion`, `string PayloadSnapshotJson`, `string PayloadHash`, `Guid PublishedByUserId`, `string PublishReason` |
| `ConfigurationRefreshStatus.cs` | `public class ConfigurationRefreshStatus : BaseEntity` | `Guid Id`, `Guid PublishedSetId`, `string ConsumerKey`, `string Status`, `DateTimeOffset? RefreshedAt`, `string? ErrorSummary` |
| `ConfigurationAuditEvent.cs` | `public class ConfigurationAuditEvent : BaseEntity` | `Guid Id`, `string DomainKey`, nullable draft/published refs, `string Action`, `string Outcome`, `Guid ActorUserId`, `string SummaryJson` |

Domain invariants:

- `ConfigurationDraft.PayloadHash` is recalculated on every update.
- `ConfigurationDraft.Status` is limited to `Draft`, `ValidationPassed`, `ValidationFailed`, `Published`, `Superseded`.
- `PublishedOperationalConfigurationSet.PublishedVersion` is monotonically increasing per `DomainKey`.
- Rollback creates a new `PublishedOperationalConfigurationSet`; it never deletes or edits prior published sets.
- `ConfigurationAuditEvent` is append-only.

### Persistence

Modify:

- `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs`
- `engine/src/Nebula.Infrastructure/DependencyInjection.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

Create:

- `engine/src/Nebula.Infrastructure/Persistence/Configurations/ConfigurationDomainConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/ConfigurationDraftConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/ConfigurationValidationResultConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/PublishedOperationalConfigurationSetConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/ConfigurationRefreshStatusConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Configurations/ConfigurationAuditEventConfiguration.cs`
- `engine/src/Nebula.Infrastructure/Persistence/Migrations/20260706140000_F0032_AdminConfiguration.cs`

Migration requirements:

- Create `ConfigurationDomains`, `ConfigurationDrafts`, `ConfigurationValidationResults`, `PublishedOperationalConfigurationSets`, `ConfigurationRefreshStatuses`, and `ConfigurationAuditEvents`.
- Use `jsonb` columns for payload, validation issue, warning, compare summary, and audit summary JSON.
- Add row-version concurrency to `ConfigurationDraft`.
- Index `ConfigurationDrafts(DomainKey, Status)`, `PublishedOperationalConfigurationSets(DomainKey, PublishedVersion) unique`, `ConfigurationAuditEvents(DomainKey, CreatedAt)`, and `ConfigurationRefreshStatuses(PublishedSetId, ConsumerKey)`.
- Seed first-release domain rows:
  - `queue-routing`
  - `workflow-sla-thresholds`
  - `search-report-defaults`
  - `template-metadata`

### Application Interfaces And DTOs

Create:

- `engine/src/Nebula.Application/DTOs/AdminConfigurationDtos.cs`
- `engine/src/Nebula.Application/Interfaces/IAdminConfigurationRepository.cs`
- `engine/src/Nebula.Application/Interfaces/IAdminConfigurationDomainAdapter.cs`
- `engine/src/Nebula.Application/Interfaces/IAdminConfigurationRefreshNotifier.cs`
- `engine/src/Nebula.Application/Services/AdminConfigurationService.cs`
- `engine/src/Nebula.Application/Validators/AdminConfigurationValidators.cs`
- `engine/src/Nebula.Infrastructure/Repositories/AdminConfigurationRepository.cs`
- `engine/src/Nebula.Infrastructure/Services/AdminConfigurationDomainAdapters.cs`
- `engine/src/Nebula.Infrastructure/Services/InProcessAdminConfigurationRefreshNotifier.cs`

Required DTO signatures:

```csharp
public sealed record AdminConfigurationDomainDto(
    string DomainKey,
    string DisplayName,
    string OwningModule,
    string Status,
    string EditableSchemaRef,
    bool SupportsRollback,
    int? CurrentPublishedVersion,
    string? DraftStatus,
    string? LastValidationStatus,
    string? LastPublishedBy,
    DateTimeOffset? LastPublishedAt);

public sealed record AdminConfigurationDomainDetailDto(
    AdminConfigurationDomainDto Domain,
    AdminConfigurationDraftDto? ActiveDraft,
    AdminConfigurationPublishedSetDto? CurrentPublishedSet,
    IReadOnlyList<AdminConfigurationRefreshStatusDto> RefreshStatuses);

public sealed record AdminConfigurationDraftUpdateRequestDto(
    JsonElement Payload,
    string? Reason);

public sealed record AdminConfigurationPublishRequestDto(
    string Reason,
    int? TargetPublishedVersion);
```

Service methods:

```csharp
Task<IReadOnlyList<AdminConfigurationDomainDto>> ListDomainsAsync(ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationDomainDetailDto? Result, string? Error)> GetDomainAsync(string domainKey, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationDraftDto? Result, string? Error)> CreateDraftAsync(string domainKey, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationDraftDto? Result, string? Error)> UpdateDraftAsync(Guid draftId, AdminConfigurationDraftUpdateRequestDto request, byte[] rowVersion, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationValidationResultDto? Result, string? Error)> ValidateDraftAsync(Guid draftId, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationValidationResultDto? Result, string? Error)> CompareDraftAsync(Guid draftId, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationPublishedSetDto? Result, string? Error)> PublishDraftAsync(Guid draftId, AdminConfigurationPublishRequestDto request, ICurrentUserService user, CancellationToken ct);
Task<(AdminConfigurationPublishedSetDto? Result, string? Error)> RollbackAsync(string domainKey, AdminConfigurationPublishRequestDto request, ICurrentUserService user, CancellationToken ct);
Task<PaginatedResult<AdminConfigurationAuditEventDto>> ListAuditEventsAsync(AdminConfigurationAuditQuery query, ICurrentUserService user, CancellationToken ct);
```

Adapter boundaries:

- `QueueRoutingConfigurationAdapter` reads and validates F0022 queue/routing shapes but does not route work or alter queue execution outside publish.
- `WorkflowSlaConfigurationAdapter` governs `WorkflowSlaThreshold` payloads and validates default plus LOB-specific thresholds.
- `SearchReportDefaultsConfigurationAdapter` validates saved-view/report default metadata without replacing F0023 saved-view CRUD or report projection freshness.
- `TemplateMetadataConfigurationAdapter` validates template metadata visibility/status without handling upload, preview, issue, or regeneration.

### Endpoint Plan

Create `engine/src/Nebula.Api/Endpoints/AdminConfigurationEndpoints.cs` and register `app.MapAdminConfigurationEndpoints();` in `Program.cs`.

Endpoint table:

| Method + Path | Handler | Policy Action | Success | Notable Failure Responses |
|---------------|---------|---------------|---------|---------------------------|
| `GET /admin/configuration-domains` | `ListDomains` | `admin_configuration:read` | `200` list | `401`, `403` |
| `GET /admin/configuration-domains/{domainKey}` | `GetDomain` | `read` | `200` detail | `403`, `404` |
| `POST /admin/configuration-domains/{domainKey}/drafts` | `CreateDraft` | `draft` | `201` draft | `403`, `404`, `409` active draft |
| `PATCH /admin/configuration-drafts/{draftId}` | `UpdateDraft` | `draft` | `200` draft | `400`, `403`, `404`, `412` stale row version |
| `POST /admin/configuration-drafts/{draftId}/validation` | `ValidateDraft` | `validate` | `200` validation result | `403`, `404`, `409` unsupported/conflict |
| `GET /admin/configuration-drafts/{draftId}/comparison` | `CompareDraft` | `validate` | `200` compare result | `403`, `404`, `409` stale base |
| `POST /admin/configuration-drafts/{draftId}/publish` | `PublishDraft` | `publish` | `200` published set | `403`, `404`, `409` stale/validation mismatch |
| `POST /admin/configuration-domains/{domainKey}/rollback` | `Rollback` | `rollback` | `200` published set | `403`, `404`, `409` unsupported/ineligible |
| `GET /admin/configuration-audit-events` | `ListAuditEvents` | `audit` | `200` page | `403` |

All non-2xx responses must use existing `ProblemDetailsHelper` patterns or add narrow helper methods for `validation_required`, `active_draft_exists`, `stale_published_version`, and `rollback_not_supported`.

### Authorization And Policy Sync

Modify:

- `planning-mds/security/policies/policy.csv` only if runtime policy rows are missing or need exact action/resource sync.
- Existing embedded/runtime Casbin policy resource if policy rows are duplicated outside planning docs.

Rules:

- Admin can `read`, `draft`, `validate`, `publish`, `rollback`, `audit`.
- ConfigurationSteward can `read`, `draft`, `validate`.
- OperationsManager can `read`, `validate`.
- ComplianceQualityLead can `read`, `audit`.
- External users must receive `403` without hidden draft/audit/payload details.

## Frontend Implementation Plan

Create:

- `experience/src/pages/AdminConfigurationPage.tsx`
- `experience/src/features/admin-configuration/index.ts`
- `experience/src/features/admin-configuration/types.ts`
- `experience/src/features/admin-configuration/hooks.ts`
- `experience/src/features/admin-configuration/components/AdminConfigurationWorkspace.tsx`
- `experience/src/features/admin-configuration/components/ConfigurationDomainCatalog.tsx`
- `experience/src/features/admin-configuration/components/ConfigurationDomainDetail.tsx`
- `experience/src/features/admin-configuration/components/ConfigurationDraftEditor.tsx`
- `experience/src/features/admin-configuration/components/ValidationCompareDrawer.tsx`
- `experience/src/features/admin-configuration/components/PublishRollbackDialog.tsx`
- `experience/src/features/admin-configuration/components/ConfigurationAuditWorkspace.tsx`
- `experience/src/features/admin-configuration/tests/AdminConfigurationWorkspace.test.tsx`
- `experience/src/features/admin-configuration/tests/ConfigurationDraftEditor.test.tsx`

Modify:

- `experience/src/App.tsx` to add protected route `/admin/configuration`.
- `experience/src/components/layout/Sidebar.tsx` to add an internal Admin Configuration navigation item if the existing sidebar pattern supports admin/internal links.
- `experience/src/services/api.ts` to add typed API functions or reuse the existing request helper pattern.
- `experience/src/mocks/handlers.ts` and `experience/src/mocks/data.ts` for UI tests when MSW handlers are the local pattern.

Frontend guardrails:

- No marketing/hero layout; build a dense operational workspace.
- Use existing layout, `Tabs`, `Modal`, `Select`, `Badge`, `Skeleton`, and form components.
- Keep domain catalog, detail, editor, validation/compare, publish/rollback, and audit visible as task surfaces.
- Disable publish unless the latest validation is passing and matches the current draft hash.
- Show unsupported domains without edit controls.
- Do not expose payload snippets in audit rows unless the API returns them.

## Mutation Traceability

| Mutation | Source Method | Persistent Effects | Audit Action | Timeline |
|----------|---------------|--------------------|--------------|----------|
| Create draft | `CreateDraftAsync` | `ConfigurationDraft` from current published snapshot | `DraftCreated` | No global timeline required for catalog read; audit is required |
| Update draft | `UpdateDraftAsync` | Draft payload/hash/status, row version | `DraftUpdated` | No runtime behavior change |
| Validate draft | `ValidateDraftAsync` | `ConfigurationValidationResult`, draft status | `ValidationPassed` or `ValidationFailed` | No runtime behavior change |
| Publish draft | `PublishDraftAsync` | New `PublishedOperationalConfigurationSet`, refresh rows, draft published/superseded | `Published` or `PublishFailed` | Append activity event for operational governance |
| Rollback | `RollbackAsync` | New published set copied from eligible prior version, refresh rows | `RollbackPublished` or `RollbackFailed` | Append activity event |

## Validation Checkpoints

| Gate | Required Checks |
|------|-----------------|
| G0 | `validate-feature-evidence.py --feature F0032 --run-id 2026-07-06-f0ef8526 --stage G0`, `git diff --check` |
| G1 | Runtime preflight for existing backend/frontend before edits; dependency restore/build status captured |
| G2 | Backend unit/integration tests for lifecycle and authorization; frontend component tests for workspace/editor; OpenAPI/schema parity; self-review |
| G3 | Code review and security review, including Casbin policy checks, payload redaction, and audit immutability |
| G4 | Operator approval after review evidence is complete |
| G5 | Required signoffs from Quality Engineer, Code Reviewer, Security Reviewer, DevOps, and Architect |
| G6 | Candidate evidence validation with manifest, commands log, lifecycle log, test artifacts, and diff artifact |
| G7 | KG regeneration/checks and as-built binding reconciliation for F0032 entities/endpoints/policies/schemas |
| G8 | PM closeout, tracker sync, archive decision, deferred follow-ups |

## Known Non-Blocking Follow-Ups

- Cross-instance cache invalidation remains deferred; first implementation uses in-process refresh status as approved in Phase B.
- Additional no-code schema authoring remains out of scope and stays with F0034.
