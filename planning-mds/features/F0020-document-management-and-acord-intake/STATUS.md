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
