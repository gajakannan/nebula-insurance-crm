import type React from 'react'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { HttpResponse, http } from 'msw'
import { Route } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import BrokerDetailPage from '@/pages/BrokerDetailPage'
import { renderRouteWithProviders } from '@/test-utils/render-app'
import { API_ORIGIN } from '@/mocks/data'
import { server } from '@/mocks/server'
import type { BrokerDto, ContactDto } from '@/features/brokers'
import type { TimelineEventDto } from '@/contracts/timeline'

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

const brokerSeed: BrokerDto = {
  id: 'broker-1',
  legalName: 'Blue Horizon Risk Partners',
  licenseNumber: 'CA-445512',
  state: 'CA',
  status: 'Active',
  email: 'team@bluehorizon.test',
  phone: '+12025550101',
  createdAt: '2026-03-01T00:00:00Z',
  updatedAt: '2026-03-19T00:00:00Z',
  rowVersion: 3,
  isDeactivated: false,
}

const contactSeeds: ContactDto[] = [
  {
    id: 'contact-1',
    brokerId: 'broker-1',
    accountId: null,
    fullName: 'Nadia Brooks',
    email: 'nadia@bluehorizon.test',
    phone: '+12025550111',
    role: 'Underwriter',
    rowVersion: 1,
  },
  {
    id: 'contact-2',
    brokerId: 'broker-1',
    accountId: null,
    fullName: 'Alex Kim',
    email: null,
    phone: null,
    role: 'Assistant Underwriter',
    rowVersion: 2,
  },
]

