import { useState, useEffect, useCallback, useRef } from 'react';
import { createPortal } from 'react-dom';
import { X, Trash2, Pencil, Check } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { Select } from '@/components/ui/Select';
import { TextInput } from '@/components/ui/TextInput';
import { AssigneePicker } from './AssigneePicker';
import { useUpdateTask, useDeleteTask } from '../hooks/useTaskMutations';
import { useCurrentUser } from '@/features/auth';
import { cn } from '@/lib/utils';
import { formatRelativeTime } from '@/lib/format';
import type {
  TaskDto,
  TaskStatus,
  TaskPriority,
  TaskUpdateRequest,
  UserSummaryDto,
} from '../types';

const MANAGER_ROLES = [
  'DistributionManager',
  'Admin',
];
function isManager(roles: string[]): boolean {
  return roles.some((r) => MANAGER_ROLES.includes(r));
}

function statusBadgeVariant(status: TaskStatus): 'default' | 'info' | 'success' {
  if (status === 'InProgress') return 'info';
  if (status === 'Done') return 'success';
  return 'default';
}

function priorityBadgeVariant(priority: TaskPriority): 'default' | 'warning' | 'error' {
  if (priority === 'High') return 'warning';
  if (priority === 'Urgent') return 'error';
  return 'default';
}

function formatStatusLabel(status: TaskStatus): string {
  if (status === 'InProgress') return 'In Progress';
  return status;
}

const PRIORITY_OPTIONS = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Urgent', label: 'Urgent' },
];

interface TaskDetailPanelProps {
  task: TaskDto;
  onClose: () => void;
}

