import { AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAction } from '../registry/actionContext';

/**
 * Registered component `renewals.needs_attention_list` (F0038-S0002/S0003). Renders the
 * Renewals zone's needs-attention items with a per-row "View" that dispatches the
 * `drill_renewal` action (F0038-S0003). The per-row [Draft] CTA is wired in F0038-S0005.
 */
export const renewalsNeedsAttentionSchema = {
  type: 'object',
  required: ['items'],
  additionalProperties: true,
  properties: {
    items: {
      type: 'array',
      items: {
        type: 'object',
        required: ['renewalId', 'accountName', 'expiresInDays', 'workflowState'],
        additionalProperties: true,
        properties: {
          renewalId: { type: 'string' },
          accountName: { type: 'string' },
          expiresInDays: { type: 'integer' },
          workflowState: { type: 'string' },
          noBrokerContact30d: { type: 'boolean' },
        },
      },
    },
  },
} as const;

interface NeedsAttentionItem {
  renewalId: string;
  accountName: string;
  expiresInDays: number;
  workflowState: string;
  noBrokerContact30d?: boolean;
}

export function RenewalsNeedsAttentionList({ props }: { props: Record<string, unknown> }) {
  const items = (props.items as NeedsAttentionItem[] | undefined) ?? [];
  const { dispatch, pending } = useAction();

  if (items.length === 0) {
    return <p className="text-xs text-text-muted">Nothing needs your attention right now.</p>;
  }

  return (
    <ul className="space-y-2" data-testid="needs-attention-list">
      {items.map((item) => (
        <li
          key={item.renewalId}
          className="rounded-md border border-surface-border bg-surface-card px-3 py-2"
        >
          <div className="flex items-center justify-between gap-2">
            <span className="text-sm font-medium text-text-primary">{item.accountName}</span>
            <span className="text-xs text-text-muted">
              exp {item.expiresInDays}d · {item.workflowState}
            </span>
          </div>
          {item.noBrokerContact30d ? (
            <span className={cn('mt-1 inline-flex items-center gap-1 text-xs text-amber-500')}>
              <AlertTriangle size={12} /> no broker contact 30d+
            </span>
          ) : null}
          <div className="mt-2 flex items-center gap-2">
            <button
              type="button"
              onClick={() => dispatch({ actionType: 'drill_renewal', payload: { renewalId: item.renewalId } })}
              disabled={pending}
              className="inline-flex items-center rounded-md border border-surface-border px-2 py-1 text-xs text-text-secondary hover:bg-surface-highlight disabled:cursor-not-allowed disabled:opacity-65"
            >
              View
            </button>
            <button
              type="button"
              onClick={() => dispatch({ actionType: 'draft_outreach', payload: { renewalId: item.renewalId, accountName: item.accountName } })}
              disabled={pending}
              className="inline-flex items-center rounded-md border border-surface-border px-2 py-1 text-xs text-text-secondary hover:bg-surface-highlight disabled:cursor-not-allowed disabled:opacity-65"
            >
              Draft outreach
            </button>
          </div>
        </li>
      ))}
    </ul>
  );
}
