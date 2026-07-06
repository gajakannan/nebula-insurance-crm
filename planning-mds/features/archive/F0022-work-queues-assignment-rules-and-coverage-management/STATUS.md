# F0022 — Work Queues, Assignment Rules & Coverage Management — Status

**Overall Status:** Done — archived 2026-07-03 under run `2026-07-03-b9f40b31`; deferred test hardening accepted at PM closeout.
**Last Updated:** 2026-07-03

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0022-S0001 | Manage work queues and memberships | Done |
| F0022-S0002 | Define assignment rules and precedence | Done |
| F0022-S0003 | Route work from tasks, submissions, and renewals | Done |
| F0022-S0004 | Manage backup coverage windows | Done |
| F0022-S0005 | Queue worklists and aging visibility | Done |
| F0022-S0006 | Reassign and rebalance queued work | Done |
| F0022-S0007 | Routing audit, permissions, and exceptions | Done |

## Required Role Matrix

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | Queue routing, idempotency, coverage windows, source ABAC, and reassignment behavior require structured validation. | Architect | 2026-07-03 |
| Code Reviewer | Yes | Assignment-rule evaluation, coverage logic, and source assignment ports require independent review. | Architect | 2026-07-03 |
| Security Reviewer | Yes | Queue visibility, reassignment powers, fallback queues, and source-record ABAC intersections change access and work ownership boundaries. | Architect | 2026-07-03 |
| DevOps | Yes | Feature-action G0 forced DevOps evidence because F0022 is runtime-bearing and expected to add EF migrations/deployability-sensitive backend surfaces. No new hosted infrastructure is planned. | Architect | 2026-07-03 |
| Architect | Yes | F0022 introduces the shared routing and queue execution engine governed by accepted ADR-013. | Architect | 2026-07-03 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0022-S0001 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Assembly plan validated. |
| F0022-S0001 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Queue setup covered by build/runtime evidence; broader tests accepted as deferred PM follow-up. |
| F0022-S0001 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Queue edit issue fixed during review; broader tests accepted as deferred PM follow-up. |
| F0022-S0001 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Queue management authorization verified; denial-path tests accepted as deferred PM follow-up. |
| F0022-S0001 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0002 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Assignment-rule design validated. |
| F0022-S0002 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Rule endpoint/build evidence passed; rule precedence tests accepted as deferred PM follow-up. |
| F0022-S0002 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Rule implementation reviewed; service tests accepted as deferred PM follow-up. |
| F0022-S0002 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Manage authorization verified. |
| F0022-S0002 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0003 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Task/submission/renewal routing scope validated. |
| F0022-S0003 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Routing command and smoke evidence passed; route matrix tests accepted as deferred PM follow-up. |
| F0022-S0003 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Source adapter and routing service reviewed. |
| F0022-S0003 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Source detail expansion must remain ABAC-gated. |
| F0022-S0003 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0004 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Coverage window model validated. |
| F0022-S0004 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Coverage endpoints compile/build; overlap and routing tests accepted as deferred PM follow-up. |
| F0022-S0004 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Coverage implementation reviewed. |
| F0022-S0004 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Coverage management authorization verified. |
| F0022-S0004 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0005 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Queue worklist and aging surface validated. |
| F0022-S0005 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Worklist runtime smoke passed; UI component tests accepted as deferred PM follow-up. |
| F0022-S0005 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Worklist implementation reviewed. |
| F0022-S0005 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Queue item details remain limited pending source ABAC expansion. |
| F0022-S0005 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0006 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Reassignment/rebalance scope validated. |
| F0022-S0006 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Reassignment endpoint/build evidence passed; service tests accepted as deferred PM follow-up. |
| F0022-S0006 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Source assignment write-back reviewed. |
| F0022-S0006 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Assign authorization and active-user guard reviewed. |
| F0022-S0006 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |
| F0022-S0007 | Architect | Architect | PASS | g0-assembly-plan-validation.md | 2026-07-03 | Audit/permissions scope validated. |
| F0022-S0007 | Quality Engineer | Quality Engineer | PASS | test-execution-report.md | 2026-07-03 | Audit/event listing evidence passed; denial-path tests accepted as deferred PM follow-up. |
| F0022-S0007 | Code Reviewer | Code Reviewer | APPROVED | code-review-report.md | 2026-07-03 | Routing decision log implementation reviewed. |
| F0022-S0007 | Security Reviewer | Security Reviewer | PASS | security-review-report.md | 2026-07-03 | Queue policy and secret scan passed. |
| F0022-S0007 | DevOps | DevOps | PASS | deployability-check.md | 2026-07-03 | Migration/runtime deployability checked. |

## Planning Decisions

- 2026-07-03 — G1 clarification approved for plan run `2026-07-03-8aa72827`: F0022 initial release routes tasks, submissions, and renewals; rule precedence is explicit manual override → coverage/out-of-office → territory/ownership → workload balancing → fallback queue; no-match work lands in `Unassigned Operations Queue`; coverage requires explicit out-of-office/coverage windows.
- 2026-07-03 — G3 Phase A approval received from operator; Phase B architecture started under Nebula Agents plan harness.
- 2026-07-03 — ADR-013 accepted for F0022 execution contract; queue/rule/coverage data model, API contracts, schemas, and authorization deltas drafted.
- 2026-07-03 — Feature action run `2026-07-03-b9f40b31` started after G5 plan approval; G0 assembly plan authored and DevOps signoff set to required by feature evidence contract because migrations/runtime deployability are in scope.
