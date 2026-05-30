import { useEffect, useMemo } from 'react'
import type { FieldValues, UseFormReturn } from 'react-hook-form'
import type { FormSnapshotRecord } from '@/features/session-continuity'
import { useRegisteredForm } from '@/features/forms/useRegisteredForm'
import { rhfDirtyAdapter } from './rhfDirtyAdapter'

/**
 * F0036-S0006 — mounts F0035 preservation for an engine RHF form.
 *
 * Rendered as a child ONLY when a `preserve` config is supplied, so engine forms
 * rendered outside a `DirtyFormRegistryProvider` (e.g. unit tests) never touch
 * the registry. Registers the RHF form via the shared library-agnostic helper
 * and rehydrates a prior snapshot on mount. Renders nothing.
 */
export interface PreserveConfig {
  userId: string
  formKey: string
  route: string
}

export function FormPreservation<TValues extends FieldValues>({
  form,
  preserve,
  onRestore,
}: {
  form: UseFormReturn<TValues>
  preserve: PreserveConfig
  onRestore: (record: FormSnapshotRecord<TValues>) => void
}) {
  // Read the dirty state during render so RHF tracks it and the registration
  // closures observe live values at snapshot time.
  const { isDirty, dirtyFields } = form.formState
  useEffect(() => {
    // Reference the subscribed fields; no side effect needed.
  }, [isDirty, dirtyFields])

  const registration = useMemo(
    () => rhfDirtyAdapter(form, { formKey: preserve.formKey, route: preserve.route }),
    [form, preserve.formKey, preserve.route],
  )

  useRegisteredForm<TValues>({ registration, userId: preserve.userId, onRestore })

  return null
}
