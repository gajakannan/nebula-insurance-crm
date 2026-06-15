# F0019 — Submission Quoting, Proposal & Approval Workflow — Status

**Overall Status:** Done — implementation complete, evidence validated through G8, and approved for archive on 2026-06-03.
**Last Updated:** 2026-06-03

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0019-S0001 | Activate downstream submission workflow | Done |
| F0019-S0002 | Submission quote/proposal packet lifecycle | Done |
| F0019-S0003 | Underwriting approval checkpoint | Done |
| F0019-S0004 | Bind decision and policy handoff | Done |
| F0019-S0005 | Decline and withdraw terminal decisions | Done |
| F0019-S0006 | Submission archive and deactivate | Done |
| F0019-S0007 | Downstream submission pipeline list & workflow visibility | Done |
| F0019-S0008 | Downstream submission workflow timeline & audit trail | Done |

**Total Stories:** 8 · **Completed:** 8 / 8

## Planning Decisions (Locked 2026-06-01, plan G1)

- **Lifecycle:** full downstream path through Bind (`InReview → Quoted → BindRequested → Bound`, + `Declined`/`Withdrawn`).
- **Approval:** single authorized approver (audit + reason; extensible to maker-checker / authority-limits later).
- **Archive:** in scope now — terminal-state-only, explicit lifecycle action, audit-preserving (replaces F0006 soft-delete).
- **Packet:** thin CRM coordination record — *recorded, never computed*; reuses F0034 attributes + F0020 documents.

## Refinement Guardrails

- F0006 remains closed at `ReadyForUWReview`; downstream submission transitions must remain disabled until F0019 stories explicitly turn them on (S0001 owns the deliberate boundary move).
- The first F0019 story (S0001) owns activation of `ReadyForUWReview -> InReview` and the downstream state machine, with direct reference to F0006 as the prior workflow boundary, plus authorization changes, UI exposure, and regression coverage.
- F0019 refinement is complete only when the implementation contract identifies the code path, authorization changes, UI exposure, and regression coverage that deliberately move the shared submission workflow beyond F0006.
- F0019 owns the replacement submission archive/deactivate contract for F0006's descoped soft delete: terminal states only, explicit lifecycle action, list visibility, and audit retention (S0006) — no generic CRUD delete.
- **CRM workflow, not underwriting workbench:** no rating/pricing/scoring/quote-comparison. Quote figures are recorded reference values, never computed (enforced via PRD non-goals, packet contract, story acceptance criteria, a Phase B architecture decision, and a boundary regression).

## Required Role Matrix

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Workflow state machine, approval/bind behavior, idempotency, and the boundary regression require test validation. | Architect | 2026-06-01 |
| Code Reviewer | Yes | Workflow orchestration, approval logic, and the recorded-not-computed boundary require independent review. | Architect | 2026-06-01 |
| Security Reviewer | Yes | F0019 introduces approval authority (`submission:approve`) and archive (`submission:archive`) authorization deltas plus an audit-bearing approval decision record. | Architect | 2026-06-01 |
| Architect | Yes | Downstream state-machine activation, single-approver model, packet contract, and the CRM-not-workbench boundary are explicit architecture decisions (ADR-025). | Architect | 2026-06-01 |
| DevOps | Yes | G2 scope reconciliation found deployment_config_changed=true because F0019 adds an EF migration; deployability evidence and migration rollback are required. | Feature Orchestrator | 2026-06-03 |

