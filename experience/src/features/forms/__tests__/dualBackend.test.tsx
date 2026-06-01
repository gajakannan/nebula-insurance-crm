import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  snapshotDirtyForm,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { useRegisteredForm } from '../useRegisteredForm'
import { useControlledDirtyTracker } from '../useControlledDirtyTracker'
import { FormPreservation } from '@/features/lob-attributes/engine/FormPreservation'

/**
 * F0036-S0007 — both DirtyFormRegistration backends (RHF adapter for Workstream A,
 * controlled tracker for Workstream B) register, snapshot, and restore through
 * the single shared `useRegisteredForm` helper, equivalently.
 */

let registry: DirtyFormRegistry | undefined
function Grab() {
  registry = useDirtyFormRegistry()
  return null
}

function RhfBackend({ onRestore }: { onRestore: () => void }) {
  const form = useForm({ defaultValues: { notes: '' } })
  return (
    <>
      <FormPreservation
        form={form}
        preserve={{ userId: 'u1', formKey: 'rhf-key', route: '/r' }}
        onRestore={onRestore}
      />
      <input aria-label="rhf-notes" {...form.register('notes')} />
    </>
  )
}

function ControlledBackend({ onRestore }: { onRestore: () => void }) {
  const [v, setV] = useState({ notes: '' })
  const initialRef = useRef({ notes: '' })
  const tracker = useControlledDirtyTracker(v, initialRef.current)
  useRegisteredForm({
    registration: { formKey: 'ctrl-key', route: '/r', ...tracker },
    userId: 'u1',
    onRestore,
  })
  return <input aria-label="ctrl-notes" value={v.notes} onChange={(e) => setV({ notes: e.target.value })} />
}

function DynamicKeyBackend({
  enabled = true,
  formKey,
  onRestore,
}: {
  enabled?: boolean
  formKey: string
  onRestore: () => void
}) {
  const values = { notes: '' }
  const tracker = useControlledDirtyTracker(values, values)
  useRegisteredForm({
    registration: { formKey, route: '/r', ...tracker },
    userId: 'u1',
    enabled,
    onRestore,
  })
  return null
}

beforeEach(() => {
  window.sessionStorage.clear()
  registry = undefined
})
afterEach(() => vi.clearAllMocks())

describe('shared registration helper — dual backend (F0036-S0007)', () => {
  it('snapshots both an RHF form and a controlled form through the same helper', () => {
    render(
      <DirtyFormRegistryProvider>
        <Grab />
        <RhfBackend onRestore={vi.fn()} />
        <ControlledBackend onRestore={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )

    fireEvent.change(screen.getByLabelText('rhf-notes'), { target: { value: 'from-rhf' } })
    fireEvent.change(screen.getByLabelText('ctrl-notes'), { target: { value: 'from-controlled' } })

    act(() => {
      registry?.snapshotAllDirty('u1', '/r')
    })

    const rhfSnap = consumeFormSnapshot<{ notes: string }>('u1', 'rhf-key')
    const ctrlSnap = consumeFormSnapshot<{ notes: string }>('u1', 'ctrl-key')
    expect(rhfSnap?.form_values.notes).toBe('from-rhf')
    expect(rhfSnap?.dirty_field_paths).toContain('notes')
    expect(ctrlSnap?.form_values.notes).toBe('from-controlled')
    expect(ctrlSnap?.dirty_field_paths).toContain('notes')
  })

  it('restores both backends on mount through the same helper', () => {
    snapshotDirtyForm({
      user_id: 'u1', route: '/r', form_key: 'rhf-key',
      form_values: { notes: 'rhf-restored' }, dirty_field_paths: ['notes'],
      snapshot_timestamp: new Date().toISOString(),
    })
    snapshotDirtyForm({
      user_id: 'u1', route: '/r', form_key: 'ctrl-key',
      form_values: { notes: 'ctrl-restored' }, dirty_field_paths: ['notes'],
      snapshot_timestamp: new Date().toISOString(),
    })
    const rhfRestore = vi.fn()
    const ctrlRestore = vi.fn()

    render(
      <DirtyFormRegistryProvider>
        <RhfBackend onRestore={rhfRestore} />
        <ControlledBackend onRestore={ctrlRestore} />
      </DirtyFormRegistryProvider>,
    )

    expect(rhfRestore).toHaveBeenCalledTimes(1)
    expect(ctrlRestore).toHaveBeenCalledTimes(1)
  })

  it('restores when a modal parent changes the registered form key after mount', () => {
    snapshotDirtyForm({
      user_id: 'u1', route: '/r', form_key: 'ctrl-key:existing',
      form_values: { notes: 'restored' }, dirty_field_paths: ['notes'],
      snapshot_timestamp: new Date().toISOString(),
    })
    const restore = vi.fn()
    const { rerender } = render(<DynamicKeyBackend formKey="ctrl-key:new" onRestore={restore} />)

    expect(restore).not.toHaveBeenCalled()
    rerender(<DynamicKeyBackend formKey="ctrl-key:existing" onRestore={restore} />)

    expect(restore).toHaveBeenCalledTimes(1)
  })

  it('does not consume a modal snapshot until registration is enabled', () => {
    snapshotDirtyForm({
      user_id: 'u1', route: '/r', form_key: 'modal-key',
      form_values: { notes: 'restored' }, dirty_field_paths: ['notes'],
      snapshot_timestamp: new Date().toISOString(),
    })
    const restore = vi.fn()
    const { rerender } = render(<DynamicKeyBackend enabled={false} formKey="modal-key" onRestore={restore} />)

    expect(restore).not.toHaveBeenCalled()
    rerender(<DynamicKeyBackend enabled formKey="modal-key" onRestore={restore} />)

    expect(restore).toHaveBeenCalledTimes(1)
  })
})
