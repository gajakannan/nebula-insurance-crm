import { useState, type FormEvent } from 'react';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { ApiError } from '@/services/api';
import {
  useDistributionAncestors,
  useDistributionDescendants,
  useSetDistributionParent,
} from '../hooks/useDistributionHierarchy';

interface HierarchyPanelProps {
  nodeId: string;
}

function parentErrorMessage(error: unknown): string {
  const code = error instanceof ApiError ? error.code : undefined;
  switch (code) {
    case 'distribution_node_self_parent':
      return 'A node cannot be its own parent.';
    case 'distribution_node_cycle':
      return 'That parent is below this node — the move would create a cycle.';
    case 'invalid_distribution_parent':
      return 'The selected parent does not exist or is inactive.';
    case 'precondition_failed':
      return 'The hierarchy changed since you loaded it. Refresh and retry.';
    default:
      return 'Unable to change the parent.';
  }
}

export function HierarchyPanel({ nodeId }: HierarchyPanelProps) {
  const ancestors = useDistributionAncestors(nodeId);
  const descendants = useDistributionDescendants(nodeId);
  const setParent = useSetDistributionParent(nodeId);
  const [parentId, setParentId] = useState('');

  if (ancestors.isLoading) {
    return <Skeleton className="h-32 w-full" />;
  }
  if (ancestors.isError || !ancestors.data) {
    return <ErrorFallback message="Unable to load hierarchy." onRetry={() => ancestors.refetch()} />;
  }

  const { node, ancestors: chain } = ancestors.data;
  const breadcrumb = [...chain, node];

  function onSetParent(event: FormEvent) {
    event.preventDefault();
    setParent.mutate(
      { request: { parentId: parentId.trim() || null }, rowVersion: node.rowVersion },
      { onSuccess: () => setParentId('') },
    );
  }

  return (
    <section className="space-y-4" aria-label="Hierarchy">
      <div className="flex flex-wrap items-center gap-1 text-sm text-text-secondary">
        <span className="text-text-muted">(root)</span>
        {breadcrumb.map((n) => (
          <span key={n.id} className="flex items-center gap-1">
            <span aria-hidden>▸</span>
            <span className={n.id === node.id ? 'font-medium text-text-primary' : ''}>{n.displayName}</span>
          </span>
        ))}
      </div>

      <div>
        <h4 className="mb-2 text-xs font-medium uppercase tracking-wide text-text-muted">Children</h4>
        {descendants.isLoading ? (
          <Skeleton className="h-16 w-full" />
        ) : descendants.data && descendants.data.data.length > 0 ? (
          <ul className="space-y-2">
            {descendants.data.data.map((child) => (
              <li
                key={child.id}
                className="flex items-center justify-between rounded-lg border border-surface-border p-2 text-sm"
              >
                <span className="flex items-center gap-2">
                  <Badge variant="default">{child.nodeType}</Badge>
                  <span className="text-text-secondary">{child.displayName}</span>
                </span>
                <span className="text-xs text-text-muted">{child.childCount} children</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="py-4 text-center text-sm text-text-muted">No children.</p>
        )}
      </div>

      <form onSubmit={onSetParent} className="flex flex-wrap items-end gap-2 border-t border-surface-border pt-3">
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Set / change parent (node id, blank = root)
          <input
            value={parentId}
            onChange={(e) => setParentId(e.target.value)}
            placeholder="parent node id"
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <button
          type="submit"
          disabled={setParent.isPending}
          className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-primary disabled:opacity-50"
        >
          {setParent.isPending ? 'Saving…' : 'Set parent'}
        </button>
        {setParent.isError && (
          <p role="alert" className="w-full text-xs text-status-error">
            {parentErrorMessage(setParent.error)}
          </p>
        )}
      </form>
    </section>
  );
}
