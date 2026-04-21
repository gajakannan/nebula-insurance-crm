import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useCurrentUser } from '../useCurrentUser'

const oidcMock = vi.hoisted(() => ({
  mockGetUser: vi.fn(),
  addUserLoaded: vi.fn(),
  addUserUnloaded: vi.fn(),
  removeUserLoaded: vi.fn(),
  removeUserUnloaded: vi.fn(),
  state: {
    loadedHandler: undefined as ((user: { profile: Record<string, unknown> }) => void) | undefined,
    unloadedHandler: undefined as (() => void) | undefined,
  },
}))

vi.mock('../oidcUserManager', () => ({
  oidcUserManager: {
    getUser: oidcMock.mockGetUser,
    events: {
      addUserLoaded: vi.fn((handler: (user: { profile: Record<string, unknown> }) => void) => {
        oidcMock.state.loadedHandler = handler
        oidcMock.addUserLoaded(handler)
      }),
      addUserUnloaded: vi.fn((handler: () => void) => {
        oidcMock.state.unloadedHandler = handler
        oidcMock.addUserUnloaded(handler)
      }),
      removeUserLoaded: vi.fn((handler: unknown) => oidcMock.removeUserLoaded(handler)),
      removeUserUnloaded: vi.fn((handler: unknown) => oidcMock.removeUserUnloaded(handler)),
    },
  },
}))

describe('useCurrentUser', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    oidcMock.state.loadedHandler = undefined
    oidcMock.state.unloadedHandler = undefined
  })

  it('maps the active oidc profile into the current user model', async () => {
    oidcMock.mockGetUser.mockResolvedValue({
      expired: false,
      profile: {
        sub: 'user-123',
        email: 'nadia@nebula.test',
        name: 'Nadia Brooks',
        nebula_roles: ['DistributionManager'],
        broker_tenant_id: 'broker-1',
      },
    })

    const { result } = renderHook(() => useCurrentUser())

    await waitFor(() => {
      expect(result.current).toEqual({
        sub: 'user-123',
        email: 'nadia@nebula.test',
        displayName: 'Nadia Brooks',
        roles: ['DistributionManager'],
        brokerTenantId: 'broker-1',
      })
    })

    expect(oidcMock.addUserLoaded).toHaveBeenCalledOnce()
    expect(oidcMock.addUserUnloaded).toHaveBeenCalledOnce()
  })

  it('reacts to oidc user lifecycle events', async () => {
    oidcMock.mockGetUser.mockResolvedValue(null)

    const { result } = renderHook(() => useCurrentUser())

    await waitFor(() => {
      expect(result.current).toBeNull()
    })

    act(() => {
      oidcMock.state.loadedHandler?.({
        profile: {
          sub: 'user-456',
          email: 'alex@nebula.test',
          preferred_username: 'Alex Kim',
          nebula_roles: 'Underwriter',
        },
      })
    })

    await waitFor(() => {
      expect(result.current).toEqual({
        sub: 'user-456',
        email: 'alex@nebula.test',
        displayName: 'Alex Kim',
        roles: ['Underwriter'],
        brokerTenantId: null,
      })
    })

    act(() => {
      oidcMock.state.unloadedHandler?.()
    })

    await waitFor(() => {
      expect(result.current).toBeNull()
    })
  })
})
