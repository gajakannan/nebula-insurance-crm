# Test Execution Report — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Commands Executed

```text
- dotnet build engine/src/Nebula.Api/Nebula.Api.csproj -> passed with warnings
- pnpm --dir experience build -> passed with warnings
- dotnet build engine/tests/Nebula.Tests/Nebula.Tests.csproj -> failed once for missing fake method, then passed after code fix
- dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build -> sandbox denied once, then passed outside sandbox
- pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx -> failed once for missing localStorage in test setup, then passed after setup guard
- pnpm --dir experience lint:theme -> passed
```

## Pass/Fail Counts

| Lane | Total | Pass | Fail | Skip | Retries |
|------|------:|-----:|-----:|-----:|--------:|
| Unit (engine targeted) | 3 | 3 | 0 | 0 | 1 |
| Component (experience targeted) | 1 | 1 | 0 | 0 | 1 |
| Build (API) | 1 | 1 | 0 | 0 | 0 |
| Build (experience) | 1 | 1 | 0 | 0 | 0 |
| Theme guard | 1 | 1 | 0 | 0 | 0 |

## Skipped Tests And Rationale

- Full backend integration suite: deferred to G6/QE because G2 self-review is validating the implemented slice and immediate regressions.
- E2E browser flow: deferred to G6/QE after endpoint integration fixtures are in place.

## Raw Test Artifact Paths

- `engine/tests/Nebula.Tests/TestResults/e59f00e1-1d0f-4371-9b3d-01942eee61aa/coverage.cobertura.xml`

## Failed / Retried Command History

- `dotnet test ... --filter OutboundDocumentGenerationServiceTests --no-build` first failed because VSTest could not bind a local socket inside the sandbox. Same command passed outside sandbox with no code changes.
- `pnpm --dir experience test src/features/documents/tests/ParentDocumentsPanel.test.tsx` first failed in `src/test-setup.ts` because `window.localStorage` was unavailable in this runtime. A narrow optional-chain guard fixed the setup and the same test passed.

## AC Coverage Result

- S0001: covered by unpublished-template unit test.
- S0002: covered by preview unit test.
- S0003: covered by issue unit test and document-panel component smoke.
- S0004: partial; implementation exists, dedicated test recommended before G6.
- S0005: partial; shared renderer boundary and family typing implemented, production renderer hardening recommended.

## Frontend Feature Notes

- `GenerateDocumentPanel.tsx` is mounted inside `ParentDocumentsPanel.tsx`.
- `GeneratedArtifactProvenance.tsx` is mounted inside `DocumentDetailView.tsx`.
- `ParentDocumentsPanel.test.tsx` passed after the new panel mounted, proving the existing parent document flow still renders with F0027 controls present.
- `pnpm --dir experience build` and `pnpm --dir experience lint:theme` both passed.

## Recommendations

- [medium] Add regenerate and endpoint integration tests before G6 — owner: Quality Engineer; follow-up: F0027-G6-api-tests

## Result

PASS WITH RECOMMENDATIONS
