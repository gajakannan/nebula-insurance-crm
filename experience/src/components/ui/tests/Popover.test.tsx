import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { Popover } from '../Popover'

function setViewportWidth(width: number) {
  Object.defineProperty(window, 'innerWidth', {
    configurable: true,
    writable: true,
    value: width,
  })
  window.dispatchEvent(new Event('resize'))
}

describe('Popover', () => {
  beforeEach(() => {
    setViewportWidth(1280)
  })

  afterEach(() => {
    setViewportWidth(1280)
  })

  it('opens from a native trigger, traps focus, and closes on Escape', async () => {
    const user = userEvent.setup()

    render(
      <Popover
        trigger={<button type="button">Open broker popover</button>}
        contentAriaLabel="Broker details"
      >
        <button type="button">First action</button>
        <button type="button">Second action</button>
      </Popover>,
    )

    const trigger = screen.getByRole('button', { name: 'Open broker popover' })

    await user.click(trigger)

    const dialog = await screen.findByRole('dialog', { name: 'Broker details' })
    const firstAction = screen.getByRole('button', { name: 'First action' })
    const secondAction = screen.getByRole('button', { name: 'Second action' })

    expect(dialog).toBeInTheDocument()

    await waitFor(() => {
      expect(firstAction).toHaveFocus()
    })

    await user.tab()
    expect(secondAction).toHaveFocus()

    await user.tab()
    expect(firstAction).toHaveFocus()

    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Broker details' })).not.toBeInTheDocument()
    })
    await waitFor(() => {
      expect(trigger).toHaveFocus()
    })
  })

  it('supports non-native triggers and overlay dismissal on small screens', async () => {
    const user = userEvent.setup()
    setViewportWidth(500)

    render(
      <Popover trigger={<div>Open compact summary</div>} contentAriaLabel="Compact summary">
        <button type="button">Compact action</button>
      </Popover>,
    )

    const trigger = screen.getByRole('button', { name: 'Open compact summary' })
    trigger.focus()
    fireEvent.keyDown(trigger, { key: 'Enter' })

    const dialog = await screen.findByRole('dialog', { name: 'Compact summary' })
    expect(dialog).toHaveAttribute('aria-modal', 'true')

    await user.click(screen.getByRole('button', { name: 'Dismiss dialog' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Compact summary' })).not.toBeInTheDocument()
    })
  })

  it('closes when clicking outside the popover content', async () => {
    const user = userEvent.setup()

    render(
      <Popover trigger={<button type="button">Open details</button>} contentAriaLabel="Details">
        <button type="button">Inside action</button>
      </Popover>,
    )

    await user.click(screen.getByRole('button', { name: 'Open details' }))
    await screen.findByRole('dialog', { name: 'Details' })

    fireEvent.mouseDown(document.body)

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Details' })).not.toBeInTheDocument()
    })
  })
})
