import { ChevronUp, ChevronDown, ChevronsUpDown } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useTaskList } from '../hooks/useTaskList';
import { useUpdateTask } from '../hooks/useTaskMutations';
import { useCurrentUser } from '@/features/auth';
import { cn } from '@/lib/utils';
import { getEntityPath } from '@/lib/navigation';
import { Link } from 'react-router-dom';
import type { MouseEvent } from 'react';
import type {
  TaskListFilters,
  TaskListItemDto,
  TaskStatus,
  TaskPriority,
  TaskView,
} from '../types';

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

function daysDiff(dateStr: string): number {
  const now = Date.now();
  const due = new Date(dateStr).getTime();
  return Math.ceil((now - due) / (1000 * 60 * 60 * 24));
}

type SortDir = 'asc' | 'desc';

interface SortHeaderProps {
  column: string;
  label: string;
  currentSort: string;
  currentDir: SortDir;
  onSort: (col: string) => void;
}

function SortHeader({ column, label, currentSort, currentDir, onSort }: SortHeaderProps) {
  const active = currentSort === column;
  return (
    <th
      scope="col"
      className="pb-3 pr-3 text-left text-xs font-medium uppercase tracking-wider text-text-muted"
    >
      <button
        type="button"
        onClick={() => onSort(column)}
        className="flex items-center gap-1 hover:text-text-secondary transition-colors"
        aria-label={`Sort by ${label}`}
      >
        {label}
        {active ? (
          currentDir === 'asc' ? (
            <ChevronUp size={12} className="text-nebula-violet" />
          ) : (
            <ChevronDown size={12} className="text-nebula-violet" />
          )
        ) : (
          <ChevronsUpDown size={12} className="opacity-40" />
        )}
      </button>
    </th>
  );
}

interface TaskCenterListProps {
  filters: TaskListFilters;
  view: TaskView;
  selectedTaskId: string | null;
  onSelectTask: (task: TaskListItemDto) => void;
  onFiltersChange: (patch: Partial<TaskListFilters>) => void;
  onCreateClick: () => void;
}

