import { fireEvent, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { NotificationDropdown } from '../components/NotificationDropdown'
import { renderWithProviders } from '@/test-utils/render-app'

const { mockGetUser } = vi.hoisted(() => ({
  mockGetUser: vi.fn(),
}))

mockGetUser.mockResolvedValue({
  expired: false,
  access_token: 'test-token',
  profile: {
    sub: 'user-dist-manager',
    email: 'sarah.chen@nebula.local',
    name: 'Sarah Chen',
    nebula_roles: ['DistributionManager'],
  },
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

describe('NotificationDropdown', () => {
  it('opens and shows notifications from API', async () => {
    const user = userEvent.setup()

    renderWithProviders(<NotificationDropdown />)

    const trigger = screen.getByRole('button', { name: 'Notifications' })
    await user.click(trigger)

    await waitFor(() => {
      expect(screen.getByText('Broker created')).toBeInTheDocument()
    })

    expect(screen.getByText('Submission received')).toBeInTheDocument()
    expect(screen.getByText('Task overdue')).toBeInTheDocument()
    expect(screen.getByText('Opportunity moved stage')).toBeInTheDocument()
    expect(screen.getByText('Data sync warning')).toBeInTheDocument()
  })

  it('shows unread badge count', async () => {
    const user = userEvent.setup()

    renderWithProviders(<NotificationDropdown />)

    const trigger = screen.getByRole('button', { name: 'Notifications' })

    await waitFor(() => {
      expect(trigger).toHaveTextContent('3')
    })
  })

  it('filters to unread tab', async () => {
    const user = userEvent.setup()

    renderWithProviders(<NotificationDropdown />)

    await user.click(screen.getByRole('button', { name: 'Notifications' }))

    await waitFor(() => {
      expect(screen.getByText('Broker created')).toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Unread' }))

    await waitFor(() => {
      expect(screen.getByText('Broker created')).toBeInTheDocument()
    })
    expect(screen.getByText('Submission received')).toBeInTheDocument()
    expect(screen.getByText('Task overdue')).toBeInTheDocument()
  })

  it('closes on Escape key', async () => {
    const user = userEvent.setup()

    renderWithProviders(<NotificationDropdown />)

    await user.click(screen.getByRole('button', { name: 'Notifications' }))

    await waitFor(() => {
      expect(screen.getByText('Broker created')).toBeInTheDocument()
    })

    fireEvent.keyDown(document, { key: 'Escape' })
    await waitFor(() => {
      expect(screen.queryByText('Broker created')).not.toBeInTheDocument()
    })
  })

  it('closes on outside click', async () => {
    const user = userEvent.setup()

    renderWithProviders(<NotificationDropdown />)

    await user.click(screen.getByRole('button', { name: 'Notifications' }))

    await waitFor(() => {
      expect(screen.getByText('Broker created')).toBeInTheDocument()
    })

    fireEvent.mouseDown(document.body)
    await waitFor(() => {
      expect(screen.queryByText('Broker created')).not.toBeInTheDocument()
    })
  })
})
