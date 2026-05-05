# ADR-012: Establish Shared Document Storage and Metadata Architecture

**Status:** Accepted
**Date:** 2026-05-04 (originally Proposed 2026-03-23; finalized during F0020 Phase B planning)
**Owners:** Architect
**Related Features:** F0006, F0018, F0019, F0020, F0027, F0029
**Supersedes:** —
**Superseded by:** —

## Context

Documents are central to submission intake, policy context, quote and proposal workflows, outbound document generation, and external collaboration. A fragmented per-feature file model would duplicate metadata rules, weaken auditability, and complicate access control.

Nebula needs a shared document subsystem with stable metadata and linkage rules so multiple features can rely on one document system of record. The original (Proposed) version of this ADR fixed the boundary; the F0020 Phase B planning round (2026-05-04) locks in the concrete shape: an on-disk repository with sidecar JSON metadata, a reserved `configuration/` folder for canonical YAML, a quarantine-then-promote ingest pipeline, classification-tier ABAC, and a YAML-driven retention sweep.

## Decision

The Nebula document subsystem is a **single on-disk repository** with the following invariants:

### 1. Repository layout

```
<docroot>/
  configuration/
    taxonomy.yaml                          # canonical document type taxonomy
    metadata-schemas/
      registry.yaml                        # type -> versioned metadata schema registry
      {type}.v{N}.schema.json              # JSON Schema for type-specific metadata
    document-retention-policies.yaml       # per-type retention (MVP cap: 10 days)
    casbin-document-roles.yaml             # classification-policy table
    retention-sweeps.jsonl                 # append-only sweep audit log
  quarantine/
    {upload-id}                            # binary held during scan; promoted on success
  templates/
    {templateId}/
      {logicalName}-v{N}.{ext}             # template binary, colocated versions
      {logicalName}.json                   # single sidecar JSON for the template
  {parent-type}/                           # parent-type ∈ {account, submission, policy, renewal}
    {parent-id}/
      {logicalName}-v1.{ext}
      {logicalName}-v2.{ext}
      ...
      {logicalName}.json                   # single sidecar JSON for all versions
```

### 2. Sidecar JSON is the canonical metadata record

There is exactly **one** sidecar JSON per logical document, regardless of how many versions exist. The JSON carries the version chain (`versions[]` with `supersedes`), classification, parent linkage, uploader identity, audit timestamps, append-only `events[]` log, `provenance`, and type-specific JSON `metadata`. Schema: `planning-mds/schemas/document-sidecar.schema.json`.

Sidecar `metadata` is validated against the pinned `metadataSchema` reference stored on the same sidecar:

```json
{
  "metadataSchema": {
    "id": "acord",
    "version": 1,
    "schemaHash": "sha256:..."
  },
  "metadata": {
    "formNumber": "125",
    "effectiveDate": "2026-05-01"
  }
}
```

The `metadataSchema.id` normally matches `type`; `metadataSchema.version` records the version that accepted the metadata at upload or explicit type-change time. Documents remain readable against the version recorded on the sidecar even after the current schema advances.

### 3. Versioning is colocated and immutable

New versions append a `-v{N+1}` binary next to existing versions and append to `versions[]` with `supersedes: N`. **Previous binaries are never overwritten or deleted by replacement.** Retention sweep is the only path that removes binaries.

### 4. Quarantine-then-promote ingest pipeline

Every uploaded binary lands first in `<docroot>/quarantine/{upload-id}` with the sidecar JSON `versions[N-1].status = quarantined`. After the configured hold (default 60 s; bounds 30-300 s), an idempotent worker atomically moves the binary into the parent's directory and flips status to `available`. The 60-second hold is the **mock-scanner contract** in MVP; ADR-019 records the pipeline decision in detail. Replacing the mock with a real scanner is an interface swap, not a redesign.

### 5. Classification + ABAC layered model

Every operation is gated by **`parent_abac(user, parent, op) ∧ classification_policy(role, classification, op)`**. Classifications are `public | confidential | restricted`. The classification policy table is loaded from `<docroot>/configuration/casbin-document-roles.yaml`. Closed-by-default: missing entries deny. Hot reload within 60 s on file change. Schema: `planning-mds/schemas/document-classification-policy.schema.json`.

### 6. Configuration is canonical YAML on disk

The reserved `<docroot>/configuration/` folder is the source of truth for:
- `taxonomy.yaml` — canonical document type list (e.g., `acord, loss-run, financials, supplemental, template`)
- `metadata-schemas/registry.yaml` — active and deprecated metadata schema versions by document type
- `metadata-schemas/{type}.v{N}.schema.json` — JSON Schema documents for type-specific metadata fields
- `document-retention-policies.yaml` — per-type retention days (MVP hard ceiling: 10 days)
- `casbin-document-roles.yaml` — classification policy table

Code never embeds these values. Loaders validate against JSON Schema on read; bad files fail closed (prior policy stays in force).

### 6.1. Type-specific metadata schema evolution

