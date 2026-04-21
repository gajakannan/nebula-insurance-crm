import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { TaskListFilters, TaskView, UserSummaryDto } from '../types';
import { AssigneePicker } from './AssigneePicker';

interface TaskFilterToolbarProps {
  filters: TaskListFilters;
  view: TaskView;
  assigneeUser: UserSummaryDto | null;
  onAssigneeChange: (user: UserSummaryDto | null) => void;
  onChange: (patch: Partial<TaskListFilters>) => void;
  onClear: () => void;
  hasActiveFilters: boolean;
}

const STATUS_OPTIONS = [
  { value: 'Open', label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Done', label: 'Done' },
];

const PRIORITY_OPTIONS = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Urgent', label: 'Urgent' },
];

const ENTITY_TYPE_OPTIONS = [
  { value: 'Broker', label: 'Broker' },
  { value: 'Submission', label: 'Submission' },
  { value: 'Renewal', label: 'Renewal' },
  { value: 'Task', label: 'Task' },
];

interface MultiCheckProps {
  label: string;
  options: { value: string; label: string }[];
  selected: string[];
  onChange: (values: string[]) => void;
}

function MultiCheck({ label, options, selected, onChange }: MultiCheckProps) {
  function toggle(value: string) {
    onChange(
      selected.includes(value)
        ? selected.filter((v) => v !== value)
        : [...selected, value],
    );
  }

  return (
    <fieldset className="space-y-1">
      <legend className="text-xs font-medium text-text-secondary">{label}</legend>
      <div className="flex flex-wrap gap-1.5">
        {options.map((opt) => {
          const checked = selected.includes(opt.value);
          return (
            <button
              key={opt.value}
              type="button"
              onClick={() => toggle(opt.value)}
              aria-pressed={checked}
              className={cn(
                'rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors',
                checked
                  ? 'border-nebula-violet bg-nebula-violet/20 text-text-primary'
                  : 'border-surface-border bg-surface-card text-text-secondary hover:bg-surface-highlight',
              )}
            >
              {opt.label}
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}

export function TaskFilterToolbar({
  filters,
  view,
  assigneeUser,
  onAssigneeChange,
  onChange,
  onClear,
  hasActiveFilters,
}: TaskFilterToolbarProps) {
  return (
    <div className="glass-card operational-panel rounded-xl p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-widest text-text-muted">
          Filters
        </h3>
        {hasActiveFilters && (
          <button
            type="button"
            onClick={onClear}
            className="flex items-center gap-1 text-xs text-nebula-violet hover:underline"
          >
            <X size={12} />
            Clear all
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        <MultiCheck
          label="Status"
          options={STATUS_OPTIONS}
          selected={filters.status ?? []}
          onChange={(values) => onChange({ status: values, page: 1 })}
        />

        <MultiCheck
          label="Priority"
          options={PRIORITY_OPTIONS}
          selected={filters.priority ?? []}
          onChange={(values) => onChange({ priority: values, page: 1 })}
        />

        {/* Date range */}
        <div className="space-y-1">
          <p className="text-xs font-medium text-text-secondary">Due Date From</p>
          <input
            type="date"
            value={filters.dueDateFrom ?? ''}
            onChange={(e) => onChange({ dueDateFrom: e.target.value || undefined, page: 1 })}
            aria-label="Due date from"
            className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
          />
        </div>

        <div className="space-y-1">
          <p className="text-xs font-medium text-text-secondary">Due Date To</p>
          <input
            type="date"
            value={filters.dueDateTo ?? ''}
            onChange={(e) => onChange({ dueDateTo: e.target.value || undefined, page: 1 })}
            aria-label="Due date to"
            className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
          />
        </div>

        {/* Overdue toggle */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            role="switch"
            aria-checked={filters.overdue === true}
            onClick={() => onChange({ overdue: filters.overdue ? undefined : true, page: 1 })}
            className={cn(
              'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus:ring-2 focus:ring-nebula-violet focus:ring-offset-1',
              filters.overdue
                ? 'bg-status-error'
                : 'bg-surface-border',
            )}
          >
            <span
              className={cn(
                'pointer-events-none inline-block h-4 w-4 rounded-full bg-white shadow-lg transition-transform',
                filters.overdue ? 'translate-x-4' : 'translate-x-0',
              )}
            />
          </button>
          <span className="text-xs font-medium text-text-secondary">Overdue only</span>
        </div>

        {/* Assignee filter (assignedByMe view only) */}
        {view === 'assignedByMe' && (
          <div className="sm:col-span-2">
            <AssigneePicker
              label="Filter by Assignee"
              selectedUser={assigneeUser}
              onSelect={(user) => {
                onAssigneeChange(user);
                onChange({ assigneeId: user?.userId ?? undefined, page: 1 });
              }}
            />
          </div>
        )}

        {/* Linked entity type */}
        <MultiCheck
          label="Linked Entity Type"
          options={ENTITY_TYPE_OPTIONS}
          selected={filters.linkedEntityType ?? []}
          onChange={(values) => onChange({ linkedEntityType: values, page: 1 })}
        />
      </div>
    </div>
  );
}
