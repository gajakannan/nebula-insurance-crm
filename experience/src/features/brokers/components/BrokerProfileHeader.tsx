import { BrokerStatusBadge } from './BrokerStatusBadge';
import type { BrokerDto } from '../types';

interface BrokerProfileHeaderProps {
  broker: BrokerDto;
  onEdit: () => void;
  onDeactivate: () => void;
  onDelete: () => void;
  onReactivate: () => void;
}

export function BrokerProfileHeader({
  broker,
  onEdit,
  onDeactivate,
  onDelete,
  onReactivate,
}: BrokerProfileHeaderProps) {
  const isInactive = broker.status === 'Inactive';

  if (broker.isDeactivated) {
    return (
      <div className="space-y-4">
        <div className="flex items-start gap-3 rounded-lg border border-status-warning/30 bg-status-warning/10 px-4 py-3">
          <svg className="mt-0.5 h-4 w-4 shrink-0 text-status-warning" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
          </svg>
          <div className="flex-1">
            <p className="text-sm font-medium text-status-warning">Broker Deactivated</p>
            <p className="mt-0.5 text-xs text-text-secondary">
              This broker has been deactivated. All contact PII is masked. Reactivate to restore full access.
            </p>
          </div>
          <button
            onClick={onReactivate}
            className="shrink-0 rounded-lg bg-nebula-violet px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-nebula-violet/90"
          >
            Reactivate Broker
          </button>
        </div>
        <div className="flex items-center gap-3">
          <h1 className="text-lg font-semibold text-text-muted">{broker.legalName}</h1>
          <BrokerStatusBadge status={broker.status} />
        </div>
        <p className="font-mono text-xs text-text-muted">{broker.licenseNumber}</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <div className="flex items-center gap-3">
          <h1 className="text-lg font-semibold text-text-primary">{broker.legalName}</h1>
          <BrokerStatusBadge status={broker.status} />
        </div>
        <p className="mt-1 font-mono text-xs text-text-muted">{broker.licenseNumber}</p>
      </div>
      <div className="flex gap-2">
        <button
          onClick={onEdit}
          className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
        >
          Edit
        </button>
        <button
          onClick={onDeactivate}
          className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary"
        >
          {isInactive ? 'Activate' : 'Deactivate'}
        </button>
        <button
          onClick={onDelete}
          className="rounded-lg bg-status-error/15 px-3 py-1.5 text-xs font-medium text-status-error transition-colors hover:bg-status-error/25"
        >
          Delete
        </button>
      </div>
    </div>
  );
}
