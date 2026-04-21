# F0018 — Policy Lifecycle & Policy 360 — Getting Started

## Prerequisites

- [ ] Read the current release framing in [COMMERCIAL-PC-CRM-RELEASE-PLAN.md](../COMMERCIAL-PC-CRM-RELEASE-PLAN.md)
- [ ] Review [F0016 PRD](../archive/F0016-account-360-and-insured-management/PRD.md) for the Account 360 + tombstone-forward fallback contract (F0018 consumes these on the policy side)
- [ ] Review [F0007 PRD](../archive/F0007-renewal-pipeline/PRD.md) for Renewal entity shape (`PolicyId`, `BoundPolicyId`) — F0018 exposes successor linkage from Policy 360
- [ ] Confirm ADR-009 (`WorkflowSlaThreshold`) and ADR-011 (workflow transitions / timeline) before refinement sign-off
- [ ] Architect to confirm whether ADR-009 will be extended with a `PolicyReinstatementWindow` category or whether a new ADR is needed during Phase B

## Refinement Notes (Phase A)

- Scope was finalized on 2026-04-18. 10 scope-shaping clarifications resolved before stories were written:
  - **Policy number format:** Globally unique within Nebula; human-readable (`NEB-{LOB-prefix}-{yyyy}-{6-digit-sequence}`), not a GUID. User-supplied accepted on import with conflict detection.
  - **Coverage authoring:** Structured `PolicyCoverageLine` child records (not freeform text). Full structured sub-limit model is a follow-up; MVP captures a `SubLimitDescription` free-text field.
  - **Lifecycle states:** `Pending → Issued → Expired`, `Issued → Cancelled`, `Cancelled → Issued` (reinstate within LOB window). Upstream submission/quote/bind states (Cleared, Quoted, Bound) are owned by F0019; Nebula's policy record becomes authoritative at `Issued`.
  - **Reinstatement window:** Per-LOB via `WorkflowSlaThreshold` (Property 30, GeneralLiability 30, WorkersCompensation 60, ProfessionalLiability 30, Cyber 15, Default 30); no admin backdoor after window lapses.
  - **F0019 bind-hook:** Contract specified (`POST /api/policies/from-bind`); implementation deferred until F0019 ships.
  - **Carrier handling:** Lightweight `CarrierRef` seed now; F0028 will replace with full carrier master.
  - **Documents rail:** Empty-state placeholder until F0020 lands; full integration via F0020 when live.
  - **Expiration job:** MVP nightly cron-style job at 00:15 UTC; Temporal durable workflow migration is a follow-up per ADR-010.
  - **Story slate:** 11 stories covering list, create, detail/edit, 360 composition, version history, endorsements, cancellation, reinstatement, renewal linkage, timeline, summary projection.
  - **Fallback contract:** Consumes F0016 tombstone-forward on account references; denormalizes `AccountDisplayNameAtLink` on policy rows.

## Dependencies

- **Done / archived:** F0002, F0007, F0009, F0016 (all consumed)
- **Planned in this release:** F0020 (Documents); F0018 integrates with empty-state fallback
- **Future consumers:** F0019 (Submission Quote/Bind), F0028 (Carrier Master), F0025 (Billing), F0027 (Document Templates)

## How to Verify

1. Confirm `Policy` is a first-class aggregate with its own lifecycle state machine, not display-only policy metadata.
2. Confirm every lifecycle transition (issue, endorse, cancel, reinstate, expire) is role-gated, audited, and produces the expected version / transition / timeline artifacts.
3. Confirm Policy 360 rails load independently and the Policy List + Account 360 summary numbers compose without N+1.
4. Confirm the fallback contract with F0016 holds: policies on merged / deleted accounts never 500 and render tombstone-forward.
5. Confirm reinstatement window enforcement is server-side (not client-advisory) and per-LOB.
6. Validate tracker sync (`validate-trackers.py`) and story index (`generate-story-index.py`) after refinement.

## Ownership for Phase B

- **Architect:** ADR(s) for Policy aggregate + versioning + reinstatement-window extension of ADR-009; canonical-nodes entries for Policy / PolicyVersion / PolicyEndorsement / PolicyCoverageLine / CarrierRef; solution-ontology edges (Policy ↔ Account, Policy ↔ Renewal, Policy ↔ Submission); OpenAPI contracts under `/api/policies/**`; Casbin `policy:*` rules; expiration-job scheduling contract.
- **Product Manager:** Owns PRD, stories, personas, tracker updates. No further PRD changes without clarification round.
