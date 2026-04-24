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
| **Sentinel product** | `_unspecified/0.0.0` — a generic pass-through product used for backfill of existing rows and for lifecycle stages where LOB is null (e.g., early Submission). |

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

Adding a new attribute, raising a TIV cap, adding a new enum value, introducing a new conditional section becomes: bump the schema bundle, update examples, ship. No platform code change. No FE deploy required to activate new versions server-side. No drift between FE and BE validation because the schema is the contract and a CI conformance harness proves equivalence.

### 1.3 Scope boundaries

**In scope (variant-typed lifecycle entities):** `Submission`, `Policy`, `PolicyVersion`, `PolicyEndorsement`, `Renewal`.

**Out of scope (stay strictly columned):** `Account`, `Contact`, `Broker`, `Carrier`, `UserProfile`, `Task`, `Activity`. Don't generalize JSONB to these — that path leads to a document-store-in-Postgres anti-pattern.

The architecture earns its complexity only on entities whose shape varies by product.

---

## 2. Core architectural principles

These principles are the "why" every framework artifact and every future agent-driven session must internalize. They are embedded verbatim into `agents/architect/references/extensible-attribute-architecture.md` so agents designing or reviewing variant-typed entities see them before making choices.

### 2.1 Core / extension split

Every variant-typed lifecycle entity gets:

- **Core columns** — what the platform operates on regardless of LOB: identifiers, FKs, lifecycle stamps, audit, money totals, denormalized display fields, `rowVersion`.
- **`attributes_json` (JSONB, NOT NULL DEFAULT '{}')** — LOB-specific attributes. Platform code never reaches in; LOB-aware services may.
- **`lob_product_version_id` (UUID, NOT NULL, FK → `lob_product_version.id`)** — immutable per row. The schema version used to validate this row's `attributes_json`.

`lineOfBusiness` (existing column) is operational — not cosmetic. See §2.11 for the consistency invariant that ties it to `lob_product_version_id`.

### 2.2 Validation taxonomy

Most validation logic in an insurance CRM is "this number is in this range," "this string matches this enum," "this object has these required fields." JSON Schema was built for that; AJV and Json.Schema.Net handle it identically across layers. Only the truly contextual or stateful rules go elsewhere.

| Validation kind | Belongs in | Examples |
|---|---|---|
| Type / structure | JSON Schema | `type: number`, required, `additionalProperties: false` |
| Bounded primitives | JSON Schema | `minimum: 100`, `maximum: 1_000_000_000`, `multipleOf: 0.01` |
| Enums / consts | JSON Schema | propertyType, constructionType, US states, currency codes |
| String shape | JSON Schema | `pattern`, `format: email\|uri\|date\|uuid`, `minLength`/`maxLength` |
| Array shape | JSON Schema | `minItems`, `uniqueItems`, `contains`, per-position `prefixItems` |
| Local conditionals | JSON Schema | `if/then/else`, `dependentRequired`, `dependentSchemas`, `oneOf` |
| Cross-field business rules | JsonLogic (`rules.json`) | "personalPropertyAmount ≤ 70% of dwellingAmount", "deductible ≤ 5% of TIV" |
| External-context rules | JsonLogic + custom ops, or domain code | "premium ≤ broker credit limit", "effectiveDate ≥ today" |
| Role / state-aware rules | Domain code (Casbin + service) | "broker can't set commission > 15%", "only UW can override TIV cap" |
| Workflow guards | Domain code (state machine) | "can't bind if completeness < 100%" |
| Storage invariants | Database (FK, unique, check) | rowVersion, FK integrity, unique policy number |

**This table is embedded in three places in the framework:** the master reference, the `entity-model-template.md`, and the architect SKILL self-validation. An agent designing a new attribute always asks "which row of this taxonomy does this validation belong to?" — and the answer determines where the rule lives.

### 2.3 Validator equivalence — restricted profile + normalized envelope

JSON Schema (draft 2020-12) is the portable contract. AJV (browser) and Json.Schema.Net (.NET) are independent implementations that pass the JSON Schema Test Suite — they do **not** share code. Parity is engineered, not assumed.

**Pinned tooling:**
- JSON Schema draft **2020-12** declared in every bundle's `$schema`
- FE: `ajv@^8` + `ajv-formats@^3` + `ajv-errors@^3`, options `{ strict: true, allErrors: true, useDefaults: false, allowUnionTypes: false }`
- BE: `JsonSchema.Net` latest for draft 2020-12, options `{ OutputFormat = List, RequireFormatValidation = true }`
- Both forced into format-validation mode (both treat `format` as annotation-only by default)

**Restricted schema profile.** Bundles must pass a profile linter at activation time.

