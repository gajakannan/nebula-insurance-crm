/**
 * F0036-S0003 — Cyber client/backend parity fixture matrix.
 *
 * Each row is a payload validated by BOTH the client AJV (over `data-schema.json`)
 * and the backend. `backendErrors` are the recorded backend `(code, pointer)`
 * results, transcribed from the authoritative backend validator
 * `engine/src/Nebula.Application/Services/LobAttributeService.cs` (the "recorded
 * responses" half of the ADR-022 parity harness).
 *
 * Parity is measured on the DATA-SCHEMA layer only (`required` / `invalid_enum`
 * / `minimum`). Cross-field codes (`mfa_required_for_high_record_count`,
 * `minimum_retention_not_met`) are BACKEND-authoritative and are listed
 * separately — the client AJV must NOT emit them (ADR-021 §3).
 *
 * FOLLOW-UP: replace `backendErrors` with live-endpoint captures once the .NET
 * runtime is available (frontend-only runtime this run); the recorded values are
 * transcribed directly from the backend validator source above.
 */

export const DATA_SCHEMA_CODES = ['required', 'invalid_enum', 'minimum'] as const
export const CROSS_FIELD_CODES = ['mfa_required_for_high_record_count', 'minimum_retention_not_met'] as const

export interface BackendError {
  code: string
  pointer: string
}

export interface ParityExample {
  id: string
  description: string
  attributes: Record<string, unknown>
  /** Full recorded backend error set (data-schema + cross-field). */
  backendErrors: BackendError[]
}

const validControls = {
  mfaEnabled: true,
  mfaMaturity: 'Implemented',
  edrEnabled: true,
  backupEnabled: true,
  trainingFrequency: 'Annual',
}

export const CYBER_PARITY_EXAMPLES: ParityExample[] = [
  {
    id: 'valid-baseline',
    description: 'Fully valid Cyber attributes — no errors on either side.',
    attributes: {
      revenueBand: '10-50M',
      recordsHeld: 5000,
      controls: validControls,
      requestedLimit: { amountMinor: 500_000_000, currency: 'USD' },
      requestedRetention: { amountMinor: 10_000_000, currency: 'USD' },
    },
    backendErrors: [],
  },
  {
    id: 'missing-required-top-level',
    description: 'revenueBand and recordsHeld omitted — two data-schema required errors.',
    attributes: {
      controls: { mfaEnabled: false, edrEnabled: false, backupEnabled: false, trainingFrequency: 'Annual' },
      requestedLimit: { amountMinor: 100, currency: 'USD' },
      requestedRetention: { amountMinor: 10, currency: 'USD' },
    },
    backendErrors: [
      { code: 'required', pointer: '$.attributes.revenueBand' },
      { code: 'required', pointer: '$.attributes.recordsHeld' },
    ],
  },
  {
    id: 'invalid-enum-and-negative-minimum',
    description: 'Out-of-enum revenueBand and negative recordsHeld.',
    attributes: {
      revenueBand: 'BOGUS',
      recordsHeld: -5,
      controls: { mfaEnabled: false, edrEnabled: false, backupEnabled: false, trainingFrequency: 'Annual' },
      requestedLimit: { amountMinor: 100, currency: 'USD' },
      requestedRetention: { amountMinor: 10, currency: 'USD' },
    },
    backendErrors: [
      { code: 'invalid_enum', pointer: '$.attributes.revenueBand' },
      { code: 'minimum', pointer: '$.attributes.recordsHeld' },
    ],
  },
  {
    id: 'invalid-enum-nested-control',
    description: 'Unsupported trainingFrequency enum on a nested control.',
    attributes: {
      revenueBand: '0-10M',
      recordsHeld: 10,
      controls: { mfaEnabled: false, edrEnabled: false, backupEnabled: false, trainingFrequency: 'Weekly' },
      requestedLimit: { amountMinor: 100, currency: 'USD' },
      requestedRetention: { amountMinor: 10, currency: 'USD' },
    },
    backendErrors: [{ code: 'invalid_enum', pointer: '$.attributes.controls.trainingFrequency' }],
  },
  {
    id: 'cross-field-only-backend-authoritative',
    description:
      'Data-schema valid but recordsHeld>=1,000,000 with MFA off — backend-only cross-field error; client AJV must be clean.',
    attributes: {
      revenueBand: '250M+',
      recordsHeld: 2_000_000,
      controls: { mfaEnabled: false, edrEnabled: true, backupEnabled: true, trainingFrequency: 'Annual' },
      requestedLimit: { amountMinor: 1_000_000_000, currency: 'USD' },
      requestedRetention: { amountMinor: 50_000_000, currency: 'USD' },
    },
    backendErrors: [{ code: 'mfa_required_for_high_record_count', pointer: '$.attributes.controls.mfaEnabled' }],
  },
]
