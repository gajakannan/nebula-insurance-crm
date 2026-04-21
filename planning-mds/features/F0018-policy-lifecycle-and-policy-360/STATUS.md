# F0018 — Policy Lifecycle & Policy 360 — Status

**Overall Status:** In Refinement
**Last Updated:** 2026-04-18

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0018-S0001 | Policy list with search and filtering | In Refinement |
| F0018-S0002 | Create policy (manual, import-lite, F0019 bind-hook contract) | In Refinement |
| F0018-S0003 | Policy detail and profile edit | In Refinement |
| F0018-S0004 | Policy 360 composed workspace | In Refinement |
| F0018-S0005 | Immutable policy version snapshots and version history | In Refinement |
| F0018-S0006 | Policy endorsement events and term changes | In Refinement |
| F0018-S0007 | Policy cancellation (mid-term and flat) | In Refinement |
| F0018-S0008 | Policy reinstatement within LOB-configurable window | In Refinement |
| F0018-S0009 | Policy renewal linkage (predecessor / successor) | In Refinement |
| F0018-S0010 | Policy activity timeline and audit trail | In Refinement |
| F0018-S0011 | Policy summary projection for Account 360 | In Refinement |

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Workflow transition matrix, version/endorsement/cancellation/reinstatement semantics, ABAC scope coverage, reinstatement-window enforcement, rail-isolation tests, expiration-job idempotency. | Product Manager | 2026-04-18 |
| Code Reviewer | Yes | Policy aggregate modeling, version snapshot immutability, coverage-line materialization, atomic endorsement transactions, fallback-contract consumption. | Product Manager | 2026-04-18 |
| Security Reviewer | Yes | New `policy:*` Casbin actions, sensitive-data classification, reinstatement authority gate, cancellation reason-code governance, cross-role visibility on a hub entity. | Product Manager | 2026-04-18 |
| DevOps | Yes | New Policies / PolicyVersions / PolicyEndorsements / PolicyCoverageLines / CarrierRef migrations, index plan, CarrierRef seed, denormalized `AccountDisplayNameAtLink` backfill on policy rows, daily expiration job scheduling. | Product Manager | 2026-04-18 |
| Architect | Yes | New ADR(s) for policy aggregate + versioning + reinstatement-window category; extension of ADR-009 `WorkflowSlaThreshold` for `PolicyReinstatementWindow`; API-contract authority on `POST /api/policies/from-bind` hook consumed by F0019; integration surface with F0007 / F0016 / F0020. | Product Manager | 2026-04-18 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0018-S0001 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0001 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0001 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0001 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0001 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0002 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0002 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0002 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0002 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0002 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0003 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0003 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0003 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0003 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0003 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0004 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0004 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0004 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0004 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0004 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0005 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0005 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0005 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0005 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0005 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0006 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0006 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0006 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0006 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0006 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0007 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0007 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0007 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0007 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0007 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0008 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0008 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0008 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0008 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0008 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0009 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0009 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0009 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0009 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0009 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0010 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0010 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0010 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0010 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0010 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0011 | Quality Engineer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0011 | Code Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0011 | Security Reviewer | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0011 | DevOps | - | N/A | - | - | Populate after Phase B architect design lands. |
| F0018-S0011 | Architect | - | N/A | - | - | Populate after Phase B architect design lands. |
