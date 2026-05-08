import type React from 'react'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import PolicyDetailPage from '@/pages/PolicyDetailPage'
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

describe('PolicyDetailPage integration', () => {
  it('enables and saves Cyber attributes for a pending policy', async () => {
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {
        sub: 'user-underwriter-1',
        name: 'Nadia Brooks',
        email: 'nadia.brooks@nebula.local',
        nebula_roles: ['Underwriter'],
      },
    })

    const user = userEvent.setup()

    renderRouteWithProviders(<PolicyDetailPage />, {
      route: '/policies/policy-2',
      path: '/policies/:policyId',
    })

    expect(await screen.findByRole('heading', { name: 'NEB-CYBR-2026-000002' })).toBeInTheDocument()
    expect(screen.getByLabelText(/Records held/)).toBeDisabled()

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    expect(screen.getByLabelText(/Records held/)).toBeEnabled()

    await user.selectOptions(screen.getByLabelText(/Revenue band/), '50-250M')
    await user.type(screen.getByLabelText(/Records held/), '250000')
    await user.type(screen.getByLabelText(/Requested limit/), '2000000')
    await user.type(screen.getByLabelText(/Requested retention/), '50000')
    await user.selectOptions(screen.getByLabelText(/Training frequency/), 'Annual')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()
    })
    expect(screen.getByLabelText(/Records held/)).toBeDisabled()
    expect(screen.getByLabelText(/Records held/)).toHaveValue(250000)
  }, 15000)
})
