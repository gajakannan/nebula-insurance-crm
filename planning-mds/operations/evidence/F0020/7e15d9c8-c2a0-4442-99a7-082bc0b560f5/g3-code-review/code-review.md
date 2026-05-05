# G3 Code Review - F0020

Run ID: `7e15d9c8-c2a0-4442-99a7-082bc0b560f5`
Date: 2026-05-04
Reviewer: Codex feature runner

Verdict: PASS

## Findings

No critical or high severity findings remain.

Resolved finding:

- Hosted services consumed scoped document services directly and caused the rebuilt runtime container to restart during service-provider validation. Fixed by resolving scoped document services inside per-loop `IServiceScopeFactory` scopes in `QuarantinePromotionWorker` and `DocumentRetentionHostedService`.

Residual notes:

- G4 design gap repair was reviewed after implementation: sidecar schema, API DTOs, repository writes, config loader, API endpoint, frontend forms, OpenAPI, ADR, and KG mappings are aligned around pinned metadata schema versions.
- Existing nullable warnings remain in unrelated dashboard/submission/task/workflow files during `dotnet build`.
- Vite build still reports the existing bundle chunk-size warning.
- Existing Popover tests still emit React `act(...)` warnings; suite passes.

Evidence:

- `planning-mds/operations/evidence/F0020/7e15d9c8-c2a0-4442-99a7-082bc0b560f5/runtime-validation/commands.md`
