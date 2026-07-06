import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CommunicationPanel } from '../CommunicationPanel'

const createCommunication = vi.fn()

vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({
    sub: 'user-1',
    email: 'underwriter@nebula.local',
    displayName: 'Underwriter',
    roles: ['Underwriter'],
    brokerTenantId: null,
  }),
}))

vi.mock('@/features/tasks', () => ({
  AssigneePicker: ({ onSelect }: { onSelect: (user: { userId: string; displayName: string; email: string | null }) => void }) => (
    <button
      type="button"
      onClick={() => onSelect({ userId: 'assignee-1', displayName: 'Case Owner', email: null })}
    >
      Pick assignee
    </button>
  ),
}))

vi.mock('../../hooks', () => ({
  useCommunicationHistory: () => ({
    isLoading: false,
    error: null,
    data: { data: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
  }),
  useCreateCommunication: () => ({ mutateAsync: createCommunication, isPending: false }),
  useCreateCommunicationFollowUp: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useCorrectCommunication: () => ({ mutateAsync: vi.fn(), isPending: false }),
}))

beforeEach(() => {
  createCommunication.mockReset()
  createCommunication.mockResolvedValue({})
})

describe('CommunicationPanel', () => {
  it('renders the empty history state for the scoped record', () => {
    render(<CommunicationPanel entityType="Account" entityId="account-1" entityLabel="Acme Holdings" />)

    expect(screen.getByText('Communications')).toBeInTheDocument()
    expect(screen.getByText('No communication activity captured for this record yet.')).toBeInTheDocument()
    expect(screen.getByText('0 captured · 0 follow-up links')).toBeInTheDocument()
  })

  it('submits a structured communication with primary link and follow-up linkage', async () => {
    const user = userEvent.setup()
    render(<CommunicationPanel entityType="Account" entityId="account-1" entityLabel="Acme Holdings" />)

    await user.click(screen.getByRole('button', { name: /add/i }))
    const dialog = within(screen.getByRole('dialog', { name: /add communication/i }))
    fireEvent.change(dialog.getByLabelText(/^Summary$/i), { target: { value: 'Broker confirmed renewal timing' } })
    fireEvent.change(dialog.getByLabelText(/Notes/i), { target: { value: 'Follow up with underwriting before next review.' } })
    fireEvent.change(dialog.getByLabelText(/^Participant$/i), { target: { value: 'Jane Broker' } })
    fireEvent.change(dialog.getByLabelText(/Participant email/i), { target: { value: 'jane@example.com' } })
    await user.click(dialog.getByLabelText(/Create follow-up task/i))
    fireEvent.change(dialog.getByLabelText(/Task title/i), { target: { value: 'Confirm renewal appetite' } })
    await user.click(dialog.getByRole('button', { name: /pick assignee/i }))
    await user.click(dialog.getByRole('button', { name: /save/i }))

    await waitFor(() => expect(createCommunication).toHaveBeenCalledTimes(1))
    expect(createCommunication.mock.calls[0][0]).toMatchObject({
      type: 'Note',
      direction: 'Internal',
      summary: 'Broker confirmed renewal timing',
      body: 'Follow up with underwriting before next review.',
      participants: [{
        displayName: 'Jane Broker',
        email: 'jane@example.com',
        participantType: 'Other',
      }],
      links: [{ entityType: 'Account', entityId: 'account-1', isPrimary: true, label: 'Acme Holdings' }],
      followUp: {
        title: 'Confirm renewal appetite',
        assignedToUserId: 'assignee-1',
        linkedEntityType: 'Account',
        linkedEntityId: 'account-1',
      },
    })
  })
})
