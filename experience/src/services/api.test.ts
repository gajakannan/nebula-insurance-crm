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

describe('api multipart and binary helpers', () => {
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

  it('does not force a JSON content type for multipart requests', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ok: true }, 202))
    vi.stubGlobal('fetch', fetchMock)

    const body = new FormData()
    body.set('parentType', 'submission')

    await api.postMultipart('/documents', body)

    const [, init] = fetchMock.mock.calls[0]
    const headers = init.headers as Headers
    expect(headers.get('Content-Type')).toBeNull()
    expect(headers.get('Authorization')).toBe('Bearer test-token')
  })

  it('returns blobs for download helpers without parsing JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(new Blob(['pdf']), { status: 200 })),
    )

    await expect(api.downloadBlob('/documents/doc_1/versions/latest/binary')).resolves.toBeInstanceOf(Blob)
  })
})
