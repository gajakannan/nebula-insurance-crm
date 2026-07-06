import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { resetRenewalStateForTests } from '@/features/session-continuity/sessionRenewal'

const authMocks = vi.hoisted(() => ({
  emitAuthEvent: vi.fn(),
  getUser: vi.fn(),
  signinSilent: vi.fn(),
}))

const telemetryMocks = vi.hoisted(() => ({
  buildSessionContinuityEvent: vi.fn((user, eventName, payload) => {
    if (!user?.profile?.sub || !user?.profile?.sid) {
      return null
    }

    return {
      event_name: eventName,
      event_version: 1,
      timestamp: '2026-05-24T12:00:00.000Z',
      user_id: user.profile.sub,
      session_id: user.profile.sid,
      payload,
    }
  }),
  emitSessionContinuityEvent: vi.fn(),
  persistFailureClassEvent: vi.fn(),
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
    signinSilent: authMocks.signinSilent,
  },
}))

vi.mock('@/features/session-continuity/sessionTelemetry', () => ({
  buildSessionContinuityEvent: telemetryMocks.buildSessionContinuityEvent,
  emitSessionContinuityEvent: telemetryMocks.emitSessionContinuityEvent,
}))

vi.mock('@/features/session-continuity/deferredTelemetryBuffer', () => ({
  persistFailureClassEvent: telemetryMocks.persistFailureClassEvent,
}))

import { ApiError, MutationRetryRequiredError, api } from './api'
import { getDevToken } from './dev-auth'

const activeUser = {
  access_token: 'stale-token',
  expired: false,
  profile: {
    sub: '11111111-1111-1111-1111-111111111111',
    sid: 'session-1',
  },
}

const expiredUser = {
  access_token: 'expired-token',
  expired: true,
  profile: {
    sub: '11111111-1111-1111-1111-111111111111',
    sid: 'session-1',
  },
}

const renewedUser = {
  access_token: 'renewed-token',
  expired: false,
  profile: {
    sub: '11111111-1111-1111-1111-111111111111',
    sid: 'session-1',
  },
}

function jsonResponse(
  body: unknown,
  status: number,
  headers?: Record<string, string>,
): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  })
}

function tokenExpiredResponse(): Response {
  return jsonResponse(
    {
      type: 'https://nebula.local/problems/auth/token-expired',
      code: 'token_expired',
      title: 'Token expired',
    },
    401,
    { 'WWW-Authenticate': 'Bearer error="invalid_token", error_description="token expired"' },
  )
}

async function pendingOutcome<T>(promise: Promise<T>): Promise<string> {
  return Promise.race([
    promise.then(
      () => 'resolved',
      () => 'rejected',
    ),
    new Promise<string>((resolve) => setTimeout(() => resolve('pending'), 20)),
  ])
}

