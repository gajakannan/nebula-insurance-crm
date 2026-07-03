import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  buildSessionContinuityEvent,
  emitSessionContinuityEvent,
  flushSessionContinuityEvents,
  resetSessionTelemetryForTests,
  type SessionContinuityEvent,
} from '../sessionTelemetry'
import {
  drainDeferredEvents,
  persistFailureClassEvent,
} from '../deferredTelemetryBuffer'

const mocks = vi.hoisted(() => ({
  getUser: vi.fn(),
}))

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: mocks.getUser,
  },
}))

const currentUser = {
  access_token: 'telemetry-token',
  expired: false,
  profile: {
    sub: '11111111-1111-1111-1111-111111111111',
    sid: 'session-1',
  },
}

const sampleEvent: SessionContinuityEvent = {
  event_name: 'auth-classifier-fallback',
  event_version: 1,
  timestamp: new Date().toISOString(),
  user_id: currentUser.profile.sub,
  session_id: currentUser.profile.sid,
  payload: {
    endpoint_route: '/tasks',
    response_status: 401,
  },
}

describe('session continuity telemetry', () => {
  beforeEach(() => {
    resetSessionTelemetryForTests()
    mocks.getUser.mockReset()
    mocks.getUser.mockResolvedValue(currentUser)
    window.localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('builds events from non-PII session identity claims', () => {
    expect(
      buildSessionContinuityEvent(currentUser, 'forced-redirect', {
        cause: 'auth_unknown',
      }),
    ).toMatchObject({
      event_name: 'forced-redirect',
      user_id: currentUser.profile.sub,
      session_id: currentUser.profile.sid,
      payload: { cause: 'auth_unknown' },
    })
  })

  it('posts buffered events to the protected telemetry endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }))
    vi.stubGlobal('fetch', fetchMock)

    emitSessionContinuityEvent(sampleEvent)
    await flushSessionContinuityEvents()

    expect(fetchMock).toHaveBeenCalledWith(
      '/internal/telemetry/session-continuity',
      expect.objectContaining({
        method: 'POST',
        credentials: 'include',
        headers: expect.objectContaining({
          Authorization: 'Bearer telemetry-token',
          'Content-Type': 'application/json',
        }),
      }),
    )
    const [, init] = fetchMock.mock.calls[0]
    expect(JSON.parse(init.body as string)).toEqual({ events: [sampleEvent] })
  })

  it('persists and drains deferred failure-class events by user', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }))
    vi.stubGlobal('fetch', fetchMock)

    persistFailureClassEvent(sampleEvent)
    expect(window.localStorage.length).toBe(1)

    await drainDeferredEvents(currentUser.profile.sub)

    expect(fetchMock).toHaveBeenCalled()
    expect(window.localStorage.length).toBe(0)
  })

  it('drops expired deferred events instead of sending them', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 202 }))
    vi.stubGlobal('fetch', fetchMock)

    persistFailureClassEvent({
      ...sampleEvent,
      timestamp: '2026-01-01T00:00:00.000Z',
    })

    await drainDeferredEvents(currentUser.profile.sub)

    expect(fetchMock).not.toHaveBeenCalled()
    expect(window.localStorage.length).toBe(0)
  })
})
