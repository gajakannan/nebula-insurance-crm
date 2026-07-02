# Action Context

- DEFECT_SUMMARY: F0021 correction/redaction E2E fails because communication correction persistence returns `500 internal_error`.
- OBSERVED_BEHAVIOR: `POST /communications/{id}/corrections` with `Action=Correct` returns HTTP 500.
- EXPECTED_BEHAVIOR: Authorized correction requests should persist an append-only correction audit row, update the communication display fields, emit timeline evidence, and return the updated communication.
- REPRO_STEPS: Run `pnpm --dir experience exec playwright test tests/e2e/f0021-communications.spec.ts --config=playwright.f0021.config.ts`; scenario `corrects and redacts communication content with audit-safe read behavior` fails on correction.
- AFFECTED_PATHS: `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs`, `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs`, `engine/src/Nebula.Application/Services/CommunicationService.cs`
- AGENT_ROLES: architect, backend-developer, quality-engineer
- FEATURE_REFS: F0021
- ALLOW_FEATURE_PROPOSAL: false
- Lifecycle Authority: none

## Scope Boundary

This defect run may update the communication correction persistence path and focused test evidence only. It must not create feature evidence, write `latest-run.json`, or modify archived F0021 feature status.
