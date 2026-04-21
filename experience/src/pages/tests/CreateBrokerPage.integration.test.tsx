import type React from 'react'
import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { HttpResponse, http } from 'msw'
import { Route, useParams } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import CreateBrokerPage from '@/pages/CreateBrokerPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'
import { API_ORIGIN } from '@/mocks/data'
import { server } from '@/mocks/server'

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

function BrokerDetailDestination() {
  const { brokerId } = useParams()
  return <div>broker-detail:{brokerId}</div>
}

describe('CreateBrokerPage integration', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {},
    })
  })

  it('shows validation errors and submits a valid broker create request', async () => {
    const user = userEvent.setup()
    server.use(
      http.post(`${API_ORIGIN}/brokers`, async ({ request }) => {
        const body = await request.json()
        expect(body).toEqual({
          legalName: 'North Shore Wholesale',
          licenseNumber: 'CA-778899',
          state: 'CA',
          email: 'hello@northshore.test',
          phone: '+12025550199',
        })

        return HttpResponse.json(
          {
            id: 'broker-created-1',
            ...body,
            status: 'Active',
            createdAt: '2026-03-21T00:00:00Z',
            updatedAt: '2026-03-21T00:00:00Z',
            rowVersion: 1,
            isDeactivated: false,
          },
          { status: 201 },
        )
      }),
    )

    renderRouteWithProviders(<CreateBrokerPage />, {
      route: '/brokers/new',
      path: '/brokers/new',
      additionalRoutes: [
        <Route key="broker-detail" path="/brokers/:brokerId" element={<BrokerDetailDestination />} />,
      ],
    })

    await user.click(screen.getByRole('button', { name: 'Create Broker' }))

    expect(await screen.findByText('Legal name is required.')).toBeInTheDocument()
    expect(screen.getByText('License number is required.')).toBeInTheDocument()
    expect(screen.getByText('State is required.')).toBeInTheDocument()

    await user.type(screen.getByLabelText(/^Legal Name/), 'North Shore Wholesale')
    await user.type(screen.getByLabelText(/^License Number/), 'CA-778899')
    await user.selectOptions(screen.getByLabelText(/^State/), 'CA')
    await user.type(screen.getByLabelText('Email'), 'hello@northshore.test')
    await user.type(screen.getByLabelText('Phone'), '+12025550199')

    await user.click(screen.getByRole('button', { name: 'Create Broker' }))

    expect(await screen.findByText('broker-detail:broker-created-1')).toBeInTheDocument()
  })

  it('renders duplicate-license and generic server errors honestly', async () => {
    const user = userEvent.setup()

    renderRouteWithProviders(<CreateBrokerPage />, {
      route: '/brokers/new',
      path: '/brokers/new',
    })

    await user.type(screen.getByLabelText(/^Legal Name/), 'North Shore Wholesale')
    await user.type(screen.getByLabelText(/^License Number/), 'CA-778899')
    await user.selectOptions(screen.getByLabelText(/^State/), 'CA')

    server.use(
      http.post(
        `${API_ORIGIN}/brokers`,
        () =>
          HttpResponse.json(
            { title: 'Conflict', status: 409, code: 'duplicate_license' },
            { status: 409 },
          ),
      ),
    )

    await user.click(screen.getByRole('button', { name: 'Create Broker' }))
    expect(
      await screen.findByText('A broker with this license number already exists.'),
    ).toBeInTheDocument()

    server.use(
      http.post(
        `${API_ORIGIN}/brokers`,
        () => HttpResponse.json({ title: 'Server error', status: 500 }, { status: 500 }),
      ),
    )

    await user.clear(screen.getByLabelText(/^License Number/))
    await user.type(screen.getByLabelText(/^License Number/), 'CA-778800')
    await user.click(screen.getByRole('button', { name: 'Create Broker' }))

    expect(
      await screen.findByText('Unable to create broker. Please try again.'),
    ).toBeInTheDocument()
  })
})
