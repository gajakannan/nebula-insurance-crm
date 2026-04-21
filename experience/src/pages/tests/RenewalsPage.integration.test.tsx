import type React from 'react'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import RenewalDetailPage from '@/pages/RenewalDetailPage'
import RenewalsPage from '@/pages/RenewalsPage'
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

describe('RenewalsPage integration', () => {
  it('loads overdue renewals and creates a new renewal that navigates to detail', async () => {
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {
        sub: 'user-dist-manager',
        name: 'Sarah Chen',
        email: 'sarah.chen@nebula.local',
        nebula_roles: ['DistributionManager'],
      },
    })

    const user = userEvent.setup()

    renderRouteWithProviders(<RenewalsPage />, {
      route: '/renewals?urgency=overdue',
      path: '/renewals',
      additionalRoutes: [
        <Route
          key="renewal-detail"
          path="/renewals/:renewalId"
          element={<RenewalDetailPage />}
        />,
      ],
    })

    expect(await screen.findByText('Renewal pipeline')).toBeInTheDocument()
    expect(await screen.findByText('1 renewal')).toBeInTheDocument()
    expect(await screen.findByRole('link', { name: 'Blue Horizon Manufacturing' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Northstar Contractors Collective' })).not.toBeInTheDocument()

    await user.click(await screen.findByRole('button', { name: 'Create Renewal' }))
    const createDialog = await screen.findByRole('dialog', { name: 'Create Renewal' })
    fireEvent.change(within(createDialog).getByLabelText(/Policy ID/i), {
      target: { value: 'policy-4' },
    })
    await user.click(within(createDialog).getByRole('button', { name: 'Create renewal' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Create Renewal' })).not.toBeInTheDocument()
    }, { timeout: 5000 })

    expect(await screen.findByRole('heading', { name: 'Northstar Contractors Collective' }, { timeout: 5000 })).toBeInTheDocument()
    expect(await screen.findByText('Renewal created for policy NC-2026-990.')).toBeInTheDocument()
  }, 15000)
})
