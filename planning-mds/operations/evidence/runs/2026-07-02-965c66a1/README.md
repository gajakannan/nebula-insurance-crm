# Defect Run 2026-07-02-965c66a1

## Run Summary

- Defect summary: F0021 contextual communication panels show `Unable to load communications.`
- Observed behavior: Account, Broker, Policy, and Renewal communication panels render a generic load error.
- Expected behavior: Communication history requests are proxied to the API and empty records show the F0021 empty state.
- Feature references: F0021, F0009.

## Status

Fixed and validated.

## Evidence Index

- `action-context.md`
- `artifact-trace.md`
- `gate-decisions.md`
- `commands.log`
- `lifecycle-gates.log`
- `architect-analysis.md`
- `frontend-fix-report.md`
- `quality-report.md`
- `artifacts/`

## Validation Summary

- Direct proxy check now reaches Kestrel and returns `application/problem+json` for unauthenticated direct curl instead of Vite HTML.
- Browser app-path smoke passed: `/communications` returned `application/json`, the panel did not show the load error, and the F0021 empty state rendered.
- `pnpm --dir experience test src/features/communications/components/__tests__/CommunicationPanel.test.tsx` passed: 2 tests.
- `pnpm --dir experience lint` passed with 6 pre-existing warnings.
- `pnpm --dir experience lint:theme` passed.
- `pnpm --dir experience build` passed with the existing large chunk warning.

## Open Follow-ups

- Existing lint warning in `CommunicationPanel.tsx` remains a known non-blocking cleanup from the F0021 closeout; this defect did not change component logic.
