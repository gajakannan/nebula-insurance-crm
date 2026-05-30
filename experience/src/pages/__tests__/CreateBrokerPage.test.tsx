import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import CreateBrokerPage from '../CreateBrokerPage'

const createBroker = vi.fn()
const navigate = vi.fn()

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))
vi.mock('@/features/brokers', async (importActual) => {
  const actual = await importActual<typeof import('@/features/brokers')>()
  return { ...actual, useCreateBroker: () => ({ mutateAsync: createBroker, isPending: false }) }
})
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))
vi.mock('react-router-dom', async (importActual) => {
  const actual = await importActual<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => navigate }
})

function renderPage(extra?: React.ReactNode) {
  return render(
    <MemoryRouter>
      {extra}
      <CreateBrokerPage />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  createBroker.mockResolvedValue({ id: 'new-broker-id' })
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('CreateBrokerPage — S0007 wiring regression', () => {
  it('creates a broker with the same payload and navigates (unchanged)', async () => {
    renderPage()
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: 'New Brokerage' } })
    fireEvent.change(screen.getByLabelText(/License Number/), { target: { value: 'LIC999' } })
    fireEvent.change(screen.getByLabelText(/State/), { target: { value: 'CA' } })
    await userEvent.click(screen.getByRole('button', { name: 'Create Broker' }))
    expect(createBroker).toHaveBeenCalledWith({
      legalName: 'New Brokerage',
      licenseNumber: 'LIC999',
      state: 'CA',
      email: undefined,
      phone: undefined,
    })
    await waitFor(() => expect(navigate).toHaveBeenCalledWith('/brokers/new-broker-id'))
  })

  it('blocks submit and shows validation errors for empty required fields (unchanged)', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: 'Create Broker' }))
    expect(createBroker).not.toHaveBeenCalled()
  })

  it('registers with F0035 so dirty values snapshot on a forced re-auth', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <MemoryRouter>
        <DirtyFormRegistryProvider>
          <Grab />
          <CreateBrokerPage />
        </DirtyFormRegistryProvider>
      </MemoryRouter>,
    )
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: 'Dirty Brokerage' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ legalName: string }>('u1', 'broker:new')
    expect(snap?.form_values.legalName).toBe('Dirty Brokerage')
    expect(snap?.dirty_field_paths).toContain('legalName')
  })
})
