# F0025 Polish Validation - 2026-07-07

## Scope

PRD-aligned polish pass for F0025 after functional validation.

The pass stayed inside the PRD boundaries:

- Improve commission workspace readability for policy, producer, broker, and carrier/market context.
- Improve commission detail clarity for saved schedules and saved producer splits.
- Do not add accounting, payment, reconciliation, payout, tax, statement, or GL export scope.

## Changes

- Added optional friendly-label fields to expected commission DTOs:
  - `policyNumber`
  - `accountDisplayName`
  - `carrierMarketName`
  - `brokerName`
  - `producerUserId`
  - `producerDisplayName`
  - `lineOfBusiness`
- Added optional `producerDisplayName` to producer split participant DTOs.
- Hydrated labels from existing policy, broker, carrier, and user-profile records.
- Updated revenue attribution rollups to resolve group labels for producer, broker, territory, and carrier/market groupings.
- Updated the Commissions list UI to show policy/account/carrier context instead of UUID-first rows.
- Updated the Commission Detail UI to show saved schedule and saved split summaries above the create-new forms.
- Kept raw IDs available as secondary detail for traceability.

## Validation

- Frontend F0025 test: PASS, 2 passed.
- Frontend theme guard: PASS.
- Backend build: PASS, 0 errors.
- Focused backend F0025/Casbin tests: PASS, 22 passed.
- API health after rebuild: PASS, `GET /healthz` returned `200 Healthy`.
- Live expected commission list through Vite/API returned policy numbers, account names, carrier names, and broker names.
- Live revenue rollups returned producer display names.
- Live commission detail returned schedule, split, adjustment, and split participant display name.

## Remaining Non-Blocking Notes

- Policy-level producer may still be null on some demo/source policies; split participant names are shown where split data exists.
- Existing unrelated nullable warnings remain in dashboard/task/workflow test/build areas.
