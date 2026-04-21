import userEvent from '@testing-library/user-event'
import { screen, waitFor } from '@testing-library/react'
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

describe('BrokerListPage integration', () => {
  it('loads brokers from the shared MSW harness and filters them by search and status', async () => {
    renderRouteWithProviders(<BrokerListPage />, {
      route: '/brokers',
      path: '/brokers',
    })

    expect(await screen.findByRole('heading', { name: 'Broker Directory' })).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: 'Blue Horizon Risk Partners' })).toBeInTheDocument()
    expect(screen.getAllByText('Summit Specialty Group').length).toBeGreaterThan(0)

    const user = userEvent.setup()
    await user.type(screen.getByLabelText('Search brokers'), 'summit')

    await waitFor(() => {
      expect(screen.queryByRole('link', { name: 'Blue Horizon Risk Partners' })).not.toBeInTheDocument()
      expect(screen.getAllByText('Summit Specialty Group').length).toBeGreaterThan(0)
    })

    await user.clear(screen.getByLabelText('Search brokers'))
    await user.selectOptions(screen.getByLabelText('Filter brokers by status'), 'Inactive')

    await waitFor(() => {
      expect(screen.getAllByText('Atlas Wholesale Brokerage').length).toBeGreaterThan(0)
      expect(screen.queryAllByText('Summit Specialty Group')).toHaveLength(0)
      expect(screen.getAllByText('Masked').length).toBeGreaterThan(0)
    })
  })
})
