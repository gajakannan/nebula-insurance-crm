# F0020 — Document Management & ACORD Intake — Status

**Overall Status:** Draft
**Last Updated:** 2026-05-04

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0020-S0001 | Upload single document with metadata to a parent record | [ ] Not Started |
| F0020-S0002 | Bulk multi-file upload to a parent record | [ ] Not Started |
| F0020-S0003 | Quarantine and mock-scan workflow | [ ] Not Started |
| F0020-S0004 | List documents on a parent record with classification filtering | [ ] Not Started |
| F0020-S0005 | Document detail view with preview and provenance | [ ] Not Started |
| F0020-S0006 | Download a document for current and prior versions | [ ] Not Started |
| F0020-S0007 | Replace a document with immutable supersedes lineage | [ ] Not Started |
| F0020-S0008 | Update document metadata without creating a new binary version | [ ] Not Started |
| F0020-S0009 | Classification-based access control on document operations | [ ] Not Started |
| F0020-S0010 | Document completeness signal endpoint | [ ] Not Started |
| F0020-S0011 | Retention policy YAML and scheduled cleanup | [ ] Not Started |
| F0020-S0012 | Document templates library | [ ] Not Started |

## Backend Progress

- [ ] Entities and EF configurations
- [ ] Repository implementations
- [ ] Service layer with business logic
- [ ] API endpoints (controllers / minimal API)
- [ ] Authorization policies
- [ ] Unit tests passing
- [ ] Integration tests passing

## Frontend Progress

- [ ] Page components created (Documents List, Document Detail, Upload Dialog, Templates Library)
- [ ] API hooks / data fetching
- [ ] Form validation
- [ ] Routing configured
- [ ] Component/integration tests added or updated for changed behavior
- [ ] Accessibility validation recorded
- [ ] Coverage artifact recorded
- [ ] Responsive layout verified
- [ ] Visual regression tests

## Cross-Cutting

- [ ] Seed data (sample templates, taxonomy YAML, retention YAML, casbin-document-roles YAML)
- [ ] Migration(s) applied (if backend persistence backs sidecar JSON indexing — Architect decides in Phase B)
- [ ] API documentation updated
- [ ] Runtime validation evidence recorded
- [ ] No TODOs remain in code

## Required Signoff Roles (Set in Planning)

Architect sets this matrix during feature planning. Mark only truly required roles as `Yes`.

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Upload, quarantine, versioning, classification, retention, and parent linkage all need acceptance and integration coverage. | Architect | 2026-05-04 |
| Code Reviewer | Yes | Storage boundary, sidecar JSON contracts, and classification gating need independent review. | Architect | 2026-05-04 |
| Security Reviewer | Yes | Classification-based access control, file ingestion pipeline, content-type validation, and audit logging are security-sensitive. ADR-012 + ADR-019 + the combined-gate model require security signoff. | Architect | 2026-05-04 |
| DevOps | Yes | Document repository layout, scheduled retention sweeper, configuration YAML loading, and runtime hot-reload affect deployment + ops. | Architect | 2026-05-04 |
| Architect | Yes | Storage abstraction (`IDocumentRepository`), ADR-012 finalisation, ADR-019 ingest pipeline, and cross-feature signal contract (S0010) require explicit architecture signoff. | Architect | 2026-05-04 |

## Story Signoff Provenance

Complete this before moving `Overall Status` to `Done`/`Archived`.
Every story in scope must have passing evidence for every role marked `Required = Yes`.
`Evidence` must reference solution artifacts, not `agents/**` guidance files.

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0020-S0001 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0001 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0001 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0001 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0001 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0002 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0002 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0002 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0002 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0002 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0003 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0003 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0003 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0003 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0003 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0004 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0004 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0004 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0004 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0004 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0005 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0005 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0005 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0005 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0005 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0006 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0006 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0006 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0006 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0006 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0007 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0007 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0007 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0007 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0007 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0008 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0008 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0008 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0008 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0008 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0009 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0009 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0009 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0009 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0009 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0010 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0010 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0010 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0010 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0010 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0011 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0011 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0011 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0011 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0011 | Architect | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0012 | Quality Engineer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0012 | Code Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0012 | Security Reviewer | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0012 | DevOps | - | N/A | - | - | Populate when implementation begins. |
| F0020-S0012 | Architect | - | N/A | - | - | Populate when implementation begins. |

## G4.5 Signoff Addendum

