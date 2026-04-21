import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const authMocks = vi.hoisted(() => ({
  emitAuthEvent: vi.fn(),
  getUser: vi.fn(),
}))

vi.mock('./dev-auth', () => ({
  getDevToken: vi.fn(),
}))

vi.mock('@/features/auth/authEvents', () => ({
  emitAuthEvent: authMocks.emitAuthEvent,
}))

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: authMocks.getUser,
  },
}))

import { ApiError, api } from './api'

function jsonResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('api.delete', () => {
  beforeEach(() => {
    authMocks.emitAuthEvent.mockReset()
    authMocks.getUser.mockReset()
    authMocks.getUser.mockResolvedValue({
      access_token: 'test-token',
      expired: false,
    })
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('stays pending after a 401 triggers session teardown', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ code: 'unauthorized' }, 401)),
    )

    const pendingDelete = api.delete('/tasks/task-123').then(
      () => 'resolved',
      () => 'rejected',
    )

    const outcome = await Promise.race([
      pendingDelete,
      new Promise<string>((resolve) => setTimeout(() => resolve('pending'), 20)),
    ])

    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith('session_expired')
    expect(outcome).toBe('pending')
  })

  it('stays pending after broker scope failure triggers redirect flow', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          jsonResponse({ code: 'broker_scope_unresolvable' }, 403),
        ),
    )

    const pendingDelete = api.delete('/tasks/task-123').then(
      () => 'resolved',
      () => 'rejected',
    )

    const outcome = await Promise.race([
      pendingDelete,
      new Promise<string>((resolve) => setTimeout(() => resolve('pending'), 20)),
    ])

    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'broker_scope_unresolvable',
    )
    expect(outcome).toBe('pending')
  })

  it('throws ApiError for non-auth delete failures', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({ code: 'not_found', title: 'Not Found' }, 404),
      ),
    )

    await expect(api.delete('/tasks/task-123')).rejects.toBeInstanceOf(ApiError)
  })
})
