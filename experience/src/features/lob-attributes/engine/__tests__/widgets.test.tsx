import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  CheckboxWidget,
  MoneyMinorWidget,
  MultiSelectWidget,
  NumberWidget,
  ReadonlySummaryWidget,
  SelectWidget,
  TextWidget,
  registerMvpWidgets,
  MVP_WIDGET_NAMES,
} from '../widgets'
import { assertOptionsSubsetOfEnum, deriveOptions } from '../options'
import { createWidgetRegistry, UnknownWidgetOptionError } from '../widgetRegistry'

describe('MVP widget vocabulary (F0036-S0002)', () => {
  it('registers all ten MVP widgets', () => {
    const registry = registerMvpWidgets(createWidgetRegistry())
    for (const name of MVP_WIDGET_NAMES) {
      expect(registry.has(name)).toBe(true)
    }
    expect(registry.registeredNames().sort()).toEqual([...MVP_WIDGET_NAMES].sort())
  })

  it('text widget propagates input to onChange (engine-controlled, not lifted)', async () => {
    const onChange = vi.fn()
    render(<TextWidget fieldPath="company" label="Company" value="" onChange={onChange} />)
    await userEvent.type(screen.getByLabelText('Company'), 'Acme')
    expect(onChange).toHaveBeenCalled()
    // Controlled input: last call carries the latest keystroke value.
    expect(onChange.mock.calls.at(-1)?.[0]).toBe('e')
  })

  it('number widget emits a number, empty clears to undefined', async () => {
    const onChange = vi.fn()
    render(<NumberWidget fieldPath="recordsHeld" label="Records held" value={undefined} onChange={onChange} />)
    const input = screen.getByLabelText('Records held')
    await userEvent.type(input, '5')
    expect(onChange).toHaveBeenLastCalledWith(5)
  })

  it('money-minor stores integer minor units while displaying the major unit (round-trip)', async () => {
    const onChange = vi.fn()
    const { rerender } = render(
      <MoneyMinorWidget fieldPath="requestedLimit" label="Requested limit" value={250000} onChange={onChange} />,
    )
    // 250000 minor units => $2500.00 displayed.
    expect(screen.getByLabelText('Requested limit')).toHaveValue(2500)
    // Clearing the populated field stores undefined (no zero-coercion).
    fireEvent.change(screen.getByLabelText('Requested limit'), { target: { value: '' } })
    expect(onChange).toHaveBeenLastCalledWith(undefined)
    // Entering $12.34 stores 1234 minor units (single change event — the engine/RHF
    // owns value accumulation; the widget only converts major -> minor).
    rerender(
      <MoneyMinorWidget fieldPath="requestedLimit" label="Requested limit" value={undefined} onChange={onChange} />,
    )
    fireEvent.change(screen.getByLabelText('Requested limit'), { target: { value: '12.34' } })
    expect(onChange).toHaveBeenLastCalledWith(1234)
  })

  it('checkbox widget toggles boolean', async () => {
    const onChange = vi.fn()
    render(<CheckboxWidget fieldPath="mfaEnabled" label="MFA enabled" value={false} onChange={onChange} />)
    await userEvent.click(screen.getByLabelText('MFA enabled'))
    expect(onChange).toHaveBeenLastCalledWith(true)
  })

  it('select widget renders derived options and emits the chosen value', async () => {
    const onChange = vi.fn()
    const options = deriveOptions(['low', 'high'], { low: 'Low', high: 'High' })
    render(
      <SelectWidget fieldPath="revenueBand" label="Revenue band" value="" onChange={onChange} options={options} />,
    )
    await userEvent.selectOptions(screen.getByLabelText('Revenue band'), 'high')
    expect(onChange).toHaveBeenLastCalledWith('high')
  })

  it('multi-select toggles membership in a string array', async () => {
    const onChange = vi.fn()
    const options = deriveOptions(['a', 'b'])
    render(
      <MultiSelectWidget fieldPath="controls" label="Controls" value={['a']} onChange={onChange} options={options} />,
    )
    await userEvent.click(screen.getByLabelText('b'))
    expect(onChange).toHaveBeenLastCalledWith(['a', 'b'])
  })

  it('required widget exposes a required affordance and renders an inline error slot', () => {
    render(<TextWidget fieldPath="company" label="Company" value="" onChange={vi.fn()} required error="Required" />)
    const input = screen.getByLabelText(/Company/)
    expect(input).toHaveAttribute('aria-required', 'true')
    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(screen.getByRole('alert')).toHaveTextContent('Required')
  })

  it('disabled widget renders a non-editable control', () => {
    render(<TextWidget fieldPath="company" label="Company" value="x" onChange={vi.fn()} disabled />)
    expect(screen.getByLabelText('Company')).toBeDisabled()
  })

  it('read-only summary renders a value without an editable control', () => {
    render(<ReadonlySummaryWidget fieldPath="band" label="Band" value="high" onChange={vi.fn()} />)
    expect(screen.queryByRole('textbox')).toBeNull()
    expect(screen.getByText('high')).toBeInTheDocument()
  })

  it('fails closed: a configured option outside the data-schema enum throws', () => {
    const enumValues = ['low', 'high']
    expect(() => assertOptionsSubsetOfEnum('select', deriveOptions(['low', 'high']), enumValues)).not.toThrow()
    expect(() =>
      assertOptionsSubsetOfEnum('select', [{ value: 'rogue', label: 'Rogue' }], enumValues),
    ).toThrow(UnknownWidgetOptionError)
  })
})
