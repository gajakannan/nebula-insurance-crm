import { beforeEach, describe, expect, it } from 'vitest'
import {
  buildRestoreKey,
  clearSnapshotsForOtherUsers,
  clearSnapshotsForUser,
  consumeFormSnapshot,
  listFormSnapshotKeysForUser,
  sanitizeReturnTo,
  snapshotDirtyForm,
} from '../sessionRestore'

describe('session restore helpers', () => {
  beforeEach(() => {
    window.sessionStorage.clear()
    window.history.replaceState({}, '', '/dashboard')
  })

  it('accepts same-origin return paths and rejects unsafe destinations', () => {
    expect(sanitizeReturnTo('/policies/pol-1?tab=activity')).toBe(
      '/policies/pol-1?tab=activity',
    )
    expect(sanitizeReturnTo('https://evil.example/phish')).toBeNull()
    expect(sanitizeReturnTo('//evil.example/phish')).toBeNull()
    expect(sanitizeReturnTo('/login')).toBeNull()
    expect(sanitizeReturnTo('/auth/callback?code=abc')).toBeNull()
  })

  it('stores and consumes a same-user dirty form snapshot once', () => {
    const result = snapshotDirtyForm({
      user_id: 'user-1',
      route: '/policies/pol-1',
      form_key: 'policy-edit',
      form_values: { premium: 1200 },
      dirty_field_paths: ['premium'],
      snapshot_timestamp: new Date().toISOString(),
    })

    expect(result).toEqual({ stored: true, formKey: 'policy-edit' })
    expect(consumeFormSnapshot('user-1', 'policy-edit')).toMatchObject({
      form_values: { premium: 1200 },
      dirty_field_paths: ['premium'],
    })
    expect(consumeFormSnapshot('user-1', 'policy-edit')).toBeNull()
  })

  it('lists valid same-user snapshot form keys by prefix without consuming them', () => {
    snapshotDirtyForm({
      user_id: 'user-1',
      route: '/brokers/b1',
      form_key: 'contact:b1:c1',
      form_values: { role: 'updated' },
      dirty_field_paths: ['role'],
      snapshot_timestamp: new Date().toISOString(),
    })
    snapshotDirtyForm({
      user_id: 'user-1',
      route: '/brokers/b1',
      form_key: 'broker:b1',
      form_values: { legalName: 'Broker' },
      dirty_field_paths: ['legalName'],
      snapshot_timestamp: new Date().toISOString(),
    })

    expect(listFormSnapshotKeysForUser('user-1', 'contact:b1:')).toEqual(['contact:b1:c1'])
    expect(consumeFormSnapshot('user-1', 'contact:b1:c1')).toMatchObject({
      form_values: { role: 'updated' },
    })
  })

  it('clears snapshots by current user and by other users', () => {
    window.sessionStorage.setItem(buildRestoreKey('user-1', 'a'), '{}')
    window.sessionStorage.setItem(buildRestoreKey('user-2', 'b'), '{}')

    clearSnapshotsForOtherUsers('user-1')

    expect(window.sessionStorage.getItem(buildRestoreKey('user-1', 'a'))).toBe('{}')
    expect(window.sessionStorage.getItem(buildRestoreKey('user-2', 'b'))).toBeNull()

    clearSnapshotsForUser('user-1')
    expect(window.sessionStorage.length).toBe(0)
  })
})
