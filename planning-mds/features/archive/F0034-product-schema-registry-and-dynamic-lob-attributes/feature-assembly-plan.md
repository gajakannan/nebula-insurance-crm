# Feature Assembly Plan - F0034: Product Schema Registry and Dynamic LOB Attributes

**Created:** 2026-05-07
**Author:** Architect Agent
**Status:** Draft - ready for G0 validation; runtime work remains blocked until explicit Phase B approval

## Overview

F0034 introduces a governed product schema registry, schema-pinned product attribute payloads on lifecycle carriers, frontend/backend validator parity, and the first Cyber `1.0.0` dynamic attribute panel. Existing lifecycle entities keep stable core columns; product-specific data is stored in `attributes_json`, pinned by `lob_product_version_id`, validated against one resolved bundle, and rendered from schema metadata instead of Cyber-specific JSX or ordinary relational columns.

Policy remains an ADR-018 parent aggregate. Policy-level product attributes persist on `PolicyVersion`; `Policy` has no independent first-slice product attribute payload.

## Source Artifacts

| Artifact | Role in this plan |
|----------|-------------------|
| `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/PRD.md` | Product scope, release slicing, screen responsibilities, acceptance criteria |
| `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/F0034-S0001-*.md` through `F0034-S0007-*.md` | Story-level requirements and edge cases |
| `planning-mds/architecture/decisions/ADR-018-policy-aggregate-versioning-and-reinstatement-window.md` | Policy parent/version semantics and immutable Policy LOB rule |
| `planning-mds/architecture/decisions/ADR-020-lob-extensible-attribute-architecture.md` | Registry tables, carrier columns, sentinels, invariants, deterministic ids |
| `planning-mds/architecture/decisions/ADR-021-form-engine-rhf-ajv-shadcn-registry.md` | Dynamic form engine, pin-on-open, widget governance |
| `planning-mds/architecture/decisions/ADR-022-validator-equivalence-restricted-profile.md` | Restricted schema profile and normalized `lobErrors[]` parity contract |
| `planning-mds/architecture/decisions/ADR-023-rules-governance-jsonlogic.md` | JsonLogic rule envelope, custom operation parity, rule evaluation order |
| `planning-mds/architecture/lob-extensible-attribute-plan.md` | Stage-0 source plan for runtime delivery details |
| `planning-mds/architecture/data-model.md` | F0034 registry and carrier table contract |
| `planning-mds/architecture/openapi-30-projection-matrix.md` | OpenAPI 3.0.3 projection limits |
| `planning-mds/architecture/validation-perf-baseline.md` | Required runtime measurement gates |
| `planning-mds/api/nebula-api.yaml` | Existing F0034 API paths and OpenAPI schemas |
| `planning-mds/schemas/lob-attribute-envelope.schema.json` | Static envelope schema |
| `planning-mds/schemas/lob-schema-bundle.schema.json` | Static bundle response schema |
| `planning-mds/schemas/lob-validation-problem-details.schema.json` | Static LOB ProblemDetails schema |
| `planning-mds/security/authorization-matrix.md` and `planning-mds/security/policies/policy.csv` | `lob_schema:*` authorization contract |

## Assembly Slice Order

Use this order for implementation. Items inside the same bracket may be done in parallel after prerequisites are met; do not cross-parallelize across numbered entries.

| Entry | Slice | Stories | Owners | Rationale |
|-------|-------|---------|--------|-----------|
| 1 | `[Decision Lock Evidence]` | F0034-S0001 | Architect, Code Reviewer | Confirm ADR-018 and ADR-020 through ADR-023 are Accepted, OpenAPI projection matrix is accepted, and validation baseline exists before runtime work. |
| 2 | `[Registry Foundation]` | F0034-S0002 | Backend, DevOps, Security, QE | Registry entities, migrations, resolver, cache, endpoints, and sentinel seed data must exist before carriers can pin versions. |
| 3 | `[Lifecycle Carrier Pinning]` | F0034-S0003 | Backend, Security, QE | Submission, Renewal, PolicyVersion, and PolicyEndorsement gain pinned envelopes and write guards; Policy stays a read-through parent. |
| 4 | `[Cyber Bundle Sources and Validator Equivalence]` | F0034-S0004, F0034-S0006 | Architect, Backend, Frontend, QE | Author draft Cyber bundle artifacts and shared fixtures first, then prove FE/BE parity. Activation is not allowed until parity passes. |
| 5 | `[Dynamic Attribute Panel]` | F0034-S0005 | Frontend, Backend, QE | Frontend consumes registry bundles, snapshots `(productVersionId, stage)` on open, maps normalized pointers to fields, and embeds into lifecycle screens. |
| 6 | `[Cyber Activation and Lifecycle Proof]` | F0034-S0006, F0034-S0007 | Backend, Frontend, QE, Security, DevOps | Activate Cyber `1.0.0`, prove submission/policy/endorsement/renewal surfaces, and record the F0019 hardcoded-field guardrail. |
| 7 | `[Review, Evidence, and Closeout]` | F0034-S0001-S0007 | QE, DevOps, Code Reviewer, Security, PM | Runtime evidence, signoff provenance, tracker sync, and deferred follow-up capture. |

## Existing Code (Must Be Modified)