export function TaskCenterList({
  filters,
  view,
  selectedTaskId,
  onSelectTask,
  onFiltersChange,
  onCreateClick,
}: TaskCenterListProps) {
  const { data, isLoading, isError, refetch } = useTaskList(filters);
  const { mutate: updateTask } = useUpdateTask();
  const currentUser = useCurrentUser();

  function handleSort(column: string) {
    if (filters.sort === column) {
      onFiltersChange({
        sortDir: filters.sortDir === 'asc' ? 'desc' : 'asc',
        page: 1,
      });
    } else {
      onFiltersChange({ sort: column, sortDir: 'asc', page: 1 });
    }
  }

  // Optimistic status toggle for own tasks (Open <-> InProgress)
  function handleStatusToggle(task: TaskListItemDto) {
    if (task.status === 'Done') return;
    const next: TaskStatus = task.status === 'Open' ? 'InProgress' : 'Open';
    updateTask({
      id: task.id,
      body: { status: next },
      // We don't have rowVersion from the list DTO — use 0 to let the backend
      // handle it; in practice the detail panel is the canonical place for
      // concurrency-safe edits. This keeps the dashboard feel snappy.
      rowVersion: 0,
    });
  }

  const hasActiveFilters = Boolean(
    filters.status?.length ||
    filters.priority?.length ||
    filters.dueDateFrom ||
    filters.dueDateTo ||
    filters.overdue ||
    filters.assigneeId ||
    filters.linkedEntityType?.length,
  );

  if (isLoading) {
    return <TaskListSkeleton />;
  }

  if (isError) {
    return (
      <ErrorFallback
        message="Unable to load tasks. Please try again."
        onRetry={() => refetch()}
      />
    );
  }

  if (!data) return null;

  if (data.data.length === 0) {
    return (
      <EmptyState
        view={view}
        hasActiveFilters={hasActiveFilters}
        onClearFilters={() =>
          onFiltersChange({
            status: [],
            priority: [],
            dueDateFrom: undefined,
            dueDateTo: undefined,
            overdue: undefined,
            assigneeId: undefined,
            linkedEntityType: [],
            page: 1,
          })
        }
        onCreateClick={onCreateClick}
      />
    );
  }

  const startItem = (filters.page - 1) * filters.pageSize + 1;
  const endItem = Math.min(filters.page * filters.pageSize, data.totalCount);

  return (
    <div className="space-y-3">
      {/* Desktop table */}
      <div className="hidden md:block overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-surface-border">
              <th scope="col" className="pb-3 pr-3 w-8" aria-label="Status toggle" />
              <SortHeader
                column="title"
                label="Title"
                currentSort={filters.sort}
                currentDir={filters.sortDir}
                onSort={handleSort}
              />
              <SortHeader
                column="priority"
                label="Priority"
                currentSort={filters.sort}
                currentDir={filters.sortDir}
                onSort={handleSort}
              />
              <SortHeader
                column="dueDate"
                label="Due Date"
                currentSort={filters.sort}
                currentDir={filters.sortDir}
                onSort={handleSort}
              />
              {view === 'assignedByMe' && (
                <th scope="col" className="pb-3 pr-3 text-left text-xs font-medium uppercase tracking-wider text-text-muted">
                  Assignee
                </th>
              )}
              <th scope="col" className="pb-3 pr-3 text-left text-xs font-medium uppercase tracking-wider text-text-muted">
                Linked
              </th>
              <SortHeader
                column="createdAt"
                label="Created"
                currentSort={filters.sort}
                currentDir={filters.sortDir}
                onSort={handleSort}
              />
            </tr>
          </thead>
          <tbody className="divide-y divide-surface-border">
            {data.data.map((task) => (
              <DesktopTaskRow
                key={task.id}
                task={task}
                view={view}
                selected={task.id === selectedTaskId}
                isOwn={currentUser?.sub === task.assignedToUserId}
                onSelect={() => onSelectTask(task)}
                onStatusToggle={() => handleStatusToggle(task)}
              />
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile cards */}
      <div className="space-y-2 md:hidden">
        {data.data.map((task) => (
          <MobileTaskCard
            key={task.id}
            task={task}
            selected={task.id === selectedTaskId}
            onSelect={() => onSelectTask(task)}
          />
        ))}
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between border-t border-surface-border pt-3">
        <span className="text-xs text-text-muted">
          Showing {startItem}–{endItem} of {data.totalCount}
        </span>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => onFiltersChange({ page: filters.page - 1 })}
            disabled={filters.page <= 1}
            className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
          >
            Previous
          </button>
          <span className="text-xs text-text-muted">
            Page {data.page} of {data.totalPages}
          </span>
          <button
            type="button"
            onClick={() => onFiltersChange({ page: filters.page + 1 })}
            disabled={filters.page >= data.totalPages}
            className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}

function DesktopTaskRow({
  task,
  view,
  selected,
  isOwn,
  onSelect,
  onStatusToggle,
}: {
  task: TaskListItemDto;
  view: TaskView;
  selected: boolean;
  isOwn: boolean;
  onSelect: () => void;
  onStatusToggle: () => void;
}) {
  const linkedPath =
    task.linkedEntityType && task.linkedEntityId
      ? getEntityPath(task.linkedEntityType, task.linkedEntityId)
      : null;

  return (
    <tr
      onClick={onSelect}
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') onSelect();
      }}
      aria-selected={selected}
      className={cn(
        'cursor-pointer text-text-secondary transition-colors',
        selected
          ? 'bg-nebula-violet/10'
          : task.isOverdue && task.status !== 'Done'
            ? 'bg-status-error/5 hover:bg-status-error/10'
            : 'hover:bg-surface-highlight',
      )}
    >
      {/* Status dot toggle */}
      <td
        className="py-3 pr-3"
        onClick={(e) => {
          e.stopPropagation();
          if (isOwn) onStatusToggle();
        }}
      >
        <button
          type="button"
          disabled={!isOwn || task.status === 'Done'}
          aria-label={`Toggle status for ${task.title}`}
          className={cn(
            'h-4 w-4 rounded-full border-2 transition-colors',
            task.status === 'Done'
              ? 'border-status-success bg-status-success/40'
              : task.status === 'InProgress'
                ? 'border-status-info bg-status-info/30'
                : 'border-surface-border bg-transparent hover:border-nebula-violet',
          )}
        />
      </td>

      {/* Title */}
      <td className="py-3 pr-3 max-w-xs">
        <p
          className={cn(
            'truncate text-sm font-medium',
            task.status === 'Done'
              ? 'text-text-muted line-through'
              : task.isOverdue
                ? 'text-status-error'
                : 'text-text-primary',
          )}
        >
          {task.title}
        </p>
        {task.description && (
          <p className="mt-0.5 truncate text-xs text-text-muted">{task.description}</p>
        )}
      </td>

      {/* Priority */}
      <td className="py-3 pr-3">
        <Badge variant={priorityBadgeVariant(task.priority)}>{task.priority}</Badge>
      </td>

      {/* Due date */}
      <td className="py-3 pr-3">
        {task.dueDate ? (
          <div className="flex items-center gap-1.5">
            <span
              className={cn(
                'text-xs',
                task.isOverdue && task.status !== 'Done'
                  ? 'font-medium text-status-error'
                  : 'text-text-muted',
              )}
            >
              {new Date(task.dueDate).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
              })}
            </span>
            {task.isOverdue && task.status !== 'Done' && (
              <Badge variant="error">{daysDiff(task.dueDate)}d</Badge>
            )}
          </div>
        ) : (
          <span className="text-xs text-text-muted">—</span>
        )}
      </td>

      {/* Assignee (assignedByMe view) */}
      {view === 'assignedByMe' && (
        <td className="py-3 pr-3">
          <div className="flex items-center gap-1.5">
            <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-nebula-violet/20 text-[10px] font-bold text-nebula-violet">
              {(task.assignedToDisplayName ?? task.assignedToUserId).charAt(0).toUpperCase()}
            </span>
            <span className="truncate text-xs text-text-secondary max-w-[100px]">
              {task.assignedToDisplayName ?? task.assignedToUserId}
            </span>
          </div>
        </td>
      )}

      {/* Linked entity */}
      <td className="py-3 pr-3">
        {task.linkedEntityName ? (
          linkedPath ? (
            <Link
              to={linkedPath}
              onClick={(e: MouseEvent<HTMLAnchorElement>) => e.stopPropagation()}
              className="truncate text-xs text-nebula-violet hover:underline max-w-[120px] block"
            >
              {task.linkedEntityName}
            </Link>
          ) : (
            <span className="truncate text-xs text-text-muted max-w-[120px] block">
              {task.linkedEntityName}
            </span>
          )
        ) : (
          <span className="text-xs text-text-muted">—</span>
        )}
      </td>

      {/* Created date */}
      <td className="py-3 pr-3">
        <span className="text-xs text-text-muted">
          {new Date(task.createdAt).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
          })}
        </span>
      </td>
    </tr>
  );
}

