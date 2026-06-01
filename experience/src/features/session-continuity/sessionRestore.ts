export const SESSION_RESTORE_PREFIX = 'nebula.session-restore.v1'
export const SESSION_RESTORE_TTL_MS = 3_600_000
export const SESSION_RESTORE_MAX_BYTES = 262_144

export interface FormSnapshotRecord<TValues = unknown> {
  user_id: string
  route: string
  form_key: string
  form_values: TValues
  dirty_field_paths: string[]
  snapshot_timestamp: string
}

export interface SnapshotResult {
  stored: boolean
  skippedCause?: 'oversize' | 'classifier_uncertain' | 'storage_unavailable'
  formKey?: string
}

export interface ProfileCarrier {
  profile?: {
    sub?: string
    nebula_user_id?: string
    [key: string]: unknown
  }
}

export function buildRestoreKey(userId: string, formKey: string): string {
  return `${SESSION_RESTORE_PREFIX}.${userId}.${formKey}`
}

export function sanitizeReturnTo(raw: string | null): string | null {
  if (!raw) {
    return null
  }

  const trimmed = raw.trim()
  if (!trimmed || trimmed.startsWith('//')) {
    return null
  }

  try {
    const origin = window.location.origin
    const url = new URL(trimmed, origin)
    if (url.origin !== origin || url.pathname === '/login' || url.pathname === '/auth/callback') {
      return null
    }

    return `${url.pathname}${url.search}${url.hash}`
  } catch {
    return null
  }
}

export function snapshotDirtyForm(
  record: FormSnapshotRecord,
): SnapshotResult {
  const storage = safeSessionStorage()
  if (!storage) {
    return { stored: false, skippedCause: 'storage_unavailable', formKey: record.form_key }
  }

  const payload = JSON.stringify(record)
  if (new Blob([payload]).size > SESSION_RESTORE_MAX_BYTES) {
    return { stored: false, skippedCause: 'oversize', formKey: record.form_key }
  }

  try {
    storage.setItem(buildRestoreKey(record.user_id, record.form_key), payload)
    return { stored: true, formKey: record.form_key }
  } catch {
    return { stored: false, skippedCause: 'storage_unavailable', formKey: record.form_key }
  }
}

export function consumeFormSnapshot<TValues>(
  userId: string,
  formKey: string,
): FormSnapshotRecord<TValues> | null {
  const storage = safeSessionStorage()
  if (!storage) {
    return null
  }

  const key = buildRestoreKey(userId, formKey)
  const raw = storage.getItem(key)
  if (!raw) {
    return null
  }

  storage.removeItem(key)

  try {
    const record = JSON.parse(raw) as FormSnapshotRecord<TValues>
    const timestamp = Date.parse(record.snapshot_timestamp)
    if (
      record.user_id !== userId ||
      !Number.isFinite(timestamp) ||
      Date.now() - timestamp > SESSION_RESTORE_TTL_MS
    ) {
      return null
    }

    return record
  } catch {
    return null
  }
}

export function listFormSnapshotKeysForUser(
  userId: string,
  formKeyPrefix = '',
): string[] {
  const storage = safeSessionStorage()
  if (!storage) {
    return []
  }

  const storageKeyPrefix = `${SESSION_RESTORE_PREFIX}.${userId}.`
  const matches: string[] = []
  const invalidKeys: string[] = []

  for (let index = 0; index < storage.length; index += 1) {
    const storageKey = storage.key(index)
    if (!storageKey?.startsWith(storageKeyPrefix)) {
      continue
    }

    const formKey = storageKey.slice(storageKeyPrefix.length)
    if (formKeyPrefix && !formKey.startsWith(formKeyPrefix)) {
      continue
    }

    const raw = storage.getItem(storageKey)
    if (!raw) {
      continue
    }

    try {
      const record = JSON.parse(raw) as FormSnapshotRecord
      const timestamp = Date.parse(record.snapshot_timestamp)
      if (
        record.user_id !== userId ||
        record.form_key !== formKey ||
        !Number.isFinite(timestamp) ||
        Date.now() - timestamp > SESSION_RESTORE_TTL_MS
      ) {
        invalidKeys.push(storageKey)
        continue
      }

      matches.push(formKey)
    } catch {
      invalidKeys.push(storageKey)
    }
  }

  for (const storageKey of invalidKeys) {
    storage.removeItem(storageKey)
  }

  return matches
}

export function clearSnapshotsForUser(userId: string): void {
  removeSnapshotKeys((key) => key.startsWith(`${SESSION_RESTORE_PREFIX}.${userId}.`))
}

export function clearSnapshotsForOtherUsers(currentUserId: string): void {
  removeSnapshotKeys((key) => {
    if (!key.startsWith(`${SESSION_RESTORE_PREFIX}.`)) {
      return false
    }

    return !key.startsWith(`${SESSION_RESTORE_PREFIX}.${currentUserId}.`)
  })
}

export function readSessionUserId(user: ProfileCarrier | null | undefined): string | null {
  const nebulaUserId = user?.profile?.nebula_user_id
  if (typeof nebulaUserId === 'string' && nebulaUserId.trim()) {
    return nebulaUserId
  }

  const sub = user?.profile?.sub
  return typeof sub === 'string' && sub.trim() ? sub : null
}

function removeSnapshotKeys(shouldRemove: (key: string) => boolean): void {
  const storage = safeSessionStorage()
  if (!storage) {
    return
  }

  const keys: string[] = []
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index)
    if (key && shouldRemove(key)) {
      keys.push(key)
    }
  }

  for (const key of keys) {
    storage.removeItem(key)
  }
}

function safeSessionStorage(): Storage | null {
  try {
    return globalThis.sessionStorage ?? null
  } catch {
    return null
  }
}