Type-specific document metadata is JSON, not new relational columns. Schema evolution is intentionally lightweight:

- `registry.yaml` maps each document type to `currentVersion` and a list of versioned schema files.
- Upload uses the current active schema for the selected or detected `type`.
- Metadata patch validates against the document's stored `metadataSchema` unless the patch explicitly changes `type`; a type change pins the current schema for the new type.
- Breaking changes require a new schema version. Historical sidecars are not silently rewritten.
- Deprecated schema versions remain loadable until no retained sidecars reference them.
- The frontend may render forms from JSON Schema but must not expose raw JSON editing as the normal user workflow.

### 7. Retention is YAML-driven and capped at 10 days for MVP

A scheduled sweeper (default every 1 hour, bounded 5 m - 24 h) reads `document-retention-policies.yaml` and removes documents whose latest version's `uploadedAt` exceeds the per-type retention. Sweeps are recorded in `<docroot>/configuration/retention-sweeps.jsonl`. The 10-day ceiling is enforced at YAML load time; any value above 10 is rejected. Long-horizon retention (insurance recordkeeping) is **Future scope** and explicitly out of MVP.

### 8. Storage abstraction boundary

MVP backs `<docroot>` with the local filesystem. The repository is accessed exclusively through an interface (`IDocumentRepository`) so a future migration to object storage (S3, Azure Blob) is a backend swap without changing feature contracts. **No feature code may read or write directly under `<docroot>`** — all access flows through the interface.

### 9. Audit model

The sidecar JSON `events[]` is the **document-level** audit log (append-only, fsync on append). Every document operation also produces a single domain-level `ActivityTimelineEvent` (per SOLUTION-PATTERNS §2) so the feed-level audit aligns with the rest of Nebula. Sidecar `events[]` is the granular record; `ActivityTimelineEvent` is the cross-cutting summary.

### 10. Templates are documents

The Templates Library (F0020-S0012) lives in `<docroot>/templates/` with the same sidecar JSON shape (plus `useCount` and `lastUsedAt`). Templates are subject to the same classification, retention, and ABAC rules.

### 11. Forbidden patterns

- Per-feature document tables in the relational database. Documents are filesystem-first.
- Storing classification rules in code. The YAML is the policy.
- Mutating prior versions, sidecar JSONs (except via append), or the configuration files outside their loaders.
- Range/partial uploads or downloads in MVP. They are Future.
- Office-format previews in MVP. Inline preview is `pdf, png` only; other types render a placeholder.

## Scope

This ADR governs:

- document metadata contracts, metadata schema evolution, and the canonical sidecar JSON shape
- versioning and supersession behaviour
- parent-entity linkage rules (parent-type ∈ `account, submission, policy, renewal` in MVP)
- storage abstraction boundary and the `IDocumentRepository` contract
- audit and authorisation expectations for document actions
- classification policy YAML and retention policy YAML formats
- template library layout and lifecycle

## Consequences

### Positive

- Multiple features reuse one document model and access-control contract.
- Document lineage and auditability become consistent across the platform.
- Storage implementation can evolve without changing every feature contract.
- Configuration is auditable as code (YAML files in `configuration/`).
- Replacing the mock scanner with a real one is a single interface implementation swap.

### Negative

- Filesystem-first storage requires careful path-safety and content-type validation at the upload boundary.
- A future move to object storage will require tooling to migrate the existing tree without breaking sidecar provenance.
- Sidecar JSON sweeps need to be coordinated with retention; the per-document lock pattern (used by replace and metadata-edit) extends to the sweeper.
- Cross-feature search across the corpus is not supported in MVP (read paths are per-parent only).

## Implementation Notes

- Schemas land in `planning-mds/schemas/` (`document-sidecar.schema.json`, `document-metadata-schema-registry.schema.json`, `document-list-item.schema.json`, `document-detail.schema.json`, `document-completeness.schema.json`, `document-retention-policy.schema.json`, `document-classification-policy.schema.json`, `document-template.schema.json`).
- API endpoints land in `planning-mds/api/nebula-api.yaml` under tag `Documents` and `DocumentTemplates`.
- Casbin rows for the new permissions (`document:create`, `document:read`, `document:replace`, `document:update_metadata`, `document:download`, `document:create:restricted`, `document:declassify`, `template:create`, `template:read`, `template:replace`, `template:link`) land in `planning-mds/security/policies/policy.csv` and `authorization-matrix.md`.
- Knowledge graph: `entity:document` (existing) gains a richer rationale; new canonical nodes — `entity:document-template`, `workflow:document-ingest`, schemas, and policy rules — are added in `planning-mds/knowledge-graph/canonical-nodes.yaml`.

## Follow-up

- ADR-019 (companion): Mock-quarantine-then-promote ingest pipeline.
- F0020 stories S0001-S0012 implement this ADR.
- Future: replace mock scanner; introduce object-storage backend; introduce long-horizon retention; introduce cross-corpus search.
