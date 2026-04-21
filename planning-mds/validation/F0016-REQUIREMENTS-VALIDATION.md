# Requirements Validation Report — F0016 Account 360 & Insured Management

**Scope:** Feature-specific (F0016) — requirements + implementation coverage
**Date:** 2026-04-15
**Reviewer:** Product Manager role (independent re-validation)
**Source artifacts:** `planning-mds/features/archive/F0016-account-360-and-insured-management/` (PRD.md, STATUS.md, 11 stories, feature-assembly-plan.md) and uncommitted implementation on branch `feat/F0016-account-360-and-insured-management`.

## Summary

- **Assessment:** VALID WITH RECOMMENDATIONS
- **Sections checked:** PRD (all sections), 11 story files, feature-assembly-plan, REGISTRY/ROADMAP entries, implementation surfaces (`AccountEndpoints.cs`, `AccountService.cs`, account UI routes, dependent fallback wiring).
- **Issues found:** 0 critical, 3 high, 2 medium, 2 low.

---

## Findings by Severity

### Critical (blocks building)

None.

### High (should fix before closeout is final)

1. **Story F0016-S0008 checklist item not implemented: `GET /api/accounts/{id}/merge-preview?survivorId=...`**
   - Location: planned in `F0016-S0008-account-merge-and-duplicate-handling.md:29-31, 55`; absent from `engine/src/Nebula.Api/Endpoints/AccountEndpoints.cs:25-40` (only `/merge` is mapped); grep across `engine/src` for `merge-preview` / `MergePreview` returns no hits.
   - Impact: PRD §"UX/Screens" merge flow and the "Duplicate Resolution (Merge) Flow" depend on the preview to show "preview impact (N submissions, M policies, P renewals)". Without it, the UI cannot meet the AC. STATUS.md records S0008 as PASS across all five roles, which means the signoff did not catch this.
   - Recommendation: implement `GET /api/accounts/{id}/merge-preview?survivorId=...` returning `{ submissionCount, policyCount, renewalCount, contactCount, timelineCount, totalLinked }` per the story's data section, or explicitly de-scope from MVP and update STATUS.md.

2. **Story F0016-S0008 checklist item not implemented: server-side enforcement of the 500-linked-record merge threshold (HTTP 413 `merge_too_large`).**
   - Location: planned in `F0016-S0008.md:50-51, 61`; not present in `engine/src/Nebula.Api/Endpoints/AccountEndpoints.cs:255-265` (no `merge_too_large` branch in the error switch); no occurrences of `merge_too_large` / `413` in `engine/src/Nebula.Application/Services/AccountService.cs` or anywhere under `engine/src` outside unrelated migrations.
   - Impact: A merge against an account with thousands of linked records will execute synchronously, violating the PRD NFR ("p95 ≤ 2s for ≤ 500 linked records" — bounded by design) and potentially holding a long transaction.
   - Recommendation: enforce the threshold in `AccountService.MergeAsync` and surface `merge_too_large` → 413 in the endpoint, mirroring the story.

3. **Story F0016-S0008 checklist item not implemented: `Idempotency-Key` retry semantics on merge.**
   - Location: planned in `F0016-S0008.md:52`; grep across `engine/src` for `Idempotency-Key` / `IdempotencyKey` returns no hits.
   - Impact: A retried merge POST after a partial network failure can produce duplicate timeline events on the survivor (the AC explicitly forbids this). NFR also lists "merge actions idempotent on retry … no duplicate timeline events" (PRD §Reliability).
   - Recommendation: accept and de-duplicate by `Idempotency-Key` on the merge endpoint, or explicitly de-scope and update STATUS.md and PRD's Reliability NFR.

### Medium (address when convenient)

4. **STATUS.md signoff cells overstate merge coverage.**
   - Location: `STATUS.md:80-84` (S0008 row, all five PASS verdicts) vs. actual gaps in #1-#3 above. The QE row in particular cites "Merge semantics … covered end-to-end in API integration tests" while three explicit checklist items are absent.
   - Impact: provenance log no longer matches reality after independent re-validation; closeout audit trust degraded.
   - Recommendation: re-open S0008 verdicts (QE, Code Reviewer, Architect, Security) until #1-#3 are resolved or formally de-scoped.

