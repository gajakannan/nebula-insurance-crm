import { useState, type FormEvent } from 'react';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { ApiError } from '@/services/api';
import { useAssignProducerOwnership, useProducerOwnership } from '../hooks/useProducerOwnership';
import type { ScopeType } from '../types';

interface OwnershipPanelProps {
  scopeType: ScopeType;
  scopeId: string;
}

function assignErrorMessage(error: unknown): string {
  const code = error instanceof ApiError ? error.code : undefined;
  switch (code) {
    case 'ownership_period_overlap':
      return 'That period overlaps an existing ownership period.';
    case 'ownership_period_invalid':
      return 'The effective date must be after the current owner’s start date.';
    case 'precondition_failed':
      return 'Ownership changed since you loaded it. Refresh and retry.';
    case 'not_found':
      return 'The selected producer node does not exist.';
    default:
      return 'Unable to assign ownership.';
  }
}

export function OwnershipPanel({ scopeType, scopeId }: OwnershipPanelProps) {
  const [asOf, setAsOf] = useState('');
  const lookup = useProducerOwnership(scopeType, scopeId, asOf || undefined);
  const assign = useAssignProducerOwnership();
  const [producerNodeId, setProducerNodeId] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState('');

  function onAssign(event: FormEvent) {
    event.preventDefault();
    assign.mutate(
      {
        request: { scopeType, scopeId, producerNodeId: producerNodeId.trim(), effectiveFrom },
        rowVersion: lookup.data?.ownership?.rowVersion,
      },
      {
        onSuccess: () => {
          setProducerNodeId('');
          setEffectiveFrom('');
        },
      },
    );
  }

  const owner = lookup.data?.ownership ?? null;

  return (
    <section className="space-y-3" aria-label="Ownership">
      <div className="flex items-center justify-between gap-2">
        <h4 className="text-xs font-medium uppercase tracking-wide text-text-muted">Producer ownership</h4>
        <label className="flex items-center gap-1 text-xs text-text-muted">
          As of
          <input
            type="date"
            value={asOf}
            onChange={(e) => setAsOf(e.target.value)}
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-text-primary"
          />
        </label>
      </div>

      {lookup.isLoading ? (
        <Skeleton className="h-10 w-full" />
      ) : lookup.isError ? (
        <ErrorFallback message="Unable to load ownership." onRetry={() => lookup.refetch()} />
      ) : owner ? (
        <div className="rounded-lg border border-surface-border p-3 text-sm">
          <p className="text-text-secondary">
            Owner: <span className="font-medium text-text-primary">{owner.producerDisplayName ?? owner.producerNodeId}</span>
          </p>
          <p className="text-xs text-text-muted">
            Effective {owner.effectiveFrom} → {owner.effectiveTo ?? 'open'}
          </p>
        </div>
      ) : (
        <p className="py-3 text-center text-sm text-text-muted">No owner for this date.</p>
      )}

      <form onSubmit={onAssign} className="flex flex-wrap items-end gap-2 border-t border-surface-border pt-3">
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Producer node id
          <input
            value={producerNodeId}
            onChange={(e) => setProducerNodeId(e.target.value)}
            placeholder="producer node id"
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Effective from
          <input
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <button
          type="submit"
          disabled={assign.isPending || !producerNodeId.trim() || !effectiveFrom}
          className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-primary disabled:opacity-50"
        >
          {assign.isPending ? 'Assigning…' : 'Assign / reassign'}
        </button>
        {assign.isError && (
          <p role="alert" className="w-full text-xs text-status-error">
            {assignErrorMessage(assign.error)}
          </p>
        )}
      </form>
    </section>
  );
}
