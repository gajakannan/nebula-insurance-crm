import type React from 'react'
import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import RenewalDetailPage from '@/pages/RenewalDetailPage'
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

describe('RenewalDetailPage integration', () => {
  beforeEach(() => {
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
  })

  it('supports reassignment and completion against the shared renewal state', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<RenewalDetailPage />, {
      route: '/renewals/renewal-2',
      path: '/renewals/:renewalId',
    })

    expect(await screen.findByRole('heading', { name: 'Compass Markets Retail Group' })).toBeInTheDocument()
    expect(await screen.findByText('Renewal advanced to Quoted.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Reassign' }))
    const assignDialog = await screen.findByRole('dialog', { name: 'Reassign renewal' })
    await user.click(within(assignDialog).getByRole('button', { name: 'Remove assignee Nadia Brooks' }))
    await user.type(within(assignDialog).getByRole('combobox', { name: 'Renewal owner' }), 'alex')
    await user.click(await within(assignDialog).findByText('Alex Kim'))
    await user.click(within(assignDialog).getByRole('button', { name: 'Save owner' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Reassign renewal' })).not.toBeInTheDocument()
    })
    expect(await screen.findByText('Alex Kim')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Complete Renewal' }))
    const transitionDialog = await screen.findByRole('dialog', { name: 'Complete Renewal' })
    fireEvent.change(within(transitionDialog).getByLabelText('Bound Policy ID'), {
      target: { value: 'bound-policy-2002' },
    })
    await user.click(within(transitionDialog).getByRole('button', { name: 'Confirm transition' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Complete Renewal' })).not.toBeInTheDocument()
    })

    expect(await screen.findByText('Completed')).toBeInTheDocument()
    expect(await screen.findByText(/Renewal completed with bound policy bound-policy-2002\./)).toBeInTheDocument()
    expect(await screen.findByText('No further status transitions are available for this renewal.')).toBeInTheDocument()
  }, 15000)
})
