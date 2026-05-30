import type { FieldValues, UseFormReturn } from 'react-hook-form'
import type { DirtyFormRegistration } from '@/features/session-continuity'

/**
 * F0036-S0006 — RHF -> F0035 `DirtyFormRegistration` adapter (Workstream A backend).
 *
 * Flattens RHF `formState.dirtyFields` (a nested object mirroring the form, with
 * `true` at dirty leaves) into the string-path array F0035 requires — including
 * nested paths like `controls.mfaEnabled` and `requestedLimit.amountMinor`.
 */
export function flattenDirtyFields(dirty: unknown, prefix = ''): string[] {
  if (dirty === true) {
    return prefix ? [prefix] : []
  }
  if (Array.isArray(dirty)) {
    return dirty.flatMap((entry, index) =>
      flattenDirtyFields(entry, prefix ? `${prefix}.${index}` : String(index)),
    )
  }
  if (dirty && typeof dirty === 'object') {
    return Object.entries(dirty as Record<string, unknown>).flatMap(([key, val]) =>
      flattenDirtyFields(val, prefix ? `${prefix}.${key}` : key),
    )
  }
  return []
}

/** Builds the F0035 registration source from an RHF form. */
export function rhfDirtyAdapter<TValues extends FieldValues>(
  form: UseFormReturn<TValues>,
  opts: { formKey: string; route: string },
): DirtyFormRegistration<TValues> {
  return {
    formKey: opts.formKey,
    route: opts.route,
    isDirty: () => form.formState.isDirty,
    getValues: () => form.getValues(),
    getDirtyFieldPaths: () => flattenDirtyFields(form.formState.dirtyFields),
  }
}
