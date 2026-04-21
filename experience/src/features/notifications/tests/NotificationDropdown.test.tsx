import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { NotificationDropdown } from '../components/NotificationDropdown'

describe('NotificationDropdown', () => {
  it('opens, filters, toggles read state, and clears notifications', async () => {
    const user = userEvent.setup()

    render(<NotificationDropdown />)

    const trigger = screen.getByRole('button', { name: 'Notifications' })
    expect(trigger).toHaveTextContent('3')

    await user.click(trigger)

    expect(screen.getByText('Notifications')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Mark all read' })).toBeEnabled()

    await user.click(screen.getByRole('button', { name: 'Unread' }))
    expect(screen.getByText('Broker created')).toBeInTheDocument()
    expect(screen.queryByText('Data sync warning')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Assigned' }))
    expect(screen.getByText('Task overdue')).toBeInTheDocument()
    expect(screen.queryByText('Opportunity moved stage')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'All' }))
    await user.click(screen.getByRole('button', { name: 'Open broker' }))
    expect(trigger).toHaveTextContent('2')

    await user.click(screen.getByRole('button', { name: 'Mark all read' }))
    expect(trigger).not.toHaveTextContent(/[1-9]/)

    await user.click(screen.getByRole('button', { name: 'Clear all' }))
    expect(screen.getByText("You're all caught up.")).toBeInTheDocument()
  })

  it('supports dismiss, closes on Escape, and closes on outside click', async () => {
    const user = userEvent.setup()

    render(<NotificationDropdown />)

    await user.click(screen.getByRole('button', { name: 'Notifications' }))
    expect(screen.getByText('Broker created')).toBeInTheDocument()

    await user.click(screen.getAllByRole('button', { name: 'Dismiss' })[0])
    expect(screen.queryByText('Broker created')).not.toBeInTheDocument()

    fireEvent.keyDown(document, { key: 'Escape' })
    await waitFor(() => {
      expect(screen.queryByText('Notifications')).not.toBeInTheDocument()
    })

    await user.click(screen.getByRole('button', { name: 'Notifications' }))
    expect(screen.getByText('Notifications')).toBeInTheDocument()

    fireEvent.mouseDown(document.body)
    await waitFor(() => {
      expect(screen.queryByText('Notifications')).not.toBeInTheDocument()
    })
  })
})