| File | Current State | F0034 Change |
|------|---------------|--------------|
| `engine/src/Nebula.Domain/Entities/Submission.cs` | 15 scalar/core properties plus 4 navigation properties; no product version pin or attributes JSON. | Add `Guid LobProductVersionId`, `string AttributesJson = "{}"`, and navigation `LobProductVersion`. |
| `engine/src/Nebula.Domain/Entities/Renewal.cs` | 18 scalar/core properties plus policy/submission/account/broker navigation; no product version pin or attributes JSON. | Add `Guid LobProductVersionId`, `string AttributesJson = "{}"`, and navigation `LobProductVersion`. |
| `engine/src/Nebula.Domain/Entities/PolicyVersion.cs` | Version snapshot stores `ProfileSnapshotJson`, `CoverageSnapshotJson`, `PremiumSnapshotJson`; no denormalized LOB or product attributes. | Add immutable `string LineOfBusiness`, `Guid LobProductVersionId`, `string AttributesJson = "{}"`, and navigation `LobProductVersion`. |
| `engine/src/Nebula.Domain/Entities/PolicyEndorsement.cs` | Endorsement reason/effective date/premium only; no denormalized LOB or product attributes. | Add immutable `string LineOfBusiness`, `Guid LobProductVersionId`, `string AttributesJson = "{}"`, and navigation `LobProductVersion`. |
| `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs` | DbSets exist for lifecycle entities, workflow, auth-adjacent tables, and idempotency. | Add `DbSet<LobProduct>`, `DbSet<LobProductVersion>`, `DbSet<LobSchemaBundle>`, `DbSet<LobBundleActivationEvent>`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs` | Maps core submission fields and indexes; no JSONB or product-version FK. | Map `AttributesJson` as `jsonb`, `LobProductVersionId` FK, and product/version indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/RenewalConfiguration.cs` | Maps renewal workflow fields and active renewal unique index; no JSONB or product-version FK. | Map `AttributesJson` as `jsonb`, `LobProductVersionId` FK, and product/version indexes. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyVersionConfiguration.cs` | Maps profile/coverage/premium snapshots as JSONB and unique `(PolicyId, VersionNumber)`. | Map `LineOfBusiness`, `AttributesJson`, `LobProductVersionId`, and projection indexes declared by activated bundles. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/PolicyEndorsementConfiguration.cs` | Maps endorsement reason/date/premium and unique `(PolicyId, EndorsementNumber)`. | Map `LineOfBusiness`, `AttributesJson`, `LobProductVersionId`, and endorsement-stage projection indexes if declared. |
| `engine/src/Nebula.Application/DTOs/SubmissionDto.cs` | `SubmissionDto` has 28 parameters; create/update DTOs do not carry `lobAttributes`. | Add `LobAttributeEnvelopeDto LobAttributes` to detail, create, and update DTOs. |
| `engine/src/Nebula.Application/DTOs/RenewalDto.cs` | `RenewalDto` has 31 parameters; create DTO carries optional `lineOfBusiness` only. | Add `LobAttributeEnvelopeDto LobAttributes` to detail and create DTOs; add update path only if a renewal attribute edit endpoint exists in scope. |
| `engine/src/Nebula.Application/DTOs/PolicyDtos.cs` | Policy create/from-bind/endorsement/version DTOs have coverage/profile data only. | Add `LobAttributeEnvelopeDto LobAttributes` to create, from-bind, endorsement request, PolicyVersionDto, and PolicyEndorsementDto. Add current-version `LobAttributes` to PolicyDto/PolicySummaryDto if UI reads through Policy. |
| `engine/src/Nebula.Application/Services/SubmissionService.cs` | `CreateAsync`, `UpdateAsync`, and `MapToDtoAsync` handle static fields, workflow/timeline, ABAC, completeness. | Resolve and validate product attributes before persistence, enforce triage transition, append timeline payload for attribute changes, return `LobValidationProblemDetails` on LOB failures. |
| `engine/src/Nebula.Application/Services/RenewalService.cs` | `CreateAsync`, `TransitionAsync`, `AssignAsync`, and mappers handle renewal workflow only. | Resolve and validate product attributes for create and authorized same-version writes; support `_unspecified` null-LOB path and legacy read path. |
| `engine/src/Nebula.Application/Services/PolicyService.cs` | `CreateAsync`, `EndorseAsync`, `BuildVersion`, `MapVersion`, `MapEndorsement` build static policy versions and endorsements. | Carry attributes into PolicyVersion and PolicyEndorsement. Reject independent Policy attribute payload and Policy LOB mutation after create. |
| `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs` | Minimal API group maps submission list/detail/create/update/transition/assign/timeline. | Preserve existing routes; pass `lobAttributes` DTOs into service and emit `422 application/problem+json` with LOB problem details for validation failures. |
| `engine/src/Nebula.Api/Endpoints/RenewalEndpoints.cs` | Minimal API group maps renewal list/detail/create/transition/assign/timeline. | Preserve existing routes; add LOB attribute payload handling and validation problem response mapping. |
| `engine/src/Nebula.Api/Endpoints/PolicyEndpoints.cs` | Minimal API group maps policy create/update/issue/endorse/cancel/reinstate/version/coverage routes. | Add attribute payloads to create/from-bind/endorse/version responses without adding a Policy parent attributes source. |
| `engine/src/Nebula.Api/Program.cs` | Registers endpoint groups and app services; no LOB schema endpoint group. | Register `LobSchemaService`, validators, and call `app.MapLobSchemaEndpoints();`. |
| `engine/src/Nebula.Infrastructure/DependencyInjection.cs` | Registers repositories, authorization, documents, background services, memory cache. | Register LOB schema repository/source, resolver, validator, rule evaluator, profile linter, activation service, and cache. |
| `engine/src/Nebula.Infrastructure/Nebula.Infrastructure.csproj` | No dynamic schema or JsonLogic packages. | Add exact, non-floating package references for backend JSON Schema and JsonLogic engines selected during implementation. |
| `experience/package.json` | Has TanStack Query and lucide; no direct AJV, AJV formats/errors, React Hook Form, or JsonLogic dependency. | Add exact, non-caret versions for `ajv`, `ajv-formats`, `ajv-errors`, `react-hook-form`, and the selected JsonLogic runtime. |
| `experience/src/services/api.ts` | JSON helper understands global ProblemDetails but not `lobErrors[]` specifically. | Extend types with `LobValidationProblemDetails` while preserving `ProblemDetails.errors` behavior for non-LOB failures. |
| `experience/src/features/submissions/types.ts` | Submission DTOs do not model `lobAttributes`. | Add shared LOB envelope types and include on submission detail/create/update DTOs. |
| `experience/src/features/renewals/types.ts` | Renewal DTOs do not model `lobAttributes`. | Add shared LOB envelope types and include on renewal detail/create DTOs. |
| `experience/src/features/policies/types.ts` | Policy DTOs, versions, endorsements, create, and endorse requests do not model `lobAttributes`. | Add shared LOB envelope types and include Policy current-version, PolicyVersion, PolicyEndorsement, create, and endorse shapes. |
| `experience/src/pages/CreateSubmissionPage.tsx` | Static submission form with account, broker, dates, LOB, premium estimate, description. | Embed `DynamicAttributePanel` when LOB/product version is selected; save attributes through `lobAttributes`. |
| `experience/src/pages/SubmissionDetailPage.tsx` | Static detail and edit modal; no product attribute surface. | Render pinned product attributes, legacy read state, validation errors, and pin-on-open editing for authorized edits. |
| `experience/src/pages/PolicyDetailPage.tsx` and `experience/src/features/policies/components/PolicyRails.tsx` | Policy summary, versions, endorsements, coverages, timeline only. | Display current PolicyVersion product attributes; add endorsement attribute edit flow. |
| `experience/src/pages/RenewalDetailPage.tsx` | Renewal context, transitions, owner, timeline only. | Render renewal-stage product attributes and legacy/read-only states. |
| `experience/src/features/documents/components/DocumentMetadataFields.tsx` | Lightweight JSON Schema renderer for document metadata, not AJV/RHF and not product-version pinned. | Use only as visual/contract precedent. Do not extend it into the LOB engine; create separate `features/lob-attributes/**`. |

## New Files

