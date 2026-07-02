# Action Context

- DEFECT_SUMMARY: F0021 follow-up assignee selection cannot load user suggestions in the local E2E flow.
- OBSERVED_BEHAVIOR: The Add Communication dialog accepts `Sarah` in the Assignee combobox, but no `Sarah Chen` option appears and the E2E times out.
- EXPECTED_BEHAVIOR: The AssigneePicker should query the API `/users` endpoint through the local frontend dev proxy and render matching users.
- REPRO_STEPS: Run `pnpm --dir experience exec playwright test tests/e2e/f0021-communications.spec.ts --config=playwright.f0021.config.ts`; scenario `captures a communication with participant and follow-up through the UI` times out waiting for `Sarah Chen`.
- AFFECTED_PATHS: `experience/vite.config.ts`, `experience/tests/e2e/f0021-communications.spec.ts`
- AGENT_ROLES: architect, frontend-developer, quality-engineer
- FEATURE_REFS: F0021
- ALLOW_FEATURE_PROPOSAL: false
- Lifecycle Authority: none

## Scope Boundary

This defect run may update the local frontend proxy configuration and focused test evidence only. It must not create feature evidence, write `latest-run.json`, or modify archived F0021 feature status.
