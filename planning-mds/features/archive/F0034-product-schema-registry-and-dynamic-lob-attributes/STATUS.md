# F0034 - Product Schema Registry and Dynamic LOB Attributes - Status

**Overall Status:** Phase B approved - implementation in progress
**Last Updated:** 2026-05-07

## Planning State

Phase A requirements and story breakdown are approved. Phase B architecture artifacts are approved by user decision on 2026-05-07; implementation is authorized to proceed through the feature action gates.

## Story Checklist

| Story | Title | Status |
|-------|-------|--------|
| F0034-S0001 | Lock product-attribute decision set | Not Started |
| F0034-S0002 | Establish product schema registry foundation | Not Started |
| F0034-S0003 | Pin attributes on lifecycle carriers | Not Started |
| F0034-S0004 | Prove frontend and backend validator equivalence | Not Started |
| F0034-S0005 | Render dynamic attribute panel from schema metadata | Not Started |
| F0034-S0006 | Activate Cyber product bundle | Not Started |
| F0034-S0007 | Prove lifecycle integration and F0019 handoff | Not Started |

## Required Signoff Roles (Set in Planning)

| Role | Required | Why Required | Set By | Date |
|------|----------|--------------|--------|------|
| Quality Engineer | Yes | FE/BE parity, fixtures, dynamic form coverage, and E2E validation are core acceptance criteria. | Product Manager | 2026-05-06 |
| Code Reviewer | Yes | Registry, carrier persistence, dynamic form, and F0019 guardrail changes cross backend and frontend boundaries. | Product Manager | 2026-05-06 |
| Security Reviewer | Yes | Product schemas influence user-provided data, tenant bundle visibility, validation behavior, and authorization-sensitive fields. | Product Manager | 2026-05-06 |
| DevOps | Yes | Bundle activation, runtime cache, validation harness, and deployment evidence affect release operations. | Product Manager | 2026-05-06 |
| Architect | Yes | The feature introduces cross-cutting product attribute, validation, and dynamic form architecture. | Product Manager | 2026-05-06 |

## Story Signoff Provenance

| Story | Role | Reviewer | Verdict | Evidence | Date | Notes |
|-------|------|----------|---------|----------|------|-------|
| F0034-S0001 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0001 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0001 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0001 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0001 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0002 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0002 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0002 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0002 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0002 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0003 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0003 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0003 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0003 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0003 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0004 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0004 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0004 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0004 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0004 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0005 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0005 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0005 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0005 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0005 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0006 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0006 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0006 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0006 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0006 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0007 | Quality Engineer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0007 | Code Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0007 | Security Reviewer | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0007 | DevOps | - | N/A | - | - | Populate after implementation evidence exists. |
| F0034-S0007 | Architect | - | N/A | - | - | Populate after implementation evidence exists. |

## Product Manager Planning Tasks

- [x] Expand the minimal PRD into the full Phase A planning artifact.
- [x] Use Cyber as the first product/LOB pilot based on the existing LOB extensibility plan.
- [x] Define story files using strict `F0034-SNNNN-*` naming.
- [x] Define product, validation, compatibility, dynamic form, and F0019 guardrail acceptance criteria.
- [x] Refresh tracker and story index artifacts before Phase A approval.
- [x] Receive explicit Phase A approval.

## Architect Planning Tasks

- [x] Publish Stage 0 ADRs for LOB attribute architecture, form engine, validator equivalence, and rules governance.
- [x] Publish the OpenAPI 3.0.3 projection matrix.
- [x] Publish the validation performance baseline artifact with implementation measurement gates.
- [x] Update static schemas and OpenAPI contracts with `lobAttributes`, schema-bundle endpoints, and LOB validation error shape.
- [x] Update authorization matrix and policy CSV for bundle read/resolve and Admin-only lifecycle writes.
- [x] Update data model and knowledge graph mappings for registry entities, endpoints, schemas, ADRs, and policy rules.
- [x] Receive explicit Phase B approval.

## Deferred Scope

- No-code product administration console.
- Full rollout to remaining LOBs.
- Heavy custom widgets for Commercial Auto, Property, D&O, and other post-Cyber LOBs.
- External broker self-service product capture.

## Closeout Addendum - 2026-05-07

**Final Overall Status:** Done

**Implementation Summary:** F0034 delivered the product schema registry foundation, Cyber 1.0.0 schema bundle, non-null lifecycle product-version pinning for Submission/Renewal/PolicyVersion/PolicyEndorsement, LOB validation problem details, schema resolver endpoints, frontend Cyber dynamic attribute panel, and product-version/stage route integration.

**Story Closeout:** F0034-S0001 through F0034-S0007 are closed as Done under the F0034 implementation gate evidence.

**Validation Evidence:**
- G0/G1 runtime preflight: `planning-mds/operations/evidence/F0034/gates/G0-G1-preflight.md`
- G2 self-review: `planning-mds/operations/evidence/F0034/gates/G2-self-review.md`
- G3 code/security review: `planning-mds/operations/evidence/F0034/gates/G3-code-security-review.md`
- G4 approval and G4.5 signoff: `planning-mds/operations/evidence/F0034/gates/G4-approval-and-signoff.md`

**Signoff Provenance:** Quality Engineer, Code Reviewer, Security Reviewer, DevOps, and Architect all recorded PASS on 2026-05-07 with reviewer/date/evidence in `planning-mds/operations/evidence/F0034/gates/G4-approval-and-signoff.md`.

**Mitigation Notes:** No critical or high findings remain. Existing nullable warnings in DashboardRepository/SubmissionRepository and the Node 22 cross-realm Blob assertion in `experience/src/services/api.test.ts` are outside F0034 scope and were not changed.

**Deferred Follow-ups:** No blocking F0034 story follow-ups remain. Future product-admin UI, non-Cyber LOB bundles, custom widgets for additional LOBs, and external broker product capture remain deferred scope.

**Archive Transition:** Move to `planning-mds/features/archive/F0034-product-schema-registry-and-dynamic-lob-attributes/` during PM closeout.

## Post-Closeout Correction - 2026-05-08

**Reason:** UI testing found that Cyber attributes rendered on Policy Detail and Renewal Detail but were effectively read-only. The implementation had satisfied the broad "display or capture" wording by displaying the panel, but it had not wired editable detail-page mutation paths for policies and renewals.

**Planning Gap:** F0034 artifacts did not explicitly require Policy Detail and Renewal Detail to expose enabled edit/save/cancel controls, nor did they identify the exact backend write contracts needed for those screens. The stories also treated legacy-pinned Cyber rows as read-only without defining the first-capture transition from `_legacy_cyber/0.0.0` to active `cyber/1.0.0` when valid attributes are saved through an approved lifecycle path.

**Correction Applied:** Policy Detail and Renewal Detail must be verified as editable for active Cyber attributes where the lifecycle state and role allow mutation. Renewal attributes write through `PUT /renewals/{renewalId}/lob-attributes`; pending policy attributes write through `PUT /policies/{policyId}`; issued policy attributes write through the endorsement path. The carrier consistency trigger must allow empty legacy Cyber rows to move to active Cyber when a valid non-empty Cyber payload is captured.

**Validation Evidence:** 2026-05-08 fix verified with `dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj` (405 passed, 1 skipped), targeted Policy/Renewal detail integration tests, and `pnpm --dir experience build`.
