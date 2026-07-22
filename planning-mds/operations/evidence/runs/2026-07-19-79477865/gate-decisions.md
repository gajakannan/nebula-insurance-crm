# Gate Decisions — F0026 plan run 2026-07-19-79477865

## Gate Decisions

| Gate | Decision | Decider | Timestamp | Rationale | Blocking | Follow-up |
|------|----------|---------|-----------|-----------|----------|-----------|
| G1 | PASS | Product Manager | 2026-07-19T13:52:06-04:00 | Operator resolved all five material requirements; the PRD and six stories encode the approved first-release boundary with no blocking open questions. | No | Preserve the approved boundary through Phase B; surface conflicts rather than silently changing the PRD. |
| G2 | PASS | Product Manager | 2026-07-19T13:58:56-04:00 | Operator promoted F0026 from `Later` to `Now`; authored KG source was recompiled, generated ROADMAP places F0026 in `Now`, and tracker validation again reports zero errors/warnings. | No | Present the corrected Phase A package at mandatory checkpoint G3. |
| G3 | PASS | Operator / Product Manager | 2026-07-19T14:01:00-04:00 | Operator explicitly approved transition to Phase B after the corrected `Now` sequencing and green Phase A validation. | No | Architect may begin Phase B and must preserve the approved PRD boundary. |
| G4 | PASS | Architect | 2026-07-19T16:10:46-04:00 | Authored F0026 capability/entity/workflow/endpoint/schema/role/policy/event/route mappings compiled; `kg validate.py --check-drift` exited 0. | No | Run ordered G5 exit validation before requesting Phase B approval. |
| G5 | PASS | Operator / Architect | 2026-07-19T16:13:50-04:00 | Operator explicitly approved ADR-034 and the feature assembly plan with token `approve` after all ordered exit-validation commands passed. | No | Use the feature-local assembly plan as the future F0026 feature action G0 contract; do not create feature evidence during plan. |

Decisions: `PASS`, `PASS WITH RECOMMENDATIONS`, `FAIL`, `SKIP`. Blocking values: `Yes` / `No`.

## G1 Clarification Decision Set

1. **Agency bill only.** First release creates operational agency-bill invoices bound to existing account/policy context; direct bill is deferred.
2. **Operational cash application.** Manual receipt entry and CSV through a mock bank/payment-vendor adapter are in scope; no real vendor connection or production credential is used.
3. **Exact-only matching.** Application requires same currency and an amount equal to the full outstanding operational balance. There are no automatic tolerances, partial allocations, write-offs, refunds, or settlement behavior.
4. **Named separation of duties.** A Finance Operations Analyst prepares and reconciles; a different Finance Manager approves correction adjustments. Distribution roles receive bounded read-only summaries; external roles receive no billing detail.
5. **Deferred integration seam.** F0030 is not a hard dependency. The mock adapter defines a replaceable seam for later production connectivity.

**Operator token:** `1. Agree; 2. Agree, mock bank/payment vendor integration; 3. Agree; 4. Agree; 5. Agree`

## Roadmap Sequencing Decision

- **Operator decision:** F0026 moves from `Later` to `Now`.
- **Rationale:** Direct dependencies F0018 and F0025 are complete, the six-story agency-bill slice is refined, and this A+B plan is intended to proceed toward build.
- **Boundary preserved:** F0030 remains `Later` and is a deferred integration seam, not a hard dependency.

## Phase A Approval Token

- **Checkpoint:** `approve-phase-a`
- **Explicit operator token:** `lets do Phase B`
- **Recorded by:** Product Manager
- **Recorded at:** 2026-07-19T14:01:00-04:00

## Phase B Architecture Reconciliation

- **Existing shell:** The inventory previously named invoice services, billing events, reconciliation workflow, finance views, batch-friendly exchange, and ADR-015 without deciding exact first-release behavior.
- **Reconciled decision:** ADR-034 retains an in-process billing/reconciliation boundary but narrows it to agency-bill invoices, manual and bounded mock-CSV receipts, explicit same-currency/full-outstanding application, exceptions, and manager-decided operational corrections.
- **Explicit removals/deferrals:** Direct bill, real bank/vendor transport, outbox/delivery operations, tolerance matching, partial allocation, write-offs, ledger/tax/settlement, and statements are absent. ADR-015/F0030 remain a future production integration seam.
- **Ontology binding:** New authored KG source uses `adr:034`; no taxonomy expansion was required. Generated canonical nodes, feature mappings, and solution ontology were compiled rather than hand-edited.

## Dependency Audit

- **F0025 direct dependency:** Approved evidence pointer resolves to feature run `2026-07-07-9859bad4`; ADR-033 and raw artifacts confirm billing/payment/reconciliation ownership is outside F0025.
- **F0018 direct dependency:** Evidence audit remains pending because no approved feature `latest-run.json` was found. ADR-018, raw artifacts, API/schema contracts, KG nodes, and implementation paths define the policy/version/account source boundary; this note is not substituted with repo-wide evidence validation.
- **F0030 impacted/deferred seam:** Not a first-release dependency. Production connector, credentials, outbox, retry/delivery, and operational exchange ownership require later F0030 approval.

## G5 Exit Validation — Green, Approval Pending

The corrected G5 replay used the resolved feature slug after the first invocation produced the invalid path `F0026-None`. The corrected ordered sequence completed on 2026-07-19:

1. Story validation — PASS (6 stories, zero blocking findings).
2. Story index generation — PASS.
3. Tracker validation with `--skip-feature-evidence` — PASS.
4. KG coverage report write — PASS.
5. KG drift check — PASS.
6. Framework template validation — PASS.

## Phase B Approval Token

- **Checkpoint:** `approve-phase-b`
- **Explicit operator token:** `approve`
- **Recorded by:** Architect
- **Recorded at:** 2026-07-19T16:13:50-04:00
- **Approved artifacts:** ADR-034, feature assembly plan, OpenAPI/JSON schemas, data model, authorization/policy contract, audit payloads, and authored KG bindings.
