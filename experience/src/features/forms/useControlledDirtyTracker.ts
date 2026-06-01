import { useMemo } from 'react'
import type { DirtyFormRegistration } from '@/features/session-continuity'

/**
 * F0036-S0007 — controlled-form dirty-tracker (Workstream B backend).
 *
 * Produces the F0035 `DirtyFormRegistration` triple (`isDirty`/`getValues`/
 * `getDirtyFieldPaths`) for a plain controlled form by deep-diffing the current
 * values against a stable initial-values reference. No field-state library is
 * introduced — the CRUD forms stay controlled. Library-agnostic: the result is
 * the same shape `rhfDirtyAdapter` produces, so both register through the shared
 * `useRegisteredForm` helper.
 */

export interface ControlledDirtyTrackerOptions {
  /** Paths excluded from getValues() and getDirtyFieldPaths() (default-deny for sensitive fields). */
  sensitiveFieldPaths?: string[]
  /** Treat arrays as ordered (default: order-insensitive — a reorder of identical contents is not dirty). */
  arrayOrderSensitive?: boolean
}

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return v !== null && typeof v === 'object' && !Array.isArray(v) && !(v instanceof Date)
}

export function deepEqual(a: unknown, b: unknown, arrayOrderSensitive = false): boolean {
  if (a === b) return true
  if (a instanceof Date || b instanceof Date) {
    return a instanceof Date && b instanceof Date && a.getTime() === b.getTime()
  }
  // null and undefined (and absent keys) are all treated as "absent" -> equal.
  if (a == null || b == null) return a == null && b == null
  if (typeof a === 'number' && typeof b === 'number') {
    return a === b || (Number.isNaN(a) && Number.isNaN(b))
  }
  if (typeof a !== typeof b) return false
  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false
    if (arrayOrderSensitive) {
      return a.every((x, i) => deepEqual(x, b[i], arrayOrderSensitive))
    }
    const used = new Array(b.length).fill(false)
    return a.every((x) => {
      const idx = b.findIndex((y, i) => !used[i] && deepEqual(x, y, arrayOrderSensitive))
      if (idx === -1) return false
      used[idx] = true
      return true
    })
  }
  if (isPlainObject(a) && isPlainObject(b)) {
    const keys = new Set([...Object.keys(a), ...Object.keys(b)])
    for (const key of keys) {
      if (!deepEqual(a[key], b[key], arrayOrderSensitive)) return false
    }
    return true
  }
  return false
}

export function diffPaths(
  current: unknown,
  initial: unknown,
  arrayOrderSensitive = false,
  prefix = '',
): string[] {
  if (deepEqual(current, initial, arrayOrderSensitive)) return []
  if (isPlainObject(current) && isPlainObject(initial)) {
    const keys = new Set([...Object.keys(current), ...Object.keys(initial)])
    return [...keys].flatMap((key) =>
      diffPaths(current[key], initial[key], arrayOrderSensitive, prefix ? `${prefix}.${key}` : key),
    )
  }
  if (Array.isArray(current) && Array.isArray(initial) && arrayOrderSensitive) {
    const len = Math.max(current.length, initial.length)
    const paths: string[] = []
    for (let i = 0; i < len; i += 1) {
      paths.push(...diffPaths(current[i], initial[i], arrayOrderSensitive, `${prefix}.${i}`))
    }
    return paths
  }
  return prefix ? [prefix] : []
}

function omitPaths<T>(values: T, paths: string[]): T {
  if (paths.length === 0) return values
  const clone = structuredClone(values) as Record<string, unknown>
  for (const path of paths) {
    const segments = path.split('.')
    let node: Record<string, unknown> | undefined = clone
    for (let i = 0; i < segments.length - 1; i += 1) {
      const next: unknown = node?.[segments[i]]
      node = isPlainObject(next) ? next : undefined
      if (!node) break
    }
    if (node) delete node[segments[segments.length - 1]]
  }
  return clone as T
}

export function useControlledDirtyTracker<T>(
  values: T,
  initialValues: T,
  options?: ControlledDirtyTrackerOptions,
): Pick<DirtyFormRegistration<T>, 'isDirty' | 'getValues' | 'getDirtyFieldPaths'> {
  const sensitive = options?.sensitiveFieldPaths ?? []
  const arrayOrderSensitive = options?.arrayOrderSensitive ?? false
  // Re-derive only when values/initial/options change; the closures read the
  // values captured at that point (the latest render), which is what the
  // registry snapshots.
  const sensitiveKey = JSON.stringify(sensitive)
  return useMemo(() => {
    const snapshotValues = omitPaths(values, sensitive)
    const snapshotInitialValues = omitPaths(initialValues, sensitive)
    const dirty = !deepEqual(snapshotValues, snapshotInitialValues, arrayOrderSensitive)
    return {
      isDirty: () => dirty,
      getValues: () => snapshotValues,
      getDirtyFieldPaths: () =>
        diffPaths(snapshotValues, snapshotInitialValues, arrayOrderSensitive).filter(
          (p) => !sensitive.some((s) => p === s || p.startsWith(`${s}.`)),
        ),
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [values, initialValues, arrayOrderSensitive, sensitiveKey])
}
