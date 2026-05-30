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
import CreateSubmissionPage from '../CreateSubmissionPage'

const createSubmission = vi.fn()

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))
vi.mock('@/features/lob-attributes', async (importActual) => {
  const actual = await importActual<typeof import('@/features/lob-attributes')>()
  return { ...actual, DynamicAttributePanel: () => null }
})
vi.mock('@/features/submissions', async (importActual) => {
  const actual = await importActual<typeof import('@/features/submissions')>()
  return {
    ...actual,
    useAccounts: () => ({ data: [], isLoading: false, isError: false }),
    usePrograms: () => ({ data: [], isLoading: false, isError: false }),
    useCreateSubmission: () => ({ mutateAsync: createSubmission, isPending: false }),
  }
})
vi.mock('@/features/brokers', async (importActual) => {
  const actual = await importActual<typeof import('@/features/brokers')>()
  return { ...actual, useBrokers: () => ({ data: { data: [] }, isLoading: false, isError: false }) }
})
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))
vi.mock('react-router-dom', async (importActual) => {
  const actual = await importActual<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => vi.fn() }
})

function renderPage(extra?: React.ReactNode) {
  return render(<MemoryRouter>{extra}<CreateSubmissionPage /></MemoryRouter>)
}

beforeEach(() => {
  createSubmission.mockResolvedValue({ id: 's1' })
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('CreateSubmissionPage — S0007 wiring regression', () => {
  it('blocks submit with validation errors for missing required fields (unchanged)', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: 'Create Submission' }))
    expect(createSubmission).not.toHaveBeenCalled()
  })

  it('registers the native submission form with F0035 so dirty values snapshot', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <MemoryRouter>
        <DirtyFormRegistryProvider>
          <Grab />
          <CreateSubmissionPage />
        </DirtyFormRegistryProvider>
      </MemoryRouter>,
    )
    fireEvent.change(screen.getByLabelText('Premium Estimate'), { target: { value: '50000' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ premiumEstimate: string }>('u1', 'submission:new')
    expect(snap?.form_values.premiumEstimate).toBe('50000')
    expect(snap?.dirty_field_paths).toContain('premiumEstimate')
  })
})
