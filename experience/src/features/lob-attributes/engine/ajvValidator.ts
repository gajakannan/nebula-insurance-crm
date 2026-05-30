import Ajv2020, { type ErrorObject, type ValidateFunction } from 'ajv/dist/2020'
import addFormats from 'ajv-formats'

/**
 * F0036-S0003 — client AJV validation over the bundle `data-schema.json`.
 *
 * The client validates the DATA-SCHEMA layer only (types, enums, minimums,
 * required). Cross-field rules (`mfa_required_for_high_record_count`,
 * `minimum_retention_not_met`) are BACKEND-authoritative and surfaced via
 * `lobErrors[]` — the client keeps no authoritative duplicate (ADR-021 §3).
 *
 * Errors are normalized to the backend's `(code, pointer)` contract so parity
 * can be measured by multiset equality (ADR-022).
 */

export interface NormalizedError {
  /** Backend-aligned code: `required` | `invalid_enum` | `minimum` | <ajv keyword>. */
  code: string
  /** Backend-aligned pointer, e.g. `$.attributes.controls.mfaEnabled`. */
  pointer: string
  /** Raw AJV keyword (kept for ADR-022 `(code, pointer, keyword, schemaPath)`). */
  keyword: string
  schemaPath: string
  /** Human-readable message for the inline error slot (frontend owns copy). */
  message: string
}

// AJV keyword -> backend code (ADR-022 normalization for the whitelisted profile).
const KEYWORD_TO_CODE: Record<string, string> = {
  required: 'required',
  enum: 'invalid_enum',
  minimum: 'minimum',
  exclusiveMinimum: 'minimum',
}

function instancePathToDotted(instancePath: string): string {
  // AJV instancePath is like `/controls/mfaEnabled`; backend uses dotted paths.
  return instancePath.replace(/^\//, '').split('/').filter(Boolean).join('.')
}

function buildPointer(error: ErrorObject): string {
  const dotted = instancePathToDotted(error.instancePath)
  let path = dotted
  if (error.keyword === 'required') {
    const missing = (error.params as { missingProperty?: string }).missingProperty
    path = dotted ? `${dotted}.${missing}` : String(missing)
  }
  return path ? `$.attributes.${path}` : '$.attributes'
}

function friendlyMessage(error: ErrorObject): string {
  switch (error.keyword) {
    case 'required':
      return 'This field is required.'
    case 'enum':
      return 'Select one of the allowed values.'
    case 'minimum':
      return `Must be greater than or equal to ${(error.params as { limit?: number }).limit ?? 0}.`
    default:
      return error.message ?? 'Invalid value.'
  }
}

export function normalizeAjvError(error: ErrorObject): NormalizedError {
  return {
    code: KEYWORD_TO_CODE[error.keyword] ?? error.keyword,
    pointer: buildPointer(error),
    keyword: error.keyword,
    schemaPath: error.schemaPath,
    message: friendlyMessage(error),
  }
}

export interface CyberValidator {
  /** Returns the normalized data-schema errors for `attributes` (empty = valid). */
  validate(attributes: unknown): NormalizedError[]
  raw: ValidateFunction
}

export function createDataSchemaValidator(dataSchema: unknown): CyberValidator {
  const ajv = new Ajv2020({ allErrors: true, strict: false })
  addFormats(ajv)
  const raw = ajv.compile(dataSchema as object)

  return {
    raw,
    validate(attributes: unknown): NormalizedError[] {
      const ok = raw(attributes)
      if (ok || !raw.errors) {
        return []
      }
      return raw.errors.map(normalizeAjvError)
    },
  }
}

/** Multiset of `(code, pointer)` strings — the parity comparison key (ADR-022). */
export function parityKeySet(errors: NormalizedError[]): string[] {
  return errors.map((e) => `${e.code}@${e.pointer}`).sort()
}
