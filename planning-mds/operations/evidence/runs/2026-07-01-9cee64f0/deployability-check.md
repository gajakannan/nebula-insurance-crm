# Deployability Check — F0021 Communication Hub And Activity Capture

**Result:** PASS WITH RECOMMENDATIONS

## Runtime Impact

F0021 is runtime-bearing and deployment-config impacting because it adds EF persistence entities and a database migration for communication events, links, participants, corrections, and follow-up task links.

## Migration Evidence

- Added migration `20260701140200_F0021CommunicationActivityCapture`.
- Rebuilt the API image successfully.
- Restarted the API service successfully after legacy F0019 migration drift repair.
- Verified F0021 migration history and communication tables in PostgreSQL.

## Service Health

- API `/healthz` passed after restart.
- Docker Compose reported API, database, Authentik server, and Authentik worker running/healthy at the checkpoint.
- Vite frontend continued serving on `127.0.0.1:5173` during implementation checks.

## Risks

- Host MSBuild may fail in restricted sandbox execution due named-pipe permissions; escalated host execution is currently required for targeted .NET tests.
- Existing `Microsoft.OpenApi 2.0.0` advisory remains and must be handled before final security signoff.
- The F0019 idempotency patch should be reviewed carefully because it changes legacy migration behavior to accommodate current local drift.

## Recommendation

Proceed to review gates with DevOps awareness. No deployment blocker remains for the local F0021 vertical slice after the migration recovery.

## Recommendations

- [medium] Review deployment migration behavior for the hand-authored F0021 migration and F0019 drift repair before final signoff - owner: DevOps; follow-up: Carry into G3/G5 review.
- [medium] Resolve or formally accept the existing `Microsoft.OpenApi` advisory before security signoff - owner: Security Reviewer; follow-up: Carry into G3 security review.