export function TaskDetailPanel({ task, onClose }: TaskDetailPanelProps) {
  const currentUser = useCurrentUser();
  const { mutate: updateTask, isPending: isUpdating } = useUpdateTask();
  const { mutate: deleteTask, isPending: isDeleting } = useDeleteTask();

  const [editingTitle, setEditingTitle] = useState(false);
  const [titleValue, setTitleValue] = useState(task.title);
  const [editingDesc, setEditingDesc] = useState(false);
  const [descValue, setDescValue] = useState(task.description ?? '');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [reassignUser, setReassignUser] = useState<UserSummaryDto | null>(null);
  const [showReassign, setShowReassign] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  const canManage =
    currentUser &&
    (currentUser.sub === task.createdByUserId || isManager(currentUser.roles));

  const isOwn = currentUser?.sub === task.assignedToUserId;

  // Keep local state in sync when a different task is selected
  useEffect(() => {
    setTitleValue(task.title);
    setDescValue(task.description ?? '');
    setEditingTitle(false);
    setEditingDesc(false);
    setConfirmDelete(false);
    setShowReassign(false);
    setReassignUser(null);
  }, [task.id, task.title, task.description]);

  const handleClose = useCallback(() => onClose(), [onClose]);

  // Escape key handler
  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault();
        handleClose();
      }
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [handleClose]);

  function applyUpdate(body: TaskUpdateRequest) {
    updateTask({ id: task.id, body, rowVersion: task.rowVersion });
  }

  function handleStatusChange(newStatus: TaskStatus) {
    applyUpdate({ status: newStatus });
  }

  function handlePriorityChange(newPriority: TaskPriority) {
    applyUpdate({ priority: newPriority });
  }

  function commitTitle() {
    const trimmed = titleValue.trim();
    if (trimmed && trimmed !== task.title) {
      applyUpdate({ title: trimmed });
    } else {
      setTitleValue(task.title);
    }
    setEditingTitle(false);
  }

  function commitDesc() {
    const trimmed = descValue.trim();
    const current = task.description ?? '';
    if (trimmed !== current) {
      applyUpdate({ description: trimmed || null });
    }
    setEditingDesc(false);
  }

  function handleReassignSave() {
    if (reassignUser) {
      applyUpdate({ assignedToUserId: reassignUser.userId });
      setShowReassign(false);
      setReassignUser(null);
    }
  }

  function handleDelete() {
    deleteTask(task.id, {
      onSuccess: () => {
        handleClose();
      },
    });
  }

  const content = (
    <div
      ref={panelRef}
      role="dialog"
      aria-modal="true"
      aria-label={`Task detail: ${task.title}`}
      className="h-full flex flex-col bg-surface-card border-l border-surface-border"
    >
      {/* Header */}
      <div className="flex items-start justify-between gap-2 border-b border-surface-border px-5 py-4">
        <div className="min-w-0 flex-1">
          {editingTitle && canManage ? (
            <div className="flex items-center gap-2">
              <input
                autoFocus
                value={titleValue}
                onChange={(e) => setTitleValue(e.target.value)}
                onBlur={commitTitle}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') commitTitle();
                  if (e.key === 'Escape') {
                    setTitleValue(task.title);
                    setEditingTitle(false);
                  }
                }}
                className="flex-1 rounded-md border border-nebula-violet bg-surface-card px-2 py-1 text-sm font-semibold text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
              <button
                type="button"
                onClick={commitTitle}
                aria-label="Save title"
                className="text-nebula-violet"
              >
                <Check size={16} />
              </button>
            </div>
          ) : (
            <div className="flex items-start gap-2 group">
              <h2 className="text-sm font-semibold text-text-primary leading-snug line-clamp-3">
                {task.title}
              </h2>
              {canManage && (
                <button
                  type="button"
                  onClick={() => setEditingTitle(true)}
                  aria-label="Edit title"
                  className="mt-0.5 shrink-0 opacity-0 group-hover:opacity-100 text-text-muted hover:text-text-primary transition-opacity"
                >
                  <Pencil size={12} />
                </button>
              )}
            </div>
          )}
        </div>
        <button
          type="button"
          onClick={handleClose}
          aria-label="Close task detail"
          className="shrink-0 rounded-md p-1 text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
        >
          <X size={16} />
        </button>
      </div>

      {/* Scrollable body */}
      <div className="flex-1 overflow-y-auto px-5 py-4 space-y-5">
        {/* Status + Priority row */}
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <span className="text-xs text-text-muted">Status</span>
            <Badge variant={statusBadgeVariant(task.status)}>
              {formatStatusLabel(task.status)}
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-text-muted">Priority</span>
            <Badge variant={priorityBadgeVariant(task.priority)}>{task.priority}</Badge>
          </div>
          {task.dueDate && new Date(task.dueDate) < new Date() && task.status !== 'Done' && (
            <Badge variant="error">Overdue</Badge>
          )}
        </div>

        {/* Status action buttons — only the assignee can change status */}
        {isOwn && (
          <div className="flex flex-wrap gap-2">
            {task.status === 'Open' && (
              <button
                type="button"
                disabled={isUpdating}
                onClick={() => handleStatusChange('InProgress')}
                className="rounded-lg border border-status-info/40 bg-status-info/10 px-3 py-1.5 text-xs font-medium text-text-primary transition-colors hover:bg-status-info/20 disabled:opacity-50"
              >
                Start
              </button>
            )}
            {task.status === 'InProgress' && (
              <>
                <button
                  type="button"
                  disabled={isUpdating}
                  onClick={() => handleStatusChange('Done')}
                  className="rounded-lg border border-status-success/40 bg-status-success/10 px-3 py-1.5 text-xs font-medium text-text-primary transition-colors hover:bg-status-success/20 disabled:opacity-50"
                >
                  Mark Done
                </button>
                <button
                  type="button"
                  disabled={isUpdating}
                  onClick={() => handleStatusChange('Open')}
                  className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-highlight disabled:opacity-50"
                >
                  Reopen
                </button>
              </>
            )}
            {task.status === 'Done' && (
              <button
                type="button"
                disabled={isUpdating}
                onClick={() => handleStatusChange('Open')}
                className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-highlight disabled:opacity-50"
              >
                Reopen
              </button>
            )}
          </div>
        )}
        {canManage && !isOwn && (
          <p className="text-xs italic text-text-muted">Only the assignee can update status.</p>
        )}

        {/* Edit Priority */}
        {canManage && (
          <Select
            label="Priority"
            value={task.priority}
            onChange={(e) => handlePriorityChange(e.target.value as TaskPriority)}
            options={PRIORITY_OPTIONS}
          />
        )}

        {/* Due date */}
        {canManage ? (
          <TextInput
            label="Due Date"
            type="date"
            defaultValue={task.dueDate?.slice(0, 10) ?? ''}
            onBlur={(e) => {
              const newDate = e.target.value || null;
              if (newDate !== (task.dueDate?.slice(0, 10) ?? null)) {
                applyUpdate({ dueDate: newDate });
              }
            }}
          />
        ) : task.dueDate ? (
          <div className="space-y-0.5">
            <p className="text-xs text-text-muted">Due Date</p>
            <p className="text-sm text-text-primary">
              {new Date(task.dueDate).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
              })}
            </p>
          </div>
        ) : null}

        {/* Description */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <p className="text-xs font-medium text-text-secondary">Description</p>
            {canManage && !editingDesc && (
              <button
                type="button"
                onClick={() => setEditingDesc(true)}
                aria-label="Edit description"
                className="text-xs text-nebula-violet hover:underline"
              >
                Edit
              </button>
            )}
          </div>
          {editingDesc && canManage ? (
            <div className="space-y-2">
              <textarea
                autoFocus
                rows={4}
                value={descValue}
                onChange={(e) => setDescValue(e.target.value)}
                className="w-full rounded-lg border border-nebula-violet bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet resize-none"
              />
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setDescValue(task.description ?? '');
                    setEditingDesc(false);
                  }}
                  className="text-xs text-text-muted hover:text-text-secondary"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={commitDesc}
                  disabled={isUpdating}
                  className="rounded-md bg-nebula-violet px-3 py-1 text-xs font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-50"
                >
                  Save
                </button>
              </div>
            </div>
          ) : (
            <p className={cn(
              'text-sm leading-relaxed',
              task.description ? 'text-text-secondary' : 'italic text-text-muted',
            )}>
              {task.description || 'No description provided.'}
            </p>
          )}
        </div>

        {/* Assignee */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <p className="text-xs font-medium text-text-secondary">Assignee</p>
            {canManage && !showReassign && (
              <button
                type="button"
                onClick={() => setShowReassign(true)}
                aria-label="Reassign task"
                className="text-xs text-nebula-violet hover:underline"
              >
                Reassign
              </button>
            )}
          </div>
          {showReassign && canManage ? (
            <div className="space-y-2">
              <AssigneePicker
                label=""
                selectedUser={reassignUser}
                onSelect={setReassignUser}
              />
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowReassign(false);
                    setReassignUser(null);
                  }}
                  className="text-xs text-text-muted hover:text-text-secondary"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleReassignSave}
                  disabled={!reassignUser || isUpdating}
                  className="rounded-md bg-nebula-violet px-3 py-1 text-xs font-medium text-white hover:bg-nebula-violet/90 disabled:opacity-50"
                >
                  Save
                </button>
              </div>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-[10px] font-bold text-nebula-violet">
                {(task.assignedToDisplayName ?? task.assignedToUserId).charAt(0).toUpperCase()}
              </span>
              <span className="text-sm text-text-primary">
                {task.assignedToDisplayName ?? task.assignedToUserId}
              </span>
            </div>
          )}
        </div>

        {/* Created by */}
        <div className="space-y-0.5">
          <p className="text-xs text-text-muted">Created by</p>
          <p className="text-sm text-text-secondary">
            {task.createdByDisplayName ?? task.createdByUserId}
          </p>
        </div>

        {/* Linked entity */}
        {task.linkedEntityType && task.linkedEntityId && (
          <div className="space-y-0.5">
            <p className="text-xs text-text-muted">Linked Entity</p>
            <p className="text-sm text-text-secondary">
              {task.linkedEntityType} — {task.linkedEntityId}
            </p>
          </div>
        )}

        {/* Timestamps */}
        <div className="space-y-1 border-t border-surface-border pt-3">
          <p className="text-xs text-text-muted">
            Created {formatRelativeTime(task.createdAt)}
          </p>
          <p className="text-xs text-text-muted">
            Updated {formatRelativeTime(task.updatedAt)}
          </p>
          {task.completedAt && (
            <p className="text-xs text-text-muted">
              Completed {formatRelativeTime(task.completedAt)}
            </p>
          )}
        </div>
      </div>

      {/* Footer — delete */}
      {canManage && (
        <div className="border-t border-surface-border px-5 py-3">
          {confirmDelete ? (
            <div className="flex items-center justify-between gap-2">
              <p className="text-xs text-status-error">Delete this task? This cannot be undone.</p>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setConfirmDelete(false)}
                  className="text-xs text-text-muted hover:text-text-secondary"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleDelete}
                  disabled={isDeleting}
                  className="rounded-md border border-status-error/40 bg-status-error/10 px-3 py-1 text-xs font-medium text-status-error transition-colors hover:bg-status-error/20 disabled:opacity-50"
                >
                  {isDeleting ? 'Deleting...' : 'Confirm Delete'}
                </button>
              </div>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => setConfirmDelete(true)}
              className="flex items-center gap-1.5 text-xs text-text-muted transition-colors hover:text-status-error"
            >
              <Trash2 size={13} />
              Delete task
            </button>
          )}
        </div>
      )}
    </div>
  );

  return content;
}

/**
 * Drawer wrapper that renders the detail panel as a right-side overlay on
 * mobile/tablet, or as an inline panel on desktop (managed by the parent).
 */
export function TaskDetailDrawer({
  task,
  onClose,
}: {
  task: TaskDto;
  onClose: () => void;
}) {
  return createPortal(
    <div className="fixed inset-0 z-40 flex lg:hidden">
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden="true"
      />
      <div className="relative ml-auto w-full max-w-sm h-full shadow-2xl">
        <TaskDetailPanel task={task} onClose={onClose} />
      </div>
    </div>,
    document.body,
  );
}
