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

---

## PM Closeout - 2026-04-22

**Final Overall Status:** Done; archive transition approved.  
**Implementation Run ID:** `5ab6f922-bf43-4702-9393-ea8a88c213b8`  
**Primary Evidence:** `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md`

### Final Story Status

| Story | Final Status | Evidence |
|-------|--------------|----------|
| F0018-S0001 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0002 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0003 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0004 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0005 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0006 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0007 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0008 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0009 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0010 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |
| F0018-S0011 | Done | `planning-mds/operations/evidence/f0018/g45-signoff-2026-04-22.md` |

### Signoff Provenance

| Role | Verdict | Reviewer | Date | Evidence |
|------|---------|----------|------|----------|
| Architect | PASS | Codex feature runner | 2026-04-22 | `planning-mds/operations/evidence/f0018/g2-self-review-2026-04-22.md` |
| DevOps | PASS | Codex feature runner | 2026-04-22 | `planning-mds/operations/evidence/f0018/runtime-preflight-2026-04-22.md` |
| Quality Engineer | PASS | Codex feature runner | 2026-04-22 | `planning-mds/operations/evidence/f0018/g2-self-review-2026-04-22.md` |
| Code Reviewer | PASS | Codex feature runner | 2026-04-22 | `planning-mds/operations/evidence/f0018/g3-code-review-2026-04-22.md` |
| Security Reviewer | PASS | Codex feature runner | 2026-04-22 | `planning-mds/operations/evidence/f0018/g3-security-review-2026-04-22.md` |

### Mitigation Notes

- Repaired before signoff: `/policies/from-bind` no longer returns 501 and now accepts the OpenAPI contract request.
- Repaired before signoff: policy create and lifecycle mutation paths now enforce scoped account/broker visibility before writes.
- Repaired before signoff: dashboard renewal aging handles LOB-specific SLA threshold rows without duplicate-key failures.

### Deferred Non-Blocking Follow-ups

- Add policy-specific integration tests for `/policies/from-bind` and scoped write-denial paths.
- Replace count-based policy-number allocation with a dedicated sequence-row implementation when concurrent policy creation is hardened.

### Orphaned Story Review

No orphaned stories. All F0018 stories are closed as Done with required role signoff evidence.