| File | Layer | Purpose |
|------|-------|---------|
| `engine/src/Nebula.Domain/Entities/LobProduct.cs` | Domain | Product identity, kind, code, LOB, display metadata. |
| `engine/src/Nebula.Domain/Entities/LobProductVersion.cs` | Domain | Deterministic product version row, lifecycle status, signature metadata. |
| `engine/src/Nebula.Domain/Entities/LobSchemaBundle.cs` | Domain | Resolved bundle per `(productVersionId, stage)`. |
| `engine/src/Nebula.Domain/Entities/LobBundleActivationEvent.cs` | Domain | Append-only activation/deprecation/retirement evidence. |
| `engine/src/Nebula.Domain/Lob/LobConstants.cs` | Domain | Stages, statuses, kinds, deterministic namespace id, sentinel ids, stable error codes. |
| `engine/src/Nebula.Application/DTOs/LobAttributeDtos.cs` | Application | LOB envelope, bundle, active set, activation, validation-error DTOs. |
| `engine/src/Nebula.Application/Interfaces/ILobSchemaRepository.cs` | Application | DB read/write abstraction for products, versions, bundles, activation events. |
| `engine/src/Nebula.Application/Interfaces/ILobSchemaResolver.cs` | Application | Resolve active and pinned bundles with ETag/cache behavior. |
| `engine/src/Nebula.Application/Interfaces/ILobAttributeValidator.cs` | Application | Validate attributes plus rules and return normalized errors. |
| `engine/src/Nebula.Application/Interfaces/ILobProductVersionGuard.cs` | Application | Enforce status/kind/write eligibility and LOB consistency before mutations. |
| `engine/src/Nebula.Application/Services/LobSchemaService.cs` | Application | List active bundles, resolve bundle detail, activation state transition orchestration. |
| `engine/src/Nebula.Application/Services/LobAttributeService.cs` | Application | Resolver-first carrier validation and canonicalization used by lifecycle services. |
| `engine/src/Nebula.Application/Validators/LobSchemaValidators.cs` | Application | Activation request and envelope validators for static request shape. |
| `engine/src/Nebula.Infrastructure/Repositories/LobSchemaRepository.cs` | Infrastructure | EF repository for product schema registry tables. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/LobProductConfiguration.cs` | Infrastructure | EF mapping for `LobProducts`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/LobProductVersionConfiguration.cs` | Infrastructure | EF mapping for `LobProductVersions`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/LobSchemaBundleConfiguration.cs` | Infrastructure | EF mapping for `LobSchemaBundles`. |
| `engine/src/Nebula.Infrastructure/Persistence/Configurations/LobBundleActivationEventConfiguration.cs` | Infrastructure | EF mapping for `LobBundleActivationEvents`. |
| `engine/src/Nebula.Infrastructure/Lob/LobSchemaResolver.cs` | Infrastructure | Cached resolver with active-set and direct pinned bundle resolution. |
| `engine/src/Nebula.Infrastructure/Lob/LobAttributeValidator.cs` | Infrastructure | Backend JSON Schema plus JsonLogic validation and normalization shim. |
| `engine/src/Nebula.Infrastructure/Lob/LobProductVersionGuard.cs` | Infrastructure | Sentinel/status/write-eligibility enforcement. |
| `engine/src/Nebula.Infrastructure/Lob/FileSystemLobBundleSource.cs` | Infrastructure | Loads filesystem-canonical bundle sources for compile/activation. |
| `engine/src/Nebula.Api/Endpoints/LobSchemaEndpoints.cs` | API | Minimal API group for `/lob-schemas/**`. |
| `engine/tests/Nebula.Tests/Unit/LobAttributeValidatorTests.cs` | Backend tests | Normalized error mapping, rule order, collect-all-errors parity fixture inputs. |
| `engine/tests/Nebula.Tests/Unit/LobProductVersionGuardTests.cs` | Backend tests | Active/deprecated/retired/internal/legacy/write-eligibility decisions. |
| `engine/tests/Nebula.Tests/Integration/LobSchemaEndpointTests.cs` | Backend tests | Active listing, pinned detail, activation, policy enforcement, ETag behavior. |
| `engine/tests/Nebula.Tests/Integration/LobCarrierEndpointTests.cs` | Backend tests | Submission/Renewal/PolicyVersion/Endorsement carrier validation and timeline evidence. |
| `experience/src/features/lob-attributes/types.ts` | Frontend | LOB bundle, envelope, normalized error, widget schema types. |
| `experience/src/features/lob-attributes/hooks.ts` | Frontend | `useActiveLobBundles`, `useLobSchemaBundle`, bundle cache helpers. |
| `experience/src/features/lob-attributes/validation.ts` | Frontend | AJV compile/cache, normalized error shim, JsonLogic rule evaluator. |
| `experience/src/features/lob-attributes/widgetRegistry.tsx` | Frontend | Explicit widget registry and option validation map. |
| `experience/src/features/lob-attributes/DynamicAttributePanel.tsx` | Frontend | Pinned schema dynamic panel for lifecycle screens. |
| `experience/src/features/lob-attributes/components/*.tsx` | Frontend | Primitive widgets: text, textarea, number, money-minor, select, multi-select, checkbox, date, section, read-only summary. |
| `experience/src/features/lob-attributes/tests/*.test.tsx` | Frontend tests | Widget registry, panel behavior, error mapping, legacy/read-only state. |
| `experience/src/features/lob-attributes/parity/*.test.ts` | Frontend tests | Browser-side fixture parity output for QE comparison. |
| `experience/tests/visual/lob-attribute-panel.theme.spec.ts` | Frontend visual | Light/dark smoke coverage for Cyber panel. |
| `planning-mds/lob-schemas/_shared/**` | Architect-owned schema source | Shared primitives for money, currency, date, US state, percentages, rules operations if needed. |
| `planning-mds/lob-schemas/cyber/1.0.0/{submission,policy,endorsement,renewal}/**` | Architect-owned schema source | Cyber data schema, UI schema, rules, projections, examples, README evidence. |
| `planning-mds/operations/evidence/F0034/**` | Evidence | Runtime command logs, parity reports, performance baselines, visual smoke outputs, signoff files. |

## Backend Code Contracts

### Domain Entities

```csharp
// engine/src/Nebula.Domain/Entities/LobProduct.cs
namespace Nebula.Domain.Entities;

public class LobProduct : BaseEntity
{
    public string Code { get; set; } = default!;
    public string ProductKind { get; set; } = default!; // Standard, Unspecified, Legacy, Bridge
    public string? LineOfBusiness { get; set; }
    public string DisplayName { get; set; } = default!;

    public ICollection<LobProductVersion> Versions { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/LobProductVersion.cs
namespace Nebula.Domain.Entities;

public class LobProductVersion : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Version { get; set; } = default!;
    public string Status { get; set; } = default!; // Draft, Active, Deprecated, Retired, Internal
    public string? Signature { get; set; }
    public string? SignatureKeyId { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
    public DateTime? RetiredAt { get; set; }

    public LobProduct Product { get; set; } = default!;
    public ICollection<LobSchemaBundle> Bundles { get; set; } = [];
}

// engine/src/Nebula.Domain/Entities/LobSchemaBundle.cs
namespace Nebula.Domain.Entities;

public class LobSchemaBundle : BaseEntity
{
    public Guid ProductVersionId { get; set; }
    public string Stage { get; set; } = default!; // submission, policy, endorsement, renewal
    public string SchemaHash { get; set; } = default!;
    public string ETag { get; set; } = default!;
    public string DataSchemaJson { get; set; } = "{}";
    public string UiSchemaJson { get; set; } = "{}";
    public string RulesJson { get; set; } = "[]";
    public string ProjectionsJson { get; set; } = "[]";

    public LobProductVersion ProductVersion { get; set; } = default!;
}

// engine/src/Nebula.Domain/Entities/LobBundleActivationEvent.cs
namespace Nebula.Domain.Entities;

public class LobBundleActivationEvent : BaseEntity
{
    public Guid ProductVersionId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }

    public LobProductVersion ProductVersion { get; set; } = default!;
}
```

```csharp
// Add to existing carriers
public Guid LobProductVersionId { get; set; }
public string AttributesJson { get; set; } = "{}";
public LobProductVersion LobProductVersion { get; set; } = default!;

// PolicyVersion and PolicyEndorsement only
public string LineOfBusiness { get; set; } = default!;
```

### DTOs

```csharp
// engine/src/Nebula.Application/DTOs/LobAttributeDtos.cs
using System.Text.Json;

namespace Nebula.Application.DTOs;

public sealed record LobAttributeEnvelopeDto(
    Guid LobProductVersionId,
    Guid? LobProductId,
    string? LobProductCode,
    string? LobProductVersionStatus,
    string? LobSchemaVersion,
    string? LobSchemaHash,
    JsonElement Attributes);

public sealed record LobSchemaBundleDto(
    Guid ProductVersionId,
    Guid ProductId,
    string ProductCode,
    string ProductKind,
    string? LineOfBusiness,
    string Version,
    string Status,
    string Stage,
    string ETag,
    string SchemaHash,
    JsonElement DataSchema,
    JsonElement UiSchema,
    JsonElement Rules,
    JsonElement Projections);

public sealed record LobActiveBundleSetDto(
    DateTime GeneratedAt,
    IReadOnlyList<LobSchemaBundleDto> Bundles);

public sealed record LobBundleActivationRequestDto(
    string TargetStatus,
    string ActivationReason,
    DateTime? EffectiveAt,
    IReadOnlyList<string>? Stages);

public sealed record LobValidationErrorDto(
    string Code,
    string Pointer,
    string? SchemaPath,
    string Keyword,
    JsonElement? Constraint);

public sealed record LobValidationProblemDetailsDto(
    string Type,
    string Title,
    int Status,
    string Code,
    string TraceId,
    Guid? ProductVersionId,
    string? Stage,
    IReadOnlyList<LobValidationErrorDto> LobErrors);
```

Add `LobAttributeEnvelopeDto LobAttributes` to:

- `SubmissionDto`, `SubmissionCreateDto`, `SubmissionUpdateDto`
- `RenewalDto`, `RenewalCreateDto`
- `PolicyCreateRequestDto`, `PolicyFromBindRequestDto`, `PolicyEndorsementRequestDto`
- `PolicyDto` and `PolicySummaryDto` as current-version read-through data
- `PolicyVersionDto`, `PolicyEndorsementDto`

### Interfaces and Services

```csharp
// engine/src/Nebula.Application/Interfaces/ILobSchemaRepository.cs
namespace Nebula.Application.Interfaces;

public interface ILobSchemaRepository
{
    Task<IReadOnlyList<LobSchemaBundle>> ListActiveBundlesAsync(
        string? stage,
        string? lineOfBusiness,
        ICurrentUserService user,
        CancellationToken ct = default);

    Task<LobSchemaBundle?> FindBundleAsync(
        Guid productVersionId,
        string stage,
        CancellationToken ct = default);

    Task<LobProductVersion?> FindProductVersionAsync(
        Guid productVersionId,
        CancellationToken ct = default);

    Task AddActivationEventAsync(
        LobBundleActivationEvent activationEvent,
        CancellationToken ct = default);
}

// engine/src/Nebula.Application/Interfaces/ILobSchemaResolver.cs
public interface ILobSchemaResolver
{
    Task<LobActiveBundleSetDto> ListActiveAsync(
        string? stage,
        string? lineOfBusiness,
        ICurrentUserService user,
        CancellationToken ct = default);

    Task<LobSchemaBundleDto?> ResolveAsync(
        Guid productVersionId,
        string stage,
        ICurrentUserService user,
        CancellationToken ct = default);
}

// engine/src/Nebula.Application/Interfaces/ILobAttributeValidator.cs
public interface ILobAttributeValidator
{
    Task<LobValidationResult> ValidateAsync(
        Guid productVersionId,
        string stage,
        JsonElement attributes,
        IReadOnlyDictionary<string, object?> core,
        ICurrentUserService user,
        CancellationToken ct = default);
}

public sealed record LobValidationResult(
    bool IsValid,
    string? ProblemCode,
    string? ProductVersionStatus,
    string? SchemaHash,
    IReadOnlyList<LobValidationErrorDto> Errors,
    JsonElement CanonicalAttributes);

// engine/src/Nebula.Application/Interfaces/ILobProductVersionGuard.cs
public interface ILobProductVersionGuard
{
    Task<LobWriteEligibilityResult> EnsureWriteAllowedAsync(
        LobWriteEligibilityRequest request,
        CancellationToken ct = default);
}

public sealed record LobWriteEligibilityRequest(
    string CarrierType,
    Guid? CarrierId,
    string Operation,
    string? ExistingLineOfBusiness,
    Guid? ExistingProductVersionId,
    JsonElement? ExistingAttributes,
    string? RequestedLineOfBusiness,
    Guid RequestedProductVersionId,
    JsonElement RequestedAttributes);

public sealed record LobWriteEligibilityResult(
    bool Allowed,
    string? ErrorCode,
    string? ErrorDetail,
    bool IsDeprecated,
    string? WarningHeader);
```

### `LobSchemaService` Logic Flow

```
ListActiveAsync(stage, lineOfBusiness, user, ct) -> LobActiveBundleSetDto
```

1. Enforce Casbin resource `lob_schema`, action `read`; hydrate role, tenant, stage, and LOB attrs.
2. Query only `Status == Active` product versions available to the tenant and stage.
3. Exclude `ProductKind in (Unspecified, Legacy, Bridge)` and `Status == Internal`.
4. Return bundles sorted by `productCode`, `version`, `stage` with stable `etag`.
5. Emit `lob.resolver.resolve` span with result count and stage.

```
ResolveAsync(productVersionId, stage, user, ct) -> LobSchemaBundleDto?
```

1. Enforce Casbin resource `lob_schema`, action `resolve`.
2. Resolve exact `(productVersionId, stage)` bundle from cache or repository.
3. Allow direct resolution for `Deprecated`, `Retired`, and `Internal` bundles so historical rows render.
4. Return `404` when absent; do not fall back to latest.
5. Emit `lob.resolver.resolve` span with `lob.product_version_id`, `lob.stage`, `lob.schema_hash`.

```
ActivateAsync(productVersionId, request, user, ct) -> LobSchemaBundleDto
```

1. Map `TargetStatus` to Casbin action: `Active -> activate`, `Deprecated -> deprecate`, `Retired -> retire`.
2. Enforce Admin/steward policy through resource `lob_schema`.
3. Load filesystem source bundle for every requested stage.
4. Run restricted-profile linter, widget registry compatibility check, projection matrix check, examples check, FE/BE parity evidence check, and HMAC signature check.
5. Reject with `422 LobValidationProblemDetails` when profile or examples fail.
6. Reject with `409` when status transition is not valid.
7. Update product-version status fields and append one `LobBundleActivationEvent` per transition.
8. Commit through `unitOfWork.CommitAsync(ct)`.
9. Return the activated stage bundle and `ETag`.

### Carrier Write Logic Flow

```
LobAttributeService.ValidateCarrierWriteAsync(request, ct) -> LobCarrierWriteResult
```

1. Call `ILobProductVersionGuard.EnsureWriteAllowedAsync`.
2. If denied, return `409` for status/legacy/version-switch failures or `422` for schema/profile failures using stable codes:
   - `LOB_PRODUCT_MISMATCH`
   - `LEGACY_SENTINEL_WRITE_BLOCKED`
   - `LOB_SCHEMA_NOT_ACTIVE`
   - `LOB_SCHEMA_NOT_FOUND`
   - `LOB_RULE_FAILED`
   - `LOB_VALIDATION_TIMEOUT`
3. Resolve exact bundle by requested `lobProductVersionId` and carrier stage.
4. Run schema validation in collect-all-errors mode.
5. If schema succeeds, run JsonLogic rules using `{ data, core, context }`.
6. Canonicalize attributes to deterministic JSON for legacy same-version comparison.
7. Return `LobAttributeEnvelopeDto` metadata for DTO mapping.

### Timeline Events

Use existing `ActivityTimelineEvent` patterns. Emit exactly one event for each successful product attribute mutation:

| Carrier | EventType | EntityType | EventDescription | ExternalDescription | Payload |
|---------|-----------|------------|------------------|---------------------|---------|
| Submission | `SubmissionProductAttributesUpdated` | `Submission` | `Product attributes updated for {productCode}/{version}.` | `null` | productVersionId, schemaHash, changedPointers |
| Renewal | `RenewalProductAttributesUpdated` | `Renewal` | `Renewal product attributes updated for {productCode}/{version}.` | `null` | productVersionId, schemaHash, changedPointers |
| PolicyVersion | `PolicyVersionProductAttributesCaptured` | `Policy` | `Policy version {versionNumber} captured product attributes for {productCode}/{version}.` | `null` | policyVersionId, productVersionId, schemaHash |
| PolicyEndorsement | `PolicyEndorsementProductAttributesCaptured` | `Policy` | `Endorsement {endorsementNumber} captured product attributes for {productCode}/{version}.` | `null` | endorsementId, productVersionId, schemaHash |
| Registry | `LobSchemaBundleStatusChanged` | `LobProductVersion` | `LOB schema bundle moved from {fromStatus} to {toStatus}.` | `null` | reason, stage list, schemaHash |

## HTTP Response Tables

### `GET /lob-schemas/active`

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `LobActiveBundleSetDto` | Authenticated user has `lob_schema:read`; bundles are active and tenant-available |
| 400 | ProblemDetails (`bad_request`) | Invalid `stage` or LOB filter |
| 401 | ProblemDetails (`unauthorized`) | Missing/invalid token |
| 403 | ProblemDetails (`forbidden`) | Casbin deny |
| 503 | ProblemDetails (`service_unavailable`) | Resolver/source unavailable after runtime preflight is healthy |

### `GET /lob-schemas/{productVersionId}/{stage}`

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `LobSchemaBundleDto` | Exact bundle exists and user has `lob_schema:resolve` |
| 400 | ProblemDetails (`bad_request`) | Invalid UUID or stage |
| 401 | ProblemDetails (`unauthorized`) | Missing/invalid token |
| 403 | ProblemDetails (`forbidden`) | Casbin deny or user lacks record/bundle visibility |
| 404 | ProblemDetails (`not_found`) | Exact pinned bundle is absent |

### `POST /lob-schemas/{productVersionId}/activate`

| Status | Body | Condition |
|--------|------|-----------|
| 200 | `LobSchemaBundleDto` | Activation/deprecation/retirement succeeded |
| 400 | ProblemDetails (`bad_request`) | Invalid activation request |
| 401 | ProblemDetails (`unauthorized`) | Missing/invalid token |
| 403 | ProblemDetails (`forbidden`) | Caller lacks matching `lob_schema` action |
| 409 | ProblemDetails (`invalid_status_transition`) | Invalid lifecycle transition or stale target status |
| 422 | `LobValidationProblemDetailsDto` | Profile, widget, projection, signature, fixture, or parity evidence failure |

### Existing Carrier Writes

| Endpoint | Stage | New LOB Behavior |
|----------|-------|------------------|
| `POST /submissions` | `submission` | Accept `lobAttributes`; `_unspecified/0.0.0` allowed only when `lineOfBusiness == null` and attributes are `{}`. |
| `PUT /submissions/{submissionId}` | `submission` | Same-version edits allowed by existing workflow/ABAC; `_unspecified -> Active Cyber` allowed only with null-LOB triage transition in the same write. |
| `POST /renewals` | `renewal` | Accept `lobAttributes`; default from expiring policy current version when not supplied only if product version/stage is resolvable and line-of-business matches. |
| `PUT /renewals/{renewalId}/lob-attributes` | `renewal` | Detail-page Cyber edits require `If-Match`, existing renewal update authorization, non-terminal lifecycle state, schema validation, and timeline evidence. |
| `POST /policies` and `POST /policies/from-bind` | `policy` | Accept product attributes and persist them on the initial PolicyVersion. |
| `PUT /policies/{policyId}` | `policy` | Pending policy detail-page Cyber edits may update current PolicyVersion attributes when validation and concurrency checks pass. |
| `POST /policies/{policyId}/endorse` | `endorsement` then `policy` | Validate endorsement-stage payload; persist on PolicyEndorsement and resulting PolicyVersion per service rules. |

Detail-page write requirement:

- Policy Detail and Renewal Detail must not ship as display-only for active Cyber rows. Both pages need an enabled edit affordance and integration coverage proving a field can be changed and persisted.
- Empty `_legacy_cyber/0.0.0` rows may move to active `cyber/1.0.0` only when a valid non-empty Cyber payload is saved through an approved lifecycle mutation. Other legacy sentinel attribute changes remain blocked.

Carrier write failures:

| Status | Body | Condition |
|--------|------|-----------|
| 422 | `LobValidationProblemDetailsDto` | Schema or rule validation failure |
| 409 | ProblemDetails (`LOB_SCHEMA_NOT_ACTIVE`, `LEGACY_SENTINEL_WRITE_BLOCKED`, `LOB_PRODUCT_MISMATCH`) | Status/kind/version-switch invariant failure |
| 403 | ProblemDetails (`forbidden`) | Existing resource ABAC deny before attributes are exposed |

## Migration SQL Requirements

EF Core migration may express ordinary tables and FKs. Use raw SQL for deterministic seed ids, invariant triggers, and projection indexes.

```sql
-- Deterministic namespace from ADR-020:
-- NEBULA_LOB_NAMESPACE = 7be9dc8e-e507-4c41-bcf7-8f1ddbb0d9c6

CREATE TABLE "LobProducts" (
    "Id" uuid PRIMARY KEY,
    "Code" varchar(120) NOT NULL UNIQUE,
    "ProductKind" varchar(30) NOT NULL,
    "LineOfBusiness" varchar(50) NULL,
    "DisplayName" varchar(200) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "UpdatedByUserId" uuid NOT NULL,
    "DeletedAt" timestamptz NULL,
    "DeletedByUserId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);

CREATE TABLE "LobProductVersions" (
    "Id" uuid PRIMARY KEY,
    "ProductId" uuid NOT NULL REFERENCES "LobProducts"("Id") ON DELETE RESTRICT,
    "Version" varchar(40) NOT NULL,
    "Status" varchar(30) NOT NULL,
    "Signature" varchar(80) NULL,
    "SignatureKeyId" varchar(80) NULL,
    "ActivatedAt" timestamptz NULL,
    "DeprecatedAt" timestamptz NULL,
    "RetiredAt" timestamptz NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "UpdatedByUserId" uuid NOT NULL,
    "DeletedAt" timestamptz NULL,
    "DeletedByUserId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "UX_LobProductVersions_ProductId_Version" UNIQUE ("ProductId", "Version")
);

CREATE TABLE "LobSchemaBundles" (
    "Id" uuid PRIMARY KEY,
    "ProductVersionId" uuid NOT NULL REFERENCES "LobProductVersions"("Id") ON DELETE RESTRICT,
    "Stage" varchar(30) NOT NULL,
    "SchemaHash" varchar(80) NOT NULL,
    "ETag" varchar(120) NOT NULL,
    "DataSchemaJson" jsonb NOT NULL,
    "UiSchemaJson" jsonb NOT NULL,
    "RulesJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
    "ProjectionsJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "UpdatedByUserId" uuid NOT NULL,
    "DeletedAt" timestamptz NULL,
    "DeletedByUserId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "UX_LobSchemaBundles_ProductVersionId_Stage" UNIQUE ("ProductVersionId", "Stage")
);

CREATE TABLE "LobBundleActivationEvents" (
    "Id" uuid PRIMARY KEY,
    "ProductVersionId" uuid NOT NULL REFERENCES "LobProductVersions"("Id") ON DELETE RESTRICT,
    "FromStatus" varchar(30) NULL,
    "ToStatus" varchar(30) NOT NULL,
    "Reason" varchar(500) NOT NULL,
    "ActorUserId" uuid NOT NULL,
    "OccurredAt" timestamptz NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "UpdatedByUserId" uuid NOT NULL,
    "DeletedAt" timestamptz NULL,
    "DeletedByUserId" uuid NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);

ALTER TABLE "Submissions"
    ADD COLUMN "LobProductVersionId" uuid NULL,
    ADD COLUMN "AttributesJson" jsonb NOT NULL DEFAULT '{}'::jsonb;

ALTER TABLE "Renewals"
    ADD COLUMN "LobProductVersionId" uuid NULL,
    ADD COLUMN "AttributesJson" jsonb NOT NULL DEFAULT '{}'::jsonb;

ALTER TABLE "PolicyVersions"
    ADD COLUMN "LineOfBusiness" varchar(50) NULL,
    ADD COLUMN "LobProductVersionId" uuid NULL,
    ADD COLUMN "AttributesJson" jsonb NOT NULL DEFAULT '{}'::jsonb;

ALTER TABLE "PolicyEndorsements"
    ADD COLUMN "LineOfBusiness" varchar(50) NULL,
    ADD COLUMN "LobProductVersionId" uuid NULL,
    ADD COLUMN "AttributesJson" jsonb NOT NULL DEFAULT '{}'::jsonb;
```

Seed requirements:

- `_unspecified/0.0.0`: version id `aa901058-2402-5370-9978-66eb184066be`, kind `Unspecified`, status `Internal`, null LOB, empty-object bundle for `submission` and `renewal`.
- `cyber/1.0.0`: version id `48f5f86a-7396-50bf-92dd-a3a36fe63c20`, kind `Standard`, status `Draft` until activation, LOB `Cyber`.
- `_legacy_cyber/0.0.0`: version id `4ffc79e6-4e32-5d39-a82c-891b6034ab9e`, kind `Legacy`, status `Internal`, pass-through bundles.
- Per-LOB legacy sentinels for every canonical LOB in `line-of-business.schema.json`.

Backfill order:

1. Insert sentinel products and versions.
2. Backfill null-LOB Submission/Renewal rows to `_unspecified/0.0.0` and `{}`.
3. Backfill non-null LOB Submission/Renewal rows to their `_legacy_<lob>/0.0.0` sentinel.
4. Backfill PolicyVersion and PolicyEndorsement rows from parent policy LOB to per-LOB legacy sentinels.
5. Alter carrier `LobProductVersionId` and PolicyVersion/PolicyEndorsement `LineOfBusiness` to `NOT NULL`.
6. Install invariant triggers after backfill.

Trigger requirements:

```sql
-- Name: trg_lob_carrier_consistency
-- Install on Submissions, Renewals, PolicyVersions, PolicyEndorsements.
-- Behavior:
-- 1. Load LobProductVersion -> LobProduct for NEW.LobProductVersionId.
-- 2. Reject missing product version with LOB_SCHEMA_NOT_FOUND.
-- 3. Allow _unspecified only for Submissions/Renewals where NEW.LineOfBusiness IS NULL
--    and NEW.AttributesJson = '{}'::jsonb.
-- 4. Reject product LOB mismatch with LOB_PRODUCT_MISMATCH.
-- 5. Reject product-version changes unless:
--    a. OLD is _unspecified, OLD.LineOfBusiness IS NULL, NEW product is Active,
--       NEW.LineOfBusiness = product.LineOfBusiness, and NEW.AttributesJson <> '{}'::jsonb; or
--    b. current_setting('app.lob_migration_in_progress', true) = 'true'.
```

Projection SQL is generated only from activated bundle `projections.json`. Do not add a default GIN index on raw `AttributesJson`.

## Frontend Code Contracts

### Shared Types

```ts
// experience/src/features/lob-attributes/types.ts
export type LobStage = 'submission' | 'policy' | 'endorsement' | 'renewal'
export type LobProductKind = 'Standard' | 'Unspecified' | 'Legacy' | 'Bridge'
export type LobProductVersionStatus = 'Draft' | 'Active' | 'Deprecated' | 'Retired' | 'Internal'
export type LobAttributes = Record<string, unknown>

export interface LobAttributeEnvelope {
  lobProductVersionId: string
  lobProductId: string | null
  lobProductCode: string | null
  lobProductVersionStatus: LobProductVersionStatus | null
  lobSchemaVersion: string | null
  lobSchemaHash: string | null
  attributes: LobAttributes
}

export interface LobSchemaBundle {
  productVersionId: string
  productId: string
  productCode: string
  productKind: LobProductKind
  lineOfBusiness: string | null
  version: string
  status: LobProductVersionStatus
  stage: LobStage
  etag: string
  schemaHash: string
  dataSchema: Record<string, unknown>
  uiSchema: LobUiSchema
  rules: LobRule[]
  projections: unknown[]
}

export interface LobValidationError {
  code: string
  pointer: string
  schemaPath: string | null
  keyword: string
  constraint?: unknown
}

export interface LobValidationProblemDetails {
  type?: string
  title?: string
  status: number
  code: string
  traceId: string
  productVersionId?: string | null
  stage?: LobStage | null
  lobErrors: LobValidationError[]
}
```

### Dynamic Panel Contract

```tsx
// experience/src/features/lob-attributes/DynamicAttributePanel.tsx
interface DynamicAttributePanelProps {
  carrierType: 'submission' | 'policyVersion' | 'policyEndorsement' | 'renewal'
  stage: LobStage
  lineOfBusiness: string | null
  envelope: LobAttributeEnvelope
  mode: 'read' | 'edit'
  readOnlyReason?: 'legacy' | 'retired' | 'unauthorized' | 'workflow'
  onValidate?: (next: LobAttributes) => Promise<LobValidationError[]>
  onSave?: (next: LobAttributeEnvelope) => Promise<void>
}

export function DynamicAttributePanel(props: DynamicAttributePanelProps): JSX.Element
```

Panel rules:

1. Snapshot `productVersionId`, `stage`, `etag`, `schemaHash`, and initial attributes on mount.
2. Do not rebind to a newer active bundle while mounted.
3. Resolve cache misses by `GET /lob-schemas/{productVersionId}/{stage}`.
4. Map errors by RFC 6901 pointer, not message text.
5. Legacy/internal rows render read-only with a compact legacy badge and no attribute save control.
6. Use semantic theme classes only; no raw palette utilities.

Widget registry:

| Widget | Data shape | Notes |
|--------|------------|-------|
| `text` | string | Single-line input. |
| `textarea` | string | Multi-line text, max length from schema. |
| `number` | number/integer | Use schema min/max and integer mode. |
| `money-minor` | `{ amountMinor, currency }` | UI displays major units; payload remains integer minor units. |
| `select` | enum scalar | Options from UI schema or enum. |
| `multi-select` | string array | Only when schema permits array of strings. |
| `checkbox` | boolean | Label and description from UI schema. |
| `date` | string date | Native date input. |
| `section` | object layout | Used for Cyber controls object and nested groups. |
| `read-only summary` | any | Legacy/deprecated display rows. |

Cyber `priorIncidents` uses a repeatable section over an array-of-object schema. This is not a new heavy domain widget; it is a governed primitive layout path backed by ordinary text/date/select widgets. If implementation requires a new widget name beyond ADR-021, halt and route to Architect before activation.

## Cyber Bundle Contract

Create source bundle files under:

```text
planning-mds/lob-schemas/cyber/1.0.0/
  README.md
  submission/data.schema.json
  submission/ui.schema.json
  submission/rules.json
  submission/projections.json
  submission/examples/valid/*.json
  submission/examples/invalid/*.json
  submission/examples/rules/*.json
  policy/data.schema.json
  policy/ui.schema.json
  policy/rules.json
  policy/projections.json
  endorsement/data.schema.json
  endorsement/ui.schema.json
  endorsement/rules.json
  endorsement/projections.json
  renewal/data.schema.json
  renewal/ui.schema.json
  renewal/rules.json
  renewal/projections.json
```

Cyber `1.0.0` data schema fields:

| Field | Type | Rule |
|-------|------|------|
| `revenueBand` | enum string | Required; bounded Cyber revenue bands. |
| `recordsHeld` | integer | Required; `minimum: 0`. |
| `controls.mfaEnabled` | boolean | Required. |
| `controls.mfaMaturity` | enum string | Required when MFA enabled; model as JsonLogic rule if schema profile cannot express conditional without projection loss. |
| `controls.edrEnabled` | boolean | Required. |
| `controls.edrMaturity` | enum string | Optional or required by rule according to bundle examples. |
| `controls.backupEnabled` | boolean | Required. |
| `controls.trainingFrequency` | enum string | Required. |
| `priorIncidents` | array object | Max 20 items; object contains date/type/description/loss amount when supplied. |
| `requestedLimit.amountMinor` | integer | Required; money minor units. |
| `requestedLimit.currency` | enum string | Required, `USD` for MVP. |
| `requestedRetention.amountMinor` | integer | Required; money minor units. |
| `requestedRetention.currency` | enum string | Required, `USD` for MVP. |

Rules:

- `recordsHeld > 1000000` requires `controls.mfaEnabled = true`.
- `requestedRetention.amountMinor >= requestedLimit.amountMinor / 100`.
- No rule runs if schema validation fails.
- Rule failures return `lobErrors[]` with `code = RULE_FAILED` or the stable rule code declared by the bundle.

Fixtures:

- At least 5 valid Cyber examples.
- At least 5 invalid Cyber examples.
- Passing and failing rule-case examples.
- Fixture expected result includes accept/reject plus expected `(code, pointer)` multiset.

## Dependency Order

```text
Step 0 (Architect):   Confirm decision-lock package and Phase B approval state.
Step 1 (Backend):     Registry entities, migrations, repository, resolver, schema endpoints.
Step 2 (Backend):     Carrier fields, guards, lifecycle service integration, timeline events.
  ---- Backend checkpoint: active listing/detail endpoints pass and carrier writes fail closed. ----
Step 3 (Architect/Backend/Frontend/QE): Cyber draft bundle, shared fixtures, validator engines, parity harness.
  ---- Parity checkpoint: FE and BE fixture reports match on decision and (code,pointer) multiset. ----
Step 4 (Frontend):    DynamicAttributePanel, widget registry, hooks, lifecycle screen embeds.
  ---- Frontend checkpoint: pinned Cyber panel renders in light/dark and maps normalized errors. ----
Step 5 (Backend/DevOps): Activation path, cache/ETag, performance telemetry, deployment config.
Step 6 (QE):          Lifecycle E2E, legacy read, validation error, pin-on-open, F0019 guardrail evidence.
```

## Integration Checkpoints

### After Registry Foundation

- [ ] `GET /lob-schemas/active?stage=submission` returns only active, tenant-available standard products and excludes `_unspecified`, `_legacy_*`, and `_bridge_*`.
- [ ] `GET /lob-schemas/{productVersionId}/{stage}` resolves exact active, deprecated, retired, and internal bundles for authorized users.
- [ ] `POST /lob-schemas/{productVersionId}/activate` rejects unknown widgets, forbidden schema keywords, failed examples, missing signatures, and missing parity evidence.
- [ ] Casbin policy rows for `lob_schema` read/resolve/activate/deprecate/retire are enforced.

### After Carrier Pinning

- [ ] Submission and Renewal rows cannot persist without `LobProductVersionId`.
- [ ] Null-LOB Submission/Renewal rows pin `_unspecified/0.0.0` with `{}` only.
- [ ] Non-null legacy rows pin per-LOB legacy sentinel and reject attribute changes.
- [ ] PolicyVersion, not Policy, owns policy-level product attributes.
- [ ] Product-version switches fail except null-LOB triage or explicit migration GUC path.
- [ ] Every successful product attribute mutation appends timeline evidence.

### After Validator Equivalence

- [ ] FE AJV and backend JSON Schema engines run collect-all-errors mode.
- [ ] Both layers normalize errors to stable `code`, `pointer`, `keyword`, and `schemaPath`.
- [ ] Message text is ignored in parity.
- [ ] Rule fixtures compare pass/fail result and pointer-mapped errors.
- [ ] Existing global `ProblemDetails.errors` semantics remain unchanged for non-LOB FluentValidation failures.

### After Dynamic Panel

- [ ] Cyber panel renders from schema metadata for submission, policy, endorsement, and renewal stages.
- [ ] Panel snapshots opened `(productVersionId, stage, etag)` and ignores newly activated versions until reopen.
- [ ] Deprecated versions render warning state but allow same-version edits when backend allows them.
- [ ] Legacy rows render read-only and do not expose save actions.
- [ ] Light/dark visual smoke coverage includes Cyber submission panel, legacy state, and field-level error state.

### Cross-Story Verification

- [ ] Full lifecycle: create or triage Cyber submission, inspect detail, issue policy/current PolicyVersion, endorse, inspect renewal-stage attributes.
- [ ] Authorization: Admin can activate/deprecate/retire; business users can read/resolve only; unauthorized users receive 401/403 without attribute payload leakage.
- [ ] Timeline events are present and ordered for submission attribute save, policy version capture, endorsement capture, and bundle activation.
- [ ] F0019 planning/implementation references F0034 for Cyber product attributes and does not add hardcoded Cyber fields.
- [ ] Performance baseline rows in `validation-perf-baseline.md` are replaced with measured p50/p95/p99 values for introduced engines and renderers.

## Integration Checklist

- [ ] API contract compatibility validated against `planning-mds/api/nebula-api.yaml`.
- [ ] Static schemas under `planning-mds/schemas/lob-*.schema.json` match runtime DTOs.
- [ ] Frontend contract compatibility validated through TypeScript types and tests.
- [ ] No AI/MCP scope for this feature.
- [ ] Test cases mapped to every F0034 story acceptance criterion.
- [ ] Developer-owned fast tests identified by layer.
- [ ] Runtime evidence artifacts written under `planning-mds/operations/evidence/F0034/**`.
- [ ] Framework vs solution boundary reviewed; no `agents/**` changes are part of F0034 runtime implementation.
- [ ] Run/deploy instructions updated in `GETTING-STARTED.md` after implementation.

## Required Runtime Evidence

| Evidence | Command/Source | Owner |
|----------|----------------|-------|
| Backend build/test | `dotnet test` inside application runtime container | Backend/QE |
| LOB endpoint integration | `engine/tests/Nebula.Tests/Integration/LobSchemaEndpointTests.cs` | Backend/QE |
| Carrier integration | `engine/tests/Nebula.Tests/Integration/LobCarrierEndpointTests.cs` | Backend/QE |
| FE unit/component | `pnpm --dir experience test` in frontend runtime | Frontend/QE |
| FE lint/theme/build | `pnpm --dir experience lint`, `lint:theme`, `build` | Frontend |
| Visual theme smoke | `pnpm --dir experience test:visual:theme` or feature-specific Playwright visual command | Frontend/QE |
| Parity report | FE and backend fixture report files under `planning-mds/operations/evidence/F0034/parity/` | QE |
| Performance report | p50/p95/p99 measurements updating `validation-perf-baseline.md` | QE/DevOps |
| Security review | Authorization and error-leakage review report under evidence path | Security |

## Risks and Blockers

| Item | Severity | Mitigation | Owner |
|------|----------|------------|-------|
| Explicit Phase B approval is still pending in `STATUS.md`. | High | Do not start runtime implementation until approval is recorded. | Architect/PM |
| Story dependency loop between S0004 parity and S0006 Cyber activation. | Medium | Treat Cyber source bundle and fixtures as draft inputs to parity; activation happens only after parity passes. | Architect/QE |
| Widget vocabulary could drift beyond ADR-021. | High | Use primitive `section`/repeatable layout for Cyber arrays; route any new widget name to Architect before activation. | Frontend/Architect |
| Legacy sentinel writes could accidentally allow product data changes. | High | Guard compares canonical attributes byte-for-byte and blocks any change on `Legacy` kind. | Backend/Security |
| OpenAPI remains 3.0.3 while runtime bundles are 2020-12. | Medium | Projection generator rejects semantic-loss keywords per matrix; runtime validation dispatches by product version. | Architect/Backend |
| Dynamic validator package versions are not yet pinned. | Medium | Add exact non-floating frontend/backend package versions in implementation and record versions in evidence. | Backend/Frontend/DevOps |

## JSON Serialization Convention

- API payloads use existing ASP.NET JSON defaults and frontend camelCase fields.
- Carrier persistence stores canonical JSON text in `AttributesJson` mapped to PostgreSQL `jsonb`.
- `LobAttributeEnvelopeDto.Attributes` serializes as a JSON object; use `{}` for empty attributes.
- Money uses integer minor units plus currency, never floating decimal money inside product attributes.
- Dates in dynamic attributes use JSON Schema `format: date` as `YYYY-MM-DD`.
- Error pointers use RFC 6901 and point inside the `attributes` object, for example `/controls/mfaEnabled`.
- `ProblemDetails.errors` remains reserved for existing FluentValidation field arrays. LOB validation uses sibling `lobErrors[]`.

## DI Registration Changes

Add these registrations:

```csharp
// engine/src/Nebula.Infrastructure/DependencyInjection.cs
services.AddScoped<ILobSchemaRepository, LobSchemaRepository>();
services.AddScoped<ILobSchemaResolver, LobSchemaResolver>();
services.AddScoped<ILobAttributeValidator, LobAttributeValidator>();
services.AddScoped<ILobProductVersionGuard, LobProductVersionGuard>();
services.AddSingleton<ILobBundleSource, FileSystemLobBundleSource>();

// engine/src/Nebula.Api/Program.cs
builder.Services.AddScoped<LobSchemaService>();
builder.Services.AddScoped<LobAttributeService>();
app.MapLobSchemaEndpoints();
```

Use `IMemoryCache` for resolved bundles. Cache key: `lob-bundle:{productVersionId}:{stage}:{etag}`. Invalidate active-set cache after activation/deprecation/retirement.

## Casbin Policy Sync

`planning-mds/security/policies/policy.csv` already includes:

- Admin: `lob_schema` read, resolve, activate, deprecate, retire.
- Underwriter, DistributionUser, DistributionManager, RelationshipManager, ProgramManager, Coordinator: read and resolve.

Implementation must ensure the embedded resource copy is current through the existing `Nebula.Infrastructure.csproj` `EmbeddedResource` entry. Security review must verify:

- Active listing filters tenant availability after policy allow.
- Direct resolve checks record/bundle visibility and does not expose unavailable product attributes to unauthorized users.
- Activation/deprecation/retirement are Admin/steward-only.

## G0 Validation Checklist

- [ ] Plan references all F0034 stories and raw architecture decisions.
- [ ] Plan follows ADR-020: carriers are Submission, Renewal, PolicyVersion, PolicyEndorsement; Policy has no independent attributes.
- [ ] Plan follows ADR-021: custom RHF/AJV/shadcn registry, pin-on-open, no unknown widgets.
- [ ] Plan follows ADR-022: restricted schema profile and `lobErrors[]` parity.
- [ ] Plan follows ADR-023: JsonLogic envelope and FE/BE custom operation parity.
- [ ] Build order resolves S0004/S0006 by using draft Cyber sources before activation.
- [ ] Required signoff roles in `STATUS.md` remain Quality Engineer, Code Reviewer, Security Reviewer, DevOps, and Architect.
