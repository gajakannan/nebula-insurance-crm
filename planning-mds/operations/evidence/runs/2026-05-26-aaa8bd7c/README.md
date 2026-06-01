# Plan Review Run — F0036 Form Engine and Form-State Preservation — 2026-05-26-aaa8bd7c

> Base run evidence package per `feature-evidence-package-standardization-plan-v2.md` §8.
> Read-only post-plan readiness audit (`agents/actions/plan-review.md`). Answers: "Is this plan ready to build?" Does NOT write into any feature evidence package or repair planning artifacts.

## Run Summary

- Action: `plan-review` (read-only readiness audit)
- Scope: `feature` · Target: **F0036** (`dynamic-product-attribute-form-engine`)
- DIFF_RANGE: none (full-artifact review)
- PLAN_REVIEW_RUN_ID: `2026-05-26-aaa8bd7c`
- PRODUCT_ROOT (absolute): `/mnt/c/Users/gajap/sandbox/nebula/nebula-insurance-crm`
- Subject plan: plan run `2026-05-25-51ff2a92` (A1 + B2 approved) — findings independently re-derived from raw artifacts, not from that run's approvals.

## Status

- **COMPLETE — Readiness Decision: NOT READY** (1 critical, 1 high, 2 medium, 1 low)

### Gate Summary

| Gate | Decision |
|------|----------|
| PR0 SCOPE LOCK | PASS |
| PR1 PARALLEL READINESS REVIEW | COMPLETE |
| PR2 VALIDATOR PASS | PASS (5/5 exit 0) |
| PR3 SELF-REVIEW GATE | PASS |
| PR4 READINESS GATE | **NOT READY** |

## Readiness Decision

**NOT READY.** A competent implementer could not begin `feature.md` Step 0 for Workstream A without making a new architecture decision. The plan's central parity mechanism (F0036-S0003 + ADR-021 amendment §2–§3: "client evaluates `rules.json` per ADR-023 for 0-disagreement backend parity") is not buildable: the backend validates Cyber attributes and cross-field rules in **hardcoded C#** (`LobAttributeService.cs`), `rules.json` is **unwired and non-conformant** to ADR-023, and ADR-022/023's schema/rules-driven parity platform is **Accepted-but-unimplemented** (the same drift class as ADR-021). All five structural validators PASS — but structural validation cannot detect this gap.

Workstream B (CRUD RHF migration + F0035 preservation, S0007/S0008), the requirements/story quality, and the tracker/KG structure are otherwise build-ready; the NOT-READY verdict is targeted at Workstream A's validation architecture.

## Evidence Index

- `plan-review-report.md` — primary deliverable (decision, findings by severity, Product/Architecture/Buildability readiness, validation evidence, artifact trace)
- `action-context.md` — PR0 scope lock, inputs, assumptions, boundaries
- `gate-decisions.md` — PR0–PR4 decisions
- `commands.log` — JSONL telemetry (lookup + 5 validators)
- `lifecycle-gates.log` — PR2 validator invocations
- `artifact-trace.md` — what was read; confirmation nothing was edited
- `artifacts/*.txt` — validator console captures

## Validation Summary

| Command | Result |
|---------|--------|
| `validate-stories.py {FEATURE_PATH}` | PASS (8/8) |
| `validate-trackers.py` | PASS (0/0) |
| `kg/validate.py` | PASS |
| `kg/validate.py --check-drift` | PASS (pre-existing F0028 + Casbin warnings) |
| `validate_templates.py` | PASS |

Structural validators are necessary but not sufficient: all pass, yet readiness is NOT READY (raw artifacts win over generated checklists).

## Open Follow-ups (route to plan.md / owning role — not repaired here)

- **[critical → blocks build] Architect:** rework the client/backend parity strategy for S0003 / ADR-021 amendment §3 against the hardcoded backend within (or with an explicit expansion of) F0036's "no backend change" scope; then re-run `plan-review.md`.
- **[high] Architect:** decide the governed source for the MFA-maturity conditional gating (S0005) — engine convention, permitted additive ui-schema extension, or scoped exception — since it is hardcoded in both runtimes and absent from the (frozen) bundle, and ADR-022 forbids `if/then/else` in bundle schemas.
- **[medium] PM/Code Reviewer:** exhaustively sweep mutation forms (not just the 6 named) before claiming F0035 finding #1 fully closed; `PoliciesPage.tsx` has an un-inventoried `onSubmit`.
- **[medium] Architect:** record the ADR-023 implementation gap as an explicit F0036 assumption/risk (the KG binds `capability:lob-rules-governance`, which is unimplemented).
- **[low] PM:** add `Schema Steward` / `Frontend Platform Engineer` to `nebula-personas.md` or footnote them as F0036-local archetypes.
- **(Carried forward, pre-existing):** F0028/F0018 low-confidence KG edge; Casbin `(renewal, update)` policy_rule gap. Predate F0036; out of scope.
