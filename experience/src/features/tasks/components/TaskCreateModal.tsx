import { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { TextInput } from '@/components/ui/TextInput';
import { Select } from '@/components/ui/Select';
import { AssigneePicker } from './AssigneePicker';
import { useCreateTask } from '../hooks/useTaskMutations';
import type { TaskPriority, UserSummaryDto } from '../types';
import { useCurrentUser } from '@/features/auth';

const MANAGER_ROLES = [
  'DistributionManager',
  'Admin',
];

function isManager(roles: string[]): boolean {
  return roles.some((r) => MANAGER_ROLES.includes(r));
}

const PRIORITY_OPTIONS = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Urgent', label: 'Urgent' },
];

const ENTITY_TYPE_OPTIONS = [
  { value: '', label: '— none —' },
  { value: 'Broker', label: 'Broker' },
  { value: 'Submission', label: 'Submission' },
  { value: 'Renewal', label: 'Renewal' },
];

interface TaskCreateModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormState {
  title: string;
  description: string;
  priority: TaskPriority;
  dueDate: string;
  linkedEntityType: string;
  linkedEntityId: string;
}

const EMPTY_FORM: FormState = {
  title: '',
  description: '',
  priority: 'Normal',
  dueDate: '',
  linkedEntityType: '',
  linkedEntityId: '',
};

export function TaskCreateModal({ open, onClose }: TaskCreateModalProps) {
  const currentUser = useCurrentUser();
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [assigneeUser, setAssigneeUser] = useState<UserSummaryDto | null>(null);
  const [errors, setErrors] = useState<Partial<Record<keyof FormState | 'assignee', string>>>({});

  const { mutate: createTask, isPending } = useCreateTask();

  const canAssignToOthers = currentUser ? isManager(currentUser.roles) : false;

  function patch(field: Partial<FormState>) {
    setForm((prev) => ({ ...prev, ...field }));
  }

  function validate(): boolean {
    const next: typeof errors = {};
    if (!form.title.trim()) next.title = 'Title is required.';
    if (!assigneeUser && !canAssignToOthers) {
      // Will be self-assigned; no error
    } else if (!assigneeUser && canAssignToOthers) {
      next.assignee = 'Please select an assignee.';
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!validate()) return;

    const assignedToUserId = assigneeUser?.userId ?? (currentUser?.sub ?? '');

    createTask(
      {
        title: form.title.trim(),
        description: form.description.trim() || undefined,
        priority: form.priority,
        dueDate: form.dueDate || undefined,
        assignedToUserId,
        linkedEntityType: form.linkedEntityType || undefined,
        linkedEntityId: form.linkedEntityId.trim() || undefined,
      },
      {
        onSuccess: () => {
          setForm(EMPTY_FORM);
          setAssigneeUser(null);
          setErrors({});
          onClose();
        },
      },
    );
  }

  function handleClose() {
    setForm(EMPTY_FORM);
    setAssigneeUser(null);
    setErrors({});
    onClose();
  }

  // Build a synthetic UserSummaryDto from the current user for read-only display
  const selfUser: UserSummaryDto | null = currentUser
    ? {
        userId: currentUser.sub,
        displayName: currentUser.displayName,
        email: currentUser.email,
        roles: currentUser.roles,
        isActive: true,
      }
    : null;

  return (
    <Modal open={open} onClose={handleClose} title="Create Task">
      <form onSubmit={handleSubmit} className="space-y-4" noValidate>
        <TextInput
          label="Title"
          value={form.title}
          onChange={(e) => patch({ title: e.target.value })}
          error={errors.title}
          required
          placeholder="Enter task title..."
        />

        <div className="space-y-1.5">
          <label
            htmlFor="task-description"
            className="block text-xs font-medium text-text-secondary"
          >
            Description
          </label>
          <textarea
            id="task-description"
            rows={3}
            value={form.description}
            onChange={(e) => patch({ description: e.target.value })}
            placeholder="Optional description..."
            className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted transition-colors focus:outline-none focus:ring-1 focus:ring-nebula-violet resize-none"
          />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Priority"
            value={form.priority}
            onChange={(e) => patch({ priority: e.target.value as TaskPriority })}
            options={PRIORITY_OPTIONS}
          />
          <TextInput
            label="Due Date"
            type="date"
            value={form.dueDate}
            onChange={(e) => patch({ dueDate: e.target.value })}
          />
        </div>

        {canAssignToOthers ? (
          <AssigneePicker
            label="Assign To"
            selectedUser={assigneeUser}
            onSelect={setAssigneeUser}
            required
            error={errors.assignee}
          />
        ) : (
          <AssigneePicker
            label="Assign To"
            selectedUser={selfUser}
            onSelect={() => {}}
            readOnly
          />
        )}

        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Linked Entity Type"
            value={form.linkedEntityType}
            onChange={(e) => patch({ linkedEntityType: e.target.value })}
            options={ENTITY_TYPE_OPTIONS}
            placeholder=""
          />
          <TextInput
            label="Linked Entity ID"
            value={form.linkedEntityId}
            onChange={(e) => patch({ linkedEntityId: e.target.value })}
            placeholder="UUID..."
            disabled={!form.linkedEntityType}
          />
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={handleClose}
            className="rounded-lg border border-surface-border bg-surface-card px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isPending}
            className="rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90 disabled:opacity-50"
          >
            {isPending ? 'Creating...' : 'Create Task'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
