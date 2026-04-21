import type React from 'react'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import SubmissionsPage from '@/pages/SubmissionsPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'

const authMocks = vi.hoisted(() => ({
  getUser: vi.fn(),
}))

vi.mock('@/features/auth/oidcUserManager', () => ({
  oidcUserManager: {
    getUser: authMocks.getUser,
    events: {
      addUserLoaded: vi.fn(),
      addUserUnloaded: vi.fn(),
      removeUserLoaded: vi.fn(),
      removeUserUnloaded: vi.fn(),
    },
  },
}))

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({
    title,
    children,
  }: {
    title?: string
    children: React.ReactNode
  }) => (
    <div>
      {title && <h1>{title}</h1>}
      {children}
    </div>
  ),
}))

describe('SubmissionsPage integration', () => {
  it('loads stale submissions and applies status filters against the shared runtime harness', async () => {
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {},
    })

    const user = userEvent.setup()

    renderRouteWithProviders(<SubmissionsPage />, {
      route: '/submissions?stale=true',
      path: '/submissions',
    })

    expect(await screen.findByText('Submission pipeline')).toBeInTheDocument()
    expect(await screen.findByText('2 submissions')).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: 'Compass Markets Retail Group' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Northstar Contractors Collective' })).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Filter submissions by status'), 'Received')

    await waitFor(() => {
      expect(screen.getByText('1 submission')).toBeInTheDocument()
      expect(screen.getByRole('link', { name: 'Compass Markets Retail Group' })).toBeInTheDocument()
      expect(screen.queryByRole('link', { name: 'Blue Horizon Manufacturing' })).not.toBeInTheDocument()
    })
  })
})