function MobileTaskCard({
  task,
  selected,
  onSelect,
}: {
  task: TaskListItemDto;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      aria-selected={selected}
      className={cn(
        'w-full rounded-lg border px-3 py-3 text-left transition-colors',
        selected
          ? 'border-nebula-violet/50 bg-nebula-violet/10'
          : task.isOverdue && task.status !== 'Done'
            ? 'border-status-error/30 bg-status-error/5'
            : 'border-surface-border bg-surface-card hover:bg-surface-highlight',
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <p
          className={cn(
            'truncate text-sm font-medium',
            task.status === 'Done'
              ? 'text-text-muted line-through'
              : task.isOverdue
                ? 'text-status-error'
                : 'text-text-primary',
          )}
        >
          {task.title}
        </p>
        <Badge variant={statusBadgeVariant(task.status)} className="shrink-0">
          {formatStatusLabel(task.status)}
        </Badge>
      </div>
      <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
        <Badge variant={priorityBadgeVariant(task.priority)}>{task.priority}</Badge>
        {task.dueDate && (
          <span
            className={cn(
              'text-xs',
              task.isOverdue && task.status !== 'Done' ? 'text-status-error font-medium' : 'text-text-muted',
            )}
          >
            Due{' '}
            {new Date(task.dueDate).toLocaleDateString('en-US', {
              month: 'short',
              day: 'numeric',
            })}
          </span>
        )}
        {task.isOverdue && task.status !== 'Done' && (
          <Badge variant="error">{daysDiff(task.dueDate!)}d overdue</Badge>
        )}
      </div>
      {task.assignedToDisplayName && (
        <p className="mt-1 text-xs text-text-muted">
          Assigned to {task.assignedToDisplayName}
        </p>
      )}
    </button>
  );
}

function EmptyState({
  view,
  hasActiveFilters,
  onClearFilters,
  onCreateClick,
}: {
  view: TaskView;
  hasActiveFilters: boolean;
  onClearFilters: () => void;
  onCreateClick: () => void;
}) {
  if (hasActiveFilters) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <p className="text-sm text-text-secondary">No tasks match your filters.</p>
        <button
          type="button"
          onClick={onClearFilters}
          className="mt-2 text-xs text-nebula-violet hover:underline"
        >
          Clear filters
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <p className="text-sm text-text-secondary">
        {view === 'myWork'
          ? 'No tasks yet. Create your first task to get started.'
          : "You haven't assigned any tasks to your team yet."}
      </p>
      <button
        type="button"
        onClick={onCreateClick}
        className="mt-3 rounded-lg bg-nebula-violet px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-nebula-violet/90"
      >
        Create Task
      </button>
    </div>
  );
}

function TaskListSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <Skeleton key={i} className="h-14 w-full" />
      ))}
    </div>
  );
}
