# Code Quality Review Report

Scope: F0013 (`planning-mds/features/F0013-dashboard-framed-storytelling-canvas/`)  
Date: 2026-03-16

## Summary
- Assessment: REJECTED
- Files reviewed: backend + frontend F0013 implementation paths, tests, and planning/status artifacts
- Total issues: 7 (4 Critical, 3 High)

## Runtime and Automation Evidence

Runtime health:
- `docker compose ps --all` => PASS (all required services up/healthy)

Automation scripts:
- `python3 agents/code-reviewer/scripts/check-code-quality.py engine/src` => PASS (warnings only)
- `python3 agents/code-reviewer/scripts/check-code-quality.py experience/src` => PASS (warnings only)
- `python3 agents/code-reviewer/scripts/check-code-quality.py planning-mds/features/F0013-dashboard-framed-storytelling-canvas` => FAIL (`TODO` token in checklist text)
- `sh agents/code-reviewer/scripts/check-lint.sh --strict` => FAIL (strict script expects `format` script in `experience/package.json`)
- `sh agents/code-reviewer/scripts/check-pr-size.sh --base main --max 500` => FAIL (5167 insertions > threshold)
- `sh agents/code-reviewer/scripts/check-test-coverage.sh --min 80 --auto` => FAIL (no coverage artifact detected)
- `python3 agents/product-manager/scripts/validate-trackers.py` => PASS

Targeted regression commands:
- `dotnet test ... --filter "DashboardRepositoryBreakdownAndAgingTests|LineOfBusinessValidationTests|DashboardEndpointTests.GetOpportunityBreakdown"` => PASS (`13/13`)  
  TRX: `engine/tests/Nebula.Tests/TestResults/f0013-code-review-targeted-backend.trx`
- `CI=true pnpm --dir experience exec vitest run src/features/opportunities/tests/OpportunitiesSummary.test.tsx` => TIMED OUT (hung in this environment; QE evidence shows this suite passing earlier)

## Findings by Severity

### Critical Issues (must fix before approval)

1. Breakdown data is eagerly fetched at mount instead of lazy per stop/toggle.
   - Location:
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:460`
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:470`
     - `experience/src/features/opportunities/hooks/useOpportunityBreakdown.ts:22`
   - Impact:
     - Violates F0013 data-loading contract (`breakdown` must be lazy, initial request budget bounded).
     - Creates request bursts on initial dashboard render and regresses performance budget in S0005.
   - Recommendation:
     - Query only the currently visible view per stage and enable request on explicit toggle/interaction (or viewport-triggered lazy activation), not on component mount.

2. Flow alternate view state is lost after chapter override, violating S0004 behavior contract.
   - Location:
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:930`
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:934`
   - Impact:
     - Switching Flow -> Friction/Outcomes -> Flow does not restore previously selected per-stop alternate view.
   - Recommendation:
     - Persist last flow-selected view per stage separately from override-mode view state and restore it when chapter returns to `flow`.

3. Triaging SLA gauge uses a hardcoded heuristic, not server SLA threshold/band data from ADR-009.
   - Location:
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:615`
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:616`
     - `experience/src/features/opportunities/types.ts:103`
   - Impact:
     - Breaks backend/frontend contract introduced by F0013 (aging endpoint SLA object).
     - Dashboard visualization can diverge from configured SLA thresholds.
   - Recommendation:
     - Extend frontend aging DTO with `sla` fields and feed gauge/callouts from `/dashboard/opportunities/aging` per-status SLA data.

4. Dashboard aggregate reads still lack query-layer role scope enforcement (cross-story correctness blocker).
   - Location:
     - `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs:61`
     - `engine/src/Nebula.Api/Endpoints/DashboardEndpoints.cs:104`
     - `engine/src/Nebula.Infrastructure/Repositories/DashboardRepository.cs:69`
     - `engine/src/Nebula.Infrastructure/Repositories/DashboardRepository.cs:331`
   - Impact:
     - Role-visibility requirements are not fully honored for internal scoped roles; behavior can leak out-of-scope aggregates.
   - Recommendation:
     - Add explicit role-scope filtering in dashboard repository queries and integration tests for cross-scope exclusion.

### High Priority (should fix)

1. Popover dialog accessibility is incomplete (focus management).
   - Location:
     - `experience/src/components/ui/Popover.tsx:48`
     - `experience/src/components/ui/Popover.tsx:179`
   - Impact:
     - Dialog opens/closes but does not move focus into the dialog, trap focus, or restore focus on close; conflicts with S0005 accessibility intent and UX rules P0.
   - Recommendation:
     - Implement focus entry, tab-loop trapping while open, and trigger-focus restoration on close.

2. Component decomposition drift: timeline/story panel logic is concentrated in very large files.
   - Location:
     - `experience/src/features/opportunities/components/StageNodeStoryPanel.tsx:1` (1056 LOC)
     - `experience/src/features/opportunities/components/ConnectedFlow.tsx:1` (447 LOC)
   - Impact:
     - Reduces maintainability and makes chart/callout behavior harder to test in isolation; deviates from F0013 planned decomposition.
   - Recommendation:
     - Extract `MiniVisualization`, `NarrativeCallout`, and stage-node subcomponents with dedicated tests per chart type/behavior.

3. Opportunity color mapping still contains hardcoded hex values and fixed palette classes.
   - Location:
     - `experience/src/features/opportunities/lib/opportunity-colors.ts:9`
     - `experience/src/features/opportunities/lib/opportunity-colors.ts:45`
   - Impact:
     - Bypasses token-driven theming objective from S0000 and weakens dark/light palette consistency.
   - Recommendation:
     - Replace hardcoded hex and raw palette classes with CSS variable-backed theme tokens for stage colors.

## Pattern Compliance
- [x] Clean architecture layers respected
- [ ] SOLID principles followed (frontend decomposition issue)
- [ ] SOLUTION-PATTERNS.md patterns applied (dashboard scope filtering gap)
- [ ] Frontend UX rule-set checks passed (popover focus-management gap)
- [x] Naming conventions consistent
- [ ] Error handling appropriate (scoped-role behavior contract not fully enforced)

## Test Quality
- Unit/integration coverage exists for many F0013 backend paths (`13/13` targeted backend pass in this gate; `40/40` in QE evidence).
- Frontend `OpportunitiesSummary` coverage exists (QE evidence: `7/7`), but this session’s direct vitest rerun timed out/hung.
- Missing/insufficient test assertions for:
  - lazy breakdown fetch/request-budget behavior
  - restoring per-stop flow alternate state after chapter overrides
  - popover focus trap/restore behavior
  - scoped internal-role aggregate filtering on dashboard endpoints

## Acceptance Criteria Mapping (Code Review Verdict)
- F0013-S0000: FAIL (hardcoded opportunity palette map still present)
- F0013-S0001: FAIL (shared blockers prevent story-level code approval)
- F0013-S0002: FAIL (decomposition and shared blockers)
- F0013-S0003: FAIL (lazy-load + SLA source-of-truth contract not met)
- F0013-S0004: FAIL (flow-view restoration regression)
- F0013-S0005: FAIL (request-budget + focus-management accessibility gaps)

## Recommendation
REJECT

## Action Items
1. Fix lazy breakdown loading strategy and cap initial dashboard request fan-out.
2. Preserve and restore per-stop flow view state across chapter overrides.
3. Wire triage SLA visualization to server-provided SLA thresholds/bands.
4. Implement scoped internal-role filtering for dashboard aggregate queries with integration tests.
5. Add popover focus entry/trap/restore behavior and tests.
6. Replace remaining hardcoded opportunity color values with theme tokens.
