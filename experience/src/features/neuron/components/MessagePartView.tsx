import { cn } from '@/lib/utils';
import type { MessagePart } from '../types';
import { renderRegisteredComponent, SafeFallback } from '../registry/componentRegistry';
import { isRegisteredAction } from '../registry/actionRegistry';

/**
 * Renders one envelope part by type (F0038-S0002). `app` parts route through the
 * component registry (registered + prop-validated only). `actions` render inert until
 * their handlers land (draft/mock-send F0038-S0005/S0006); unregistered actions are dropped.
 */
export function MessagePartView({ part }: { part: MessagePart }) {
  switch (part.part_type) {
    case 'text':
      return <p className="text-sm text-text-secondary">{part.text}</p>;

    case 'status':
      return (
        <span
          data-testid="status-part"
          className={cn(
            'inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs',
            part.state === 'failed' ? 'text-amber-500' : 'text-text-muted',
          )}
        >
          {part.detail ?? part.state}
        </span>
      );

    case 'app':
      return renderRegisteredComponent(part.component, part.props);

    case 'sources':
      return (
        <ul className="flex flex-wrap gap-1">
          {part.sources.map((source, index) => (
            <li
              key={`${source.label}-${index}`}
              className="rounded border border-surface-border px-2 py-0.5 text-xs text-text-muted"
            >
              {source.label}
            </li>
          ))}
        </ul>
      );

    case 'actions':
      return (
        <div className="flex flex-wrap gap-2">
          {part.actions
            .filter((action) => isRegisteredAction(action.action_type))
            .map((action) => (
              <button
                key={action.action_id}
                type="button"
                disabled
                title="Available in a later F0038 slice"
                className="inline-flex items-center rounded-md border border-surface-border px-2 py-1 text-xs text-text-muted disabled:cursor-not-allowed disabled:opacity-65"
              >
                {action.label}
              </button>
            ))}
        </div>
      );

    default:
      return <SafeFallback reason="unknown-part-type" />;
  }
}
