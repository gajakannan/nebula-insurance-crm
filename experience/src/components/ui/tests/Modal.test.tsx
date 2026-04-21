import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Modal } from '../Modal'

function TestModal({
  open = true,
  onClose = vi.fn(),
}: {
  open?: boolean
  onClose?: () => void
}) {
  return (
    <div>
      <button type="button">Outside trigger</button>
      <Modal open={open} onClose={onClose} title="Edit broker">
        <button type="button">Primary action</button>
        <button type="button">Secondary action</button>
      </Modal>
    </div>
  )
}

describe('Modal', () => {
  it('traps focus and closes on Escape', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()

    render(<TestModal onClose={onClose} />)

    const closeButton = screen.getByRole('button', { name: 'Close dialog' })
    const primaryButton = screen.getByRole('button', { name: 'Primary action' })
    const secondaryButton = screen.getByRole('button', { name: 'Secondary action' })

    await waitFor(() => {
      expect(closeButton).toHaveFocus()
    })
    expect(document.body.style.overflow).toBe('hidden')

    await user.tab()
    expect(primaryButton).toHaveFocus()

    await user.tab()
    expect(secondaryButton).toHaveFocus()

    await user.tab()
    expect(closeButton).toHaveFocus()

    await user.tab({ shift: true })
    expect(secondaryButton).toHaveFocus()

    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('closes on backdrop click and does not render when closed', () => {
    const onClose = vi.fn()
    const { rerender } = render(<TestModal onClose={onClose} />)

    fireEvent.click(screen.getByRole('dialog').parentElement!)
    expect(onClose).toHaveBeenCalledTimes(1)

    rerender(<TestModal open={false} onClose={onClose} />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(document.body.style.overflow).toBe('')
  })
})
