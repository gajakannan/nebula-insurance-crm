import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { ApiError } from '@/services/api'
import { EditBrokerModal } from '../components/EditBrokerModal'
import type { BrokerDto } from '../types'

const updateMutate = vi.fn()
vi.mock('../hooks/useUpdateBroker', () => ({
  useUpdateBroker: () => ({ mutateAsync: updateMutate, isPending: false }),
}))
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))

const broker: BrokerDto = {
  id: 'br1',
  legalName: 'Acme',
  state: 'CA',
  status: 'Active',
  email: 'a@example.com',
  phone: '+12025550000',
  licenseNumber: 'LIC123',
  rowVersion: 'v1',
} as BrokerDto

beforeEach(() => {
  updateMutate.mockResolvedValue({})
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('EditBrokerModal — S0007 wiring regression', () => {
  it('updates a broker with the same payload (incl. rowVersion)', async () => {
    render(<EditBrokerModal broker={broker} open onClose={vi.fn()} />)
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: 'Acme Corp' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save Changes' }))
    expect(updateMutate).toHaveBeenCalledWith({
      brokerId: 'br1',
      dto: { legalName: 'Acme Corp', state: 'CA', status: 'Active', email: 'a@example.com', phone: '+12025550000' },
      rowVersion: 'v1',
    })
  })

  it('blocks submit with a validation error when legal name is cleared (unchanged)', async () => {
    render(<EditBrokerModal broker={broker} open onClose={vi.fn()} />)
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: '' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save Changes' }))
    expect(updateMutate).not.toHaveBeenCalled()
  })

  it('still surfaces a server error on submit failure (error path unchanged)', async () => {
    updateMutate.mockReset()
    updateMutate.mockRejectedValue(new ApiError(409, null))
    render(<EditBrokerModal broker={broker} open onClose={vi.fn()} />)
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: 'Acme Corp' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save Changes' }))
    expect(await screen.findByText(/modified by another user|Unable to update broker/i)).toBeInTheDocument()
  })

  it('registers with F0035 so dirty values snapshot on a forced re-auth', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <DirtyFormRegistryProvider>
        <Grab />
        <EditBrokerModal broker={broker} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    fireEvent.change(screen.getByLabelText(/Legal Name/), { target: { value: 'Dirty Co' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ legalName: string }>('u1', 'broker:br1')
    expect(snap?.form_values.legalName).toBe('Dirty Co')
    expect(snap?.dirty_field_paths).toContain('legalName')
  })
})
