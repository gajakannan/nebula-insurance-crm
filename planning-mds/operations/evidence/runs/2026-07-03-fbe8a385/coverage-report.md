# Coverage Report - F0008 Broker Insights

## Verdict

PASS_WITH_WAIVER

## Captured Coverage

| Layer | Artifact | Status |
| --- | --- | --- |
| Backend focused service tests | `artifacts/test-results/7699d9ee-3196-487b-aced-1578a6fc406e/coverage.cobertura.xml` | Captured after repair |
| Frontend broker insights component tests | `artifacts/test-results/f0008-postcloseout-frontend-vitest.json` | Pass/fail captured, coverage not collected |

## Waivers

| Waiver | Owner | Rationale | Follow-up |
| --- | --- | --- | --- |
| Frontend container execution | Quality Engineer | No frontend Dockerfile or Compose service exists. | Add frontend container target if required by future harness policy. |
| Frontend coverage collection | Quality Engineer | Focused post-closeout run validated component behavior and production build; no standardized frontend coverage command was required in the current process. | Add `pnpm test:coverage` or equivalent to the frontend test contract. |

## Residual Risk

- EF migration designer/snapshot was not regenerated in this run; runtime migration discovery is repaired, but future migration diff tooling should regenerate metadata through the standard EF workflow.
- Browser E2E and accessibility scans were not part of this standalone post-closeout run.
