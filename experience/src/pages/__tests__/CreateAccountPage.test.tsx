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
import CreateAccountPage from '../CreateAccountPage'

const createAccount = vi.fn()
const navigate = vi.fn()

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))
vi.mock('@/features/accounts', async (importActual) => {
  const actual = await importActual<typeof import('@/features/accounts')>()
  return { ...actual, useCreateAccount: () => ({ mutateAsync: createAccount, isPending: false }) }
})
vi.mock('@/features/brokers', async (importActual) => {
  const actual = await importActual<typeof import('@/features/brokers')>()
  return { ...actual, useBrokers: () => ({ data: { data: [] }, isLoading: false, isError: false }) }
})
vi.mock('@/features/tasks', async (importActual) => {
  const actual = await importActual<typeof import('@/features/tasks')>()
  return { ...actual, AssigneePicker: () => <div data-testid="assignee-picker" /> }
})
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))
vi.mock('react-router-dom', async (importActual) => {
  const actual = await importActual<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => navigate }
})

function renderPage() {
  return render(
    <MemoryRouter>
      <CreateAccountPage />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  createAccount.mockResolvedValue({ id: 'acc-1' })
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('CreateAccountPage — S0007 wiring regression', () => {
  it('creates an account with the same display name and navigates (unchanged)', async () => {
    renderPage()
    fireEvent.change(screen.getByLabelText(/Display Name/), { target: { value: 'Acme Co' } })
    await userEvent.click(screen.getByRole('button', { name: 'Create Account' }))
    expect(createAccount).toHaveBeenCalledWith(expect.objectContaining({ displayName: 'Acme Co', primaryProducerUserId: null }))
    await waitFor(() => expect(navigate).toHaveBeenCalledWith('/accounts/acc-1'))
  })

  it('blocks submit with a validation error for an empty display name (unchanged)', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: 'Create Account' }))
    expect(screen.getByText('Display name is required.')).toBeInTheDocument()
    expect(createAccount).not.toHaveBeenCalled()
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
          <CreateAccountPage />
        </DirtyFormRegistryProvider>
      </MemoryRouter>,
    )
    fireEvent.change(screen.getByLabelText(/Display Name/), { target: { value: 'Dirty Account' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ displayName: string }>('u1', 'account:new')
    expect(snap?.form_values.displayName).toBe('Dirty Account')
    expect(snap?.dirty_field_paths).toContain('displayName')
  })
})
