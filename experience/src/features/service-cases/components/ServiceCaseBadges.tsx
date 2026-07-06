import { cn } from '@/lib/utils';
import type { ServiceCasePriority, ServiceCaseStatus } from '../types';

const statusClass: Record<ServiceCaseStatus, string> = {
  Intake: 'border-status-info/35 bg-status-info/10 text-status-info',
  InProgress: 'border-nebula-violet/35 bg-nebula-violet/10 text-nebula-violet',
  Waiting: 'border-status-warning/35 bg-status-warning/10 text-status-warning',
  Resolved: 'border-status-success/35 bg-status-success/10 text-status-success',
  Closed: 'border-surface-border bg-surface-card text-text-muted',
};

const priorityClass: Record<ServiceCasePriority, string> = {
  Low: 'border-surface-border bg-surface-card text-text-muted',
  Medium: 'border-status-info/35 bg-status-info/10 text-status-info',
  High: 'border-status-warning/35 bg-status-warning/10 text-status-warning',
  Urgent: 'border-status-error/35 bg-status-error/10 text-status-error',
};

export function ServiceCaseStatusBadge({ status }: { status: ServiceCaseStatus }) {
  return (
    <span className={cn('inline-flex rounded-full border px-2 py-0.5 text-[11px] font-medium', statusClass[status])}>
      {status}
    </span>
  );
}

export function ServiceCasePriorityBadge({ priority }: { priority: ServiceCasePriority }) {
  return (
    <span className={cn('inline-flex rounded-full border px-2 py-0.5 text-[11px] font-medium', priorityClass[priority])}>
      {priority}
    </span>
  );
}