> Finalized by the Architect in Phase B (G4/G5). Governing decision: ADR-025.

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0019-S0001 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Downstream workflow activation covered by targeted workflow tests. |
| F0019-S0001 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Approved after G3 fixes. |
| F0019-S0001 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Permission and audit controls reviewed. |
| F0019-S0001 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Assembly plan validated against ADR-025. |
| F0019-S0001 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Runtime and migration deployability evidence present. |
| F0019-S0002 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Quote/proposal packet lifecycle covered by service tests and frontend build/integration. |
| F0019-S0002 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Packet contract and recorded-not-computed boundary reviewed. |
| F0019-S0002 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Packet mutation authz and audit reviewed. |
| F0019-S0002 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Packet scope aligns with assembly plan. |
| F0019-S0002 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Packet table migration and rollback reviewed. |
| F0019-S0003 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Approval decision behavior covered by backend build/tests and schema review. |
| F0019-S0003 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Approval logic reviewed and approved. |
| F0019-S0003 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | submission:approve and audit-bearing decision record reviewed. |
| F0019-S0003 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Single-approver checkpoint aligns with ADR-025. |
| F0019-S0003 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Approval decision table migration reviewed. |
| F0019-S0004 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Bind handoff behavior covered by workflow tests and build. |
| F0019-S0004 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Bind idempotency and handoff boundary reviewed. |
| F0019-S0004 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Bind transition endpoint and idempotency controls reviewed. |
| F0019-S0004 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | In-process handoff aligns with assembly plan. |
| F0019-S0004 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Bind handoff migration and rollback reviewed. |
| F0019-S0005 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Decline/withdraw reason guards covered. |
| F0019-S0005 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Terminal transition guards reviewed. |
| F0019-S0005 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Terminal reason audit fields reviewed. |
| F0019-S0005 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Terminal state semantics align with ADR-025. |
| F0019-S0005 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | No additional deployment concern beyond migration. |
| F0019-S0006 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Archive/reactivate reason validation covered after G3 fix. |
| F0019-S0006 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Archive/reactivate G3 finding fixed before approval. |
| F0019-S0006 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | submission:archive and audit reason controls reviewed. |
| F0019-S0006 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Archive semantics align with assembly plan. |
| F0019-S0006 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Archive column migration and rollback reviewed. |
| F0019-S0007 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | List visibility covered by frontend integration and post-G3 repository filter fix. |
| F0019-S0007 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | approvalPending G3 finding fixed before approval. |
| F0019-S0007 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Scoped list visibility and archive filtering reviewed. |
| F0019-S0007 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Pipeline visibility aligns with assembly plan. |
| F0019-S0007 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | No additional deployment concern beyond migration. |
| F0019-S0008 | Quality Engineer | Quality Engineer Agent | PASS | test-execution-report.md | 2026-06-03 | Timeline/audit payload schema validation and build evidence present. |
| F0019-S0008 | Code Reviewer | Code Reviewer Agent | APPROVED | code-review-report.md | 2026-06-03 | Timeline payloads and audit events reviewed. |
| F0019-S0008 | Security Reviewer | Security Reviewer Agent | PASS | security-review-report.md | 2026-06-03 | Audit/logging reviewed; no secret leakage found. |
| F0019-S0008 | Architect | Architect Agent | PASS | g0-assembly-plan-validation.md | 2026-06-03 | Audit trail semantics align with assembly plan. |
| F0019-S0008 | DevOps | DevOps Agent | PASS | deployability-check.md | 2026-06-03 | Activity payload schema has no deployment impact. |

## PM Closeout Summary

**Closeout Date:** 2026-06-03  
**Evidence Run:** `planning-mds/operations/evidence/runs/2026-06-03-7e8e0ddc/`  
**Final Overall Status:** Done  
**Archive Decision:** Approved for archive after G8 evidence validation and tracker synchronization.

### Delivered Scope

- Activated downstream submission workflow states through quote, approval, bind request/confirmation, and terminal declined/withdrawn paths.
- Added submission quote packet, approval decision, and bind handoff records plus service/API/frontend surfaces.
- Added terminal-state-only archive/reactivate behavior with explicit reason capture and audit events.
- Added list visibility filters for approval-pending, stuck, and archived submissions.
- Extended activity payload schema and timeline evidence for F0019 events.

### Deferred Follow-ups

- None.

### Mitigation Notes

- G3 code review findings were fixed before approval: blank archive/reactivate reasons now return `missing_reason`, and `approvalPending` filtering now requires quoted status, ready quote packet, and no prior decision.
- Broad frontend unit suite still has an out-of-scope fixed-date session-continuity fixture drift; F0019 frontend build and submissions integration evidence passed.
- Validator stage support defects found during closeout were fixed before G8: `validate-feature-evidence.py` now supports explicit G4 and G7 stages, and KG explicit-check modes no longer force the G8-only coverage-report rewrite.

### Signoff Provenance

- Required role signoff is captured in `signoff-ledger.md` and mirrored above for all eight stories.
- Architect KG reconciliation is captured in `kg-reconciliation.md`.
- Final PM closeout evidence is captured in `pm-closeout.md`.
