import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { TaskCreateModal } from '../TaskCreateModal'

const createTask = vi.fn()
vi.mock('../../hooks/useTaskMutations', () => ({
  useCreateTask: () => ({ mutate: createTask, isPending: false }),
}))
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'u1', email: 'u1@x', displayName: 'U1', roles: ['Underwriter'], brokerTenantId: null }),
}))
vi.mock('../AssigneePicker', () => ({
  AssigneePicker: () => <div data-testid="assignee-picker" />,
}))

beforeEach(() => window.sessionStorage.clear())
afterEach(() => vi.clearAllMocks())

describe('TaskCreateModal — S0007 wiring regression', () => {
  it('creates a task with the same payload as before wiring', async () => {
    render(<TaskCreateModal open onClose={vi.fn()} />)
    fireEvent.change(screen.getByLabelText(/Title/), { target: { value: 'My Task' } })
    await userEvent.click(screen.getByRole('button', { name: 'Create Task' }))
    expect(createTask).toHaveBeenCalledTimes(1)
    expect(createTask.mock.calls[0][0]).toEqual({
      title: 'My Task',
      description: undefined,
      priority: 'Normal',
      dueDate: undefined,
      assignedToUserId: 'u1',
      linkedEntityType: undefined,
      linkedEntityId: undefined,
    })
  })

  it('blocks submit and shows a validation error for an empty title (unchanged)', async () => {
    render(<TaskCreateModal open onClose={vi.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: 'Create Task' }))
    expect(screen.getByText('Title is required.')).toBeInTheDocument()
    expect(createTask).not.toHaveBeenCalled()
  })

  it('registers with F0035 so dirty values snapshot on a forced re-auth', () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <DirtyFormRegistryProvider>
        <Grab />
        <TaskCreateModal open onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    fireEvent.change(screen.getByLabelText(/Title/), { target: { value: 'Dirty Task' } })
    act(() => {
      registry?.snapshotAllDirty('u1', '/')
    })
    const snap = consumeFormSnapshot<{ title: string }>('u1', 'task:new')
    expect(snap?.form_values.title).toBe('Dirty Task')
    expect(snap?.dirty_field_paths).toContain('title')
  })
})
