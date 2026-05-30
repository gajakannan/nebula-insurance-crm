import type { WidgetName, WidgetOption } from './types'
import { UnknownWidgetOptionError } from './widgetRegistry'

/**
 * Option derivation for `select` / `multi-select` widgets.
 *
 * Per ADR-021, widgets do not invent options — the option list derives from the
 * data-schema `enum` for that field. Any configured option that is not present
 * in the enum fails closed (developer-visible error), mirroring backend
 * "unknown option" bundle-activation governance.
 */

/** Builds the option list from a data-schema `enum`, applying optional labels. */
export function deriveOptions(
  enumValues: readonly string[],
  labels?: Record<string, string>,
): WidgetOption[] {
  return enumValues.map((value) => ({ value, label: labels?.[value] ?? value }))
}

/**
 * Fails closed when a configured option for a select-family widget is not in the
 * field's data-schema enum. Returns the validated options on success.
 */
export function assertOptionsSubsetOfEnum(
  widgetName: WidgetName,
  configuredOptions: readonly WidgetOption[],
  enumValues: readonly string[],
): WidgetOption[] {
  const allowed = new Set(enumValues)
  for (const option of configuredOptions) {
    if (!allowed.has(option.value)) {
      throw new UnknownWidgetOptionError(widgetName, option.value)
    }
  }
  return [...configuredOptions]
}
