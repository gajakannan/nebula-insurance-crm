# LOB-Extensible Attribute Architecture — Comprehensive Plan

**Status:** Draft — awaiting Stage 0 decision lock
**Owner:** Architect agent (with Schema-Steward sub-skill)
**Spans:** `nebula-agents/agents/**` (framework) + `nebula-insurance-crm/**` (solution)
**Replaces:** N/A (new cross-cutting plan)
**Related:** ADR-018 (policy aggregate versioning); this plan introduces four new ADRs in Stage 0.

---

## 0. Glossary

| Term | Meaning |
|---|---|
| **Core column** | A strictly-typed column on a lifecycle entity that the platform itself operates on regardless of LOB (FKs, lifecycle stamps, audit, money totals, denormalized display). |
| **Extension attribute** | A LOB-specific field carried inside `attributes_json` and governed by a versioned JSON Schema. |
| **LOB** | Line of Business (Cyber, Property, CommercialAuto, WC, GL, ProfessionalLiability, Umbrella, Surety, Marine, DirectorsOfficers). |
| **LOB product** | A coherent product offering tied to one LOB (e.g., "Property — National"). One LOB can have many products; a product has many versions. |
| **LOB product version** | An immutable semver'd snapshot of one product's schema bundle. Every variant-entity row pins one. |
| **Schema bundle** | The set of artifacts for one `(product, version, stage)`: `data.schema.json`, `ui.schema.json`, `rules.json`, `projections.json`, `examples/`, `README.md`. Optionally `migrations/`. |
| **Stage** (of a bundle) | One of `submission`, `policy`, `endorsement`, `renewal`. A product version has one bundle per stage. |
| **`_shared/` primitive** | A reusable validation primitive (money, tiv, us-state, etc.) in the shared namespace, versioned and `$ref`-able from LOB schemas. |
| **Registry** | The operational system that stores, validates, serves, and governs schema bundles. Filesystem-canonical for MVP; DB-cached at runtime. |
| **Schema-Steward** | The governance role (sub-skill + human reviewer) that approves shared-primitive and LOB-bundle changes and signs off on lifecycle transitions. |
| **Pinning** | Every variant-entity row carries an immutable `lob_product_version_id`; validation and rendering always use the bundle pinned on the row. |
| **Bundle compile** | The CI step that resolves `$ref`s in sources and emits a single inline bundle per `(product, version, stage)` for FE/BE serving and CI parity tests. |
| **Restricted profile** | The subset of JSON Schema draft-2020-12 keywords permitted in bundles (see §2.3). |
| **Normalized error envelope** | The canonical error shape produced by both AJV and Json.Schema.Net via a thin shim, so parity is asserted on codes+pointers not library output. |
| **Sentinel product** | `_unspecified/0.0.0` — an empty-attributes product used only while `lineOfBusiness IS NULL` on early Submission/Renewal rows. Status `Active`; writes are allowed only for those null-LOB rows and `attributes_json` must be `{}`. It is not a legacy sentinel. |
| **Legacy sentinel (per LOB)** | `_legacy/<lob>/0.0.0` — a per-LOB pass-through product used to carry pre-registry rows whose `lineOfBusiness` is non-null (existing PolicyVersion, PolicyEndorsement, Cyber rows at Stage 3 migration, etc.). Pass-through schema, marked `is_legacy_sentinel = true`, status `Deprecated` (reads render). Core-only writes may proceed if `attributes_json` and `lob_product_version_id` are unchanged; attribute writes, new creates, and ad hoc version switches are blocked by middleware. Endorsements against a legacy-pinned row route through §2.12 migration-on-endorsement to upgrade forward. |

---

## 1. Problem statement & motivation

### 1.1 Current state

Nebula's insurance CRM encodes its lifecycle entities — `Submission`, `Policy`, `PolicyVersion`, `PolicyEndorsement`, `Renewal` — as rigidly columned C# entities. `LineOfBusiness` is a flat enum (`schemas/line-of-business.schema.json`). Only `PolicyVersion` carries any JSON-shaped container (`ProfileSnapshotJson`, `CoverageSnapshotJson`, `PremiumSnapshotJson` — file `engine/src/Nebula.Domain/Entities/PolicyVersion.cs:13-15`), and those snapshots hold *platform* state (accounts/brokers/carriers/coverages/premiums), not LOB-specific attributes.

There is no carrier for LOB-specific underwriting data (TIV/COPE for Property, vehicle schedule for CommercialAuto, class codes for WC, controls posture for Cyber, etc.). Today every such attribute would require:

- Column migration on the relevant lifecycle entity
- EF Core entity update
- DTO updates
- Request/response JSON Schema updates
- Hand-written FE and BE validators
- Hand-written form components
- Hand-written tests

That cost is paid every time the product team wants to evolve a product line — a structural reason the application cannot move at product speed.

### 1.2 Target state

Lifecycle entities keep a small stable core of columns and carry LOB-specific attributes in a JSONB column governed by a versioned, registry-served JSON Schema bundle. The same schema validates client-side (AJV), server-side (Json.Schema.Net), and drives UI rendering (custom RHF + AJV + shadcn/ui widget registry, with JsonLogic for cross-field rules). Reusable validation primitives live once in a shared, versioned `_shared/` namespace.

Adding a new attribute, raising a TIV cap, adding a new enum value, introducing a new conditional section becomes: bump the schema bundle, update examples, ship. No platform code change. **No FE deploy required for data-schema-only changes within the existing widget vocabulary** — new custom widgets (vehicle schedule, COPE accordion, D&O tower visualizer, etc.) still require an FE deploy. No drift between FE and BE validation because the schema is the contract and a CI conformance harness proves equivalence.

### 1.3 Scope boundaries

**In scope (variant-typed attribute carriers):** `Submission`, `PolicyVersion`, `PolicyEndorsement`, `Renewal`.

`Policy` remains a lifecycle aggregate parent with `LineOfBusiness`, `CurrentVersionId`, lifecycle state, and denormalized current read fields. It is not an independent LOB-attribute carrier. Policy-facing APIs may accept `attributes` in create/endorsement flows, but the service persists those attributes onto the authoritative `PolicyVersion` row produced by the write. Any policy-list attribute filtering joins through `Policy.CurrentVersionId → PolicyVersion` or uses projections on `PolicyVersions`; it does not read a separate `Policy.attributes_json` source.

**Out of scope (stay strictly columned):** `Account`, `Contact`, `Broker`, `Carrier`, `UserProfile`, `Task`, `Activity`. Don't generalize JSONB to these — that path leads to a document-store-in-Postgres anti-pattern.

The architecture earns its complexity only on entities whose shape varies by product.

---

## 2. Core architectural principles

These principles are the "why" every framework artifact and every future agent-driven session must internalize. They are embedded verbatim into `agents/architect/references/extensible-attribute-architecture.md` so agents designing or reviewing variant-typed entities see them before making choices.

### 2.1 Core / extension split

Every variant-typed attribute carrier gets:

- **Core columns** — what the platform operates on regardless of LOB: identifiers, FKs, lifecycle stamps, audit, money totals, denormalized display fields, `rowVersion`.
- **`attributes_json` (JSONB, NOT NULL DEFAULT '{}')** — LOB-specific attributes. Platform code never reaches in; LOB-aware services may.
- **`lob_product_version_id` (UUID, NOT NULL, FK → `lob_product_version.id`)** — immutable per row. The schema version used to validate this row's `attributes_json`.

`lineOfBusiness` (existing column) is operational — not cosmetic. See §2.11 for the consistency invariant that ties it to `lob_product_version_id`.

**Policy parent exception.** `PolicyVersion.attributes_json` is the source of truth for policy-level LOB attributes. The `Policy` parent does not get an independent `attributes_json` column in Stage 2. If a future ADR introduces a policy-parent current-state cache, it must be explicitly named as a cache (`current_attributes_json` or equivalent), derived only from `CurrentVersionId`, and never accepted as an independent write source.

### 2.2 Validation taxonomy

Most validation logic in an insurance CRM is "this number is in this range," "this string matches this enum," "this object has these required fields." JSON Schema was built for that; AJV and Json.Schema.Net handle it identically across layers. Only the truly contextual or stateful rules go elsewhere.

| Validation kind | Belongs in | Examples |
|---|---|---|
| Type / structure | JSON Schema | `type: number`, required, `additionalProperties: false` |
| Bounded primitives | JSON Schema | `minimum: 100`, `maximum: 1_000_000_000`, `multipleOf: 0.01` |
| Enums / consts | JSON Schema | propertyType, constructionType, US states, currency codes |
| String shape | JSON Schema | `pattern`, `format: email\|uri\|date\|uuid`, `minLength`/`maxLength` |
| Array shape | JSON Schema | `minItems`, `uniqueItems`, `contains`, per-position `prefixItems` |
| Local conditionals | JSON Schema | `if/then/else`, `dependentRequired` in LOB bundles; broader JSON Schema features such as `oneOf` stay outside the restricted bundle profile |
| Cross-field business rules | JsonLogic (`rules.json`) | "personalPropertyAmount ≤ 70% of dwellingAmount", "deductible ≤ 5% of TIV" |
| External-context rules | JsonLogic + custom ops, or domain code | "premium ≤ broker credit limit", "effectiveDate ≥ today" |
| Role / state-aware rules | Domain code (Casbin + service) | "broker can't set commission > 15%", "only UW can override TIV cap" |
| Workflow guards | Domain code (state machine) | "can't bind if completeness < 100%" |
| Storage invariants | Database (FK, unique, check) | rowVersion, FK integrity, unique policy number |

**This table is embedded in three places in the framework:** the master reference, the `entity-model-template.md`, and the architect SKILL self-validation. An agent designing a new attribute always asks "which row of this taxonomy does this validation belong to?" — and the answer determines where the rule lives.

### 2.3 Validator equivalence — restricted profile + normalized envelope

For LOB bundles, JSON Schema draft 2020-12 is the portable contract. AJV (browser) and Json.Schema.Net (.NET) are independent implementations that pass the JSON Schema Test Suite — they do **not** share code. Parity is engineered, not assumed.

**Pinned tooling:**
- JSON Schema draft **2020-12** declared in every bundle's `$schema`
- FE: exact versions of `ajv`, `ajv-formats`, and `ajv-errors` pinned in `experience/package.json` and `pnpm-lock.yaml` (no caret ranges), options `{ strict: true, allErrors: true, useDefaults: false, allowUnionTypes: false }`
- BE: exact `JsonSchema.Net` package version pinned in the relevant `.csproj` (no floating or wildcard ranges), options `{ OutputFormat = List, RequireFormatValidation = true }`
- Both forced into format-validation mode (both treat `format` as annotation-only by default)

**Restricted schema profile.** Bundles must pass a profile linter at activation time.

