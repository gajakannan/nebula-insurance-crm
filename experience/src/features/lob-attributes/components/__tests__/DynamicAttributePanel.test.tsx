import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DynamicAttributePanel } from '../DynamicAttributePanel'
import { emptyCyberLobAttributes } from '../../lib/cyber'
import type { CyberLobAttributeValues } from '../../types'

const mockUseCyberSchemaBundle = vi.fn()
vi.mock('../../hooks/useLobSchemaBundle', () => ({
  useCyberSchemaBundle: (enabled: boolean) => mockUseCyberSchemaBundle(enabled),
}))

function bundleFile(file: string) {
  return JSON.parse(
    readFileSync(resolve(process.cwd(), `../planning-mds/lob-schemas/cyber/1.0.0/${file}`), 'utf-8'),
  )
}

function loadedBundle() {
  return {
    data: {
      status: 'Active',
      dataSchema: bundleFile('data-schema.json'),
      uiSchema: bundleFile('ui-schema.json'),
    },
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

beforeEach(() => mockUseCyberSchemaBundle.mockReturnValue(loadedBundle()))
afterEach(() => vi.clearAllMocks())

describe('DynamicAttributePanel engine swap regression (F0036-S0005)', () => {
  it('renders nothing for a non-Cyber line of business (unchanged)', () => {
    const { container } = render(<DynamicAttributePanel lineOfBusiness="Property" value={emptyCyberLobAttributes()} />)
    expect(container).toBeEmptyDOMElement()
  })

  it('renders all Cyber fields from the bundle (no hardcoded list) with the Cyber heading', () => {
    render(<DynamicAttributePanel lineOfBusiness="Cyber" value={validValues()} onChange={vi.fn()} />)
    expect(screen.getByText('Cyber attributes')).toBeInTheDocument()
    expect(screen.getByLabelText(/Revenue band/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Records held/)).toBeInTheDocument()
    expect(screen.getByLabelText(/MFA enabled/)).toBeInTheDocument()
    expect(screen.getByLabelText(/MFA maturity/)).toBeInTheDocument()
    expect(screen.getByLabelText(/EDR enabled/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Offline backups/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Training frequency/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Requested limit/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Requested retention/)).toBeInTheDocument()
  })

  it('read-only context renders disabled controls and no host-independent Save button', () => {
    render(<DynamicAttributePanel lineOfBusiness="Cyber" value={validValues()} readOnly />)
    expect(screen.getByLabelText(/Revenue band/)).toBeDisabled()
    expect(screen.queryByRole('button', { name: 'Save' })).toBeNull()
  })

  it('preserves MFA-maturity conditional gating (disabled when MFA off, enabled when on)', () => {
    const off = { ...validValues(), mfaEnabled: false }
    const { rerender } = render(<DynamicAttributePanel lineOfBusiness="Cyber" value={off} onChange={vi.fn()} />)
    expect(screen.getByLabelText(/MFA maturity/)).toBeDisabled()
    rerender(<DynamicAttributePanel lineOfBusiness="Cyber" value={validValues()} onChange={vi.fn()} />)
    expect(screen.getByLabelText(/MFA maturity/)).toBeEnabled()
  })

  it('round-trips edits back to the flat CyberLobAttributeValues shape', async () => {
    function Host() {
      const [v, setV] = useState<CyberLobAttributeValues>(validValues())
      return (
        <>
          <DynamicAttributePanel lineOfBusiness="Cyber" value={v} onChange={setV} />
          <output data-testid="band">{v.revenueBand}</output>
        </>
      )
    }
    render(<Host />)
    await userEvent.selectOptions(screen.getByLabelText(/Revenue band/), '250M+')
    expect(screen.getByTestId('band')).toHaveTextContent('250M+')
  })

  it('binds host-supplied (backend) errors to fields by pointer', () => {
    render(
      <DynamicAttributePanel
        lineOfBusiness="Cyber"
        value={validValues()}
        onChange={vi.fn()}
        errors={{ mfaEnabled: 'MFA is required for high record counts.' }}
      />,
    )
    expect(screen.getByText('MFA is required for high record counts.')).toBeInTheDocument()
  })

  it('renders a controlled error (not a guessed form) when the bundle fails to load', () => {
    mockUseCyberSchemaBundle.mockReturnValue({ data: undefined, isLoading: false, isError: true })
    render(<DynamicAttributePanel lineOfBusiness="Cyber" value={emptyCyberLobAttributes()} />)
    expect(screen.getByRole('alert')).toHaveTextContent(/unavailable/i)
    expect(screen.queryByLabelText(/Revenue band/)).toBeNull()
  })

  it('shows a loading region while the bundle resolves', () => {
    mockUseCyberSchemaBundle.mockReturnValue({ data: undefined, isLoading: true, isError: false })
    render(<DynamicAttributePanel lineOfBusiness="Cyber" value={emptyCyberLobAttributes()} />)
    expect(screen.getByText(/Loading Cyber attributes/)).toBeInTheDocument()
  })
})
