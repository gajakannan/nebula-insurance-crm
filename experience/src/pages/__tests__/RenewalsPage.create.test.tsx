import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import RenewalsPage from '../RenewalsPage'

const createRenewal = vi.fn()

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))
vi.mock('@/features/renewals', async (importActual) => {
  const actual = await importActual<typeof import('@/features/renewals')>()
  return {
    ...actual,
    useRenewals: () => ({
      data: { data: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
      isLoading: false,
      isError: false,
      isFetching: false,
    }),
    useCreateRenewal: () => ({ mutateAsync: createRenewal, isPending: false }),
  }
})
vi.mock('@/features/tasks', async (importActual) => {
  const actual = await importActual<typeof import('@/features/tasks')>()
  return { ...actual, AssigneePicker: () => <div data-testid="assignee-picker" /> }
})
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: ['DistributionManager'], brokerTenantId: null }),
}))

const POLICY_ID = '11111111-1111-1111-1111-111111111111'

function renderPage(extra?: React.ReactNode) {
  return render(
    <MemoryRouter>
      {extra}
      <RenewalsPage />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  createRenewal.mockResolvedValue({ id: 'r1' })
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('RenewalsPage create modal — S0007 wiring regression', () => {
  it('creates a renewal with the same policyId payload (unchanged)', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: 'Create Renewal' }))
    fireEvent.change(screen.getByLabelText(/Policy ID/), { target: { value: POLICY_ID } })
    await userEvent.click(screen.getByRole('button', { name: 'Create renewal' }))
    expect(createRenewal).toHaveBeenCalledWith(expect.objectContaining({ policyId: POLICY_ID }))
  })

  it('registers the create form with F0035 so dirty values snapshot on a forced re-auth', async () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <MemoryRouter>
        <DirtyFormRegistryProvider>
          <Grab />
          <RenewalsPage />
        </DirtyFormRegistryProvider>
      </MemoryRouter>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'Create Renewal' }))
    fireEvent.change(screen.getByLabelText(/Policy ID/), { target: { value: POLICY_ID } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ policyId: string }>('u1', 'renewal:new')
    expect(snap?.form_values.policyId).toBe(POLICY_ID)
    expect(snap?.dirty_field_paths).toContain('policyId')
  })
})