*Allowed keywords:*
`type`, `required`, `properties`, `additionalProperties: false`, `$ref`, `$defs`, `$id`, `$schema`, `enum`, `const`, `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `minLength`, `maxLength`, `pattern`, `minItems`, `maxItems`, `uniqueItems`, `items` (single subschema), `prefixItems`, `minProperties`, `maxProperties`, `propertyNames`, `dependentRequired`, `format` from whitelist (`email`, `uri`, `date`, `date-time`, `uuid`, `ipv4`), `allOf` (constraint stacking), `if/then/else` (depth ≤ 2).

*Forbidden keywords:*
`multipleOf` on non-integers (see money rule below), `patternProperties`, `contains`/`minContains`/`maxContains`, `dependentSchemas`, `not`, `contentSchema`/`contentMediaType`/`contentEncoding`, `anyOf`, `oneOf`, custom keywords, remote `$ref`, `format` outside the whitelist.

**No `oneOf`/`discriminator` inside bundle schemas.** Bundle schemas validate a single product version's `attributes` subtree. Dispatch across product versions happens at the service boundary via `lobProductVersionId` (the resolver picks the bundle, then validates `attributes` against that single schema). `oneOf` is a valid JSON Schema keyword but is forbidden by this restricted profile; `discriminator` is OpenAPI-only metadata. Both are used only in the OpenAPI envelope projection to shape TypeScript/C# types across known product versions. Mixing them into bundles increases portability and UI-walker risk without adding runtime value.

**Sentinel exception — `additionalProperties: true`.** Paths `_legacy/**` are the *only* bundles permitted to declare `additionalProperties: true` (pass-through). `_unspecified/0.0.0` is not pass-through; it is an empty-object bundle. All other bundles MUST declare `additionalProperties: false` on every object schema. The profile linter asserts the legacy-only exemption by path.

**Normalized error envelope.** Both engines emit raw output through a thin shim that produces this canonical shape:

```json
{
  "code": "VALIDATION_ERROR",
  "errors": [
    {
      "code": "RANGE_BELOW_MIN",
      "pointer": "/coverageInfo/dwellingAmount",
      "schemaPath": "/properties/coverageInfo/properties/dwellingAmount/minimum",
      "keyword": "minimum",
      "constraint": 0
    }
  ]
}
```

- `code` from a stable dictionary: `RANGE_BELOW_MIN`, `RANGE_ABOVE_MAX`, `ENUM_MISMATCH`, `PATTERN_MISMATCH`, `REQUIRED_MISSING`, `TYPE_MISMATCH`, `FORMAT_INVALID`, `LENGTH_BELOW_MIN`, `LENGTH_ABOVE_MAX`, `ITEMS_BELOW_MIN`, `ITEMS_ABOVE_MAX`, `UNIQUE_ITEMS_VIOLATED`, `ADDITIONAL_PROPERTY`, `DISCRIMINATOR_MISMATCH`, `CONDITIONAL_BRANCH_FAILED`, `RULE_FAILED`, `TIMEOUT`.
- `pointer` is RFC 6901, normalized for both engines.
- `keyword` is informational only; `code` is the parity primitive.
- `message` is intentionally absent — messages are FE-produced from `code` + `constraint` + i18n. This eliminates "AJV says X, .NET says Y" message drift.

**Conformance harness** (CI) asserts per example fixture:
- Decision parity (accept/reject)
- For rejects: multiset equality of `(code, pointer)` tuples
- `constraint` values match where reported
- Message text **not** compared

**Money — integer minor units.** Forbidden: JSON-number with `multipleOf: 0.01`. Drifts on float rounding between engines. Pattern: every money primitive is integer minor units (cents for USD, pence for GBP) with a currency code alongside. Display layer formats minor → major.

```jsonc
// _shared/money/1.0.0/schema.json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://nebula.local/lob-schemas/_shared/money/1.0.0/schema.json",
  "type": "object",
  "required": ["amountMinor", "currency"],
  "properties": {
    "amountMinor": { "type": "integer", "minimum": 0 },
    "currency":    { "$ref": "/lob-schemas/_shared/iso-currency/1.0.0/schema.json" }
  },
  "additionalProperties": false
}
```

### 2.4 Shared validation primitives (`_shared/`)

Reusable primitives are factored into a shared, **versioned** namespace under `planning-mds/lob-schemas/_shared/<concept>/<semver>/schema.json`. LOB schemas reference via `$ref`; they never redefine.

**Rules:**
- Versions are immutable per version. A bound change = a new version.
- Bump level reflects back-compat for existing data:
  - Loosening (raising a cap, adding an enum value) = **minor** bump
  - Tightening (lowering a cap, removing an enum value) = **major** bump
- LOB schemas `$ref` a specific version and stay frozen against it. LOB schemas opt in to new shared versions by publishing their own new version.
- Tightening allowed in a downstream LOB schema via `allOf: [$ref, {additional constraints}]`. Loosening in `_shared/` is ordinary; loosening in a LOB schema relative to the `_shared/` primitive it references is forbidden.
- LOB schemas never `$ref` across LOBs. Sharing happens only through `_shared/`. Dependency graph is a tree.
- **Bundled at serve time.** Registry stores modular `$ref`-using sources and serves resolved/inlined bundles. AJV and Json.Schema.Net never issue per-validation HTTP fetches.

**Example — clean.** Raising the TIV cap from 1B to 5B is loosening → `_shared/tiv/1.1.0` (minor). LOB schemas that want the new ceiling publish a new minor pinning `_shared/tiv/1.1.0`. Old policies remain on `tiv/1.0.0` via their pinned version. Lowering the TIV cap to 500M is tightening → `_shared/tiv/2.0.0` (major). Same opt-in pattern.

### 2.5 Schema delivery — hybrid

Four delivery axes combined:

1. **Build-time codegen** — CI emits two products from the compiled 2020-12 bundles:
   - `experience/src/generated/lob-types.ts` from the compiled bundles directly via `json-schema-to-typescript` (full TS safety for versions known at build time).
   - An **OpenAPI 3.0.3-compatible projection** into `planning-mds/api/nebula-api.yaml`, because the current repo's public API contract is OpenAPI 3.0.3. Envelope-level `oneOf` + `discriminator` keyed by `lobProductVersionId` is generated as OpenAPI codegen annotation only. Each discriminator branch inlines exactly one concrete `attributes` schema, converted to the OpenAPI 3.0 schema subset (for example, single-value `enum` instead of JSON Schema `const`, `nullable: true` instead of type arrays, and no 2020-12-only keywords). Runtime validation still dispatches via `LobSchemaResolver` and the 2020-12 bundle, not via OpenAPI.
2. **Bootstrap on app start** — after auth, one `GET /lob-schemas/active?stage=...` per lifecycle stage pulls every active bundle for the tenant. AJV instances pre-compiled and cached per `(productVersionId, stage)`. If an edge gateway later adds an external `/api` prefix, that is a gateway concern; in-repo Minimal API route groups stay consistent with the existing unprefixed `/submissions`, `/policies`, `/renewals` convention.
3. **IndexedDB persistence** — bootstrap payload persisted keyed by `(productVersionId, stage, etag)`. Survives offline. Next session load first, revalidate in background.
4. **Lazy with ETag** — for cache misses (e.g., user opens a deprecated-version submission that wasn't in the active set), `GET /lob-schemas/{productVersionId}/{stage}` with ETag cache headers. 304 most of the time.

**Pin to initial version for the duration of an editing session.** Open a form → snapshot the `productVersionId` → use that version for the entire edit. Never auto-swap a schema under a user mid-edit. New version picked up by next form open.

**Decouples product release from FE release — for data-schema-only changes within the existing widget vocabulary.** UW activates `cyber/1.1.0` on Tuesday (an additive field, a loosened cap, a new enum value, a new conditional section renderable by existing widgets) → every session opened after that picks it up via bootstrap → no FE ship required. FE *is* redeployed when: (a) new compile-time TS type safety is needed for a known version, or (b) the new version introduces a custom widget that isn't in the current widget registry (vehicle schedule, COPE accordion, D&O tower visualizer, etc.).

### 2.6 Lifecycle and request-carries-version contract

Schema versions move through: `Draft → Active → Deprecated → Retired`.

- Every write request carries `lobProductVersionId` explicitly. Backend validates against **that exact version**, never "latest."
- `Active`: normal operation.
- `Deprecated`: writes accepted only for existing rows already pinned to that product version, response carries `Warning: 299 - "Schema version <X> is deprecated; current is <Y>"` header. New creates cannot choose Deprecated versions. UI surfaces a non-blocking nudge.
- `Retired`: writes get `409 SchemaVersionRetired` ProblemDetails with the upgrade path. Reads still work.
- Historical rows read correctly forever — they validate against the version pinned on the row.
- Retirement is coordinated and rare — only when a regulator or strict policy demands it.

Status transitions require `schema-steward` role (Casbin policy). Every transition emits an audit event: `{actor, fromState, toState, productVersionId, timestamp, reason}`.

**Write eligibility.** The client carries `lobProductVersionId`, but the backend decides whether that version can be used for the requested operation:

- **Create:** only tenant-available `Active` product versions where `is_null_lob_sentinel = false` AND `is_legacy_sentinel = false` are allowed for general creates. `_unspecified/0.0.0` (the null-LOB sentinel — `is_null_lob_sentinel = true`) is allowed only for Submission/Renewal rows whose `lineOfBusiness IS NULL` and whose `attributes` is `{}`. The carve-out is a separate dimension from lifecycle status: `_unspecified/0.0.0` remains `Active` (it must validate empty payloads forever), but the `is_null_lob_sentinel` flag tells the middleware to apply the narrow null-LOB rule rather than the general "Active = any create" rule.
- **Update without migration:** the request must use the row's existing pinned `lob_product_version_id`; Deprecated is allowed here with the warning above. Switching versions is rejected unless the service is executing an explicit migration path or the triage-transition exception below.
- **Triage transition out of `_unspecified/0.0.0`:** a Submission or Renewal row pinned to `_unspecified/0.0.0` may switch to a tenant-available Active product version *in the same write* that sets `lineOfBusiness` (`NULL → non-NULL`) and supplies `attributes` valid against the new product version. This is the normal user write that converts a triaged submission into a Cyber/Property/etc. submission and is *not* a §2.12 migration. The trigger (§5.7 Step E) recognizes the pattern — `OLD.lob_product_version_id == _unspecified/0.0.0` AND `OLD.line_of_business IS NULL` AND `NEW.line_of_business == NEW.product.line_of_business` — and accepts it without the `app.lob_migration_in_progress` GUC. Outside this exact pattern, version switches still require the migration GUC.
- **Migration/endorsement:** may move a row to a different product version only through §2.12 migration-on-endorsement or an approved retirement migration job (with the GUC bypass).
- **Legacy sentinels:** create and attribute/version-changing writes are blocked regardless of status. Core-only writes against an existing legacy-pinned row are allowed when the request keeps `lob_product_version_id` unchanged, keeps `attributes_json` byte-for-byte equivalent after canonicalization, and passes the ordinary domain workflow guards. This preserves existing operations such as status transitions, assignment, cancellation/reinstatement metadata, and core policy edits while preventing new LOB attributes from landing on a pass-through schema.

**Legacy sentinels (`_legacy/<lob>/`) — narrow lifecycle exception.** Legacy sentinel product versions carry `is_legacy_sentinel = true` on `lob_product_version` (§5.3). Their lifecycle is unusual:

- **Status at activation:** `Deprecated` from the moment they are seeded — never `Active`. Reads render normally (Deprecated still renders, per the matrix above).
- **Writes:** create and attribute/version-changing writes are blocked at the validation-pipeline middleware *regardless of status*, with `ProblemDetails` code `LEGACY_SENTINEL_WRITE_BLOCKED`. Core-only updates are explicitly allowed when the pinned version and attributes are unchanged. This is the narrow exception to the general rule that Deprecated accepts unrestricted writes.
- **Endorsements against a legacy-sentinel-pinned row:** route through the §2.12 migration-on-endorsement path — the endorsement service migrates the row's `attributes_json` forward to the LOB's current Active version before applying the endorsement delta. This is the supported write path for changing LOB attributes or product version on a legacy-pinned row.
- **Retirement:** legacy sentinels may be retired once all rows pointing at them have been migrated forward (tracked via the deprecated-age KPI, §8.2). `_unspecified/0.0.0` is not a legacy sentinel and is never retired because new Submissions/Renewals with null LOB will always need it.

The flag and the middleware rule together let us satisfy both the §2.11 invariant (rows have a valid, LOB-matching pinned version) and the safety property that no new or changed LOB attribute data lands on a pass-through legacy schema.

### 2.7 Indexed projections

Reporting and portfolio filters never scan JSONB raw. The bundle's `projections.json` declares paths to be promoted to PostgreSQL generated columns + indexes.

**`projections.json` shape:**
```json
[
  {
    "id": "records-held-count",
    "path": "$.recordsHeld",
    "sqlType": "bigint",
    "entities": ["submissions", "policy_versions"],
    "stages":   ["submission", "policy", "endorsement", "renewal"],
    "nullBehavior": "null-on-missing",
    "default": null,
    "indexType": "btree",
    "indexName": "ix_{entity}_cyber_records_held",
    "queryHint": "portfolio filter: WHERE records_held_count > N",
    "materialization": "generated-stored"
  }
]
```

**Rules — scoping:**
- `entities` (required): subset of `["submissions", "policy_versions", "policy_endorsements", "renewals"]` naming which attribute-carrier tables receive the generated column + index. Never implicit — listing every target table is a deliberate act that the linter will question. Policy portfolio queries that need attribute filters join from `Policies.CurrentVersionId` to `PolicyVersions`; projections still live on `policy_versions`, not on `policies`.
- `stages` (required): subset of `["submission", "policy", "endorsement", "renewal"]` indicating which bundle stages carry the data. Informational; stages drive which `attributes_json` rows the path can actually resolve against.
- `indexName`: may use `{entity}` placeholder; expanded per target table at migration-generation time. 63-char Postgres cap enforced on the expanded name.
- Column naming: `<lob>_<concept_snake_case>`. Same column appears on every entity in `entities`.
- `entities[]` values are logical names. Migration generators MUST map them to the repo's physical EF/Postgres names (`"Submissions"`, `"PolicyVersions"`, `"PolicyEndorsements"`, `"Renewals"` and PascalCase column names unless a configuration overrides them). SQL snippets in this plan are illustrative and cannot be copied verbatim without applying that mapping.
- `path` uses a restricted dot-path grammar only: `$` followed by one or more `.identifier` segments, where `identifier` matches `[A-Za-z_][A-Za-z0-9_]*`. No wildcards, filters, array indexes, quoted keys, or recursive descent. The migration generator converts `$.requestedLimit.amountMinor` to the PostgreSQL text path `'{requestedLimit,amountMinor}'`; tests cover path conversion and reject hand-interpolated SQL.
- Explicit `sqlType` cast per projection; ambiguous casts rejected at activation.
- `nullBehavior` required: `null-on-missing` or `default-on-missing` (with an explicit `default`).
- **GIN is opt-in, not default.** Default is no GIN on `attributes_json`. GIN added only when explicit free-form key lookup is required (rare). For typed-path queries, per-path generated-column + btree is the mechanism. Filtering on un-projected paths is a code smell flagged at review.

**Rules — materialization.** Every projection declares `materialization: "generated-stored"` or `"materialized-column"`. Choice drives the migration shape; *the two are not combined.*

*Path A — `generated-stored` (small / new tables, or new entities):*
- Single `ALTER TABLE <t> ADD COLUMN <col> <sqlType> GENERATED ALWAYS AS ((attributes_json #>> '{compiled,path}')::<sqlType>) STORED;`, where `{compiled,path}` is generated from the restricted `path` grammar above. Postgres computes on write; no read-time cost.
- Accept the full-table rewrite cost; schedule during a low-traffic window or use on a freshly created entity table.
- No backfill step (values derive from existing `attributes_json`), no `NOT NULL` toggle (use `null-on-missing` or `default-on-missing` in the expression).
- Index creation: `CREATE INDEX CONCURRENTLY IF NOT EXISTS <ix> ON <t>(<col>);` — a separate migration step from the ADD COLUMN. EF Core migrations that issue `CREATE INDEX CONCURRENTLY` MUST be marked with `[SuppressTransaction]` (or equivalent raw-SQL operation with transaction suppression) because CONCURRENTLY cannot run inside a transaction block.

*Path B — `materialized-column` (large existing tables):*
- Step 1: `ALTER TABLE <t> ADD COLUMN <col> <sqlType> NULL;` (fast, metadata-only).
- Step 2: write trigger `BEFORE INSERT OR UPDATE OF attributes_json` that sets `NEW.<col> = (NEW.attributes_json #>> '{compiled,path}')::<sqlType>`. This keeps the materialized value consistent with `attributes_json` going forward.
- Step 3: backfill in batches (SQL script with `LIMIT`/`OFFSET` by PK window, e.g. 10K rows per batch, sleep between batches to yield WAL bandwidth).
- Step 4 (optional): `ALTER TABLE <t> ALTER COLUMN <col> SET NOT NULL` — only if `nullBehavior = default-on-missing` and backfill guaranteed non-null.
- Step 5: `CREATE INDEX CONCURRENTLY IF NOT EXISTS <ix>` — separate migration with `[SuppressTransaction]`.
- **Expression-index alternative** — skip the column and use `CREATE INDEX CONCURRENTLY <ix> ON <t> (((attributes_json #>> '{<path>}')::<sqlType>))` when reporting does not need to read the value out as a first-class column. Lower migration cost; slightly worse read ergonomics (query authors must write the expression). PM/architect choose.

**Rules — shared:**
- Every projection migration ships a paired `down.sql` (drop index, drop column / drop trigger, in the inverse order).
- Per projection, an EXPLAIN test asserts the index is used for the documented query pattern; PR fails if the planner skips the index.
- Migration-generation is driven by the projection's `entities` list — generator emits per-target-table migration fragments.

### 2.8 Rules governance (JsonLogic)

JsonLogic without governance becomes a second untyped platform. Governance:

**`rules.json` envelope** — meta-validated by `_meta/rules.schema.json`:
```json
[
  {
    "id": "personal-property-cap",
    "code": "PERSONAL_PROPERTY_EXCEEDS_70PCT_DWELLING",
    "pointer": "/coverageInfo/personalPropertyAmount",
    "severity": "error",
    "description": "Personal property cannot exceed 70% of dwelling amount.",
    "expression": { "<=": [{ "var": "data.coverageInfo.personalPropertyAmount" },
                           { "*":  [0.7, { "var": "data.coverageInfo.dwellingAmount" }] }] }
  }
]
```

Rule failure produces the same normalized error envelope as schema failures: `{code, pointer, ruleId, severity}`.

**Deterministic context.** Rules see exactly:
```typescript
type RuleContext = {
  data: object;         // entity's attributes_json
  core: {               // entity's core columns, read-only allowlisted projection
    lineOfBusiness: string;
    effectiveDate: string;
    accountId: string;
    brokerOfRecordId: string;
    // explicit per-stage allowlist
  };
  context: {            // request-scope, pre-resolved by service layer
    today: string;
    currency: string;
    region: string;
  };
};
```

No live database access from rules. No I/O. Anything a rule needs must be in `core` or `context`.

**Custom ops.** Under `_shared/rules/operations/<op>/<semver>/`:
```
spec.md       # human-readable contract
fe.ts         # json-logic-js plugin
be.cs         # JsonLogic.Net plugin
fixtures/
  cases.json  # input → expected output
```
CI step `validate-rule-ops` runs every fixture through both engines and asserts identical results.

**FE/BE rule parity.** Per bundle, `examples/rule-cases/{passing,failing}/*.json` with `{data, core, context, expected}`. Conformance harness runs each through both engines; asserts identical pass/fail + identical `(code, pointer)` multiset.

**Pinned tooling:**
- FE: exact `json-logic-js` version pinned in `experience/package.json` and `pnpm-lock.yaml`
- BE: exact `JsonLogic.Net` version pinned in the relevant `.csproj`
- Rule depth ≤ 8, evaluation timeout 20ms per request.

### 2.9 Security & tenancy

**Authorization matrix:**

| Operation | Required role/attribute |
|---|---|
| Read `Active` bundle | Any authenticated user with read on the lifecycle entity, tenant matches |
| Read `Deprecated` bundle | Same as Active (existing rows must render) |
| Read `Retired` bundle | Same as Active (read-only; writes blocked in middleware) |
| Read `Draft` bundle | `schema-steward` role only |
| Transition `Draft → Active` | `schema-steward` |
| Transition `Active → Deprecated` | `schema-steward` |
| Transition `Deprecated → Retired` | `schema-steward` (with cool-down: ≥30 days deprecated) |

Casbin policies: `lob_schema:read:{active,deprecated,retired,draft}`, `lob_schema:transition:{activate,deprecate,retire}`.

**Tenant/product availability.** `lob_product` has `tenant_availability text DEFAULT 'all'` (`all` | `whitelist` | `blacklist`) and `tenant_ids uuid[] DEFAULT '{}'`. Bootstrap filters by requester's tenant.

**Bundle integrity.**
- At activation, CI signs bundle with HMAC-SHA256 (key from secret store). Signature stored in `lob_schema_bundle.signature`.
- `LobSchemaResolver` verifies signature on load; tamper → startup failure with loud log.
- No remote `$ref` allowed (bundler rejects; runtime rejects).

**Resource limits:**

| Limit | Value | Enforcement |
|---|---|---|
| Bundle size (compressed) | ≤ 256 KB per `(productVersionId, stage)` | Bundler at activation |
| `attributes` payload per write | ≤ 64 KB | API gateway + middleware |
| Schema complexity score (depth × keyword count) | ≤ 200 | Profile linter at activation |
| JsonLogic rule depth | ≤ 8 | Rules linter at activation |
| Schema validation timeout | 50 ms per request | Wrapped in both engines |
| Rules evaluation timeout | 20 ms per request | Wrapped in BE |
| AJV compiled-validator LRU cache | 200 entries (FE) | Evicted LRU |
| IndexedDB schema cache | Purge entries unused > 7 days OR total > 50 MB | Background purge |
| Offline write queue | 3 retries with exponential backoff; surface `SchemaVersionRetired` to user if queued version was retired before replay | FE queue |

### 2.10 PolicyVersion existing snapshots — supplement, do not replace

`PolicyVersion.ProfileSnapshotJson`, `CoverageSnapshotJson`, `PremiumSnapshotJson` (file `engine/src/Nebula.Domain/Entities/PolicyVersion.cs:13-15`) are immutable platform-level frozen denormalizations of *core* state at version-time. They are **not** LOB-specific.

**Decision:** `PolicyVersion.attributes_json` **supplements** the existing snapshots; it does not replace or derive from them. It is the authoritative policy-level LOB-attribute record. `Policy` has no independent `attributes_json` source in Stage 2; policy-facing responses expose current attributes by reading through `Policy.CurrentVersionId → PolicyVersion`.

| Column | Carries | Source | Mutability | Validated against |
|---|---|---|---|---|
| `ProfileSnapshotJson` | Account/broker/carrier/producer at version-time | Frozen from Policy/Account state at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `CoverageSnapshotJson` | Full coverage set at version-time | Frozen from PolicyCoverageLine at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `PremiumSnapshotJson` | Premium breakdown at version-time | Frozen from premium calc at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `PolicyVersion.attributes_json` *(new)* | Policy-level LOB-specific attributes at version-time | Frozen from Submission/Endorsement input when a PolicyVersion is created | Immutable per row | Dynamic registry schema (this plan) |
| `Submission` / `Renewal` / `PolicyEndorsement.attributes_json` *(new)* | Stage-local LOB-specific attributes | Request payload for that lifecycle row/stage | Immutable unless explicit triage/migration path allows version switch | Dynamic registry schema (this plan) |
| `lob_product_version_id` *(new)* | The LOB schema version used to validate `attributes_json` | Set from request | Immutable per row | — |

Snapshot columns are not migrated. They remain platform internals, hand-shaped C# DTOs serialized as JSON-as-text. They are not subject to `_shared/` versioning.

### 2.11 `lineOfBusiness` invariant

`lineOfBusiness` is load-bearing: dashboards, SLA thresholds, role-based filters, regulatory reporting, API contracts, renewal-window rules all consume it. It must stay consistent with the registry.

**Invariant — enforced at three layers:**

```
For every row R in {Submission, PolicyVersion, PolicyEndorsement, Renewal}:
  let pv = lookup(R.lob_product_version_id)
  ( R.lineOfBusiness == pv.product.line_of_business )                  -- normal case: matching LOB
  OR ( R.lineOfBusiness IS NULL AND pv.product.code == '_unspecified') -- null-LOB case on Submission/Renewal only
```

Legacy sentinels (`_legacy/<lob>/0.0.0`) satisfy the first clause — they carry `line_of_business = <LOB>` on `lob_product` and therefore pass the normal-case check. No special case needed in the invariant itself; what distinguishes them is `is_legacy_sentinel = true` on the product version, which the validation middleware reads to block creates and attribute/version-changing writes (§2.6).

**Policy parent consistency.** `Policy` has no `lob_product_version_id`; it is checked through its current version. Application and database consistency tests assert that `Policy.LineOfBusiness` equals the `LineOfBusiness` denormalized on `Policy.CurrentVersionId → PolicyVersion` whenever `CurrentVersionId` is non-null. Policy creates/endorsements create or select a `PolicyVersion` whose product LOB matches the parent policy LOB.

**Layer 1 — Database.** Trigger `enforce_lob_consistency()` on each attribute-carrier table runs on INSERT/UPDATE. Looks up the product version's product; asserts `line_of_business` matches (or null/`_unspecified` pair). Raises `LOB_PRODUCT_MISMATCH` on violation. Cheap — one indexed lookup per write.

**Layer 2 — Service.** `LobSchemaResolver.Resolve(productVersionId)` returns `product.lineOfBusiness` and `is_legacy_sentinel` alongside the bundle. Middleware asserts LOB match BEFORE schema validation; emits `LOB_PRODUCT_MISMATCH` on mismatch, `LEGACY_SENTINEL_WRITE_BLOCKED` when a legacy-sentinel-pinned row attempts a create, attribute change, or version switch outside the migration-on-endorsement path. Core-only writes with unchanged attributes and unchanged pinned version continue to ordinary domain validation.

**Layer 3 — Test.** Conformance harness: every example fixture's `lineOfBusiness` matches its bundle's product LOB. Migration test: every backfilled row satisfies the invariant (including the per-LOB legacy-sentinel branch for pre-existing PolicyVersion / PolicyEndorsement rows and Cyber-at-Stage-3 rows). A policy-parent consistency test verifies `Policy.LineOfBusiness` matches the current version's denormalized `LineOfBusiness`.

**LineOfBusiness columns on policy attribute carriers.** Stage 2 (§5.7) adds **immutable denormalized** `line_of_business` columns to `PolicyVersion` and `PolicyEndorsement` (populated from the parent `Policy.LineOfBusiness` at row creation; never user-editable; trigger enforces immutability). Without these, the invariant has no input on those two tables.

**Null handling:**
- `Submission` and `Renewal` may have `lineOfBusiness IS NULL` early in the lifecycle → `lob_product_version_id` MUST point at `_unspecified/0.0.0`.
- `PolicyVersion` and `PolicyEndorsement` always have non-null `line_of_business` (denormalized from `Policy.LineOfBusiness` per above). `Policy.LineOfBusiness` remains required on the parent and is checked against `CurrentVersionId`.
- Transition `lineOfBusiness: NULL → 'Cyber'` (e.g., Submission triage) must happen in the same write that updates `lob_product_version_id` from `_unspecified/0.0.0` to a real Cyber product version.

### 2.12 Upgrade / migration path

Most version bumps are additive (new optional field) and need no migration. For rare cases that need data transformation, bundles may include a `migrations/` folder:

```
cyber/
  1.1.0/
    migrations/
      from-1.0.0/
        upgrade.json        # JsonLogic transformation 1.0.0 → 1.1.0
        downgrade.json      # optional reverse for rollback
        fixtures/
          input.json        # 1.0.0-shaped attributes
          expected-output.json  # 1.1.0-shaped result
```

**Rules:**
- Migrations are explicit JsonLogic transformations using the governed op set.
- CI tests: `apply(upgrade.json, input.json) == expected-output.json`, then validate output against `1.1.0/data.schema.json`.
- Migrations are **NOT** automatically applied to existing rows. Historical rows stay pinned. Migrations exist for three cases:
  1. **Endorsement against a stale version** — when an endorsement creates a new PolicyVersion, the steward may opt to migrate the prior `attributes_json` forward to the latest active version before applying the endorsement delta. Configurable per LOB.
  2. **Endorsement against a legacy sentinel** — **mandatory**, not optional. When an endorsement is applied to a row pinned at `_legacy/<lob>/0.0.0`, the endorsement service MUST migrate the row's `attributes_json` forward to the LOB's current Active version (or Deprecated, if no Active exists) before applying the delta. If no migration path exists (`LobSchemaResolver.canMigrate(_legacy/..., target)` is false because the bundle carries no `migrations/` folder — which legacy sentinels don't), the service falls back to **field-initialized migration**: start from the target schema's defaults/examples, surface the PolicyVersion attributes as required UI input to the user as part of endorsement capture, and fail the endorsement if required fields remain unfilled. This is the only supported path for changing LOB attributes or product version on a legacy-pinned row; core-only writes remain allowed per §2.6.
  3. **Mass migration on retirement** — when a version is being retired and a few rows still exist, an admin job applies the upgrade path (or the field-initialized fallback).
- `LobSchemaResolver.canMigrate(fromV, toV)` returns whether a migration path exists (transitively via intermediates).

### 2.13 Anti-patterns

Future agent sessions must refuse these:

- Adding a column for a LOB-specific attribute. (Use `attributes_json` + schema bump.)
- Generalizing JSONB to non-variant entities (Account, Broker, Contact).
- Skipping `lob_product_version_id` on a write.
- Loosening or redefining a `_shared/` primitive. (New version only.)
- Custom AJV keywords. (Push to JsonLogic with governed ops.)
- Auto-swapping schema versions under an open form. (Pin on open.)
- Hand-editing OpenAPI `components/schemas` for LOB attributes. (Build output only.)
- Filtering portfolio queries on un-projected JSON paths.
- Using `multipleOf` on non-integer money. (Integer minor units only.)
- Using `anyOf`, `not`, `patternProperties`, `contains`, `dependentSchemas` in bundle schemas. (Outside restricted profile.)

### 2.14 Deterministic UUIDs for `lob_product` and `lob_product_version`

`lob_product.id` and `lob_product_version.id` are **derived deterministically**, not randomly generated. Same inputs produce the same UUID across dev, staging, prod, CI, and developer machines. This is load-bearing because:

- The OpenAPI 3.0.3 `discriminator.mapping` (§4.3.4) bakes literal UUIDs into the generated TypeScript and C# types. If UUIDs were per-environment, the API contract — and every consumer's generated types — would also be per-environment.
- Test fixtures, migration constants, and seeded sentinel references can hard-code UUIDs without environment-specific lookup tables.
- A UUID consistently means "Cyber 1.0.0" in any log line, error message, or audit row, regardless of which environment produced it.

**Algorithm — UUIDv5 (RFC 4122 §4.3, name-based with SHA-1):**

```
NEBULA_LOB_NAMESPACE = UUIDv5(uuid.NAMESPACE_DNS, "lob.nebula.local")
                     = "<fixed UUID, declared once in code, never changes>"

lob_product.id          = UUIDv5(NEBULA_LOB_NAMESPACE, productCode)
                          # e.g., "cyber" → same UUID everywhere

lob_product_version.id  = UUIDv5(NEBULA_LOB_NAMESPACE, f"{productCode}/{semver}")
                          # e.g., "cyber/1.0.0" → same UUID everywhere
```

**Rules:**
- `NEBULA_LOB_NAMESPACE` declared once in `Nebula.Domain.LobNamespaces` (BE) and `experience/src/lib/dynamic-form/lob-namespace.ts` (FE). The literal UUID lives in source — never regenerate.
- `productCode` is the lower-snake-case product code (`cyber`, `_unspecified`, `_legacy_cyber`). Sentinel names use the same scheme.
- The semver fragment is the bare semver string (`1.0.0`, `1.1.0`, `0.0.0`); no leading `v`.
- A unit test asserts `UUIDv5(NEBULA_LOB_NAMESPACE, "cyber/1.0.0") == <hard-coded literal>` for at least one product version per LOB — a tripwire that catches accidental namespace changes.
- Renaming a `productCode` is a breaking change that produces a different UUID. Treat it as a new product with a migration path.

**Why not random UUIDs.** Random UUIDs would force the discriminator mapping in `nebula-api.yaml` to be regenerated per environment, would force every consumer's generated codegen to be environment-specific, and would prevent CI fixtures from pinning UUIDs in expected outputs. The cost is exactly one fixed namespace UUID.

---

## 3. Stage 0 — Decision lock (ADRs)

**Owner:** architect agent + schema-steward sub-skill
**Approver:** human reviewer (you) in PR
**Gates:** all downstream stages

### 3.0 Prerequisites (must be satisfied before Stage 0 begins)

- **ADR-018 promoted from `Proposed` → `Accepted`.** The Policy aggregate / version semantics in ADR-018 underpin §2.10 (PolicyVersion snapshot interaction and Policy-parent exception) and §5.7 (attribute-carrier column additions). Promoting it here closes a load-bearing dependency that this plan already assumes.

### 3.1 Deliverables

Four binding ADRs in `nebula-insurance-crm/planning-mds/architecture/decisions/`:

| ADR | Pins |
|---|---|
| `ADR-NNN-lob-extensible-attribute-architecture.md` | Core/extension split; which entities qualify; `_shared/` layout; `lob_product_version_id` immutability; `attributes_json` JSONB; indexed-projection rule; PolicyVersion snapshot interaction and Policy-parent exception (§2.10); `lineOfBusiness` invariant (§2.11); migration path (§2.12); security & tenancy (§2.9); operational limits (§2.9); `_unspecified/0.0.0` null-LOB sentinel + `is_null_lob_sentinel` flag; per-LOB legacy-sentinel backfill + `is_legacy_sentinel` flag; deterministic UUIDv5 derivation rule (§2.14) |
| `ADR-NNN-form-engine-rhf-ajv-shadcn-registry.md` | Custom RHF + AJV + shadcn widget registry over RJSF/JSONForms/Formily; pin-to-initial-version-during-edit; widget registry contract; theme rules for dynamic widgets |
| `ADR-NNN-validator-equivalence-restricted-profile.md` | Draft 2020-12 for LOB bundles; exact AJV + Json.Schema.Net versions pinned; restricted schema profile (§2.3 keyword allow/deny lists); normalized error envelope + stable error-code dictionary; money as integer minor units; parity-as-CI-contract |
| `ADR-NNN-rules-governance-jsonlogic.md` | `rules.schema.json` meta-schema; rule envelope (id/code/pointer/severity/expression); deterministic context shape; custom-op registry under `_shared/rules/operations/`; FE/BE op and rule parity tests |

### 3.2 Acceptance criteria (Stage 0 → Phase A / Phase B unlock)

- [ ] ADR-018 in `Status: Accepted` (prerequisite, see §3.0)
- [ ] All 4 ADRs in `Status: Accepted`
- [ ] Each ADR cross-links the others where decisions interlock
- [ ] Each ADR has a "Consequences" section (what becomes invalid — e.g., LOB bundles use the 2020-12 restricted profile while existing `planning-mds/schemas/` draft-07 contracts stay on the static track unless a separate OpenAPI 3.1 migration ADR is accepted)
- [ ] Each ADR names the framework reference docs it will produce in Stage 1
- [ ] Knowledge graph: each ADR added as a canonical node; `scripts/kg/validate.py` exits 0
- [ ] All 13 decisions (§13) captured in the ADRs with reasoning

Stage 0 is typically one or two architect-sessions of focused authoring.

---

## 4. Stage 1 / Phase A — Framework foundation (`nebula-agents/agents/**`)

**Repo:** `nebula-agents`. **Cadence:** framework release cadence, independent of CRM deploys.
**Owners:** architect agent + framework maintainers.
**Ships when:** all Phase A acceptance criteria (§4.5) green and the framework version is tagged. Phase B (§5) does NOT block Phase A merging or releasing — Phase B can pin to a specific Phase A framework version.

Pure framework work. No CRM code touched. Lands the design rationale into agent references so future agent-driven sessions follow the new patterns by default.

### 4.1 New files

| Path | Content |
|---|---|
| `agents/architect/references/extensible-attribute-architecture.md` | The master reference. Sections mirror §2 of this plan verbatim. Validation taxonomy (§2.2) embedded as-is. Pointers to ADRs from Stage 0. |
| `agents/architect/references/dynamic-form-engine.md` | FE engine spec. Why custom RHF+AJV+shadcn over RJSF/JSONForms/Formily. Engine architecture (schema walker, widget registry, AJV cache, RHF adapter, JsonLogic evaluator). Pin-to-initial-version rule. Theme and dark-mode rules. Custom widget registration contract. Error mapping (pointer → form field). Performance budgets. Testing patterns. |
| `agents/architect/schema-steward/SKILL.md` | New sub-skill. Owns registry governance: `_shared/` additions/bumps, additive-vs-breaking gate, `Draft → Active` activation sign-off, deprecation/retirement schedule, `projections.json` query-pattern review. In-scope/out-of-scope per standard SKILL format. Tools: Read, Write, Edit, AskUserQuestion. References master doc. |

### 4.2 Updated files

Each file below gets targeted additions — not rewrites. Existing content stays unless explicitly superseded.

#### 4.2.1 `agents/architect/references/json-schema-validation-architecture.md`

- Add a leading **"Static vs Dynamic Schemas"** section that frames two coexisting patterns:
  - **Static** (existing): request/response contracts for non-variant entities (Account, Broker, Contact, Task, Activity). Hand-authored draft-07 schemas live in `planning-mds/schemas/` and remain on the current static documentation/contract path. Runtime request validation in the current codebase is FluentValidation at Minimal API endpoints; any static-schema runtime validation or NJsonSchema adoption is a separate migration ADR.
  - **Dynamic** (new): registry-served bundles, version-pinned per row, `$ref`-shared-primitives, drives both validation and UI rendering.
- Add a decision prompt near the top: "Is this entity variant-typed? If yes → dynamic track; see `extensible-attribute-architecture.md`. If no → static track; follow the rest of this document."
- Keep existing static-track content unchanged below the new framing.
- Cross-link to `extensible-attribute-architecture.md` for all dynamic-track guidance.

#### 4.2.2 `agents/architect/SKILL.md`

- New primary responsibility under Responsibilities: **"Decide extension surface for variant-typed entities."** Determines whether attributes are core (columned), LOB-extension (registry-governed JSON), or both with materialized projections. Records decision in an ADR.
- New entry in Self-Validation checklist: "If entity has product/LOB variants, has the core/extension boundary been declared and an ADR recorded?"
- New entry in Definition of Done: "No column was added for a LOB-specific attribute."
- Add `agents/architect/references/extensible-attribute-architecture.md` to Required Resources.
- Validation Strategy section cross-references the validation taxonomy table (§2.2 of master reference) — an agent reading this SKILL sees "for every new validation, choose where it lives by consulting the taxonomy table."
- New handoff path: any work touching `_shared/` or LOB bundles routes through the Schema-Steward sub-skill.
- `agent-map.yaml` must either register `schema-steward` as a delegated Architect capability or as a first-class agent entry, with explicit read/write scope for `{PRODUCT_ROOT}/planning-mds/lob-schemas/**`, `{PRODUCT_ROOT}/planning-mds/operations/lob-schema-*`, and schema lifecycle PR-template files.

#### 4.2.3 `agents/backend-developer/SKILL.md`

- Tech stack additions for the **dynamic LOB bundle path**: **`Json.Schema.Net`** (2020-12 bundle validation), **`JsonLogic.Net`** (rules evaluation). Existing static request validation remains on the current FluentValidation endpoint path until a separate static-schema runtime-validation migration is accepted.
- New service pattern: **`LobSchemaResolver`** — startup-loads bundles from filesystem, verifies HMAC signatures, caches by `(productVersionId, stage)`, exposes `Resolve()` and `canMigrate()`.
- New middleware pattern: **validation pipeline order** is `lob-consistency → schema → rules → domain → persist → audit`. Middleware is request-scoped; errors short-circuit with normalized envelope (§2.3 of master reference).
- EF Core conventions for attribute carriers: `attributes_json` mapped via `HasColumnType("jsonb")` with owned-type wrapper exposing `JsonElement`; `lob_product_version_id` as required immutable FK. `Policy` parent does not get an independent attribute column in Stage 2; it reads current attributes through `CurrentVersionId`.
- Migration conventions — two **mutually exclusive** paths per projection, chosen by `materialization` field (§2.7):
  - **Path A — `generated-stored`** (new tables / small tables): single `ADD COLUMN … GENERATED ALWAYS AS (…) STORED`; Postgres populates on write. No backfill. Index via a *separate* migration step using `CREATE INDEX CONCURRENTLY IF NOT EXISTS`, which requires `[SuppressTransaction]` on the EF Core migration operation (CONCURRENTLY cannot run in a transaction block).
  - **Path B — `materialized-column`** (large existing tables): `ADD COLUMN … NULL` → write trigger on `INSERT OR UPDATE OF attributes_json` → batched backfill → (optional) `SET NOT NULL` → `CREATE INDEX CONCURRENTLY` in a separate `[SuppressTransaction]` step. Alternative: expression index on `((attributes_json #>> '{path}')::type)` when a first-class column is not needed.
  - Never combine `GENERATED ALWAYS STORED` with add-NULL/backfill — generated columns are derived, not backfilled.
  - Every projection migration ships a paired `down.sql`.
- GIN indexing on `attributes_json` is opt-in, not default — justified per projection.
- Anti-pattern callout in Troubleshooting: **"Never add a column for a LOB-specific attribute."** Points at master reference.
- Add `agents/architect/references/extensible-attribute-architecture.md` to Required Resources.

#### 4.2.4 `agents/frontend-developer/SKILL.md`

- RJSF removed from the default stack. New canonical pattern: **custom RHF + AJV + shadcn/ui widget registry**, implemented in `experience/src/lib/dynamic-form/`. See `agents/architect/references/dynamic-form-engine.md`.
- Integration contract: `<DynamicAttributePanel productVersionId={...} stage={...} />` is the drop-in for the LOB-specific section of any variant-entity screen; core/columned fields stay bespoke React around it.
- Delivery contract: **bootstrap at auth → IndexedDB cache → lazy-with-ETag fallback**. Never per-render HTTP fetches of schemas. AJV validators pre-compiled at bootstrap and cached (LRU 200).
- **Pin-to-initial-version-during-edit rule:** `useDynamicForm({ productVersionId, stage })` snapshots the version at open and never auto-swaps. A new version activated mid-session takes effect only on next form open.
- Widget registry: custom widgets registered under `experience/src/lib/dynamic-form/widgets/<name>.tsx`. Registration is declarative; widget picks up theme tokens automatically.
- Theme rules: all dynamic widgets use semantic shadcn tokens (text/surface/border). Light + dark visual smoke coverage required per LOB's widget set.
- Error mapping: server JSON Pointers (normalized envelope) map to RHF field paths via `error-normalizer.ts`.
- New FE dependencies to be added in Stage 2 (§5.9): exact pinned versions of `ajv`, `ajv-formats`, `ajv-errors`, `react-hook-form`, `json-logic-js`, `idb`, plus dev `json-schema-to-typescript` and `@types/json-logic-js` (no caret ranges).

#### 4.2.5 `agents/quality-engineer/SKILL.md`

- New required practice: every schema bundle ships fixture sets — `examples/valid/*.json`, `examples/invalid/*.json`, and `examples/rule-cases/{passing,failing}/*.json`. QE owns fixture review on every bundle PR.
- **Conformance harness (CI)** asserts FE/BE parity on the normalized error envelope — same `(code, pointer)` multisets, messages not compared. Divergence blocks merge.
- **Backwards-compat test** required on every minor bump: records written under `vN` must still validate under `vN+minor` without modification.
- **Migration fixture test** required on every migration: `apply(upgrade.json, input.json) == expected-output.json`, then output validates against target version's `data.schema.json`.
- **Rule parity test** required per bundle: FE `json-logic-js` and BE `JsonLogic.Net` produce identical rule outcomes against `examples/rule-cases/*`.
- Visual regression: light + dark smoke coverage for the dynamic panel per LOB; Playwright theme-smoke targets extend to include each LOB's first screen.
- Performance acceptance gates (§10 of master reference) are QE-owned: bootstrap payload size, validation latency p95, dynamic-form initial-render time, projection query p95.
- Add `agents/architect/references/extensible-attribute-architecture.md` to Required Resources.

#### 4.2.6 `agents/product-manager/SKILL.md`

- New story-write-time duty: **classify every new attribute** as core vs LOB-extension. Recorded in the story's Extension Surface section (see §4.3.2). Don't leave classification for the architect to retro-fit.
- **`_shared/` primitive sourcing:** if the attribute maps to an existing shared primitive (money, tiv, us-state, percent, etc.), reference it. If it proposes a new one, flag it in the story and route to Schema-Steward review before the story ships.
- **Schema bump impact:** PM declares expected bump level (none/minor/major) so Architect and QE can plan backwards-compat and migration work.
- New handoff: any story proposing a new `_shared/` primitive, or a major bump on an existing one, goes through the Schema-Steward sub-skill before entering the implementation backlog.
- New input requirement in Prerequisites: "Identified target product versions and which stage(s) — submission, policy, endorsement, renewal — the attribute lives on."

#### 4.2.7 `agents/devops/SKILL.md`

- New CI pipeline branch triggered by `planning-mds/lob-schemas/**` changes, in this order:
  1. Profile linter — enforce restricted JSON Schema profile (§2.3)
  2. Schema-diff — additive-vs-breaking detector; PR fails if minor bump contains breaking change
  3. Bundle compiler — resolve `$ref`s, inline, emit `compiled/<lob>/<semver>/<stage>.bundle.json`
  4. Bundle HMAC-SHA256 signing — key from secret store; signature persisted
  5. Schema parity harness — FE AJV + BE Json.Schema.Net against every `examples/{valid,invalid}/*`
  6. Rule parity harness — FE + BE JsonLogic against every `examples/rule-cases/*`
  7. Custom-op parity — `_shared/rules/operations/*/fixtures/cases.json` through both impls
  8. OpenAPI regeneration — rebuild the OpenAPI 3.0.3-compatible `components/schemas` discriminator block in `planning-mds/api/nebula-api.yaml`; fail if a 2020-12 bundle keyword cannot be projected without semantic loss
  9. FE TS type regeneration — `experience/src/generated/lob-types.ts` via `json-schema-to-typescript` from compiled 2020-12 bundles
- Activation-time gates: complexity score ≤ 200; bundle size ≤ 256KB compressed; rule depth ≤ 8.
- Audit table `lob_schema_validation_audit` partitioned monthly; 7-year retention; storage tier guidance.
- New secret managed: `LOB_SCHEMA_SIGNING_KEY` (HMAC-SHA256); rotation runbook in `planning-mds/operations/lob-schema-lifecycle.md`.
- Add `agents/architect/references/extensible-attribute-architecture.md` to Required Resources.

#### 4.2.8 `agents/ROUTER.md` + `agents/agent-map.yaml`

- New routes in `ROUTER.md`:
  - "Add a new LOB attribute" → PM (classify) → Architect (extension surface) → Schema-Steward (review bundle PR) → Backend/Frontend/QE (parallel implementation)
  - "Ship a new product version" → Schema-Steward as primary agent; Architect consulted for ADR if cross-cutting
  - "Deprecate schema X" / "Retire schema X" → Schema-Steward (own the transition and communications)
  - "Raise/lower a bound on a shared primitive" → Schema-Steward (determines major vs minor bump, runs steward review)
- New entries in `agent-map.yaml` for the Schema-Steward sub-skill with its in-scope actions: `approve-shared-primitive`, `approve-bundle-activation`, `schedule-deprecation`, `schedule-retirement`, `review-projection-query-pattern`.
- Update existing agent read/write boundaries: Architect, Backend, Frontend, QE, and DevOps read `{PRODUCT_ROOT}/planning-mds/lob-schemas/**`; only Schema-Steward/Architect own bundle governance writes, while Backend owns generated migrations/runtime rails and Frontend owns widget code.

### 4.3 Updated templates

Each template below gets new sections or prompts — existing template content stays intact.

#### 4.3.1 `agents/templates/entity-model-template.md`

New section **Extension Surface** inserted between Relationships (§3) and Indexes (§4). Structure:

- **Core columns** — table of name, type, required, purpose. The stable columned surface the platform operates on regardless of variant.
- **Extension attributes** — declares whether this entity is variant-typed. If yes:
  - Schema id pattern: `<lob>/<semver>/data.schema.json`
  - `attributes_json` JSONB column (NOT NULL DEFAULT `'{}'`)
  - `lob_product_version_id` immutable FK (`NOT NULL REFERENCES lob_product_version(id)`)
  - Policy parent exception: policy-level attributes live on `PolicyVersion`; the `Policy` parent references the current version and does not own an independent attributes column.
- **Indexed projections** — table of generated-column name, source JSON path, SQL type, null behavior, index type, index name.
- **Version-pinning invariant** — the `lineOfBusiness` consistency rule (§2.11) and what happens on null.

The validation taxonomy table (§2.2 of master reference) is embedded as a sub-block so template users see it on every entity design — reminding them "for every new validation, pick its row before writing the rule."

#### 4.3.2 `agents/templates/feature-template.md`

New section **Variant-entity declarations** (filled in only when the feature touches a variant-typed entity):

- Target LOB(s) — explicit list; "all" is never acceptable.
- Schema bump level — none / minor / major. Declared with rationale (what changes? additive? breaking?).
- New `_shared/` primitives proposed — path + proposed schema + steward-review pointer.
- New projections proposed — per projection: path, SQL type, null behavior, documented query pattern.
- Migration story — does existing data need `migrations/from-<prior>/upgrade.json`? Or is this additive-only and historical rows stay on prior version?
- Checklist item added to feature-ready gate: "If adding a LOB-specific attribute, no column has been proposed on any entity."

#### 4.3.3 `agents/templates/screen-spec-template.md`

New section **Dynamic attribute rendering** (filled in only when screen renders a variant entity):

- Target `(productVersionId, stage)` — explicit schema the panel binds to. If multiple products (e.g., a cross-product dashboard), declare the selection logic.
- Custom widget list — per widget: name, LOB-specific vs `_shared`, package path under `experience/src/lib/dynamic-form/widgets/`.
- Theme coverage — light + dark smoke required per widget variant; Playwright target declared.
- Fast-test layer — which tests at the component layer (`<DynamicAttributePanel>` unit), which at the integration layer (form + API), which at the E2E layer (Playwright flow). QE uses this to scope test authoring.
- Pin-during-edit note — reminds screen designers that a new schema version activated mid-edit does not appear in the open form.

#### 4.3.4 `agents/templates/api-contract-template.yaml`

New example block added for variant entities, showing:

- Variant-entity request/response body as `oneOf` across active product versions **at the envelope level** (where `lobProductVersionId` actually sits), with explicit `discriminator`. `attributes` inside each branch is a *single* concrete schema — never a `oneOf`. The example is OpenAPI 3.0.3-compatible because the current `planning-mds/api/nebula-api.yaml` is OpenAPI 3.0.3:
  ```yaml
  SubmissionBody:
    oneOf:
      - $ref: '#/components/schemas/SubmissionBodyCyberV1_0_0'
      - $ref: '#/components/schemas/SubmissionBodyPropertyV1_0_0'
      - $ref: '#/components/schemas/SubmissionBodyUnspecifiedV0_0_0'
    discriminator:
      propertyName: lobProductVersionId
      mapping:
        <uuid-cyber-1.0.0>:        '#/components/schemas/SubmissionBodyCyberV1_0_0'
        <uuid-property-1.0.0>:     '#/components/schemas/SubmissionBodyPropertyV1_0_0'
        <uuid-unspecified-0.0.0>:  '#/components/schemas/SubmissionBodyUnspecifiedV0_0_0'

  # Each branch fixes lobProductVersionId and inlines its own attributes schema:
  SubmissionBodyCyberV1_0_0:
    type: object
    required: [lobProductVersionId, attributes, ...]
    properties:
      lobProductVersionId:
        type: string
        format: uuid
        enum: [<uuid-cyber-1.0.0>] # OpenAPI 3.0-compatible stand-in for JSON Schema const
      attributes:          { $ref: '#/components/schemas/CyberAttributesV1_0_0' }
      # ...core fields
  ```
  `discriminator` is OpenAPI-only metadata that drives TS/C# codegen; runtime validation dispatches via the resolver (service layer), not via `oneOf` in JSON Schema.
- Response envelope additions for attribute carriers: `lobProductVersionId`, `lobProductId`, `lobProductVersionStatus` (`active`/`deprecated`/`retired`), `lobSchemaVersion` (semver). Policy responses expose the same metadata from `CurrentVersionId → PolicyVersion`.
- Error envelope showing the normalized `errors[]` shape with stable `code`, `pointer`, `keyword`, `constraint`.
- Inline comment: **"LOB attribute schemas in `components/schemas` are CI build-output from `scripts/build-openapi-lob-block.py`; do not hand-edit."** The generator must fail if a 2020-12 bundle keyword cannot be represented in the OpenAPI 3.0 projection without changing semantics.

#### 4.3.5 `agents/templates/adr-template.md`

New prompt added to the Decision section for variant-typed domains:

- **"Extensibility decision"** — are attributes core (columned), LOB-extension (registry-governed JSON), or both with materialized projections? Record reasoning.
- New required field: **"Related Schema Bundles"** — lists LOB bundles and `_shared/` primitives this ADR activates, creates, or changes.
- Extends existing ADR structure; does not replace. Applies only when the ADR touches variant-typed entities — harmless to skip for non-variant ADRs.

#### 4.3.6 `agents/templates/feature-assembly-plan-template.md`

New section **LOB Schema Activations** added to the per-feature execution plan that architect produces (`{PRODUCT_ROOT}/planning-mds/features/F{NNNN}-{slug}/feature-assembly-plan.md`):

- Bundles created or modified by this assembly plan — path + version + activation target date.
- `_shared/` primitives touched — path + version + steward approval status.
- Activation sequence — bundle activation relative to code merge (before, concurrent, after).
- Backfill or migration step — SQL/script path, estimated row count, rollback plan.
- Deprecation plan — if replacing a prior version, deprecation timeline and upgrade pointer.
- New exit criterion: "All schema bundles introduced by this feature pass the CI parity harness before the feature exits Phase C."

#### 4.3.7 `agents/templates/solution-patterns-template.md`

New seeded pattern entry ready to land in any new product's `SOLUTION-PATTERNS.md`:

- **Pattern name:** Schema Registry + Extensible Attributes
- **Summary:** Core/extension split on variant-typed lifecycle entities; attributes carried in JSONB governed by versioned schema bundles; one source of truth validated by AJV (FE) + Json.Schema.Net (BE) via a restricted JSON Schema profile and normalized error envelope.
- **When to apply:** Products with variant-typed lifecycle entities where attribute shape differs by variant (insurance LOBs, healthcare plan types, subscription products with tier variance, banking products with category variance).
- **When not to apply:** Entities with stable non-variant schema (reference data, user profiles, tasks, activity log). Don't generalize JSONB to non-variant entities.
- **References:** `extensible-attribute-architecture.md`, `dynamic-form-engine.md`.
- **Consequences:** listed as in master reference — compile-time safety trade-off, OpenAPI build-output requirement, projection discipline.

### 4.4 Knowledge graph template additions

Canonical node types added to the template knowledge graph (for any product adoption):
- `lob-product`
- `lob-product-version`
- `lob-schema-bundle`
- `lob-shared-primitive`
- `lob-rule-operation`

### 4.5 Stage 1 / Phase A acceptance

- [ ] Master reference exists and embeds §2 verbatim (validation taxonomy table in particular)
- [ ] Dynamic form engine reference exists
- [ ] Schema-steward sub-skill exists
- [ ] All 6 SKILL files updated with their additions
- [ ] All 7 templates updated
- [ ] Router + agent-map updated
- [ ] A walkthrough: an agent handed "add `cyber/1.0.0`" with only these references (and Stage 0 ADRs) can produce the right file layout, schema shape, and handoffs

---

## 5. Stage 2 / Phase B — Solution foundation (`nebula-insurance-crm`)

**Repo:** `nebula-insurance-crm`. **Cadence:** CRM release cadence.
**Owners:** architect + backend + frontend + devops on the CRM team.
**Ships when:** all Phase B acceptance criteria (§5.10) green; depends on Phase A being released to a tagged framework version that Phase B work pins to. Phase A and Phase B may proceed in parallel after Stage 0 completes — no in-flight cross-repo synchronization required.

Lands the rails. No LOB content yet.

### 5.1 Schema-package format spec

| Path | Content |
|---|---|
| `planning-mds/lob-schemas/README.md` | Package layout; semver rules; additive-vs-breaking definitions; example-fixture rules; steward governance pointer |
| `planning-mds/lob-schemas/_meta/schema-package.schema.json` | Meta-schema: every bundle has `data.schema.json`, `ui.schema.json`, `rules.json`, `projections.json`, `examples/{valid,invalid}/*`, `examples/rule-cases/{passing,failing}/*`, `README.md`; optional `migrations/` |
| `planning-mds/lob-schemas/_meta/rules.schema.json` | Meta-schema for `rules.json` (envelope from §2.8) |
| `planning-mds/lob-schemas/_meta/projections.schema.json` | Meta-schema for `projections.json` (shape from §2.7) — requires per-entry `entities[]`, `stages[]`, and `materialization ∈ {"generated-stored", "materialized-column"}`; rejects legacy entries missing these fields |
| `planning-mds/lob-schemas/_shared/README.md` | Shared primitive catalog; semantics per concept; versioning rules |

### 5.2 Sentinel products

**Two kinds of sentinels** are seeded in Stage 2, but only one is write-blocked legacy data.

**Null-LOB sentinel — `_unspecified/0.0.0`.** Carries Submission and Renewal rows where `lineOfBusiness IS NULL` (pre-triage state).

- `planning-mds/lob-schemas/_unspecified/0.0.0/` bundle:
  - `data.schema.json` — `{"type":"object","maxProperties":0,"additionalProperties":false}`
  - `rules.json` — `[]`
  - `projections.json` — `[]`
  - `ui.schema.json` — `{}`
  - `examples/valid/empty.json` — `{}`
  - `README.md` — "Null-LOB sentinel. Accepts only `{}`. Pinned on Submission/Renewal rows with `lineOfBusiness IS NULL`."
- Seeded into `lob_product` with `code = '_unspecified'`, `line_of_business = NULL`, `tenant_availability = 'all'`.
- `lob_product_version.status = 'Active'`, `is_legacy_sentinel = false`, `is_null_lob_sentinel = true`.
- Writes are allowed only for Submission/Renewal rows where `lineOfBusiness IS NULL` and `attributes = {}`. PolicyVersion and PolicyEndorsement can never point at `_unspecified/0.0.0`; Policy parent has no pinned version column.
- **Never deleted, never retired.** New null-LOB rows will always need it.

**Per-LOB legacy sentinels — `_legacy/<lob>/0.0.0`.** One bundle per LOB. Carries pre-registry attribute-carrier rows whose `lineOfBusiness` is non-null (existing `PolicyVersion` and `PolicyEndorsement` rows at Stage 2, existing Cyber submissions/renewals at Stage 3, existing `<LOB>` submissions/renewals at Stage 4).

- `planning-mds/lob-schemas/_legacy/<lob>/0.0.0/` bundle (per LOB):
  - `data.schema.json` — `{"type":"object","additionalProperties":true}`
  - `rules.json` — `[]`
  - `projections.json` — `[]`
  - `ui.schema.json` — `{}` (panel renders as read-only informational block)
  - `examples/valid/empty.json` — `{}`
  - `README.md` — "Legacy sentinel for <LOB>. Pinned on <LOB> rows that pre-date the real `<lob>/1.0.0` bundle. Reads render. Core-only writes may proceed when attributes/version are unchanged; attribute writes, creates, and ad hoc version switches are rejected by middleware; endorsements migrate the row forward to the LOB's current Active version (§2.12)."
- Seeded into `lob_product` with `code = '_legacy_<lob>'`, `line_of_business = '<LOB>'`, `tenant_availability = 'all'`.
- `lob_product_version.status = 'Deprecated'`, `is_legacy_sentinel = true`, `is_null_lob_sentinel = false`.
- Seeded lazily per LOB: `_legacy/_unspecified_none_needed` N/A; **Stage 2 seeds one legacy sentinel for every current LOB in `LineOfBusinessCatalog`** so PolicyVersion/PolicyEndorsement backfill can target them by LOB. With the current catalog this means ten sentinels, including Cyber. Cyber's is re-used at Stage 3 for existing Cyber rows rather than creating an additional artifact.
- Retireable once every row pointing at a given legacy sentinel has been migrated forward (tracked via §8.2 deprecation-age KPI).

### 5.3 Registry — data model and service

**Tables (new migration):**
```sql
CREATE TABLE lob_product (
  id uuid PRIMARY KEY,
  code text NOT NULL UNIQUE,               -- e.g., 'cyber', '_unspecified'
  display_name text NOT NULL,
  line_of_business text NULL,              -- null only for _unspecified
  tenant_availability text NOT NULL DEFAULT 'all',
  tenant_ids uuid[] NOT NULL DEFAULT '{}',
  created_at timestamptz NOT NULL,
  created_by_user_id uuid NULL
);

CREATE TABLE lob_product_version (
  id uuid PRIMARY KEY,
  product_id uuid NOT NULL REFERENCES lob_product(id),
  semver text NOT NULL,
  status text NOT NULL CHECK (status IN ('Draft','Active','Deprecated','Retired')),
  is_legacy_sentinel boolean NOT NULL DEFAULT false,    -- true only for _legacy/** pass-through bundles; creates and attribute/version-changing writes blocked (§2.6)
  is_null_lob_sentinel boolean NOT NULL DEFAULT false,  -- true only for _unspecified/0.0.0; carve-out for null-LOB Submission/Renewal with attributes = {} (§2.6)
  CHECK (NOT (is_legacy_sentinel AND is_null_lob_sentinel)),  -- mutually exclusive: a row is either a null-LOB sentinel, a legacy sentinel, or neither
  activated_at timestamptz NULL,
  deprecated_at timestamptz NULL,
  retired_at timestamptz NULL,
  UNIQUE (product_id, semver)
);

CREATE TABLE lob_schema_bundle (
  id uuid PRIMARY KEY,
  product_version_id uuid NOT NULL REFERENCES lob_product_version(id),
  stage text NOT NULL CHECK (stage IN ('submission','policy','endorsement','renewal')),
  data_schema_json jsonb NOT NULL,         -- bundled (ref-resolved)
  ui_schema_json jsonb NOT NULL,
  rules_json jsonb NOT NULL,
  projections_json jsonb NOT NULL,
  signature bytea NOT NULL,                -- HMAC-SHA256 of the above
  size_bytes integer NOT NULL,
  complexity_score integer NOT NULL,
  created_at timestamptz NOT NULL,
  UNIQUE (product_version_id, stage)
);

CREATE TABLE lob_schema_validation_audit (
  id bigserial PRIMARY KEY,
  entity_table text NOT NULL,
  row_id uuid NOT NULL,
  product_version_id uuid NOT NULL REFERENCES lob_product_version(id),
  schema_validation_result text NOT NULL,  -- 'accepted' | 'rejected'
  rule_evaluation_result text NOT NULL,
  error_codes text[] NOT NULL DEFAULT '{}',
  validated_at_utc timestamptz NOT NULL,
  actor_user_id uuid NULL
);
-- retention 7 years; append-only; partition by month
```

**Service — `LobSchemaResolver`:**
- Eagerly loads all **non-Draft** bundles (`Active` + `Deprecated` + `Retired`, including legacy sentinels) from filesystem at app startup — historical rows render correctly because every `lob_product_version_id` pinned on any row resolves. Alternatively, start with Active + legacy sentinels eager and lazy-resolve Deprecated/Retired on first use by `productVersionId`; choice is a memory/startup-cost tradeoff recorded in the ADR.
- `Draft` bundles not loaded at runtime (they're only visible to steward tooling, §2.9).
- **Verify HMAC-SHA256 signatures on every loaded bundle.** For each `lob_schema_bundle` row, the resolver recomputes `HMAC-SHA256(LOB_SCHEMA_SIGNING_KEY, canonical_bytes(data_schema_json || ui_schema_json || rules_json || projections_json))` and compares to the stored `signature` column. Mismatch → fail-fast startup with a structured log line carrying `productVersionId`, `stage`, expected vs. actual signature digests, and the runbook URL. The service must NOT start in degraded mode; an unverifiable bundle is treated as compromised.
- **Filesystem ↔ DB reconciliation.** A separate startup task (or post-deploy CI smoke test, see §8.1) recomputes the HMAC of the on-disk source under `planning-mds/lob-schemas/` for every `Active` and `Deprecated` row in `lob_product_version` and compares it to the DB row's `signature`. Drift → loud alert + the runbook flow for "filesystem source diverged from activated DB row." This catches half-completed activations, manual DB edits, or out-of-band tampering that the per-row signature check alone misses (because the per-row check only proves the row hasn't been edited *since signing*, not that the row matches the source-of-truth filesystem).
- Cache by `(productVersionId, stage)`.
- Expose `Resolve(productVersionId, stage) → SchemaBundle` returning schema + rules + projections + product metadata (`line_of_business`, `is_legacy_sentinel`, `status`).
- Expose `canMigrate(fromV, toV)` for migration-path checks.
- Hot-reload on SSE `schema:activated` event (Stage 5 V2; skeleton only in Stage 2).

**API endpoints:**
- `GET /lob-schemas/active?stage=...` — FE bootstrap payload for the tenant; returns **Active bundles only** (the set new writes can land on), **filtered to the requesting tenant's available LOB products** (`lob_product.tenant_availability` resolved against the requester's tenant). A tenant that only sells Cyber and Property gets only those two LOBs' active bundles, not all LOBs. ETag; `Cache-Control: private, max-age=3600`. Response keyed by `(productVersionId, stage)` so the FE can cache without knowing the tenant filter.
- `GET /lob-schemas/{productVersionId}/{stage}` — lazy fetch of a single bundle by pinned id, regardless of status (covers Deprecated/Retired/legacy-sentinel bundles needed to render historical rows). ETag.
- `GET /lob-products` — product catalog (lifecycle metadata incl. `is_legacy_sentinel`).

Returns the **bundled** form (resolved `$ref`s).

### 5.4 Validation pipeline middleware (.NET)

Request-scoped, order:

1. Resolve `lobProductVersionId` from request body.
2. `LobSchemaResolver.Resolve(productVersionId, stage)` → bundle + product.
3. Assert tenant/product availability and write eligibility (§2.6). Each rule maps to a distinct middleware test and a distinct ProblemDetails code:
   - **Create** — version MUST be `Active`, tenant-available, `is_legacy_sentinel = false`, AND `is_null_lob_sentinel = false`. The `_unspecified/0.0.0` carve-out (§2.6) applies only when `is_null_lob_sentinel = true` AND the row is a Submission/Renewal with `lineOfBusiness IS NULL` and `attributes = {}`. Violations: `SCHEMA_VERSION_NOT_ACTIVE`, `PRODUCT_NOT_AVAILABLE_FOR_TENANT`, `LEGACY_SENTINEL_WRITE_BLOCKED`, `INVALID_UNSPECIFIED_USAGE`.
   - **Update** — request MUST use the row's existing pinned `lob_product_version_id`. Deprecated permitted with `Warning: 299` header. Switching versions rejected with `SCHEMA_VERSION_SWITCH_DISALLOWED` unless either the migration GUC is set or the §2.6 triage-transition pattern matches.
   - **Migration / endorsement** — version switch allowed only when `app.lob_migration_in_progress = true` (set by the endorsement-migration service per §2.12) or the row is being moved by an approved retirement-migration job.
   - **Legacy sentinels** — creates, attribute changes, and product-version switches are rejected with `LEGACY_SENTINEL_WRITE_BLOCKED` regardless of status; core-only updates pass when `attributes` and `lobProductVersionId` remain unchanged. Endorsements route through the §2.12 migration path before writing a new attribute-bearing version.
   - **Retired** — writes always rejected with `409 SchemaVersionRetired` ProblemDetails carrying the upgrade path; reads pass through unaffected.
4. Assert `request.lineOfBusiness == bundle.product.lineOfBusiness` (see §2.11), with the `_unspecified/0.0.0` null-LOB exception for Submission/Renewal only. Emit `LOB_PRODUCT_MISMATCH` on violation.
5. `Json.Schema.Net` validates `attributes` against `data_schema_json`. Shim produces normalized envelope (§2.3).
6. `JsonLogic.Net` evaluates `rules_json` against `{data, core, context}`. Shim produces normalized envelope (§2.8).
7. Domain invariants (lifecycle, authorization via Casbin, workflow guards).
8. Persist. Emit `lob_schema_validation_audit` row.

Error contract: `application/problem+json` with stable `errors[]` shape (see §2.3).

**Telemetry contract — OpenTelemetry spans (mandatory).** Each pipeline step emits an OTel span on the request-scope trace, so the §10 performance budgets are observable per-request, not just in aggregate:

| Span name | Wraps | Required attributes |
|---|---|---|
| `lob.resolver.resolve` | `LobSchemaResolver.Resolve(productVersionId, stage)` | `lob.product_version_id`, `lob.product_code`, `lob.semver`, `lob.stage`, `lob.cache_hit` (bool) |
| `lob.middleware.lob_consistency` | Step 3–4 (eligibility + LOB invariant) | `lob.product_version_id`, `lob.line_of_business`, `lob.is_legacy_sentinel`, `lob.is_null_lob_sentinel`, `http.request.body.size` |
| `lob.validation.schema` | Step 5 (`Json.Schema.Net`) | `lob.product_version_id`, `lob.error_count`, `lob.first_error_code` (if rejected) |
| `lob.validation.rules` | Step 6 (`JsonLogic.Net`) | `lob.product_version_id`, `lob.rule_count`, `lob.error_count`, `lob.first_error_code` (if rejected) |

Spans set status to `ERROR` on validation failure (with the first normalized error code as `otel.status_description`). Latency histograms bucketed at 1ms / 5ms / 10ms / 25ms / 50ms / 100ms / 250ms align with the §10 budgets so dashboards can show p95 against budget directly. The same span names are used by the FE pre-compile path (`lob.validation.schema` on AJV evaluation) so trace timing is comparable across tiers.

### 5.5 CI hooks

| Script | Runs on | Purpose |
|---|---|---|
| `scripts/validate-lob-schemas.py` | `planning-mds/lob-schemas/**` changes | Bundle conformance to `_meta/schema-package.schema.json`; restricted-profile linter; complexity score; naming conventions |
| `scripts/schema-diff.py` | Same | Additive-vs-breaking detector between prior and new version; blocks mis-bumped PRs. **v1 scope (deliberately narrow):** uses `jsonref` to resolve `$ref`s, then catches the obvious cases — added `required` property, removed `enum` value, lowered `maximum` / `maxLength` / `maxItems`, raised `minimum` / `minLength` / `minItems`, narrowed `pattern` (best-effort string comparison), `type` change, removed property, `additionalProperties: true → false`, `format` removal/tightening from the whitelist. Anything outside this list (deep `allOf` recombination, `if/then/else` reshape, conditional branch collapse) → **conservative fallback: classify as breaking.** PR author can then justify in the PR description and explicitly bump major. The intent is "no false negatives on the obvious bumps; bias-to-breaking on the ambiguous ones." Plan to grow the catch list as real cases arrive — do NOT ship a sophisticated diff engine in v1. |
| `scripts/validate-schema-parity.ts` (+ `.csproj`) | Same | Runs every `examples/{valid,invalid}/*` through AJV (TS) and Json.Schema.Net (.NET); asserts normalized-envelope parity |
| `scripts/validate-rules-parity.ts` (+ `.csproj`) | Same | Runs every `examples/rule-cases/{passing,failing}/*` through both JsonLogic engines |
| `scripts/validate-rule-ops.ts` (+ `.csproj`) | `_shared/rules/operations/**` changes | Per custom op, runs fixtures through both implementations |
| `scripts/compile-bundles.py` | Same | Resolves `$ref`s, inlines, emits `compiled/<lob>/<semver>/<stage>.bundle.json`; signs with HMAC |
| `scripts/build-openapi-lob-block.py` | Same | Extracts active bundles → emits OpenAPI 3.0.3-compatible `oneOf`/`discriminator` block into `planning-mds/api/nebula-api.yaml` `components/schemas`; rejects non-projectable 2020-12 keywords |
| `scripts/build-fe-types.sh` | Same | `json-schema-to-typescript` over compiled 2020-12 bundles → `experience/src/generated/lob-types.ts` |
| `scripts/validate-projection-plan.py` | Same | EXPLAIN test per projection: index used for documented query |

### 5.6 Static schema and OpenAPI compatibility

Current repo state: `planning-mds/api/nebula-api.yaml` is OpenAPI **3.0.3**, and `planning-mds/schemas/*.json` are JSON Schema **draft-07**. Runtime request validation is currently implemented with FluentValidation in the Minimal API endpoint path. Stage 2 does **not** bulk-migrate static contracts to 2020-12 or replace FluentValidation for non-variant requests. Static schemas remain hand-authored documentation/contracts for non-variant entities (Account, Broker, Contact, Task, Activity, etc.) unless a separate static-schema runtime-validation ADR changes that stack.

The new LOB bundle source format is still JSON Schema 2020-12, but it lives under `planning-mds/lob-schemas/**`, not `planning-mds/schemas/**`. Compatibility rules:
- Runtime FE/BE validation uses the compiled 2020-12 bundles directly.
- `scripts/build-openapi-lob-block.py` emits an OpenAPI 3.0.3-compatible projection of active bundles into `nebula-api.yaml`; it must convert or reject unsupported constructs rather than leaking 2020-12-only keywords.
- The OpenAPI projection uses 3.0-compatible conventions (`nullable: true`, single-value `enum` for fixed `lobProductVersionId`, no `const`, no type arrays).
- A future OpenAPI 3.1 / static-schema 2020-12 migration is a separate ADR and repo-wide compatibility project, not a prerequisite for Stage 2.

### 5.7 Attribute-carrier column additions, denormalized LOB columns, LOB-aware backfill, invariant trigger

**Step A — register sentinels.** Before any entity-table migration runs, seed `lob_product` + `lob_product_version` + `lob_schema_bundle` rows for `_unspecified/0.0.0` and one `_legacy/<lob>/0.0.0` sentinel for every current LOB in `LineOfBusinessCatalog` (§5.2). Keep their UUIDs pinned in a migration constant for reuse in subsequent steps.

**Physical naming note.** The SQL below uses logical snake_case to keep the architecture readable. The implementation migration must map to the repo's physical EF names (`"PolicyVersions"`, `"PolicyEndorsements"`, `"Submissions"`, `"Renewals"`, `"Policies"`, and PascalCase columns such as `"LineOfBusiness"`) or explicitly configure snake_case mappings before using these fragments. Do not copy these snippets verbatim into an EF migration.

**Step B — add denormalized `line_of_business` to `PolicyVersion` and `PolicyEndorsement`.** These columns didn't exist before; without them the §2.11 invariant has no input on those tables. Immutable after write — the trigger rejects UPDATEs that change the value.

```sql
ALTER TABLE policy_versions
  ADD COLUMN line_of_business text NULL;  -- temp NULL while we backfill

ALTER TABLE policy_endorsements
  ADD COLUMN line_of_business text NULL;

UPDATE policy_versions pv
   SET line_of_business = p.line_of_business
  FROM policies p
 WHERE pv.policy_id = p.id;

UPDATE policy_endorsements pe
   SET line_of_business = p.line_of_business
  FROM policies p
 WHERE pe.policy_id = p.id;

ALTER TABLE policy_versions     ALTER COLUMN line_of_business SET NOT NULL;
ALTER TABLE policy_endorsements ALTER COLUMN line_of_business SET NOT NULL;

-- immutability enforced by the trigger added in Step E.
```

On all new INSERTs, the application layer MUST populate `line_of_business` from the parent `Policy.line_of_business` (EF Core entity configuration + domain service contract). User-facing APIs never accept this field as input.

**Step C — add `lob_product_version_id` and `attributes_json` columns on attribute carriers.** No `DEFAULT` on `lob_product_version_id` (it's resolved per row in Step D, by LOB).

```sql
ALTER TABLE submissions
  ADD COLUMN lob_product_version_id uuid NULL
    REFERENCES lob_product_version(id),
  ADD COLUMN attributes_json jsonb NOT NULL DEFAULT '{}';

-- identical shape for policy_versions, policy_endorsements, renewals.
-- Policies do not get attributes_json or lob_product_version_id in Stage 2;
-- policy-level attributes live on PolicyVersions and are reached through CurrentVersionId.
```

**Step D — LOB-aware backfill.** Each row is mapped to the sentinel that matches its `line_of_business`; rows with null LOB map to `_unspecified/0.0.0`. Pseudocode per table (real migration ships per-LOB `UPDATE … WHERE line_of_business = '<LOB>'`, one statement per LOB):

```sql
-- Submission / Renewal: null-LOB possible
UPDATE submissions
   SET lob_product_version_id = <uuid _unspecified/0.0.0>
 WHERE line_of_business IS NULL;

UPDATE submissions
   SET lob_product_version_id = <uuid _legacy/cyber/0.0.0>
 WHERE line_of_business = 'Cyber';
-- …one UPDATE per LOB code…

-- PolicyVersion / PolicyEndorsement: line_of_business is always non-null
-- after Step B. Same per-LOB UPDATE pattern, but no null branch.

ALTER TABLE submissions ALTER COLUMN lob_product_version_id SET NOT NULL;
-- same for the other three attribute-carrier tables.
```

A migration-level assertion runs at the end of Step D: `SELECT COUNT(*) FROM <t> WHERE lob_product_version_id IS NULL` must return 0 on every attribute-carrier table, else the migration aborts.

**Step E — invariant trigger.** `enforce_lob_consistency()` installed on each of the four attribute-carrier tables as a `BEFORE INSERT OR UPDATE` trigger:

- Resolves `NEW.lob_product_version_id` → product → `line_of_business`.
- On `submissions` / `renewals`: accepts `NEW.line_of_business IS NULL` only when the product's `code = '_unspecified'`; otherwise requires exact match.
- On `policy_versions` / `policy_endorsements`: requires exact match on non-null `line_of_business`.
- On UPDATE: rejects any change to `line_of_business` on `policy_versions` / `policy_endorsements` (immutability) and any change to `lob_product_version_id` *unless* one of two conditions holds:
  - **Migration bypass:** session-level GUC `app.lob_migration_in_progress = true`, set only by the endorsement-migration service (covers §2.12 cases).
  - **Triage transition (Submission/Renewal only):** `OLD.lob_product_version_id` resolves to product code `_unspecified` AND `OLD.line_of_business IS NULL` AND `NEW.line_of_business = NEW.product.line_of_business` (i.e., the same write supplies a real LOB matching the new product version). No GUC required — this is the user-driven triage path documented in §2.6.
- Raises `LOB_PRODUCT_MISMATCH` (or `LOB_IMMUTABLE_VIOLATION`) via `RAISE EXCEPTION` with a structured `ERRCODE`.

The trigger is installed **after** Step D completes, so the backfill itself is not subject to the invariant check (Step D asserts consistency via its own final `COUNT(*)` assertion). A separate policy-parent consistency test checks `Policies.CurrentVersionId` against the current `PolicyVersions.LineOfBusiness`.

### 5.8 Static API schema updates

Every create/update request schema for attribute-bearing lifecycle writes adds `lobProductVersionId` and `attributes` on the static draft-07 schema track. Policy create/endorsement request schemas may carry these fields, but persistence targets `PolicyVersion`, not independent policy-parent columns. Those static schemas do **not** carry OpenAPI discriminators; they define the envelope fields and leave product-version-specific validation to the runtime resolver. `build-openapi-lob-block.py` owns the OpenAPI 3.0.3 `oneOf`/`discriminator` projection inside `planning-mds/api/nebula-api.yaml`. In Stage 2 the only generated branch is `UnspecifiedAttributesV0_0_0`; branches are added per LOB in Stages 3–4.

Files updated (skeleton pattern; actual branches come from CI in later stages):
- `submission-create-request.schema.json`, `submission-update-request.schema.json`
- `policy-create-request.schema.json`, `policy-import-request.schema.json`, `policy-from-bind-request.schema.json`, `policy-update-request.schema.json`, `policy-issue-request.schema.json`
- `policy-endorsement-request.schema.json`
- `renewal-create-request.schema.json`, `renewal-update-request.schema.json`

Every response schema for attribute carriers adds `lobProductVersionId`, `lobProductId`, `lobProductVersionStatus`, `lobSchemaVersion`. Policy responses expose these as current-version metadata sourced from `Policy.CurrentVersionId → PolicyVersion`, not from independent policy-parent columns.

`problem-details.schema.json` extended (or sibling `validation-problem-details.schema.json` added) for the structured `errors[]` shape. Stable error-code dictionary in `planning-mds/architecture/error-codes.md` extended.

### 5.9 FE dependency additions

`experience/package.json` (verified current content does **not** include any of the below):

**dependencies** (exact versions; no caret ranges):
- `ajv`
- `ajv-formats`
- `ajv-errors`
- `react-hook-form`
- `json-logic-js`
- `idb`

**devDependencies** (exact versions; no caret ranges):
- `json-schema-to-typescript`
- `@types/json-logic-js`

The Stage 2 dependency PR records the resolved exact versions in the ADR and lockfile. Validator/runtime dependency upgrades happen through a deliberate compatibility PR that reruns the parity harness.

ESLint rule added banning direct schema imports outside `experience/src/lib/dynamic-form/`.

### 5.10 Stage 2 / Phase B acceptance

- [ ] Schema-package meta-schemas exist and are used by CI
- [ ] `_unspecified/0.0.0` sentinel bundle exists and is seeded
- [ ] Per-LOB `_legacy/<lob>/0.0.0` sentinel bundles exist and are seeded
- [ ] Registry tables migrated; null-LOB and legacy sentinels seeded with correct status/flags
- [ ] Registry endpoints returning bundled payloads with ETag
- [ ] Validation pipeline middleware runs (`_unspecified` accepts only `{}`; `_legacy/**` reads render; core-only writes pass with unchanged attributes/version; creates and attribute/version changes are blocked)
- [ ] All 9 CI scripts functional
- [ ] Existing draft-07 static schemas remain green; OpenAPI 3.0.3 LOB projection generator rejects unsupported 2020-12 constructs
- [ ] Every attribute-carrier table (`Submission`, `PolicyVersion`, `PolicyEndorsement`, `Renewal`) has `lob_product_version_id` (FK to the correct `_unspecified` or `_legacy/<lob>` sentinel during backfill) and `attributes_json` columns; `Policy` parent has no independent LOB-attribute source
- [ ] `enforce_lob_consistency()` trigger present on all four attribute-carrier tables, with a separate policy-parent consistency test for `Policies.CurrentVersionId`
- [ ] All existing rows successfully backfilled
- [ ] Static API schemas updated; OpenAPI regenerated; FE types regenerated
- [ ] FE deps landed; `experience` builds green
- [ ] Audit table `lob_schema_validation_audit` partitioned and retained
- [ ] `schema-steward` Casbin role seeded with policies `lob_schema:read:draft`, `lob_schema:read:active`, `lob_schema:read:deprecated`, `lob_schema:read:retired`, `lob_schema:transition:activate`, `lob_schema:transition:deprecate`, `lob_schema:transition:retire`. Verified by an integration test that asserts a non-steward request to `Draft → Active` returns `403`.

---

## 6. Stage 3 — Cyber pilot

Single LOB, end-to-end. Cyber is chosen: smallest attribute footprint, controls-posture maps cleanly to JSON nesting, no exotic widgets required, regulatory surface bounded.

### 6.1 Seed `_shared/` primitives

Six primitives Cyber and downstream LOBs all need:

| Path | Content |
|---|---|
| `_shared/money/1.0.0/schema.json` | Integer minor units + currency code (see §2.3 example) |
| `_shared/iso-currency/1.0.0/schema.json` | Enum (USD only for MVP, extensible) |
| `_shared/tiv/1.0.0/schema.json` | `allOf: [{$ref: money/1.0.0}, {properties: {amountMinor: {maximum: 100_000_000_000}}}]` — cents; 1B USD cap |
| `_shared/percent/1.0.0/schema.json` | Number, minimum 0, maximum 100 |
| `_shared/us-state/1.0.0/schema.json` | Enum of state codes |
| `_shared/effective-date/1.0.0/schema.json` | `type: string, format: date` |

### 6.2 Author `cyber/1.0.0` bundle

`planning-mds/lob-schemas/cyber/1.0.0/`:

- `data.schema.json` — attributes:
  - `revenueBand` (enum: "<10M", "10-50M", "50-100M", "100M-1B", ">1B")
  - `recordsHeld` (integer; magnitude-band presentation is a `ui.schema.json` widget hint, not `oneOf` validation)
  - `controls` (nested object: `mfaEnabled: boolean`, `mfaMaturity: enum`, `edrEnabled: boolean`, `edrMaturity: enum`, `backupEnabled: boolean`, `trainingFrequency: enum`)
  - `priorIncidents` (array of `{date, severity, description}`; `minItems: 0`, `maxItems: 20`)
  - `requestedLimit` — `$ref: /lob-schemas/_shared/tiv/1.0.0/schema.json`
  - `requestedRetention` — `$ref: /lob-schemas/_shared/money/1.0.0/schema.json`
- `ui.schema.json` — section ordering, widget hints (controls grid, magnitude-banded slider)
- `rules.json` — per §2.8 envelope:
  - `records-held-requires-mfa` → `recordsHeld > 1_000_000 ⇒ controls.mfaEnabled == true`
  - `retention-min-1pct-of-limit` → `retention.amountMinor >= limit.amountMinor / 100`
- `projections.json` — per §2.7 (each entry carries explicit `entities`, `stages`, `materialization`):
  - `records-held-count` → `bigint` via `$.recordsHeld`; `entities: ["submissions", "policy_versions"]`; `stages: ["submission", "policy", "endorsement", "renewal"]`; `materialization: "generated-stored"`.
  - `requested-limit-amount-minor` → `bigint` via `$.requestedLimit.amountMinor`; same entities/stages; `materialization: "generated-stored"`.
  - `mfa-enabled` → `boolean` via `$.controls.mfaEnabled`; `entities: ["submissions"]` (only portfolio-filterable on submission intake); `stages: ["submission"]`; `materialization: "generated-stored"`.
- `examples/valid/{1..5}.json`, `examples/invalid/{1..5}.json`, `examples/rule-cases/{passing,failing}/*.json`
- `README.md` — Cyber product narrative

### 6.3 Backend changes

- EF Core migrations: generated columns + btree indexes for the three Cyber projections, applied only to the tables listed in each projection's `entities` field (§2.7) — not indiscriminately to every variant table. For Cyber, typical target is `submissions` + `policy_versions` only (portfolio filters live there).
- EF Core: `HasColumnType("jsonb")` on `attributes_json`; owned-type wrapper exposing `JsonElement`.
- Backfill clarification: **existing Cyber rows remain pinned to `_legacy/cyber/0.0.0`**, not `cyber/1.0.0`. Stage 2 already pointed pre-registry Cyber rows at `_legacy/cyber/0.0.0` (§5.7 Step D). They stay there because their `attributes_json` is `{}` and `cyber/1.0.0` requires fields (`recordsHeld`, `controls`, `requestedLimit`, etc.) that `{}` cannot satisfy.
  - Legacy-pinned Cyber rows render via the Deprecated-bundle read path (§2.6) with the legacy sentinel's pass-through schema and a read-only UI.
  - Core-only writes against legacy-pinned Cyber rows pass when attributes and pinned version are unchanged; creates, attribute changes, and ad hoc version switches are blocked by middleware (§2.6).
  - Endorsements against legacy-pinned Cyber rows route through the migration-on-endorsement path (§2.12 case 2), migrating `attributes_json` forward to `cyber/1.0.0` via the field-initialized fallback (legacy sentinels have no `migrations/` folder) — the endorsement capture UI surfaces the `cyber/1.0.0` required fields as mandatory input.
  - Only new Cyber submissions write directly against `cyber/1.0.0`.
- `LobSchemaResolver` activates `cyber/1.0.0` on startup; `_legacy/cyber/0.0.0` is already loaded (seeded at Stage 2 in Deprecated status, `is_legacy_sentinel = true`).

### 6.4 Frontend changes

Under `experience/src/lib/dynamic-form/`:

| File | Responsibility |
|---|---|
| `schema-walker.ts` | Depth-first walk, conditional resolution via `if/then/else`; fails fast if forbidden composition keywords (`oneOf`, `anyOf`, `not`) appear in bundle schemas |
| `widget-registry.ts` | Registry API; default widget set: `string`, `number`, `integer`, `boolean`, `enum→Select`, `date→DatePicker`, `nested object→Card`, `array of objects→DataTable+Drawer` — all shadcn/ui |
| `ajv-cache.ts` | Compiled validator pool keyed by `(productVersionId, stage)`; LRU 200; populated at bootstrap |
| `rhf-adapter.ts` | Bridges schema walk to react-hook-form field state |
| `json-logic-evaluator.ts` | `rules.json` evaluation; pointer-mapped errors |
| `useDynamicForm.ts` | Main hook; snapshots `productVersionId` for session lifetime |
| `error-normalizer.ts` | Maps server + client errors to the normalized envelope (§2.3) |
| `bootstrap-loader.ts` | After auth → `GET /lob-schemas/active?stage=*` per stage → cache in IndexedDB keyed by `(productVersionId, stage, etag)` → precompile AJV |
| `lazy-fetcher.ts` | Fetch-with-ETag for cache misses |

Cyber-specific custom widgets:
- `<ControlsPostureGrid>` — 4×N grid (control × maturity) backed by the nested object
- `<MagnitudeBandedSlider>` — for records-held banded ranges

Integration: `<DynamicAttributePanel productVersionId stage />` placed into Cyber submission/policy/endorsement/renewal screens.

### 6.5 Parity testing

- `examples/valid/*` accepted by FE AJV + BE Json.Schema.Net
- `examples/invalid/*` rejected by both with identical `(code, pointer)` multisets
- `examples/rule-cases/{passing,failing}/*` agreed by both JsonLogic engines
- Backwards-compat: write under `cyber/1.0.0`, bump to `cyber/1.1.0` (additive field), record still validates

### 6.6 E2E (Playwright)

- Create Cyber submission → fill dynamic panel → submit → verify persisted JSON + `lob_product_version_id`
- Trigger cross-field rule violation → error at correct field
- Light/dark theme smoke on dynamic panel
- Deprecated-version flow: server marks `cyber/1.0.0` deprecated → FE loads for existing submission → `Warning` header nudge visible
- Pin-during-edit: open form → server activates `cyber/1.1.0` mid-edit → existing form still uses `1.0.0`; new form picks up `1.1.0`

### 6.7 Performance gates

See §10. Stage 3 ship requires these met.

### 6.8 Stage 3 acceptance

- [ ] Six `_shared/` primitives land and pass the profile linter
- [ ] `cyber/1.0.0` bundle complete, signed, activated
- [ ] Existing Cyber rows remain `_legacy/cyber/0.0.0`-pinned or are explicitly migrated through §2.12; invariant trigger passes
- [ ] Generated columns + indexes in place; EXPLAIN tests green
- [ ] Dynamic form engine functional for Cyber submission, policy, endorsement, renewal
- [ ] Conformance harness green (FE/BE schema + rule parity)
- [ ] Backwards-compat test green
- [ ] Playwright suite green (including deprecation and pin-during-edit flows)
- [ ] Performance acceptance gates met (§10)
- [ ] OpenAPI regenerated with `CyberAttributesV1_0_0` branch; FE types regenerated

---

## 7. Stage 4 — Roll-forward to remaining LOBs

Order (ascending attribute complexity; shakes engine out on small first):

**Surety → Marine → ProfessionalLiability → Umbrella → DirectorsOfficers → CommercialAuto → WorkersCompensation → GeneralLiability → Property**

Per LOB:

1. Author `<lob>/1.0.0/` bundle (data, ui, rules, projections, examples, rule-cases, README).
2. Expand `_shared/` if a new universal primitive is needed (`vehicle-vin`, `naics-code`, `cope-code`, `class-code-nces`, etc.) — steward review with additive-vs-breaking gate.
3. Activation/backfill rule:
   - New rows for that LOB write directly to `<lob>/1.0.0`.
   - Existing non-null-LOB rows remain pinned to `_legacy/<lob>/0.0.0` until an endorsement or approved mass-migration captures the required attributes and moves them through §2.12.
   - Existing null-LOB Submission/Renewal rows transition from `_unspecified/0.0.0` to `<lob>/1.0.0` only in the same write that sets `lineOfBusiness` and supplies valid required attributes.
4. Register custom widgets where needed:
   - **CommercialAuto**: vehicle schedule grid, VIN decoder integration
   - **WorkersCompensation**: class-code matrix, payroll-by-state grid
   - **Property**: COPE accordion, schedule-of-locations table
   - **DirectorsOfficers**: tower visualizer
5. Parity tests + E2E coverage (replicate Cyber pattern).
6. OpenAPI + FE types regenerated.

**Parallelism.** After CommercialAuto and Property prove out the heaviest custom-widget patterns, remaining LOBs run in parallel feature teams. Surety, Marine, ProfessionalLiability, Umbrella, DirectorsOfficers are mostly schema-plus-existing-widgets and don't block each other.

**Stage 4 acceptance** (per LOB):
- [ ] Bundle activated; invariant trigger passes for all backfilled rows
- [ ] Conformance harness green
- [ ] E2E green (submission → policy → endorsement → renewal)
- [ ] Performance gates met (§10)
- [ ] Custom widgets themed; light/dark smoke coverage green

**Stage 4 completion:** old `lineOfBusiness` enum preserved for read-side display + fast filtering, but the source of variability is now the registry. Adding a new LOB attribute, raising a cap, introducing a conditional section = schema bump. No platform code change.

---

## 8. Stage 5 — Operational guards and governance

Hardens operational discipline so the architecture stays clean.

### 8.1 Governance artifacts

| Path | Purpose |
|---|---|
| `.github/PULL_REQUEST_TEMPLATE/lob-schema.md` | Mandatory checklist: semver bump declared; additive-vs-breaking; examples updated; projections reviewed; deprecation/retirement plan if applicable; steward sign-off |
| `planning-mds/operations/lob-schema-lifecycle.md` | Runbook: activation, deprecation, retirement, emergency rollback, mass-migration of retired-version rows. **Includes the bundle-integrity verification flow:** (1) `LobSchemaResolver` startup HMAC-SHA256 verification of every loaded `lob_schema_bundle` row against `LOB_SCHEMA_SIGNING_KEY`; (2) post-deploy CI smoke test that recomputes the HMAC of every active filesystem source under `planning-mds/lob-schemas/` and compares to the `lob_schema_bundle.signature` column for the matching `(productVersionId, stage)`; (3) drift response (which row, which signature, rollback or re-sign procedure); (4) `LOB_SCHEMA_SIGNING_KEY` rotation procedure (re-sign all bundles, atomic swap, retire old key after grace period). |
| `planning-mds/operations/shared-primitive-governance.md` | Rules for adding/versioning `_shared/` primitives; tighten-via-allOf-in-LOB / never-loosen-in-LOB rule |
| `planning-mds/operations/projection-change-runbook.md` | Production migration pattern for STORED generated columns on large tables |

### 8.2 Dashboards

- Submissions/policies bound to **deprecated** versions per LOB
- Oldest-active-deprecated-version-age (KPI for steward team)
- Schema activation/deprecation/retirement event log
- Validation rejection rate per product version (catches bad activations)

### 8.3 Optional V2 enhancements (post-MVP)

- SSE channel `/lob-schemas/events` publishing `activated`/`deprecated`/`retired` events for live cache invalidation in long-lived sessions
- Steward admin UI for activate/deprecate/retire actions (currently PR + deploy)
- Schema migration tooling for the rare case where a deprecated-version row needs in-place upgrade
- Per-tenant schema overrides (`_shared/` primitive overrides for one tenant's regulatory context)

### 8.4 Stage 5 acceptance

- [ ] Governance docs published and referenced from schema-steward SKILL
- [ ] PR template in use on at least one bundle change
- [ ] Dashboards operational; deprecation-age KPI visible
- [ ] Audit table queryable; sample report produced
- [ ] `LobSchemaResolver` startup HMAC verification active in production; one fail-fast drill completed (intentionally corrupt a non-prod row, confirm startup blocks loudly with the right log shape)
- [ ] Post-deploy filesystem ↔ DB HMAC reconciliation smoke test wired into the deploy pipeline; one drill completed (tamper with a non-prod DB row, confirm the smoke test catches it)
- [ ] `LOB_SCHEMA_SIGNING_KEY` rotation procedure dry-run completed in non-prod

---

## 9. Sequencing and dependencies

```
Stage 0 (ADRs / decision lock — incl. ADR-018 promotion)
      │
      ├──► Phase A — Stage 1 (framework foundation — nebula-agents)        ──┐
      │       framework cadence; tagged release pinned by Phase B            │
      │                                                                       │
      └──► Phase B — Stage 2 (solution foundation — nebula-insurance-crm) ──┤
              CRM cadence; pins a tagged Phase A framework version            │
                                                                              ▼
                                                                  Stage 3 (Cyber pilot)
                                                                              │
                                                                              ▼
                                                            Stage 4 (roll-forward to 9 LOBs)
                                                                              │
                                                                              ▼
                                                            Stage 5 (governance hardening)
```

- **Stage 0** gates everything. ADRs land first; ADR-018 promotion is a §3.0 prerequisite.
- **Phase A and Phase B run on independent cadences.** Phase A ships to `nebula-agents` on the framework release cadence; Phase B ships to `nebula-insurance-crm` on the CRM cadence and pins a specific tagged framework version. They may proceed in parallel; Phase B does not block Phase A merging or releasing.
- **Stage 3** depends on both — framework references guide the design; solution rails provide runtime. Stage 3 work begins only after Phase B has consumed a Phase A version that contains the Stage 1 framework artifacts (§4.1–4.4).
- **Stage 4** depends on Stage 3 — pilot proves pattern before retrofit.
- **Stage 5** runs alongside late Stage 4 as more LOBs onboard.

---

## 10. Performance acceptance criteria

All apply to Stage 3 ship and must hold through Stage 4.

| Metric | Target | Measurement |
|---|---|---|
| Bootstrap payload size (all active stages, **per tenant** — filtered to tenant-available LOBs) | ≤ 500 KB gzipped | CI check on `GET /lob-schemas/active` response for representative tenant profiles (small / medium / all-LOB) |
| Schema validation latency (FE, warm) | p95 ≤ 5 ms | Vitest benchmark on realistic Cyber payloads |
| Schema validation latency (BE, warm) | p95 ≤ 10 ms | xUnit benchmark |
| Rule evaluation latency | p95 ≤ 5 ms FE / ≤ 10 ms BE | Same harnesses |
| Dynamic form initial render (Cyber submission) | ≤ 300 ms on mid-tier laptop | Playwright perf trace |
| Projection-backed portfolio query | p95 ≤ 50 ms at 100K rows | pgbench test on representative data |
| Bundle compile time (CI, all bundles) | ≤ 30 s | CI timing |
| Schema resolver startup cost | ≤ 2 s for 50 active bundles | .NET startup telemetry |

Regression beyond these gates blocks merge.

**Enforcement.** Budgets are observable in production via the OTel span contract in §5.4 (`lob.resolver.resolve`, `lob.middleware.lob_consistency`, `lob.validation.schema`, `lob.validation.rules`). Dashboards plot p95 latency against these budgets per `(productVersionId, stage)`; alert thresholds fire when p95 crosses the budget for two consecutive 5-minute windows. CI benchmarks (Vitest + xUnit) gate merges; production telemetry catches drift that benchmarks miss.

---

## 11. Operational limits (summary)

From §2.9 consolidated:

| Limit | Value |
|---|---|
| Bundle size (compressed) | ≤ 256 KB per `(productVersionId, stage)` |
| Attributes payload per write | ≤ 64 KB |
| Schema complexity score | ≤ 200 |
| Rule depth | ≤ 8 |
| Schema validation timeout | 50 ms/req |
| Rule evaluation timeout | 20 ms/req |
| AJV compiled validator LRU | 200 entries |
| IndexedDB purge threshold | > 7 days unused OR > 50 MB total |
| Offline write retries | 3 exponential backoff; `SchemaVersionRetired` surfaced on replay |
| Audit retention | 7 years; append-only; monthly partitions |

---

## 12. Risk register

| Risk | Mitigation |
|---|---|
| FE/BE validation drift | Restricted profile + normalized envelope + CI parity harness; pinned library versions; no custom AJV keywords |
| Stale FE caches missing new schemas | Bootstrap every active version on session start; lazy-ETag for misses; deprecation `Warning` nudges UI |
| `_shared/` change rippling unexpectedly | Versioned primitives never floating; LOB schemas pin versions; loosening in LOB relative to `_shared/` forbidden |
| JSONB query performance | Indexed projections mandatory for filterable paths; un-projected filtering flagged at PR; GIN opt-in |
| Loss of compile-time safety on LOB attributes | Build-time TS codegen for known versions; BE services can codegen private C# types from bundles |
| Schema steward becomes bottleneck | Role is a capability, not a person — multiple architects can hold it; PR template + automated gates handle routine cases |
| Engineers slipping back into adding columns | Validation taxonomy embedded in 3 framework artifacts; architect SKILL self-validation gate; PM classifies upfront |
| Rules engine becoming a second untyped platform | `rules.schema.json` meta-schema; stable rule codes; deterministic context; custom-op registry with FE/BE parity tests |
| Bundle tampering in transit | HMAC-SHA256 signature verified on load; startup failure on mismatch |
| Expensive schemas DoS'ing validation | Complexity score at activation; per-request timeouts; size limits |
| Money drift between engines | Integer minor units + currency code; `multipleOf` on non-integers forbidden |
| PolicyVersion snapshot confusion | Master reference explicitly separates snapshots (platform) from authoritative `PolicyVersion.attributes_json` (LOB) and states that Policy parent has no independent attribute source |

---

## 13. Decision log (all 13 decisions pinned by Stage 0 ADRs)

| # | Decision | Pinned |
|---|---|---|
| 1 | Form engine | Custom RHF + AJV + shadcn widget registry (over RJSF/JSONForms/Formily) |
| 2 | Pilot LOB | Cyber |
| 3 | Schema source-of-truth | Filesystem canonical (`planning-mds/lob-schemas/`); DB operational cache |
| 4 | Cross-field rules | JsonLogic (`json-logic-js` FE + `JsonLogic.Net` BE) with full governance (§2.8) |
| 5 | JSON Schema draft | LOB bundle source uses JSON Schema 2020-12 restricted profile (§2.3); existing `planning-mds/schemas/**` static contracts remain draft-07 until a separate OpenAPI 3.1/static-schema migration ADR |
| 6 | BE schema validator | Dynamic LOB bundles validate with `Json.Schema.Net`; existing static request validation remains FluentValidation until a separate static-schema runtime-validation ADR changes it |
| 7 | `_shared/` primitives | Versioned, immutable per version, `$ref`, served bundled, steward-owned |
| 8 | Schema delivery | Hybrid: build-time TS codegen + bootstrap + IndexedDB + lazy-with-ETag |
| 9 | Backfill strategy | `_unspecified/0.0.0` only for null-LOB Submission/Renewal rows; non-null existing attribute-carrier rows pin to `_legacy/<lob>/0.0.0` until endorsement/migration captures real attributes |
| 10 | First artifacts written | Stage 0 ADRs, then Stages 1 and 2 in parallel |
| 11 | Money representation | Integer minor units + ISO currency code; JSON-number `multipleOf` on money forbidden |
| 12 | Validator parity mechanism | Restricted schema profile + normalized error envelope (code + pointer parity, messages NOT compared); `oneOf`/`discriminator` forbidden *inside* bundle schemas — it is an OpenAPI codegen annotation at the envelope level only, with resolver-then-validate dispatch at runtime |
| 13 | PolicyVersion existing snapshots | Supplement, not replace; authoritative `PolicyVersion.attributes_json` is additive alongside `Profile/Coverage/PremiumSnapshotJson`; Policy parent reads through `CurrentVersionId` |

---

## 14. What changes in day-to-day after this lands

### Agent-driven sessions

- **PM agent** writing a feature for a variant entity → automatically classifies attributes as core vs LOB; identifies `_shared/` primitives; declares schema bump.
- **Architect agent** reviewing → consults validation taxonomy; requires ADR for any new `_shared/` primitive; validates bundle conforms to `_meta/schema-package.schema.json`.
- **Backend dev agent** implementing → never touches columns for LOB attributes; follows `LobSchemaResolver → lob-consistency → schema → rules → domain` pipeline; migrations only add JSONB + projections.
- **Frontend dev agent** implementing → drops `<DynamicAttributePanel productVersionId stage />`; registers custom widgets with the registry; knows to pin-on-open.
- **QE agent** writing tests → fixture-driven from `examples/{valid,invalid}/*` and `examples/rule-cases/*`; parity assertion automatic; backwards-compat on minor bumps.

### Human work

- **Adding a Cyber attribute:** open a PR with a schema bump + examples + projections if filterable. Steward reviews. Deploy. No platform code change.
- **Activating a new LOB version:** PR + steward sign-off + deploy. No FE deploy required to make it visible, *provided the new version stays within the existing widget vocabulary*. If the new version introduces a custom widget (vehicle schedule, COPE accordion, D&O tower visualizer, etc.), the widget ships in a paired FE deploy before activation.
- **Raising a TIV cap (loosening):** minor bump on `_shared/tiv`. LOB schemas that want the new ceiling publish a new minor pinning the new shared version. Old policies stay on their pinned versions; new ones take effect.
- **Lowering a TIV cap (tightening):** major bump on `_shared/tiv`. LOB schemas opt in by publishing a new major. Old policies remain valid against pinned version forever.
- **Introducing a cross-field rule:** add to `rules.json` with a new rule id, code, pointer. Add a rule-case fixture. Deploy.

---

## 15. Appendix: example schema bundle layout

```
planning-mds/lob-schemas/
├── _meta/
│   ├── schema-package.schema.json
│   ├── rules.schema.json
│   └── projections.schema.json
├── _shared/
│   ├── README.md
│   ├── money/
│   │   └── 1.0.0/schema.json
│   ├── iso-currency/
│   │   └── 1.0.0/schema.json
│   ├── tiv/
│   │   └── 1.0.0/schema.json    # allOf: [money/1.0.0, {amountMinor: {maximum: 100_000_000_000}}]
│   ├── percent/
│   │   └── 1.0.0/schema.json
│   ├── us-state/
│   │   └── 1.0.0/schema.json
│   ├── effective-date/
│   │   └── 1.0.0/schema.json
│   └── rules/
│       └── operations/
│           └── between/
│               └── 1.0.0/
│                   ├── spec.md
│                   ├── fe.ts
│                   ├── be.cs
│                   └── fixtures/cases.json
├── _unspecified/
│   └── 0.0.0/
│       ├── data.schema.json     # {"type": "object", "maxProperties": 0, "additionalProperties": false}
│       ├── ui.schema.json       # {}
│       ├── rules.json           # []
│       ├── projections.json     # []
│       ├── examples/valid/empty.json
│       └── README.md
├── _legacy/
│   ├── cyber/
│   │   └── 0.0.0/
│   │       ├── data.schema.json # {"type": "object", "additionalProperties": true}
│   │       └── ...
│   └── ...
├── cyber/
│   └── 1.0.0/
│       ├── data.schema.json
│       ├── ui.schema.json
│       ├── rules.json
│       ├── projections.json
│       ├── examples/
│       │   ├── valid/{1..5}.json
│       │   ├── invalid/{1..5}.json
│       │   └── rule-cases/
│       │       ├── passing/*.json
│       │       └── failing/*.json
│       └── README.md
├── property/
│   └── 1.0.0/
│       └── ...
└── compiled/                    # CI output — gitignored
    ├── _unspecified/0.0.0/
    │   └── submission.bundle.json
    ├── cyber/1.0.0/
    │   ├── submission.bundle.json
    │   ├── policy.bundle.json
    │   ├── endorsement.bundle.json
    │   └── renewal.bundle.json
    └── ...
```

---

## 16. Next action

Confirm the 13 decisions (§13), promote ADR-018 to `Accepted` per §3.0, and I proceed to **Stage 0** — drafting the four ADRs in `nebula-insurance-crm/planning-mds/architecture/decisions/`. Once they land and are signed off, **Phase A** (framework, in `nebula-agents`) and **Phase B** (CRM rails, in `nebula-insurance-crm`) proceed in parallel on their own release cadences.

Redirects on any decision are welcome — §13 is where the plan's load-bearing choices live; everything else is mechanical.
