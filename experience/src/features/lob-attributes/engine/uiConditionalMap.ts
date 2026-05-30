/**
 * F0036-S0005 — declarative UI-conditional map (ADR-021 §4, PR-H1 rework).
 *
 * The PRESENTATIONAL half of conditional gating (enable/disable + required
 * marker) lives here, at the LOB-adapter layer — versioned with the engine, NOT
 * the bundle ("no bundle change"), applied generically by the engine ("no ad-hoc
 * per-field JSX"). The VALIDATION half ("mfaMaturity required when mfaEnabled")
 * stays backend-authoritative and is surfaced via `lobErrors[]` (S0003/§3).
 */

export interface UiConditional {
  /** The gated field is enabled (and shown required) only when this holds. */
  enabledWhen: { field: string; equals: unknown }
}

/** Keyed by the gated field's path (e.g. `controls.mfaMaturity`). */
export type UiConditionalMap = Record<string, UiConditional>

/** Cyber: MFA maturity is meaningful only when MFA is enabled. */
export const CYBER_UI_CONDITIONAL_MAP: UiConditionalMap = {
  'controls.mfaMaturity': { enabledWhen: { field: 'controls.mfaEnabled', equals: true } },
}

/** Reads a dotted path out of a nested value object. */
export function getAtPath(values: unknown, dotted: string): unknown {
  return dotted.split('.').reduce<unknown>((acc, key) => {
    if (acc && typeof acc === 'object' && key in (acc as Record<string, unknown>)) {
      return (acc as Record<string, unknown>)[key]
    }
    return undefined
  }, values)
}

export interface ConditionalState {
  /** Whether this field is governed by a conditional at all. */
  gated: boolean
  /** When gated: true iff the enabling condition currently holds. */
  enabled: boolean
}

/** Evaluates a field's conditional state against the current form values. */
export function evaluateConditional(
  map: UiConditionalMap | undefined,
  fieldPath: string,
  values: unknown,
): ConditionalState {
  const conditional = map?.[fieldPath]
  if (!conditional) {
    return { gated: false, enabled: true }
  }
  const actual = getAtPath(values, conditional.enabledWhen.field)
  return { gated: true, enabled: actual === conditional.enabledWhen.equals }
}
