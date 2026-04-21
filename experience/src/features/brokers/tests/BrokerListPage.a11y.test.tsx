import { axe } from 'jest-axe'
import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import BrokerListPage from '@/pages/BrokerListPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'

const { mockGetUser } = vi.hoisted(() => ({
  mockGetUser: vi.fn(),
}))

mockGetUser.mockResolvedValue({
  expired: false,
  access_token: 'test-token',
  profile: {},
})

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: mockGetUser,
    events: {
      addUserLoaded: vi.fn(),
      addUserUnloaded: vi.fn(),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
    },
  },
}))

describe('BrokerListPage accessibility', () => {
  it('has no detectable accessibility violations for the broker directory route', async () => {
    const { container } = renderRouteWithProviders(<BrokerListPage />, {
      route: '/brokers',
      path: '/brokers',
    })

    await screen.findByRole('heading', { name: 'Broker Directory' })
    await screen.findByRole('link', { name: 'Blue Horizon Risk Partners' })

    await expect(axe(container)).resolves.toHaveNoViolations()
  })
})
