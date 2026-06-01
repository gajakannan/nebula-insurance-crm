import { useContext, useEffect, useMemo, useRef, useState } from 'react'
import {
  DirtyFormRegistryContext,
  consumeFormSnapshot,
  type DirtyFormRegistration,
  type FormSnapshotRecord,
} from '@/features/session-continuity'

/**
 * F0036-S0006/S0007 — the library-agnostic shared registration helper.
 *
 * Both workstreams register through this one path: it (1) registers a
 * `DirtyFormRegistration` source with the F0035 dirty-form registry so the
 * form's dirty values snapshot on a forced re-auth, and (2) on mount consumes
 * any prior snapshot via `consumeFormSnapshot` and hands it to `onRestore` for
 * rehydration. The source can come from either backend — an RHF adapter
 * (Workstream A) or the controlled-form dirty-tracker (Workstream B) — so the
 * registry contract never changes between them.
 *
 * It reads the registry from optional context, so a form rendered WITHOUT a
 * `DirtyFormRegistryProvider` degrades gracefully (no crash, just no
 * preservation). It never auto-replays a mutation (F0035 mandate): it only
 * rehydrates values; the user re-saves explicitly.
 */
export interface UseRegisteredFormResult {
  restored: boolean
}

export function useRegisteredForm<TValues>(opts: {
  registration: DirtyFormRegistration<TValues>
  userId: string | null | undefined
  onRestore: (record: FormSnapshotRecord<TValues>) => void
  restoreFormKeys?: string[]
  enabled?: boolean
}): UseRegisteredFormResult {
  const { registration, userId, onRestore, restoreFormKeys, enabled = true } = opts
  const registry = useContext(DirtyFormRegistryContext)

  // Keep a live ref to the latest registration and register a STABLE wrapper
  // once per (provider, form_key, route). This avoids re-registering on every
  // render/keystroke while still snapshotting the latest values.
  const registrationRef = useRef(registration)
  registrationRef.current = registration
  const stableRef = useRef<DirtyFormRegistration<TValues> | null>(null)
  if (stableRef.current === null) {
    stableRef.current = {
      formKey: registration.formKey,
      route: registration.route,
      isDirty: () => registrationRef.current.isDirty(),
      getValues: () => registrationRef.current.getValues(),
      getDirtyFieldPaths: () => registrationRef.current.getDirtyFieldPaths(),
    }
  }

  // Register with F0035 when a provider is present (snapshot-on-forced-re-auth).
  useEffect(() => {
    if (!enabled || !registry || !stableRef.current) return undefined
    stableRef.current.formKey = registration.formKey
    stableRef.current.route = registration.route
    return registry.register(stableRef.current)
  }, [enabled, registry, registration.formKey, registration.route])

  const consumedKeysRef = useRef(new Set<string>())
  const [restored, setRestored] = useState(false)
  const onRestoreRef = useRef(onRestore)
  onRestoreRef.current = onRestore
  const restoreKeys = useMemo(
    () => restoreFormKeys ?? [registration.formKey],
    [registration.formKey, restoreFormKeys],
  )

  // Rehydrate at most once per (userId, form_key). Modal parents may open a
  // restored form after the first render, so form-key changes get their own try.
  useEffect(() => {
    if (!enabled || !userId) {
      return
    }

    for (const formKey of restoreKeys) {
      if (consumedKeysRef.current.has(formKey)) {
        continue
      }

      consumedKeysRef.current.add(formKey)
      const record = consumeFormSnapshot<TValues>(userId, formKey)
      if (record) {
        onRestoreRef.current(record)
        setRestored(true)
        break
      }
    }
  }, [enabled, userId, restoreKeys])

  return { restored }
}