*Allowed keywords:*
`type`, `required`, `properties`, `additionalProperties: false`, `$ref`, `$defs`, `$id`, `$schema`, `enum`, `const`, `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `minLength`, `maxLength`, `pattern`, `minItems`, `maxItems`, `uniqueItems`, `items` (single subschema), `prefixItems`, `minProperties`, `maxProperties`, `propertyNames`, `dependentRequired`, `format` from whitelist (`email`, `uri`, `date`, `date-time`, `uuid`, `ipv4`), `allOf` (constraint stacking), `oneOf` **only with explicit `discriminator`**, `if/then/else` (depth ≤ 2).

*Forbidden keywords:*
`multipleOf` on non-integers (see money rule below), `patternProperties`, `contains`/`minContains`/`maxContains`, `dependentSchemas`, `not`, `contentSchema`/`contentMediaType`/`contentEncoding`, `anyOf`, custom keywords, remote `$ref`, `format` outside the whitelist.

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

1. **Build-time TS codegen** — CI emits `components/schemas` for every active bundle into `planning-mds/api/nebula-api.yaml` using `oneOf` + `discriminator` keyed by `lobProductVersionId`. `json-schema-to-typescript` regenerates `experience/src/generated/lob-types.ts`. Gives full TS safety for versions known at build time.
2. **Bootstrap on app start** — after auth, one `GET /api/lob-schemas/active?stage=...` per lifecycle stage pulls every active bundle for the tenant. AJV instances pre-compiled and cached per `(productVersionId, stage)`.
3. **IndexedDB persistence** — bootstrap payload persisted keyed by `(productVersionId, stage, etag)`. Survives offline. Next session load first, revalidate in background.
4. **Lazy with ETag** — for cache misses (e.g., user opens a deprecated-version submission that wasn't in the active set), `GET /api/lob-schemas/{productVersionId}/{stage}` with ETag cache headers. 304 most of the time.

**Pin to initial version for the duration of an editing session.** Open a form → snapshot the `productVersionId` → use that version for the entire edit. Never auto-swap a schema under a user mid-edit. New version picked up by next form open.

**Decouples product release from FE release.** UW activates `cyber/1.1.0` on Tuesday → every session opened after that picks it up via bootstrap → no FE ship required. FE is only redeployed when new TS type safety is needed for a known version.

### 2.6 Lifecycle and request-carries-version contract

Schema versions move through: `Draft → Active → Deprecated → Retired`.

- Every write request carries `lobProductVersionId` explicitly. Backend validates against **that exact version**, never "latest."
- `Active`: normal operation.
- `Deprecated`: writes accepted, response carries `Warning: 299 - "Schema version <X> is deprecated; current is <Y>"` header. UI surfaces a non-blocking nudge.
- `Retired`: writes get `409 SchemaVersionRetired` ProblemDetails with the upgrade path. Reads still work.
- Historical rows read correctly forever — they validate against the version pinned on the row.
- Retirement is coordinated and rare — only when a regulator or strict policy demands it.

Status transitions require `schema-steward` role (Casbin policy). Every transition emits an audit event: `{actor, fromState, toState, productVersionId, timestamp, reason}`.

### 2.7 Indexed projections

Reporting and portfolio filters never scan JSONB raw. The bundle's `projections.json` declares paths to be promoted to PostgreSQL generated columns + indexes.

**`projections.json` shape:**
```json
[
  {
    "id": "records-held-count",
    "path": "$.recordsHeld",
    "sqlType": "bigint",
    "nullBehavior": "null-on-missing",
    "default": null,
    "indexType": "btree",
    "indexName": "ix_submissions_cyber_records_held",
    "queryHint": "portfolio filter: WHERE records_held_count > N"
  }
]
```

**Rules:**
- Generated columns are STORED: `GENERATED ALWAYS AS ((attributes_json #>> '{path}')::type) STORED`. Populated at write; no read-time cost.
- Naming: column `<lob>_<concept_snake_case>`, index `ix_<table>_<lob>_<concept>`. 63-char Postgres cap enforced at activation.
- Explicit cast per projection; ambiguous casts rejected at activation.
- `nullBehavior` required: `null-on-missing` or `default-on-missing` (with an explicit `default`).
- Production migrations always use `CREATE INDEX CONCURRENTLY` and `IF NOT EXISTS`.
- For large tables, add-column-NULL → backfill-in-batches → set-NOT-NULL pattern (no single ALTER table rewrite).
- Every projection migration ships a paired `down.sql`.
- Per projection, an EXPLAIN test asserts the index is used for the documented query pattern; PR fails if the planner skips the index.
- **GIN is opt-in, not default.** Default is no GIN on `attributes_json`. GIN added only when explicit free-form key lookup is required (rare). For typed-path queries, per-path generated-column + btree is the mechanism. Filtering on un-projected paths is a code smell flagged at review.

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
- FE: `json-logic-js@^2`
- BE: `JsonLogic.Net`
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

Casbin policies: `lob_schema:read:{active,draft}`, `lob_schema:transition:{activate,deprecate,retire}`.

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

**Decision:** `attributes_json` **supplements** the existing snapshots; it does not replace or derive from them.

| Column | Carries | Source | Mutability | Validated against |
|---|---|---|---|---|
| `ProfileSnapshotJson` | Account/broker/carrier/producer at version-time | Frozen from Policy/Account state at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `CoverageSnapshotJson` | Full coverage set at version-time | Frozen from PolicyCoverageLine at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `PremiumSnapshotJson` | Premium breakdown at version-time | Frozen from premium calc at write | Immutable per row | Static `planning-mds/schemas/` (existing) |
| `attributes_json` *(new)* | LOB-specific attributes at version-time | Frozen from Submission/Endorsement input at write | Immutable per row | Dynamic registry schema (this plan) |
| `lob_product_version_id` *(new)* | The LOB schema version used to validate `attributes_json` | Set from request | Immutable per row | — |

Snapshot columns are not migrated. They remain platform internals, hand-shaped C# DTOs serialized as JSON-as-text. They are not subject to `_shared/` versioning.

### 2.11 `lineOfBusiness` invariant

`lineOfBusiness` is load-bearing: dashboards, SLA thresholds, role-based filters, regulatory reporting, API contracts, renewal-window rules all consume it. It must stay consistent with the registry.

**Invariant — enforced at three layers:**

```
For every row R in {Submission, Policy, PolicyVersion, PolicyEndorsement, Renewal}:
  R.lineOfBusiness == lookup(R.lob_product_version_id).product.line_of_business
  OR (R.lineOfBusiness IS NULL AND R.lob_product_version_id == _unspecified/0.0.0)
```

**Layer 1 — Database.** Trigger `enforce_lob_consistency()` on each entity table runs on INSERT/UPDATE. Looks up the product version's product; asserts `line_of_business` matches. Raises `LOB_PRODUCT_MISMATCH` on violation. Cheap — one indexed lookup per write.

**Layer 2 — Service.** `LobSchemaResolver.Resolve(productVersionId)` returns `product.lineOfBusiness` alongside the bundle. Middleware asserts match BEFORE schema validation; emits `LOB_PRODUCT_MISMATCH` ProblemDetails on violation.

**Layer 3 — Test.** Conformance harness: every example fixture's `lineOfBusiness` matches its bundle's product LOB. Migration test: every backfilled row satisfies the invariant.

**Null handling:**
- `Submission` and `Renewal` may have `lineOfBusiness IS NULL` early in the lifecycle → `lob_product_version_id` MUST point at `_unspecified/0.0.0`.
- `Policy`, `PolicyVersion`, `PolicyEndorsement` must have non-null `lineOfBusiness` (already required in current schemas).
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
- Migrations are **NOT** automatically applied to existing rows. Historical rows stay pinned. Migrations exist for two cases:
  1. **Endorsement against a stale version** — when an endorsement creates a new PolicyVersion, the steward may opt to migrate the prior `attributes_json` forward to the latest active version before applying the endorsement delta. Configurable per LOB.
  2. **Mass migration on retirement** — when a version is being retired and a few rows still exist, an admin job applies the upgrade path.
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

---

## 3. Stage 0 — Decision lock (ADRs)

**Owner:** architect agent + schema-steward sub-skill
**Approver:** human reviewer (you) in PR
**Gates:** all downstream stages

### 3.1 Deliverables

Four binding ADRs in `nebula-insurance-crm/planning-mds/architecture/decisions/`:

| ADR | Pins |
|---|---|
| `ADR-NNN-lob-extensible-attribute-architecture.md` | Core/extension split; which entities qualify; `_shared/` layout; `lob_product_version_id` immutability; `attributes_json` JSONB; indexed-projection rule; PolicyVersion snapshot interaction (§2.10); `lineOfBusiness` invariant (§2.11); migration path (§2.12); security & tenancy (§2.9); operational limits (§2.9); sentinel `_unspecified/0.0.0` backfill |
| `ADR-NNN-form-engine-rhf-ajv-shadcn-registry.md` | Custom RHF + AJV + shadcn widget registry over RJSF/JSONForms/Formily; pin-to-initial-version-during-edit; widget registry contract; theme rules for dynamic widgets |
| `ADR-NNN-validator-equivalence-restricted-profile.md` | Draft 2020-12; AJV 8 + Json.Schema.Net pinned; restricted schema profile (§2.3 keyword allow/deny lists); normalized error envelope + stable error-code dictionary; money as integer minor units; parity-as-CI-contract |
| `ADR-NNN-rules-governance-jsonlogic.md` | `rules.schema.json` meta-schema; rule envelope (id/code/pointer/severity/expression); deterministic context shape; custom-op registry under `_shared/rules/operations/`; FE/BE op and rule parity tests |

### 3.2 Acceptance criteria (Stage 0 → Stage 1/2 unlock)

- [ ] All 4 ADRs in `Status: Accepted`
- [ ] Each ADR cross-links the others where decisions interlock
- [ ] Each ADR has a "Consequences" section (what becomes invalid — e.g., existing draft-07 schemas must migrate to 2020-12 within Stage 2)
- [ ] Each ADR names the framework reference docs it will produce in Stage 1
- [ ] Knowledge graph: each ADR added as a canonical node; `scripts/kg/validate.py` exits 0
- [ ] All 13 decisions (§13) captured in the ADRs with reasoning

Stage 0 is typically one or two architect-sessions of focused authoring.

---

## 4. Stage 1 — Framework foundation (`nebula-agents/agents/**`)

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
  - **Static** (existing): request/response contracts for non-variant entities (Account, Broker, Contact, Task, Activity). Hand-authored. Lives in `planning-mds/schemas/`. Validated by AJV (FE) + Json.Schema.Net (BE) on the request boundary.
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

#### 4.2.3 `agents/backend-developer/SKILL.md`

- Tech stack additions: **`Json.Schema.Net`** (schema validation), **`JsonLogic.Net`** (rules evaluation). Retain `NJsonSchema` only if C# class codegen from schemas is needed elsewhere; for validation, `Json.Schema.Net` is the default.
- New service pattern: **`LobSchemaResolver`** — startup-loads bundles from filesystem, verifies HMAC signatures, caches by `(productVersionId, stage)`, exposes `Resolve()` and `canMigrate()`.
- New middleware pattern: **validation pipeline order** is `lob-consistency → schema → rules → domain → persist → audit`. Middleware is request-scoped; errors short-circuit with normalized envelope (§2.3 of master reference).
- EF Core conventions for variant entities: `attributes_json` mapped via `HasColumnType("jsonb")` with owned-type wrapper exposing `JsonElement`; `lob_product_version_id` as required immutable FK.
- Migration conventions: **STORED generated columns** for projections (`GENERATED ALWAYS AS ((attributes_json #>> '{path}')::type) STORED`); always `CREATE INDEX CONCURRENTLY IF NOT EXISTS`; for large tables use add-NULL → backfill-in-batches → set-NOT-NULL; every migration ships a paired `down.sql`.
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
- New FE dependencies to be added in Stage 2 (§5.9): `ajv@^8`, `ajv-formats@^3`, `ajv-errors@^3`, `react-hook-form@^7`, `json-logic-js@^2`, `idb@^8`, plus dev `json-schema-to-typescript@^15` and `@types/json-logic-js`.

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
  8. OpenAPI regeneration — rebuild `components/schemas` discriminator block in `planning-mds/api/nebula-api.yaml`
  9. FE TS type regeneration — `experience/src/generated/lob-types.ts` via `json-schema-to-typescript`
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

### 4.3 Updated templates

Each template below gets new sections or prompts — existing template content stays intact.

#### 4.3.1 `agents/templates/entity-model-template.md`

New section **Extension Surface** inserted between Relationships (§3) and Indexes (§4). Structure:

- **Core columns** — table of name, type, required, purpose. The stable columned surface the platform operates on regardless of variant.
- **Extension attributes** — declares whether this entity is variant-typed. If yes:
  - Schema id pattern: `<lob>/<semver>/data.schema.json`
  - `attributes_json` JSONB column (NOT NULL DEFAULT `'{}'`)
  - `lob_product_version_id` immutable FK (`NOT NULL REFERENCES lob_product_version(id)`)
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

- `attributes` field in request/response bodies as `oneOf` across active product-version schemas with explicit `discriminator`:
  ```yaml
  attributes:
    oneOf:
      - $ref: '#/components/schemas/CyberAttributesV1_0_0'
      - $ref: '#/components/schemas/PropertyAttributesV1_0_0'
    discriminator:
      propertyName: lobProductVersionId
      mapping:
        <uuid-cyber-1.0.0>:    '#/components/schemas/CyberAttributesV1_0_0'
        <uuid-property-1.0.0>: '#/components/schemas/PropertyAttributesV1_0_0'
  ```
- Response envelope additions for variant entities: `lobProductVersionId`, `lobProductId`, `lobProductVersionStatus` (`active`/`deprecated`/`retired`), `lobSchemaVersion` (semver).
- Error envelope showing the normalized `errors[]` shape with stable `code`, `pointer`, `keyword`, `constraint`.
- Inline comment: **"LOB attribute schemas in `components/schemas` are CI build-output from `scripts/build-openapi-lob-block.py`; do not hand-edit."**

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

### 4.5 Stage 1 acceptance

- [ ] Master reference exists and embeds §2 verbatim (validation taxonomy table in particular)
- [ ] Dynamic form engine reference exists
- [ ] Schema-steward sub-skill exists
- [ ] All 6 SKILL files updated with their additions
- [ ] All 7 templates updated
- [ ] Router + agent-map updated
- [ ] A walkthrough: an agent handed "add `cyber/1.0.0`" with only these references (and Stage 0 ADRs) can produce the right file layout, schema shape, and handoffs

---

## 5. Stage 2 — Solution foundation (`nebula-insurance-crm`)

Lands the rails. No LOB content yet.

### 5.1 Schema-package format spec

| Path | Content |
|---|---|
| `planning-mds/lob-schemas/README.md` | Package layout; semver rules; additive-vs-breaking definitions; example-fixture rules; steward governance pointer |
| `planning-mds/lob-schemas/_meta/schema-package.schema.json` | Meta-schema: every bundle has `data.schema.json`, `ui.schema.json`, `rules.json`, `projections.json`, `examples/{valid,invalid}/*`, `examples/rule-cases/{passing,failing}/*`, `README.md`; optional `migrations/` |
| `planning-mds/lob-schemas/_meta/rules.schema.json` | Meta-schema for `rules.json` (envelope from §2.8) |
| `planning-mds/lob-schemas/_meta/projections.schema.json` | Meta-schema for `projections.json` (shape from §2.7) |
| `planning-mds/lob-schemas/_shared/README.md` | Shared primitive catalog; semantics per concept; versioning rules |

### 5.2 Sentinel product

- `planning-mds/lob-schemas/_unspecified/0.0.0/` bundle:
  - `data.schema.json` — `{"type":"object","additionalProperties":true}`
  - `rules.json` — `[]`
  - `projections.json` — `[]`
  - `ui.schema.json` — `{}`
  - `examples/valid/empty.json` — `{}`
  - `README.md` — "Sentinel. Accepts any attributes. Used for rows predating LOB-specific schemas or where `lineOfBusiness IS NULL`."
- Seeded into `lob_product` with `line_of_business = NULL`, `status = Active`, `tenant_availability = 'all'`.
- **Never deleted.**

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
- Load all active bundles from filesystem at app startup.
- Verify HMAC signatures; tamper → startup failure.
- Cache by `(productVersionId, stage)`.
- Expose `Resolve(productVersionId, stage) → SchemaBundle` (schema + rules + projections + product metadata incl. `lineOfBusiness`).
- Expose `canMigrate(fromV, toV)` for migration-path checks.
- Hot-reload on SSE `schema:activated` event (Stage 5 V2; skeleton only in Stage 2).

**API endpoints:**
- `GET /api/lob-schemas/active?stage=...` — bootstrap payload for the tenant; ETag; `Cache-Control: private, max-age=3600`
- `GET /api/lob-schemas/{productVersionId}/{stage}` — lazy fetch single bundle; ETag
- `GET /api/lob-products` — product catalog (lifecycle metadata)

Returns the **bundled** form (resolved `$ref`s).

### 5.4 Validation pipeline middleware (.NET)

Request-scoped, order:

1. Resolve `lobProductVersionId` from request body.
2. `LobSchemaResolver.Resolve(productVersionId, stage)` → bundle + product.
3. Assert `request.lineOfBusiness == bundle.product.lineOfBusiness` (see §2.11). Emit `LOB_PRODUCT_MISMATCH` on violation.
4. `Json.Schema.Net` validates `attributes` against `data_schema_json`. Shim produces normalized envelope (§2.3).
5. `JsonLogic.Net` evaluates `rules_json` against `{data, core, context}`. Shim produces normalized envelope (§2.8).
6. Domain invariants (lifecycle, authorization via Casbin, workflow guards).
7. Persist. Emit `lob_schema_validation_audit` row.

Error contract: `application/problem+json` with stable `errors[]` shape (see §2.3).

### 5.5 CI hooks

| Script | Runs on | Purpose |
|---|---|---|
| `scripts/validate-lob-schemas.py` | `planning-mds/lob-schemas/**` changes | Bundle conformance to `_meta/schema-package.schema.json`; restricted-profile linter; complexity score; naming conventions |
| `scripts/schema-diff.py` | Same | Additive-vs-breaking detector between prior and new version; blocks mis-bumped PRs |
| `scripts/validate-schema-parity.ts` (+ `.csproj`) | Same | Runs every `examples/{valid,invalid}/*` through AJV (TS) and Json.Schema.Net (.NET); asserts normalized-envelope parity |
| `scripts/validate-rules-parity.ts` (+ `.csproj`) | Same | Runs every `examples/rule-cases/{passing,failing}/*` through both JsonLogic engines |
| `scripts/validate-rule-ops.ts` (+ `.csproj`) | `_shared/rules/operations/**` changes | Per custom op, runs fixtures through both implementations |
| `scripts/compile-bundles.py` | Same | Resolves `$ref`s, inlines, emits `compiled/<lob>/<semver>/<stage>.bundle.json`; signs with HMAC |
| `scripts/build-openapi-lob-block.py` | Same | Extracts active bundles → emits `oneOf`/`discriminator` block into `planning-mds/api/nebula-api.yaml` `components/schemas` |
| `scripts/build-fe-types.sh` | Same | `json-schema-to-typescript` over active bundles → `experience/src/generated/lob-types.ts` |
| `scripts/validate-projection-plan.py` | Same | EXPLAIN test per projection: index used for documented query |

### 5.6 Draft 2020-12 migration for existing schemas

All files in `planning-mds/schemas/` are draft-07. Migrate as part of Stage 2 while the surface is manageable:
- Bulk update `$schema` to `https://json-schema.org/draft/2020-12/schema`
- Fix the handful of keyword syntax changes (`items` as array → `prefixItems`; `definitions` → `$defs`)
- Run conformance tests against existing fixtures; assert zero-regression
- These static schemas remain hand-authored for non-variant entities (Account, Broker, Contact, etc.) — the static track per `json-schema-validation-architecture.md`

### 5.7 Entity column additions + `lineOfBusiness` invariant trigger

**Migration** — adds new columns on variant entities:
```sql
ALTER TABLE submissions
  ADD COLUMN lob_product_version_id uuid NOT NULL
    REFERENCES lob_product_version(id)
    DEFAULT '<uuid of _unspecified/0.0.0>',
  ADD COLUMN attributes_json jsonb NOT NULL DEFAULT '{}';

-- same for policies, policy_versions, policy_endorsements, renewals
```

**Backfill** — every existing row points at `_unspecified/0.0.0`, `attributes_json = '{}'`.

**Trigger** — `enforce_lob_consistency()` on each variant table enforces §2.11.

### 5.8 Static API schema updates

Every create/update request schema for variant entities adds `lobProductVersionId` and `attributes` (with `oneOf`/`discriminator` populated by `build-openapi-lob-block.py`). In Stage 2 the only discriminator branch is `UnspecifiedAttributesV0_0_0`; branches added per LOB in Stages 3–4.

Files updated (skeleton pattern; actual branches come from CI in later stages):
- `submission-create-request.schema.json`, `submission-update-request.schema.json`
- `policy-create-request.schema.json`, `policy-import-request.schema.json`, `policy-from-bind-request.schema.json`, `policy-update-request.schema.json`, `policy-issue-request.schema.json`
- `policy-endorsement-request.schema.json`
- `renewal-create-request.schema.json`, `renewal-update-request.schema.json`

Every response schema for variant entities adds `lobProductVersionId`, `lobProductId`, `lobProductVersionStatus`, `lobSchemaVersion`.

`problem-details.schema.json` extended (or sibling `validation-problem-details.schema.json` added) for the structured `errors[]` shape. Stable error-code dictionary in `planning-mds/architecture/error-codes.md` extended.

### 5.9 FE dependency additions

`experience/package.json` (verified current content does **not** include any of the below):

**dependencies:**
- `ajv@^8`
- `ajv-formats@^3`
- `ajv-errors@^3`
- `react-hook-form@^7`
- `json-logic-js@^2`
- `idb@^8`

**devDependencies:**
- `json-schema-to-typescript@^15`
- `@types/json-logic-js`

ESLint rule added banning direct schema imports outside `experience/src/lib/dynamic-form/`.

### 5.10 Stage 2 acceptance

- [ ] Schema-package meta-schemas exist and are used by CI
- [ ] `_unspecified/0.0.0` sentinel bundle exists and is seeded
- [ ] Registry tables migrated; sentinel seeded
- [ ] Registry endpoints returning bundled payloads with ETag
- [ ] Validation pipeline middleware runs (no-op against `_unspecified`)
- [ ] All 9 CI scripts functional
- [ ] All existing draft-07 schemas migrated to 2020-12; conformance tests green
- [ ] Every variant entity table has `lob_product_version_id` (FK to `_unspecified/0.0.0`) and `attributes_json` columns
- [ ] `enforce_lob_consistency()` trigger present on all 5 variant tables
- [ ] All existing rows successfully backfilled
- [ ] Static API schemas updated; OpenAPI regenerated; FE types regenerated
- [ ] FE deps landed; `experience` builds green
- [ ] Audit table `lob_schema_validation_audit` partitioned and retained

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
  - `recordsHeld` (integer, magnitude bands via `oneOf` with discriminator)
  - `controls` (nested object: `mfaEnabled: boolean`, `mfaMaturity: enum`, `edrEnabled: boolean`, `edrMaturity: enum`, `backupEnabled: boolean`, `trainingFrequency: enum`)
  - `priorIncidents` (array of `{date, severity, description}`; `minItems: 0`, `maxItems: 20`)
  - `requestedLimit` — `$ref: /lob-schemas/_shared/tiv/1.0.0/schema.json`
  - `requestedRetention` — `$ref: /lob-schemas/_shared/money/1.0.0/schema.json`
- `ui.schema.json` — section ordering, widget hints (controls grid, magnitude-banded slider)
- `rules.json` — per §2.8 envelope:
  - `records-held-requires-mfa` → `recordsHeld > 1_000_000 ⇒ controls.mfaEnabled == true`
  - `retention-min-1pct-of-limit` → `retention.amountMinor >= limit.amountMinor / 100`
- `projections.json` — per §2.7:
  - `records-held-count` → `bigint` via `$.recordsHeld`
  - `requested-limit-amount-minor` → `bigint` via `$.requestedLimit.amountMinor`
  - `mfa-enabled` → `boolean` via `$.controls.mfaEnabled`
- `examples/valid/{1..5}.json`, `examples/invalid/{1..5}.json`, `examples/rule-cases/{passing,failing}/*.json`
- `README.md` — Cyber product narrative

### 6.3 Backend changes

- EF Core migrations: generated columns + btree indexes for the three Cyber projections on every variant entity table.
- EF Core: `HasColumnType("jsonb")` on `attributes_json`; owned-type wrapper exposing `JsonElement`.
- Backfill: rows where `lineOfBusiness = 'Cyber'` migrated from `_unspecified/0.0.0` to `cyber/1.0.0`. `attributes_json` stays `{}` for legacy rows (no LOB-specific data to recover). Trigger invariant passes because LOB matches.
- `LobSchemaResolver` activates `cyber/1.0.0` on startup.

### 6.4 Frontend changes

Under `experience/src/lib/dynamic-form/`:

| File | Responsibility |
|---|---|
| `schema-walker.ts` | Depth-first walk, conditional resolution via `if/then/else`, `oneOf` discriminator |
| `widget-registry.ts` | Registry API; default widget set: `string`, `number`, `integer`, `boolean`, `enum→Select`, `date→DatePicker`, `nested object→Card`, `array of objects→DataTable+Drawer`, `oneOf→Tabs` — all shadcn/ui |
| `ajv-cache.ts` | Compiled validator pool keyed by `(productVersionId, stage)`; LRU 200; populated at bootstrap |
| `rhf-adapter.ts` | Bridges schema walk to react-hook-form field state |
| `json-logic-evaluator.ts` | `rules.json` evaluation; pointer-mapped errors |
| `useDynamicForm.ts` | Main hook; snapshots `productVersionId` for session lifetime |
| `error-normalizer.ts` | Maps server + client errors to the normalized envelope (§2.3) |
| `bootstrap-loader.ts` | After auth → `GET /api/lob-schemas/active?stage=*` per stage → cache in IndexedDB keyed by `(productVersionId, stage, etag)` → precompile AJV |
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
- [ ] Existing Cyber rows migrated; invariant trigger passes
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
3. Backfill: rows with `lineOfBusiness = '<LOB>'` migrated from `_unspecified/0.0.0` to `<lob>/1.0.0`.
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
| `planning-mds/operations/lob-schema-lifecycle.md` | Runbook: activation, deprecation, retirement, emergency rollback, mass-migration of retired-version rows |
| `planning-mds/operations/shared-primitive-governance.md` | Rules for adding/versioning `_shared/` primitives; tighten-via-allOf-in-LOB / never-loosen-in-LOB rule |
| `planning-mds/operations/projection-change-runbook.md` | Production migration pattern for STORED generated columns on large tables |

### 8.2 Dashboards

- Submissions/policies bound to **deprecated** versions per LOB
- Oldest-active-deprecated-version-age (KPI for steward team)
- Schema activation/deprecation/retirement event log
- Validation rejection rate per product version (catches bad activations)

### 8.3 Optional V2 enhancements (post-MVP)

- SSE channel `/api/lob-schemas/events` publishing `activated`/`deprecated`/`retired` events for live cache invalidation in long-lived sessions
- Steward admin UI for activate/deprecate/retire actions (currently PR + deploy)
- Schema migration tooling for the rare case where a deprecated-version row needs in-place upgrade
- Per-tenant schema overrides (`_shared/` primitive overrides for one tenant's regulatory context)

### 8.4 Stage 5 acceptance

- [ ] Governance docs published and referenced from schema-steward SKILL
- [ ] PR template in use on at least one bundle change
- [ ] Dashboards operational; deprecation-age KPI visible
- [ ] Audit table queryable; sample report produced

---

## 9. Sequencing and dependencies

```
Stage 0 (ADRs / decision lock)
      │
      ├──► Stage 1 (framework foundation — agents/**)            ──┐
      │                                                             │
      └──► Stage 2 (solution foundation — CRM rails + sentinel)  ──┤
                                                                    │
                                                                    ▼
                                                          Stage 3 (Cyber pilot)
                                                                    │
                                                                    ▼
                                                       Stage 4 (roll-forward to 9 LOBs)
                                                                    │
                                                                    ▼
                                                       Stage 5 (governance hardening)
```

- **Stage 0** gates everything. ADRs land first.
- **Stages 1 and 2** independent; parallel.
- **Stage 3** depends on both — framework references guide the design; solution rails provide runtime.
- **Stage 4** depends on Stage 3 — pilot proves pattern before retrofit.
- **Stage 5** runs alongside late Stage 4 as more LOBs onboard.

---

## 10. Performance acceptance criteria

All apply to Stage 3 ship and must hold through Stage 4.

| Metric | Target | Measurement |
|---|---|---|
| Bootstrap payload size (all active stages, all tenants) | ≤ 500 KB gzipped | CI check on `GET /api/lob-schemas/active` response |
| Schema validation latency (FE, warm) | p95 ≤ 5 ms | Vitest benchmark on realistic Cyber payloads |
| Schema validation latency (BE, warm) | p95 ≤ 10 ms | xUnit benchmark |
| Rule evaluation latency | p95 ≤ 5 ms FE / ≤ 10 ms BE | Same harnesses |
| Dynamic form initial render (Cyber submission) | ≤ 300 ms on mid-tier laptop | Playwright perf trace |
| Projection-backed portfolio query | p95 ≤ 50 ms at 100K rows | pgbench test on representative data |
| Bundle compile time (CI, all bundles) | ≤ 30 s | CI timing |
| Schema resolver startup cost | ≤ 2 s for 50 active bundles | .NET startup telemetry |

Regression beyond these gates blocks merge.

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
| PolicyVersion snapshot confusion | Master reference explicitly separates snapshots (platform) from `attributes_json` (LOB) |

---

## 13. Decision log (all 13 decisions pinned by Stage 0 ADRs)

| # | Decision | Pinned |
|---|---|---|
| 1 | Form engine | Custom RHF + AJV + shadcn widget registry (over RJSF/JSONForms/Formily) |
| 2 | Pilot LOB | Cyber |
| 3 | Schema source-of-truth | Filesystem canonical (`planning-mds/lob-schemas/`); DB operational cache |
| 4 | Cross-field rules | JsonLogic (`json-logic-js` FE + `JsonLogic.Net` BE) with full governance (§2.8) |
| 5 | JSON Schema draft | 2020-12 across the board; restricted profile (§2.3) |
| 6 | BE schema validator | `Json.Schema.Net` (not NJsonSchema for validation; NJsonSchema retained only if C# class codegen needed) |
| 7 | `_shared/` primitives | Versioned, immutable per version, `$ref`, served bundled, steward-owned |
| 8 | Schema delivery | Hybrid: build-time TS codegen + bootstrap + IndexedDB + lazy-with-ETag |
| 9 | Backfill strategy | `_unspecified/0.0.0` sentinel in Stage 2; replace per-LOB in Stages 3–4 |
| 10 | First artifacts written | Stage 0 ADRs, then Stages 1 and 2 in parallel |
| 11 | Money representation | Integer minor units + ISO currency code; JSON-number `multipleOf` on money forbidden |
| 12 | Validator parity mechanism | Restricted schema profile + normalized error envelope (code + pointer parity, messages NOT compared) |
| 13 | PolicyVersion existing snapshots | Supplement, not replace; `attributes_json` additive alongside `Profile/Coverage/PremiumSnapshotJson` |

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
- **Activating a new LOB version:** PR + steward sign-off + deploy. No FE deploy required to make it visible.
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
│       ├── data.schema.json     # {"type": "object", "additionalProperties": true}
│       ├── ui.schema.json       # {}
│       ├── rules.json           # []
│       ├── projections.json     # []
│       ├── examples/valid/empty.json
│       └── README.md
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

Confirm the 13 decisions (§13) and I proceed to **Stage 0** — drafting the four ADRs in `nebula-insurance-crm/planning-mds/architecture/decisions/`. Once they land and are signed off, Stages 1 and 2 proceed in parallel.

Redirects on any decision are welcome — §13 is where the plan's load-bearing choices live; everything else is mechanical.
