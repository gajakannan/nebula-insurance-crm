# Test Plan — F0021 Communication Hub And Activity Capture

**Result:** PASS

## Scope

Validate the MVP vertical slice for structured communication capture and contextual visibility across backend and frontend surfaces.

## Backend Coverage

- Validate create request rules for communication type, exactly one primary link, participants, email-reference message IDs, and follow-up task payloads.
- Validate correction rules for correction/redaction actions and required corrected fields.
- Validate API build and runtime migration through Docker because host MSBuild is restricted in the sandbox.
- Validate runtime persistence by checking communication tables and migration history in PostgreSQL.

## Frontend Coverage

- Validate contextual panel empty-state rendering.
- Validate structured create payload shape with primary record link, participant, notes, and follow-up task linkage.
- Validate production TypeScript/Vite build.

## Regression Focus

- Preserve existing timeline/task integration contracts.
- Ensure follow-up task linkage uses the same linked entity context as the communication event.
- Ensure controlled form state is not reset when toggling follow-up task controls.

## Deferred Full-Suite Coverage

Full repo-wide backend and frontend suites are deferred to later review gates because the current stage requires focused evidence and the host .NET test path needs escalation. Targeted tests and builds are sufficient for G2 self-review.
