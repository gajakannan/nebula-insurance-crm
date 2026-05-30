import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SchemaDrivenForm } from '../SchemaDrivenForm'
import { createWidgetRegistry } from '../widgetRegistry'
import { registerMvpWidgets } from '../widgets'
import type { PinnedBundle } from '../types'

function bundleFile(file: string) {
  return JSON.parse(
    readFileSync(resolve(process.cwd(), `../planning-mds/lob-schemas/cyber/1.0.0/${file}`), 'utf-8'),
  )
}

function pinnedBundle(): PinnedBundle {
  return {
    productVersionId: 'cyber/1.0.0',
    stage: 'Active',
    dataSchema: bundleFile('data-schema.json'),
    uiSchema: bundleFile('ui-schema.json'),
  }
}

function validAttributes() {
  return {
    revenueBand: '10-50M',
    recordsHeld: 5000,
    controls: {
      mfaEnabled: true,
      mfaMaturity: 'Implemented',
      edrEnabled: true,
      backupEnabled: true,
      trainingFrequency: 'Annual',
    },
    requestedLimit: { amountMinor: 500_000_000, currency: 'USD' },
    requestedRetention: { amountMinor: 10_000_000, currency: 'USD' },
  }
}

const registry = registerMvpWidgets(createWidgetRegistry())

describe('SchemaDrivenForm (F0036-S0003)', () => {
  it('renders fields, sections, and labels entirely from the bundle (no hardcoded list)', () => {
    render(<SchemaDrivenForm bundle={pinnedBundle()} registry={registry} value={validAttributes()} />)
    // Sections from ui-schema.
    expect(screen.getByText('Exposure')).toBeInTheDocument()
    expect(screen.getByText('Controls')).toBeInTheDocument()
    expect(screen.getByText('Requested Terms')).toBeInTheDocument()
    // Fields + derived widgets, labelled from ui-schema fieldLabels. Required
    // fields carry an aria-hidden "*", so match the label by regex.
    expect(screen.getByLabelText(/Revenue band/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Records held/)).toBeInTheDocument()
    expect(screen.getByLabelText(/MFA enabled/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Requested limit/)).toBeInTheDocument()
  })

  it('blocks its own submit affordance while data-schema-invalid and shows an inline error', () => {
    const invalid = { ...validAttributes(), revenueBand: undefined }
    render(<SchemaDrivenForm bundle={pinnedBundle()} registry={registry} value={invalid} onSubmit={vi.fn()} />)
    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
    expect(screen.getAllByRole('alert').length).toBeGreaterThan(0)
  })

  it('enables submit and calls onSubmit when data-schema-valid', async () => {
    const onSubmit = vi.fn()
    render(<SchemaDrivenForm bundle={pinnedBundle()} registry={registry} value={validAttributes()} onSubmit={onSubmit} />)
    const button = screen.getByRole('button', { name: 'Save' })
    expect(button).toBeEnabled()
    await userEvent.click(button)
    expect(onSubmit).toHaveBeenCalledTimes(1)
  })

  it('binds a backend-authoritative cross-field error to its field by pointer (submit stays enabled)', () => {
    render(
      <SchemaDrivenForm
        bundle={pinnedBundle()}
        registry={registry}
        value={validAttributes()}
        lobErrors={[
          {
            code: 'mfa_required_for_high_record_count',
            path: '$.attributes.controls.mfaEnabled',
            message: 'MFA is required when recordsHeld is at least 1,000,000.',
          },
        ]}
        onSubmit={vi.fn()}
      />,
    )
    // Client data-schema layer is valid -> submit enabled; the backend message renders inline.
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled()
    expect(screen.getByText(/MFA is required when recordsHeld/)).toBeInTheDocument()
  })

  it('read-only mode renders no submit affordance and disables controls', () => {
    render(<SchemaDrivenForm bundle={pinnedBundle()} registry={registry} value={validAttributes()} readOnly />)
    expect(screen.queryByRole('button', { name: 'Save' })).toBeNull()
    expect(screen.getByLabelText(/Revenue band/)).toBeDisabled()
  })

  it('fails closed on a malformed bundle (controlled error, not a guessed render)', () => {
    const broken = pinnedBundle()
    broken.uiSchema = { sections: [{ id: 'x', title: 'X', fields: ['nope'] }], fieldLabels: {} }
    render(<SchemaDrivenForm bundle={broken} registry={registry} value={{}} />)
    expect(screen.getByRole('alert')).toHaveTextContent(/Unable to render attributes/)
  })
})
