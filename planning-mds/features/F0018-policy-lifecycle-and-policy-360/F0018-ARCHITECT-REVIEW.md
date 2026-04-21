# Feature Architect Review

Feature: F0018 — Policy Lifecycle & Policy 360

## Summary

- Assessment: PASS (Phase B design complete; ready for implementation)
- Governing references:
  - `planning-mds/features/F0018-policy-lifecycle-and-policy-360/PRD.md`
  - `planning-mds/architecture/decisions/ADR-018-policy-aggregate-versioning-and-reinstatement-window.md`
  - `planning-mds/architecture/decisions/ADR-011-crm-workflow-state-machines-and-transition-history.md`
  - `planning-mds/architecture/decisions/ADR-017-account-merge-tombstone-and-fallback-contract.md`
  - `planning-mds/architecture/decisions/ADR-009-lob-classification-and-sla-configuration.md` (extended via ADR-018)
  - `planning-mds/architecture/decisions/ADR-014-workflow-sla-threshold-per-lob-extension.md`

## Architecture Review

### Aggregate shape

- Policy is modeled as a mutable parent row + immutable `PolicyVersion` snapshots + materialized `PolicyCoverageLine` rows, plus a denormalized `CarrierRef` seed. This is the right balance between audit rigor and query ergonomics:
  - parent row holds lifecycle state and denormalized account context (per ADR-017 fallback contract)
  - each issue/endorsement/reinstatement writes a new `PolicyVersion` with full `ProfileSnapshot` / `CoverageSnapshot` / `PremiumSnapshot` payloads
  - `PolicyCoverageLine` rows are re-materialized under the new version so "current coverages" queries are indexed, not snapshot walks
- Rejected alternatives (mutable row + event log, event-sourcing, dedicated reinstatement table, Temporal MVP) are documented in ADR-018 with rationale. These are principled choices, not omissions.

### State machine

- `workflow:policy` adopts the ADR-011 pattern verbatim: four canonical states (Pending, Issued, Cancelled, Expired), role-gated transitions, append-only `WorkflowTransition` history.
- `Cancelled → Issued` is the sole non-monotonic transition; it is window-gated via `WorkflowSlaThreshold` (category `PolicyReinstatementWindow`), which extends the ADR-009 / ADR-014-workflow-sla pattern rather than introducing a parallel config surface.
- Daily scheduled sweep at 00:15 UTC handles `Issued → Expired`; Temporal migration is deferred per ADR-010 and explicitly scoped as a follow-up.

### REST surface

- Endpoints live under `/api/policies/**` and map 1:1 to the stories:
  - list/create/detail/update for S0001–S0003
  - `POST /issue`, `POST /endorse`, `POST /cancel`, `POST /reinstate` for S0006–S0008
  - `GET /versions`, `GET /versions/{id}`, `GET /endorsements`, `GET /coverages`, `GET /timeline`, `GET /summary` for S0004–S0010
  - `GET /accounts/{id}/policies` replaces the pre-existing stub and `GET /accounts/{id}/policies/summary` powers S0011
  - `POST /api/policies/from-bind` locks down the F0019 bind-hook contract and creates a `Pending` policy when implemented (may return 501 in MVP)
  - `POST /api/policies/import` owns the import-lite CSV batch path
- Every mutating endpoint accepts `Idempotency-Key` and gates on `If-Match` / `rowVersion` per established patterns. 409 `code=must_use_endorse` guards the profile-edit path from material-term drift on Issued policies.

### Authorization

- Casbin actions added: `policy:read`, `policy:create`, `policy:update`, `policy:issue`, `policy:endorse`, `policy:cancel`, `policy:reinstate`, `policy:coverage:manage`, `policy:import`. All rule nodes captured in canonical-nodes.yaml with per-role allow lists.
- Read is broad across the internal roles. Manual create plus Pending-state profile/coverage edits are allowed for Distribution User, Distribution Manager, Underwriter, and Admin; import-lite is Distribution Manager or Admin; cancel is Distribution Manager / Underwriter / Admin; issue, endorse, and reinstate remain Underwriter-or-Admin only.

### Fallback contract adoption

- Denormalized `AccountDisplayNameAtLink`, `AccountStatusAtRead`, `AccountSurvivorId` are captured on the Policy row and propagated into list/summary payloads per ADR-017. No FK re-pointing on account merge; dependent views read tombstone-forward.

### Data integrity & performance

- Unique constraints: `(PolicyId, VersionNumber)` and `(PolicyId, EndorsementNumber)`; `PolicyNumber` globally unique.
- Indexes: `Policies(AccountId, Status)`, `Policies(ExpirationDate)` for the sweep and the next-expiring query, partial `Policies(PredecessorPolicyId)` for successor lookups.
- Account-scoped policy summary is query-time composition with a single grouped query (no N+1); a materialized projection is deferred.

### Scope discipline

- The feature explicitly stays in its lane:
  - renewal workflow remains F0007's responsibility (successor lookup reads `Renewal.BoundPolicyId` but does not mutate renewals)
  - document rail remains F0020's responsibility (Policy 360 shows a "coming soon" state when F0020 is absent)
  - bind lifecycle remains F0019's responsibility (the bind-hook is a contract in MVP, not an implementation)
  - full Carrier lifecycle (admin CRUD, ratings, licensing) is deferred to F0028

## Review Notes

- ADR-018 is the sole new ADR introduced for this feature; it absorbs the reinstatement-window category rather than amending ADR-009 or ADR-014 in place, keeping the decision trail linear.
- Timeline, version, and endorsement reason codes are validated at the API layer (enums in the OpenAPI contract and JSON schemas) rather than in a separate reference table; this matches the cancellation-reason and endorsement-reason precedent from F0007 / F0016.
- Versions and endorsements are append-only and never mutated; re-cancellation after reinstatement is explicitly supported with each cycle appending its own transition history and version rows.

## Recommendation

**PASS** — F0018 is architecturally aligned with the approved Phase A plan. All contracts (ADR, OpenAPI, JSON schemas, canonical nodes, policy rules, feature mappings) are in place and internally consistent. Proceed to implementation.
