import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  snapshotDirtyForm,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { ContactFormModal } from '../components/ContactFormModal'
import type { ContactDto } from '../types'

/**
 * F0036-S0008 — the canonical F0035 S0003 Contact-Edit restore scenario:
 * edit a contact field (the controlled-form analogue of F0035's illustrative
 * "Notes"), forced re-auth snapshots the dirty values, and on return the form
 * restores on mount with NO auto-replay (the user re-saves explicitly).
 */

const updateMutate = vi.fn()
vi.mock('../hooks/useCreateContact', () => ({
  useCreateContact: () => ({ mutateAsync: vi.fn(), isPending: false }),
}))
vi.mock('../hooks/useUpdateContact', () => ({
  useUpdateContact: () => ({ mutateAsync: updateMutate, isPending: false }),
}))
vi.mock('@/features/auth/useCurrentUser', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: [], brokerTenantId: null }),
}))

const contact: ContactDto = {
  id: 'c1',
  fullName: 'Jane Original',
  email: 'jane@example.com',
  phone: '+12025550000',
  role: 'Original Role',
  rowVersion: 'v1',
} as ContactDto

const FORM_KEY = 'contact:b1:c1'

function seedSnapshot(userId: string, role: string) {
  snapshotDirtyForm({
    user_id: userId,
    route: '/',
    form_key: FORM_KEY,
    form_values: { fullName: 'Jane Original', email: 'jane@example.com', phone: '+12025550000', role },
    dirty_field_paths: ['role'],
    snapshot_timestamp: new Date().toISOString(),
  })
}

beforeEach(() => {
  updateMutate.mockResolvedValue({})
  window.sessionStorage.clear()
})
afterEach(() => vi.clearAllMocks())

describe('ContactFormModal restore — F0036-S0008 (closes F0035 finding #1)', () => {
  it('restores the snapshot on mount (winning over the on-open reset) and does NOT auto-replay', async () => {
    seedSnapshot('u1', 'Edited Role')
    render(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    // Restored value wins over the server value the open-reset would have set.
    expect(screen.getByLabelText('Role')).toHaveValue('Edited Role')
    // No mutation was auto-replayed during the forced re-auth.
    expect(updateMutate).not.toHaveBeenCalled()

    // Explicit re-save persists the restored values.
    await userEvent.click(screen.getByRole('button', { name: 'Save Changes' }))
    expect(updateMutate).toHaveBeenCalledWith({
      contactId: 'c1',
      dto: { fullName: 'Jane Original', email: 'jane@example.com', phone: '+12025550000', role: 'Edited Role' },
      rowVersion: 'v1',
    })
  })

  it('waits to consume a modal snapshot until the parent reopens the form', () => {
    seedSnapshot('u1', 'Edited Role')
    const { rerender } = render(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open={false} onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )

    expect(screen.queryByLabelText('Role')).not.toBeInTheDocument()

    rerender(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )

    expect(screen.getByLabelText('Role')).toHaveValue('Edited Role')
  })

  it('does not restore another user’s snapshot (per-user isolation)', () => {
    seedSnapshot('someone-else', 'Edited Role')
    render(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    expect(screen.getByLabelText('Role')).toHaveValue('Original Role')
  })

  it('restores only from its own form_key (no cross-form contamination)', () => {
    // A snapshot for a different contact must not leak into this form.
    snapshotDirtyForm({
      user_id: 'u1', route: '/', form_key: 'contact:b1:c2',
      form_values: { fullName: 'X', email: 'x@x', phone: '+10000000000', role: 'Other Contact Role' },
      dirty_field_paths: ['role'], snapshot_timestamp: new Date().toISOString(),
    })
    render(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    expect(screen.getByLabelText('Role')).toHaveValue('Original Role')
  })

  it('round-trips: dirty edit -> forced-re-auth snapshot -> remount restores', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    const { unmount } = render(
      <DirtyFormRegistryProvider>
        <Grab />
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    fireEvent.change(screen.getByLabelText('Role'), { target: { value: 'Typed But Unsaved' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    unmount()

    // Return from forced re-auth: the form remounts and restores on mount.
    render(
      <DirtyFormRegistryProvider>
        <ContactFormModal brokerId="b1" contact={contact} open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    expect(screen.getByLabelText('Role')).toHaveValue('Typed But Unsaved')
  })
})