describe('api session continuity handling', () => {
  beforeEach(() => {
    resetRenewalStateForTests()
    authMocks.emitAuthEvent.mockReset()
    authMocks.getUser.mockReset()
    authMocks.signinSilent.mockReset()
    telemetryMocks.buildSessionContinuityEvent.mockClear()
    telemetryMocks.emitSessionContinuityEvent.mockClear()
    telemetryMocks.persistFailureClassEvent.mockClear()
    authMocks.getUser.mockResolvedValue(activeUser)
    authMocks.signinSilent.mockResolvedValue(renewedUser)
    vi.mocked(getDevToken).mockResolvedValue('dev-token')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renews once and retries expired GET requests with the new token', async () => {
    const fetchMock = vi.fn(async (_url: string | URL | Request, init?: RequestInit) => {
      const headers = init?.headers as Headers
      if (headers.get('Authorization') === 'Bearer stale-token') {
        return tokenExpiredResponse()
      }

      return jsonResponse({ ok: true }, 200)
    })
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.get('/tasks/task-123?include=assignee')).resolves.toEqual({ ok: true })

    expect(authMocks.signinSilent).toHaveBeenCalledWith({
      silentRequestTimeoutInSeconds: 10,
    })
    expect(fetchMock).toHaveBeenCalledTimes(2)
    const [, retryInit] = fetchMock.mock.calls[1]
    expect((retryInit?.headers as Headers).get('Authorization')).toBe('Bearer renewed-token')
    expect(telemetryMocks.emitSessionContinuityEvent).toHaveBeenCalledWith(
      expect.objectContaining({
        event_name: 'silent-renewal-success',
        payload: expect.objectContaining({ coalesced_request_count: 1 }),
      }),
    )
  })

  it('renews locally expired GET requests before dispatching the request', async () => {
    authMocks.getUser
      .mockResolvedValueOnce(expiredUser)
      .mockResolvedValue(renewedUser)
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ok: true }, 200))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.get('/tasks/task-123?include=assignee')).resolves.toEqual({ ok: true })

    expect(authMocks.signinSilent).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [, init] = fetchMock.mock.calls[0]
    expect((init?.headers as Headers).get('Authorization')).toBe('Bearer renewed-token')
    expect(authMocks.emitAuthEvent).not.toHaveBeenCalledWith('session_expired')
  })

  it('renews locally expired mutations but does not dispatch them automatically', async () => {
    authMocks.getUser
      .mockResolvedValueOnce(expiredUser)
      .mockResolvedValue(renewedUser)
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ok: true }, 200))
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.post('/tasks/task-123', { name: 'Updated' })).rejects.toBeInstanceOf(
      MutationRetryRequiredError,
    )

    expect(authMocks.signinSilent).toHaveBeenCalledTimes(1)
    expect(fetchMock).not.toHaveBeenCalled()
    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'mutation_retry_required',
      { endpointRoute: '/tasks/task-123', method: 'POST' },
    )
  })

  it('coalesces concurrent expired GET renewals into one signinSilent call', async () => {
    let resolveRenewal: (value: typeof renewedUser) => void = () => undefined
    authMocks.signinSilent.mockReturnValue(
      new Promise((resolve) => {
        resolveRenewal = resolve
      }),
    )
    const fetchMock = vi.fn(async (_url: string | URL | Request, init?: RequestInit) => {
      const headers = init?.headers as Headers
      return headers.get('Authorization') === 'Bearer stale-token'
        ? tokenExpiredResponse()
        : jsonResponse({ ok: true }, 200)
    })
    vi.stubGlobal('fetch', fetchMock)

    const requests = Array.from({ length: 6 }, () => api.get('/dashboard/summary'))
    await vi.waitFor(() => expect(authMocks.signinSilent).toHaveBeenCalledTimes(1))
    resolveRenewal(renewedUser)

    await expect(Promise.all(requests)).resolves.toEqual(Array(6).fill({ ok: true }))

    expect(fetchMock).toHaveBeenCalledTimes(12)
    expect(telemetryMocks.emitSessionContinuityEvent).toHaveBeenCalledTimes(1)
    expect(telemetryMocks.emitSessionContinuityEvent).toHaveBeenCalledWith(
      expect.objectContaining({
        event_name: 'silent-renewal-success',
        payload: expect.objectContaining({ coalesced_request_count: 6 }),
      }),
    )
  })

  it('renews expired mutations but does not replay them automatically', async () => {
    const fetchMock = vi.fn().mockResolvedValue(tokenExpiredResponse())
    vi.stubGlobal('fetch', fetchMock)

    await expect(api.post('/tasks/task-123', { name: 'Updated' })).rejects.toBeInstanceOf(
      MutationRetryRequiredError,
    )

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(authMocks.signinSilent).toHaveBeenCalledTimes(1)
    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'mutation_retry_required',
      { endpointRoute: '/tasks/task-123', method: 'POST' },
    )
  })

  it('leaves navigation pending and emits forced reauth for invalid tokens', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            type: 'https://nebula.local/problems/auth/invalid-token',
            code: 'invalid_token',
          },
          401,
        ),
      ),
    )

    const outcome = await pendingOutcome(api.get('/tasks/task-123'))

    expect(outcome).toBe('pending')
    expect(authMocks.signinSilent).not.toHaveBeenCalled()
    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'forced_reauth',
      expect.objectContaining({
        cause: 'auth_token_invalid',
        endpointRoute: '/tasks/task-123',
        method: 'GET',
      }),
    )
  })

  it('retries carrier market invalid-token responses with the local dev token', async () => {
    const fetchMock = vi.fn(async (_url: string | URL | Request, init?: RequestInit) => {
      const headers = init?.headers as Headers
      if (headers.get('Authorization') === 'Bearer stale-token') {
        return jsonResponse(
          {
            type: 'https://nebula.local/problems/auth/invalid-token',
            code: 'invalid_token',
          },
          401,
        )
      }

      return jsonResponse({ data: [{ code: 'NEB-DEMO-ATLANTIC' }] }, 200)
    })
    vi.stubGlobal('fetch', fetchMock)

    const result = await api.get<{ data: Array<{ code: string }> }>('/carrier-markets')

    expect(result.data[0].code).toBe('NEB-DEMO-ATLANTIC')
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect((fetchMock.mock.calls[1][1]?.headers as Headers).get('Authorization')).toBe(
      'Bearer dev-token',
    )
    expect(authMocks.emitAuthEvent).not.toHaveBeenCalledWith(
      'forced_reauth',
      expect.anything(),
    )
  })

  it('records fallback telemetry before forced reauth for unknown 401 responses', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(jsonResponse({ title: 'Unauthorized' }, 401)),
    )

    const outcome = await pendingOutcome(api.get('/tasks/task-123'))

    expect(outcome).toBe('pending')
    expect(telemetryMocks.persistFailureClassEvent).toHaveBeenCalledWith(
      expect.objectContaining({
        event_name: 'auth-classifier-fallback',
        payload: expect.objectContaining({
          endpoint_route: '/tasks/task-123',
          response_status: 401,
        }),
      }),
    )
  })

  it('preserves renewal failure causes in forced reauth telemetry', async () => {
    authMocks.signinSilent.mockRejectedValue(new Error('invalid_grant'))
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(tokenExpiredResponse()),
    )

    const outcome = await pendingOutcome(api.get('/tasks/task-123'))

    expect(outcome).toBe('pending')
    expect(telemetryMocks.persistFailureClassEvent).toHaveBeenCalledWith(
      expect.objectContaining({
        event_name: 'silent-renewal-fail',
        payload: { cause: 'refresh_expired' },
      }),
    )
    expect(telemetryMocks.persistFailureClassEvent).toHaveBeenCalledWith(
      expect.objectContaining({
        event_name: 'forced-redirect',
        payload: expect.objectContaining({ cause: 'refresh_expired' }),
      }),
    )
    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'forced_reauth',
      expect.objectContaining({ cause: 'refresh_expired' }),
    )
  })

  it('throws ApiError for ordinary 403 authorization denial without renewal', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse(
          {
            type: 'https://nebula.local/problems/authz/forbidden',
            code: 'forbidden',
            title: 'Forbidden',
          },
          403,
        ),
      ),
    )

    await expect(api.get('/policies/pol-1')).rejects.toBeInstanceOf(ApiError)
    expect(authMocks.signinSilent).not.toHaveBeenCalled()
    expect(authMocks.emitAuthEvent).not.toHaveBeenCalled()
  })

  it('preserves broker scope redirect behavior without session teardown', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse({ code: 'broker_scope_unresolvable' }, 403),
      ),
    )

    const outcome = await pendingOutcome(api.delete('/tasks/task-123'))

    expect(outcome).toBe('pending')
    expect(authMocks.emitAuthEvent).toHaveBeenCalledWith(
      'broker_scope_unresolvable',
    )
  })
})

describe('api multipart and binary helpers', () => {
  beforeEach(() => {
    resetRenewalStateForTests()
    authMocks.emitAuthEvent.mockReset()
    authMocks.getUser.mockReset()
    authMocks.signinSilent.mockReset()
    authMocks.getUser.mockResolvedValue(activeUser)
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
    expect(headers.get('Authorization')).toBe('Bearer stale-token')
  })

  it('returns blobs for download helpers without parsing JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(new Blob(['pdf']), { status: 200 })),
    )

    await expect(api.downloadBlob('/documents/doc_1/versions/latest/binary')).resolves.toBeInstanceOf(Blob)
  })
})