**Recorded:** 2026-05-05
**Mode:** Append-only closeout ledger; planning placeholder rows above are preserved.
**Gate Evidence:** `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md`

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0020-S0001 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Upload single document covered by backend, frontend, runtime, and full test evidence. |
| F0020-S0001 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0001 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | File validation, access control, and metadata validation reviewed. |
| F0020-S0001 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0001 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | ADR-012, sidecar schema, metadata registry, API contract, and KG aligned. |
| F0020-S0002 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Bulk upload covered by upload service and UI evidence. |
| F0020-S0002 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0002 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Batch size/type validation reviewed. |
| F0020-S0002 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0002 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Bulk flow preserves ADR-019 quarantine contract. |
| F0020-S0003 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Quarantine worker covered by runtime evidence. |
| F0020-S0003 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | Scoped hosted service defect fixed and revalidated. |
| F0020-S0003 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Quarantine path and scanner boundary reviewed. |
| F0020-S0003 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | API container rebuilt and health checked. |
| F0020-S0003 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | ADR-019 ingest pipeline preserved. |
| F0020-S0004 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Parent list and completeness tests passed. |
| F0020-S0004 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0004 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Classification-filtered visibility reviewed. |
| F0020-S0004 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0004 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Parent-scoped document contract aligned. |
| F0020-S0005 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Detail view and schema-driven metadata form built and tested. |
| F0020-S0005 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0005 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Metadata schema validation reviewed. |
| F0020-S0005 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0005 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Sidecar detail contract aligned with ADR-012. |
| F0020-S0006 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Download helper and backend stream path validated. |
| F0020-S0006 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0006 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Binary download authorization and path checks reviewed. |
| F0020-S0006 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0006 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Immutable version download contract aligned. |
| F0020-S0007 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Replace flow validated through service and runtime checks. |
| F0020-S0007 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0007 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Replace authorization and binary validation reviewed. |
| F0020-S0007 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0007 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Supersedes lineage contract aligned. |
| F0020-S0008 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Static fields plus schema-driven JSON metadata validated. |
| F0020-S0008 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0008 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Metadata schema registry validation reviewed. |
| F0020-S0008 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0008 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Metadata schema evolution added to ADR-012 and contracts. |
| F0020-S0009 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Classification paths covered by tests and security review. |
| F0020-S0009 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0009 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Classification access control reviewed. |
| F0020-S0009 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0009 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Combined ABAC and classification gate contract aligned. |
| F0020-S0010 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Completeness signal covered by parent panel and API evidence. |
| F0020-S0010 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0010 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Signal visibility uses same list authorization boundary. |
| F0020-S0010 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0010 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Soft signal contract aligned. |
| F0020-S0011 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Retention service and config covered by backend/runtime evidence. |
| F0020-S0011 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | Hosted service lifetime defect fixed and revalidated. |
| F0020-S0011 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Retention boundary reviewed. |
| F0020-S0011 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime config, volume, and health verified. |
| F0020-S0011 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Retention YAML contract aligned with ADR-012. |
| F0020-S0012 | Quality Engineer | Codex feature runner / QE | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Template library covered by service/UI/runtime evidence. |
| F0020-S0012 | Code Reviewer | Codex feature runner / Code Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md` | 2026-05-05 | No critical/high findings remain. |
| F0020-S0012 | Security Reviewer | Codex feature runner / Security Reviewer | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md` | 2026-05-05 | Template authorization and materialization reviewed. |
| F0020-S0012 | DevOps | Codex feature runner / DevOps | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md` | 2026-05-05 | Runtime preflight restored and `/healthz` passed. |
| F0020-S0012 | Architect | Codex feature runner / Architect | PASS | `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md` | 2026-05-05 | Template-as-document contract aligned with ADR-012. |

## G4.6 Product Manager Closeout Addendum

**Recorded:** 2026-05-05
**Product Manager Role Switch:** Complete; `agents/product-manager/SKILL.md` read before closeout edits.
**Final Overall Status:** Done
**Archive Target:** `planning-mds/features/archive/F0020-document-management-and-acord-intake/`

### Story Closeout

All F0020 stories are complete and covered by G4.5 required signoff:

- F0020-S0001 Upload single document with metadata to a parent record — Done
- F0020-S0002 Bulk multi-file upload to a parent record — Done
- F0020-S0003 Quarantine and mock-scan workflow — Done
- F0020-S0004 List documents on a parent record with classification filtering — Done
- F0020-S0005 Document detail view with preview and provenance — Done
- F0020-S0006 Download a document for current and prior versions — Done
- F0020-S0007 Replace a document with immutable supersedes lineage — Done
- F0020-S0008 Update document metadata without creating a new binary version — Done
- F0020-S0009 Classification-based access control on document operations — Done
- F0020-S0010 Document completeness signal endpoint — Done
- F0020-S0011 Retention policy YAML and scheduled cleanup — Done
- F0020-S0012 Document templates library — Done

### Delivered Scope

- Filesystem-first document repository with sidecar JSON metadata and immutable version records.
- Type-specific JSON metadata with pinned schema versions and a lightweight docroot metadata schema registry.
- Upload, bulk upload, quarantine promotion, list, detail, download, replace, metadata edit, completeness, retention, and template flows.
- Parent document panels for account, submission, policy, and renewal surfaces.
- Classification-aware access control, safe path handling, binary validation, and runtime configuration.

### Cross-Feature Integration Provenance

- F0018 Policy 360 document rail is fulfilled by F0020 as of 2026-05-05. `experience/src/pages/PolicyDetailPage.tsx` mounts `ParentDocumentsPanel` with `parent={{ type: 'policy', id: policy.id }}`, so policy documents now use the shared F0020 document subsystem rather than the pre-F0020 placeholder.
- KG relationship updated in `planning-mds/knowledge-graph/feature-mappings.yaml` to mark the F0018 to F0020 document-rail relationship as implemented provenance.

### Deferred Follow-Ups

- Replace the mock timer scanner with a real malware scanner through `IQuarantineScanner`.
- Object storage migration tooling remains future scope behind `IDocumentRepository`.
- Long-horizon regulatory retention remains future scope; MVP retention remains capped per ADR-012.
- Office-format previews and cross-corpus document search remain future scope.
- Richer custom widgets for future metadata schema types can build on the JSON Schema renderer.

### Mitigation Notes

- Existing unrelated nullable warnings remain in older dashboard/submission/task/workflow files.
- Existing Vite chunk-size warning remains.
- Existing Popover `act(...)` warnings remain in the frontend test suite.
- Authentik startup was rechecked during G4; current runtime has Authentik and DB healthy, API running, and `/healthz` returning `Healthy`.

### Signoff Provenance

- G4.5 signoff ledger: `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g4.5-signoff/signoff-ledger.md`
- Runtime validation: `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md`
- Code review: `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-code-review/code-review.md`
- Security review: `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/g3-security-review/security-review.md`

### Orphaned Stories

None. All 12 stories are closed as Done.
