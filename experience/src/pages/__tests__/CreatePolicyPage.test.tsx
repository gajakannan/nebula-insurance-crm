import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import CreatePolicyPage from '../CreatePolicyPage'

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
}))
vi.mock('@/features/lob-attributes', async (importActual) => {
  const actual = await importActual<typeof import('@/features/lob-attributes')>()
  return { ...actual, DynamicAttributePanel: () => null }
})
vi.mock('@/features/submissions', async (importActual) => {
  const actual = await importActual<typeof import('@/features/submissions')>()
  return { ...actual, useAccounts: () => ({ data: [], isLoading: false, isError: false }) }
})
vi.mock('@/features/brokers', async (importActual) => {
  const actual = await importActual<typeof import('@/features/brokers')>()
  return { ...actual, useBrokers: () => ({ data: { data: [] }, isLoading: false, isError: false }) }
})
vi.mock('@/features/policies', async (importActual) => {
  const actual = await importActual<typeof import('@/features/policies')>()
  return { ...actual, useCreatePolicy: () => ({ mutateAsync: vi.fn().mockResolvedValue({ id: 'p1' }), isPending: false }) }
})
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))
vi.mock('react-router-dom', async (importActual) => {
  const actual = await importActual<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => vi.fn() }
})

beforeEach(() => window.sessionStorage.clear())
afterEach(() => vi.clearAllMocks())

describe('CreatePolicyPage — S0007 wiring regression', () => {
  it('registers the native policy form with F0035 so dirty values snapshot', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <MemoryRouter>
        <DirtyFormRegistryProvider>
          <Grab />
          <CreatePolicyPage />
        </DirtyFormRegistryProvider>
      </MemoryRouter>,
    )
    // "Total premium" defaults to 25000; the Field+TextInput pairing means more
    // than one matching input, so target the first.
    fireEvent.change(screen.getAllByDisplayValue('25000')[0], { target: { value: '99999' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ totalPremium: string }>('u1', 'policy:new')
    expect(snap?.form_values.totalPremium).toBe('99999')
    expect(snap?.dirty_field_paths).toContain('totalPremium')
  })
})