5. **Three Definition-of-Done items left explicitly unchecked on STATUS.md without a deferral entry.**
   - Location: `STATUS.md:171-173` — "Dedicated accessibility audit recorded", "Dedicated visual regression evidence recorded", "Dedicated responsive layout evidence recorded".
   - Impact: NFR §Performance/Security do not require these, but the DoD checklist is the project's contract for "Done"; leaving them unchecked without a follow-up tracker entry contradicts STATUS.md's "Overall Status: Done".
   - Recommendation: either record the audits or move them into the "Deferred Non-Blocking Follow-ups" table with an owner.

### Low (suggestions)

6. **PRD §"Indexing" lists `Accounts(TaxId)` filtered unique index, but the architect-validation report flags it as raw-SQL only — flag for cross-doc coherence.**
   - Location: PRD `Account` data section line 216 + Architect validation finding #3. PRD does not currently note that EF migration & code mirror is a FOLLOW-UP, so a future planner might assume the EF model already enforces it.
   - Recommendation: add a note in the PRD Risks & Assumptions table that the unique index is enforced at the SQL layer only.

7. **Banned-word usage in F0016-S0001 NFR.**
   - Location: `F0016-S0001-account-list-with-search-and-filtering.md:85` — "Reliability: Deleted / Merged accounts never cause a 500 when `includeRemoved=true`" is fine, but the "Security: ABAC enforced in the query" line uses the verb-without-specifics pattern. PRD elsewhere is precise (Casbin actions enumerated). Tightening the story to name the policies (`account:read` + region/territory predicate) would make it independently testable without the PRD.

---

## Checklist Results

- **Completeness:** PASS — PRD all sections filled, 11 stories present, no TODO placeholders, feature-assembly-plan + STATUS + README + GETTING-STARTED present.
- **Vision & non-goals clarity:** PASS — vision is 3 sentences with concrete outcome; out-of-scope list is explicit (8 items); success metrics quantified (≤ 3 clicks, "zero broken dependent renders", count of workflows originated).
- **Persona validation:** PASS — Underwriter, Distribution User, Distribution Manager, Relationship Manager all have job-to-be-done + key pain + success; primary vs secondary is identified.
- **Feature traceability:** PASS — every story maps to a persona need; per-story role-based-visibility tables match PRD §Role-Based Access.
- **Story testability:** PASS WITH NIT — 11/11 stories use "As a / I want / So that"; AC are "Given/When/Then"; performance criteria quantified (p95 ≤ 300ms list, ≤ 500ms 360 overview, ≤ 2s merge); error scenarios enumerated (400/403/409/412/413); edge cases listed. One nit: F0016-S0001 NFR uses "ABAC enforced" without naming the policy (low #7).
- **Anti-pattern detection:** PASS — no "should be fast", "easy", "intuitive" found across the 11 stories.
- **Screen specs:** PASS — PRD §UX/Screens lists 6 screens with purpose + key actions; `experience/src/pages/Accounts*.tsx` and `experience/src/features/accounts/` implement them.
- **Consistency:** PASS — no conflicting requirements across PRD/stories/STATUS; terminology matches glossary; all business rules trace to user needs (lifecycle, fallback contract, ABAC).
- **Implementation coverage:** PASS WITH GAPS — 11/11 stories have implementation evidence (endpoints, DTOs, services, UI routes, migrations). 3 explicit checklist items in S0008 are not implemented (high #1-#3); fallback contract on submissions/renewals/policies is wired (verified in `RenewalDto.cs`, `SubmissionDto.cs` denormalized fields).

---

## Self-Review Gate

- [x] Every relevant section of the PRD checked, not sampled.
- [x] Every feature folder file inspected (PRD, STATUS, 11 stories, feature-assembly-plan, README, GETTING-STARTED).
- [x] Story testability validated per story, not just sampled.
- [x] Findings cite specific files and line numbers (`F0016-S0008.md:50-51`, `AccountEndpoints.cs:25-40`, `STATUS.md:80-84`).
- [x] Severity assignments follow validate.md's defined levels — three story-level explicit-checklist gaps are HIGH (incomplete spec coverage), provenance mismatch is MEDIUM (consistency), DoD unchecked items are MEDIUM, doc-coherence note is LOW.

---

## Recommendation

**FIX HIGH ISSUES BEFORE FINAL CLOSEOUT** (then the feature is genuinely Done).

The three high findings are scoped, well-localized, and either implement-now items (1-3) or a STATUS.md correction (4). None require re-planning. Once #1-#3 land or are explicitly de-scoped (with PRD + STATUS updates), the feature meets the intent of the plan.
