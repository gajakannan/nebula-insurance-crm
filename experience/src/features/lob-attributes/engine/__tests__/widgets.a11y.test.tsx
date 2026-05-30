import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { axe } from 'jest-axe'
import {
  CheckboxWidget,
  DateWidget,
  MoneyMinorWidget,
  MultiSelectWidget,
  NumberWidget,
  ReadonlySummaryWidget,
  SectionWidget,
  SelectWidget,
  TextWidget,
  TextareaWidget,
} from '../widgets'
import { deriveOptions } from '../options'
import type { WidgetProps } from '../types'

const noop = () => {}

function renderAll(theme: 'light' | 'dark') {
  const opts = deriveOptions(['a', 'b'], { a: 'Option A', b: 'Option B' })
  const base: Omit<WidgetProps, 'value'> = { fieldPath: 'f', label: 'Field', onChange: noop }
  return render(
    <div className={theme === 'dark' ? 'dark' : undefined} data-theme={theme}>
      <SectionWidget {...base} value={undefined} />
      <TextWidget {...base} fieldPath="text" label="Text" value="hello" />
      <TextareaWidget {...base} fieldPath="ta" label="Notes" value="lines" />
      <NumberWidget {...base} fieldPath="num" label="Number" value={3} />
      <MoneyMinorWidget {...base} fieldPath="money" label="Limit" value={250000} />
      <SelectWidget {...base} fieldPath="sel" label="Band" value="a" options={opts} />
      <MultiSelectWidget {...base} fieldPath="multi" label="Controls" value={['a']} options={opts} />
      <CheckboxWidget {...base} fieldPath="chk" label="Enabled" value={true} />
      <DateWidget {...base} fieldPath="date" label="Date" value="2026-05-29" />
      <ReadonlySummaryWidget {...base} fieldPath="ro" label="Summary" value="resolved" />
    </div>,
  )
}

describe('MVP widget accessibility (F0036-S0002)', () => {
  it('has no axe violations in the light theme', async () => {
    const { container } = renderAll('light')
    await expect(axe(container)).resolves.toHaveNoViolations()
  })

  it('has no axe violations in the dark theme (theme smoke)', async () => {
    const { container } = renderAll('dark')
    await expect(axe(container)).resolves.toHaveNoViolations()
  })

  it('labels are programmatically associated for every control', () => {
    renderAll('light')
    expect(screen.getByLabelText('Text')).toBeInTheDocument()
    expect(screen.getByLabelText('Notes')).toBeInTheDocument()
    expect(screen.getByLabelText('Number')).toBeInTheDocument()
    expect(screen.getByLabelText('Limit')).toBeInTheDocument()
    expect(screen.getByLabelText('Band')).toBeInTheDocument()
    expect(screen.getByLabelText('Enabled')).toBeInTheDocument()
    expect(screen.getByLabelText('Date')).toBeInTheDocument()
  })

  it('controls are keyboard focusable (tab order / focus-visible)', async () => {
    render(<TextWidget fieldPath="text" label="Text" value="" onChange={noop} />)
    await userEvent.tab()
    expect(screen.getByLabelText('Text')).toHaveFocus()
  })

  it('error message is announced via role=alert and aria-describedby', () => {
    render(<TextWidget fieldPath="text" label="Text" value="" onChange={vi.fn()} error="Bad value" />)
    const input = screen.getByLabelText('Text')
    expect(input).toHaveAttribute('aria-describedby', 'widget-text-error')
    expect(screen.getByRole('alert')).toHaveAttribute('id', 'widget-text-error')
  })
})
