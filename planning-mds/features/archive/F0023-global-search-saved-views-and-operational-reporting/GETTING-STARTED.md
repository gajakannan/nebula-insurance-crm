# F0023 - Global Search, Saved Views & Operational Reporting - Getting Started

## Prerequisites

- Read [PRD.md](./PRD.md) for scope, non-goals, screens, workflows, and story list.
- Review [ROADMAP.md](../ROADMAP.md) for the `Now` sequencing and F0017 dependency note.
- Review the source feature folders for records surfaced by search and reports:
  - `planning-mds/features/archive/F0016-account-360-and-insured-management/`
  - `planning-mds/features/archive/F0018-policy-lifecycle-and-policy-360/`
  - `planning-mds/features/archive/F0019-submission-quoting-proposal-and-approval/`
  - `planning-mds/features/F0017-broker-mga-hierarchy-and-producer-ownership/`
- Review [ADR-014 Search Index and Saved View Architecture](../../architecture/decisions/ADR-014-search-index-and-saved-view-architecture.md) during Phase B.

## Planning Notes

- Core search, saved views, and operational reports can be planned without waiting for F0017.
- Hierarchy/territory facets are conditional on F0017 data.
- F0037 owns hierarchy-aware access-control enforcement and distribution rollups.
- F0023 does not create a free-form report builder or full document-content search.
- Phase B selected PostgreSQL full-text search plus read-side projections for MVP. Do not add OpenSearch/Elasticsearch or a new external runtime service for this feature.
- Team saved views use `teamScopeType` + `teamScopeKey` (`Role`, `Region`, `Program`, `Territory`) and must validate the requested scope against the current authorization context.
- Saved views store criteria only. Applying a saved view reruns current source-record authorization.
- Projection lag budget is p95 under 60 seconds after source commit; source detail endpoints remain authoritative.

## How To Verify

1. Validate every story file in this folder.
2. Regenerate `STORY-INDEX.md`.
3. Confirm `REGISTRY.md`, `ROADMAP.md`, `BLUEPRINT.md`, and `STATUS.md` agree on F0023 status and story count.
4. Confirm `feature-mappings.yaml` has a PM-owned feature/story stub before Phase A approval.
5. During Phase B, confirm architecture updates complete the ontology binding and pass KG drift validation.
6. During implementation, copy the F0023 planning policy rows from `planning-mds/security/policies/policy.csv` into the embedded runtime policy at `engine/src/Nebula.Infrastructure/Authorization/policy.csv`.
7. Verify every saved-view mutation writes `SavedViewAuditEvent` and every update/default/archive path requires `If-Match`.
8. Verify search facets/counts/report rows are computed after authorization filtering.

## Developer Handoff Notes

- Use `feature-assembly-plan.md` after Phase B as the implementation contract.
- Treat `PRD.md` and story files as product truth; ontology mappings are routing context.
- Do not implement external broker/MGA search, hierarchy-aware access rollups, report scheduling, or free-form analytics under this feature.
- Use the OpenAPI paths from `planning-mds/api/nebula-api.yaml`: `/search-results`, `/saved-views`, and `/operational-reports/*`.
- Use the JSON schemas in `planning-mds/schemas/` as the request/response validation source for shared DTO shape.
- DevOps evidence is required at implementation closeout because F0023 adds projection backfill/refresh behavior and runtime observability requirements.
