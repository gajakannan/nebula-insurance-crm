import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  snapshotDirtyForm,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { DynamicAttributePanel } from '../DynamicAttributePanel'
import type { CyberLobAttributeValues } from '../../types'

const mockUseCyberSchemaBundle = vi.fn()
vi.mock('../../hooks/useLobSchemaBundle', () => ({
  useCyberSchemaBundle: (enabled: boolean) => mockUseCyberSchemaBundle(enabled),
}))

vi.mock('@/features/auth/useCurrentUser', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@nebula.local', displayName: 'U1', roles: [], brokerTenantId: null }),
}))

function bundleFile(file: string) {
  return JSON.parse(
    readFileSync(resolve(process.cwd(), `../planning-mds/lob-schemas/cyber/1.0.0/${file}`), 'utf-8'),
  )
}

function loadedBundle() {
  return {
    data: { status: 'Active', dataSchema: bundleFile('data-schema.json'), uiSchema: bundleFile('ui-schema.json') },
    isLoading: false,
    isError: false,
  }
}

function validValues(): CyberLobAttributeValues {
  return {
    revenueBand: '10-50M',
    recordsHeld: '5000',
    mfaEnabled: true,
    mfaMaturity: 'Implemented',
    edrEnabled: true,
    backupEnabled: true,
    trainingFrequency: 'Annual',
    requestedLimit: '5000000',
    requestedRetention: '100000',
  }
}

const FORM_KEY = 'cyber-attributes:/' // route is '/' in jsdom

let registry: DirtyFormRegistry | undefined
function RegistryGrabber() {
  registry = useDirtyFormRegistry()
  return null
}

function Host() {
  const [v, setV] = useState<CyberLobAttributeValues>(validValues())
  return (
    <>
      <DynamicAttributePanel lineOfBusiness="Cyber" value={v} onChange={setV} />
      <output data-testid="records">{v.recordsHeld}</output>
    </>
  )
}

beforeEach(() => {
  mockUseCyberSchemaBundle.mockReturnValue(loadedBundle())
  window.sessionStorage.clear()
  registry = undefined
})
afterEach(() => vi.clearAllMocks())

describe('DynamicAttributePanel F0035 preservation (F0036-S0006)', () => {
  it('snapshots the dirty attribute form on a forced re-auth (registry.snapshotAllDirty)', async () => {
    render(
      <DirtyFormRegistryProvider>
        <RegistryGrabber />
        <Host />
      </DirtyFormRegistryProvider>,
    )

    // Make the form dirty (single deterministic change).
    fireEvent.change(screen.getByLabelText(/Records held/), { target: { value: '9999' } })

    // Simulate F0035's forced-re-auth snapshot of all dirty forms.
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })

    const snap = consumeFormSnapshot<Record<string, unknown>>('u1', FORM_KEY)
    expect(snap).not.toBeNull()
    expect(snap?.form_values).toMatchObject({ recordsHeld: 9999 })
    // dirty-field paths were flattened from RHF.
    expect(snap?.dirty_field_paths).toContain('recordsHeld')
  })

  it('rehydrates a prior snapshot on mount and shows the restore notice (no auto-replay)', () => {
    // A snapshot exists from before the forced re-auth.
    snapshotDirtyForm({
      user_id: 'u1',
      route: '/',
      form_key: FORM_KEY,
      form_values: { ...{ revenueBand: '10-50M', recordsHeld: 4242, controls: validControls(), requestedLimit: money(5_000_000_00), requestedRetention: money(100_000_00) } },
      dirty_field_paths: ['recordsHeld'],
      snapshot_timestamp: new Date().toISOString(),
    })

    render(
      <DirtyFormRegistryProvider>
        <Host />
      </DirtyFormRegistryProvider>,
    )

    // Values are rehydrated via consumeFormSnapshot and the F0035 notice appears.
    expect(screen.getByTestId('records')).toHaveTextContent('4242')
    expect(screen.getByLabelText(/Records held/)).toHaveValue(4242)
    // (note: <output> carries an implicit role="status", so match the notice by text)
    expect(screen.getByText(/saved your edits/i)).toBeInTheDocument()
    // No host Save button is auto-clicked — the engine never auto-replays.
    expect(screen.queryByRole('button', { name: 'Save' })).toBeNull()
  })

  it('does not rehydrate another user’s snapshot (per-user isolation)', () => {
    snapshotDirtyForm({
      user_id: 'someone-else',
      route: '/',
      form_key: FORM_KEY,
      form_values: { recordsHeld: 1 },
      dirty_field_paths: ['recordsHeld'],
      snapshot_timestamp: new Date().toISOString(),
    })

    render(
      <DirtyFormRegistryProvider>
        <Host />
      </DirtyFormRegistryProvider>,
    )

    // u1 sees their own starting value, not the other user's snapshot.
    expect(screen.getByTestId('records')).toHaveTextContent('5000')
    expect(screen.queryByText(/saved your edits/i)).toBeNull()
  })
})

function validControls() {
  return { mfaEnabled: true, mfaMaturity: 'Implemented', edrEnabled: true, backupEnabled: true, trainingFrequency: 'Annual' }
}
function money(amountMinor: number) {
  return { amountMinor, currency: 'USD' }
}
