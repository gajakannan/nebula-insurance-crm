import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  DirtyFormRegistryProvider,
  useDirtyFormRegistry,
  consumeFormSnapshot,
  type DirtyFormRegistry,
} from '@/features/session-continuity'
import { TaskDetailPanel } from '../TaskDetailPanel'
import type { TaskDto } from '../../types'

const updateTask = vi.fn()
vi.mock('../../hooks/useTaskMutations', () => ({
  useUpdateTask: () => ({ mutate: updateTask, isPending: false }),
  useDeleteTask: () => ({ mutate: vi.fn(), isPending: false }),
}))
vi.mock('@/features/auth', () => ({
  useCurrentUser: () => ({ sub: 'creator-1', email: 'c@x', displayName: 'Creator', roles: [], brokerTenantId: null }),
}))
vi.mock('../AssigneePicker', () => ({ AssigneePicker: () => <div data-testid="assignee-picker" /> }))

const task: TaskDto = {
  id: 't1',
  title: 'Original Title',
  description: 'Original description',
  status: 'Open',
  priority: 'Normal',
  dueDate: null,
  assignedToUserId: 'creator-1',
  assignedToDisplayName: 'Creator',
  createdByUserId: 'creator-1',
  createdByDisplayName: 'Creator',
  linkedEntityType: null,
  linkedEntityId: null,
  linkedEntityName: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  completedAt: null,
  rowVersion: 1,
} as TaskDto

beforeEach(() => window.sessionStorage.clear())
afterEach(() => vi.clearAllMocks())

describe('TaskDetailPanel inline edit — S0007 wiring regression', () => {
  it('saves an inline title edit with the same update payload (unchanged)', async () => {
    render(<TaskDetailPanel task={task} onClose={vi.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: 'Edit title' }))
    fireEvent.change(screen.getByDisplayValue('Original Title'), { target: { value: 'Updated Title' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save title' }))
    expect(updateTask).toHaveBeenCalledWith({ id: 't1', body: { title: 'Updated Title' }, rowVersion: 1 })
  })

  it('registers the inline edit with F0035 so dirty values snapshot on a forced re-auth', async () => {
    let registry: DirtyFormRegistry | undefined
    function Grab() {
      registry = useDirtyFormRegistry()
      return null
    }
    render(
      <DirtyFormRegistryProvider>
        <Grab />
        <TaskDetailPanel task={task} onClose={vi.fn()} />
      </DirtyFormRegistryProvider>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'Edit title' }))
    fireEvent.change(screen.getByDisplayValue('Original Title'), { target: { value: 'Dirty Title' } })
    act(() => {
      registry?.snapshotAllDirty('creator-1', '/')
    })
    const snap = consumeFormSnapshot<{ title: string }>('creator-1', 'task:t1')
    expect(snap?.form_values.title).toBe('Dirty Title')
    expect(snap?.dirty_field_paths).toContain('title')
  })
})