const timelinePages: Array<{
  data: TimelineEventDto[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}> = [
  {
    data: [
      {
        id: 'timeline-1',
        entityType: 'Broker',
        entityId: 'broker-1',
        eventType: 'BrokerUpdated',
        eventDescription: 'Broker profile was refreshed.',
        entityName: 'Blue Horizon Risk Partners',
        actorDisplayName: 'Priya Patel',
        occurredAt: '2026-03-20T15:00:00Z',
      },
    ],
    page: 1,
    pageSize: 50,
    totalCount: 2,
    totalPages: 2,
  },
  {
    data: [
      {
        id: 'timeline-2',
        entityType: 'Broker',
        entityId: 'broker-1',
        eventType: 'ContactCreated',
        eventDescription: 'A new contact was added.',
        entityName: 'Blue Horizon Risk Partners',
        actorDisplayName: 'Nadia Brooks',
        occurredAt: '2026-03-18T11:00:00Z',
      },
    ],
    page: 2,
    pageSize: 50,
    totalCount: 2,
    totalPages: 2,
  },
]

function installBrokerHandlers(initialBroker: Partial<BrokerDto> = {}) {
  let broker: BrokerDto = {
    ...brokerSeed,
    ...initialBroker,
  }
  let contacts: ContactDto[] = contactSeeds.map((contact) => ({ ...contact }))

  server.use(
    http.get(`${API_ORIGIN}/brokers/:brokerId`, ({ params }) => {
      if (params.brokerId !== broker.id) {
        return HttpResponse.json({ title: 'Not found', status: 404 }, { status: 404 })
      }
      return HttpResponse.json(broker)
    }),
    http.put(`${API_ORIGIN}/brokers/:brokerId`, async ({ params, request }) => {
      if (params.brokerId !== broker.id) {
        return HttpResponse.json({ title: 'Not found', status: 404 }, { status: 404 })
      }

      const body = (await request.json()) as Partial<BrokerDto>
      broker = {
        ...broker,
        ...body,
        rowVersion: broker.rowVersion + 1,
      }

      return HttpResponse.json(broker)
    }),
    http.post(`${API_ORIGIN}/brokers/:brokerId/reactivate`, ({ params }) => {
      if (params.brokerId !== broker.id) {
        return HttpResponse.json({ title: 'Not found', status: 404 }, { status: 404 })
      }

      broker = {
        ...broker,
        status: 'Active',
        isDeactivated: false,
        rowVersion: broker.rowVersion + 1,
      }

      return HttpResponse.json(broker)
    }),
    http.delete(`${API_ORIGIN}/brokers/:brokerId`, () => new HttpResponse(null, { status: 204 })),
    http.get(`${API_ORIGIN}/contacts`, ({ request }) => {
      const url = new URL(request.url)
      const brokerId = url.searchParams.get('brokerId')
      const filtered = contacts.filter((contact) => contact.brokerId === brokerId)

      return HttpResponse.json({
        data: filtered,
        page: 1,
        pageSize: 50,
        totalCount: filtered.length,
        totalPages: 1,
      })
    }),
    http.post(`${API_ORIGIN}/contacts`, async ({ request }) => {
      const body = (await request.json()) as {
        brokerId: string
        fullName: string
        email: string
        phone: string
        role?: string
      }
      const created: ContactDto = {
        id: `contact-${contacts.length + 1}`,
        brokerId: body.brokerId,
        accountId: null,
        fullName: body.fullName,
        email: body.email,
        phone: body.phone,
        role: body.role ?? '',
        rowVersion: 1,
      }
      contacts = [...contacts, created]
      return HttpResponse.json(created, { status: 201 })
    }),
    http.put(`${API_ORIGIN}/contacts/:contactId`, async ({ params, request }) => {
      const body = (await request.json()) as Partial<ContactDto>
      contacts = contacts.map((contact) =>
        contact.id === params.contactId
          ? {
              ...contact,
              ...body,
              role: body.role ?? '',
              rowVersion: contact.rowVersion + 1,
            }
          : contact,
      )
      return HttpResponse.json(contacts.find((contact) => contact.id === params.contactId))
    }),
    http.delete(`${API_ORIGIN}/contacts/:contactId`, ({ params }) => {
      contacts = contacts.filter((contact) => contact.id !== params.contactId)
      return new HttpResponse(null, { status: 204 })
    }),
    http.get(`${API_ORIGIN}/timeline/events`, ({ request }) => {
      const url = new URL(request.url)
      const page = Number(url.searchParams.get('page') ?? '1')
      return HttpResponse.json(
        timelinePages[page - 1] ?? {
          data: [],
          page,
          pageSize: 50,
          totalCount: 0,
          totalPages: 1,
        },
      )
    }),
  )
}

function renderBrokerDetailPage(additionalRoutes: React.ReactElement[] = []) {
  return renderRouteWithProviders(<BrokerDetailPage />, {
    route: '/brokers/broker-1',
    path: '/brokers/:brokerId',
    additionalRoutes,
  })
}

describe('BrokerDetailPage integration', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    authMocks.getUser.mockResolvedValue({
      expired: false,
      access_token: 'test-token',
      profile: {},
    })
  })

  it('renders the not-found state for missing brokers', async () => {
    server.use(
      http.get(`${API_ORIGIN}/brokers/:brokerId`, () =>
        HttpResponse.json({ title: 'Not found', status: 404 }, { status: 404 }),
      ),
    )

    renderBrokerDetailPage()
    expect(await screen.findByText('Broker not found.')).toBeInTheDocument()
  })

  it('renders the forbidden state for unauthorized access', async () => {
    server.use(
      http.get(`${API_ORIGIN}/brokers/:brokerId`, () =>
        HttpResponse.json({ title: 'Forbidden', status: 403 }, { status: 403 }),
      ),
    )

    renderBrokerDetailPage()
    expect(
      await screen.findByText("You don't have permission to view this broker."),
    ).toBeInTheDocument()
  })

  it('renders a retryable generic error state', async () => {
    server.use(
      http.get(`${API_ORIGIN}/brokers/:brokerId`, () =>
        HttpResponse.json({ title: 'Server error', status: 500 }, { status: 500 }),
      ),
    )

    renderBrokerDetailPage()

    expect(await screen.findByText('Unable to load broker.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it('loads the broker profile and updates broker details from the edit modal', async () => {
    const user = userEvent.setup()
    installBrokerHandlers()

    renderBrokerDetailPage()

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Risk Partners' })).toBeInTheDocument()
    expect(screen.getAllByText('CA-445512')).toHaveLength(2)

    await user.click(screen.getByRole('button', { name: 'Edit' }))
    await screen.findByRole('dialog', { name: 'Edit Broker' })

    await user.clear(screen.getByLabelText(/^Legal Name/))
    await user.type(screen.getByLabelText(/^Legal Name/), 'Blue Horizon Specialty')
    await user.selectOptions(screen.getByLabelText(/^State/), 'NY')
    await user.selectOptions(screen.getByLabelText(/^Status/), 'Inactive')
    await user.click(screen.getByRole('button', { name: 'Save Changes' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Edit Broker' })).not.toBeInTheDocument()
    })

    expect(await screen.findByRole('heading', { name: 'Blue Horizon Specialty' })).toBeInTheDocument()
    expect(screen.getAllByText('Inactive')).toHaveLength(2)
  })

  it('adds, edits, and deletes contacts from the contacts tab', async () => {
    const user = userEvent.setup()
    installBrokerHandlers()

    renderBrokerDetailPage()

    await screen.findByRole('heading', { name: 'Blue Horizon Risk Partners' })
    await user.click(screen.getByRole('tab', { name: 'Contacts' }))

    expect(await screen.findByText('2 contacts')).toBeInTheDocument()
    expect(screen.getByText('Nadia Brooks')).toBeInTheDocument()
    expect(screen.getByText('Assistant Underwriter')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Add Contact' }))
    const addContactDialog = await screen.findByRole('dialog', { name: 'Add Contact' })
    await user.click(within(addContactDialog).getByRole('button', { name: 'Add Contact' }))

    expect(await screen.findByText('Full name is required.')).toBeInTheDocument()
    expect(screen.getByText('Email is required.')).toBeInTheDocument()
    expect(screen.getByText('Phone is required.')).toBeInTheDocument()

    await user.type(within(addContactDialog).getByLabelText(/^Full Name/), 'Jordan R')
    await user.type(within(addContactDialog).getByLabelText(/^Email/), 'jr@test.io')
    await user.type(within(addContactDialog).getByLabelText(/^Phone/), '+12025550155')
    await user.type(within(addContactDialog).getByLabelText('Role'), 'Manager')
    await user.click(within(addContactDialog).getByRole('button', { name: 'Add Contact' }))

    expect(await screen.findByText('Jordan R')).toBeInTheDocument()
    expect(screen.getByText('Manager')).toBeInTheDocument()

    const nadiaRow = screen.getByText('Nadia Brooks').closest('div.flex.items-center.justify-between.rounded-lg.border')
    expect(nadiaRow).not.toBeNull()

    await user.click(within(nadiaRow!).getByTitle('Edit contact'))
    const editContactDialog = await screen.findByRole('dialog', { name: 'Edit Contact' })
    const roleInput = within(editContactDialog).getByLabelText('Role')
    await user.clear(roleInput)
    await user.type(roleInput, 'Lead Underwriter')
    await user.click(within(editContactDialog).getByRole('button', { name: 'Save Changes' }))
    expect(await screen.findByText('Lead Underwriter')).toBeInTheDocument()

    const alexRow = screen.getByText('Alex Kim').closest('div.flex.items-center.justify-between.rounded-lg.border')
    expect(alexRow).not.toBeNull()

    await user.click(within(alexRow!).getByTitle('Delete contact'))
    const deleteContactDialog = await screen.findByRole('dialog', { name: 'Delete Contact' })
    await user.click(within(deleteContactDialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() => {
      expect(screen.queryByText('Alex Kim')).not.toBeInTheDocument()
    })
  }, 10000)

  it('paginates timeline events and supports broker reactivation', async () => {
    const user = userEvent.setup()
    installBrokerHandlers({
      status: 'Inactive',
      isDeactivated: true,
      email: null,
      phone: null,
    })

    renderBrokerDetailPage()

    expect(await screen.findByText('Broker Deactivated')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Reactivate Broker' }))
    await screen.findByRole('dialog', { name: 'Reactivate Broker' })
    await user.click(screen.getByRole('button', { name: 'Reactivate' }))

    await waitFor(() => {
      expect(screen.queryByText('Broker Deactivated')).not.toBeInTheDocument()
    })

    await user.click(screen.getByRole('tab', { name: 'Timeline' }))
    expect(await screen.findByText('Broker profile was refreshed.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next →' }))
    expect(await screen.findByText('A new contact was added.')).toBeInTheDocument()
    expect(screen.getByText('Page 2 of 2')).toBeInTheDocument()
  })

  it('navigates back to the broker list after deleting the broker', async () => {
    const user = userEvent.setup()
    installBrokerHandlers()

    renderBrokerDetailPage([
      <Route key="broker-list" path="/brokers" element={<div>broker-list-destination</div>} />,
    ])

    await screen.findByRole('heading', { name: 'Blue Horizon Risk Partners' })
    await user.click(screen.getByRole('button', { name: 'Delete' }))
    const deleteBrokerDialog = await screen.findByRole('dialog', { name: 'Delete Broker' })
    await user.click(within(deleteBrokerDialog).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('broker-list-destination')).toBeInTheDocument()
  })
})
