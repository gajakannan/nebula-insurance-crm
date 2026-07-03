import { AlertTriangle, Circle, CircleDot } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { ZonePayload } from '../types';
import { renderRegisteredComponent, SafeFallback } from '../registry/componentRegistry';

/**
 * Renders one Day-at-a-Glance zone slot by its status (F0038-S0002). A `content` zone
 * renders its registered component; `empty`/`inactive`/`error` render typed states.
 * Each slot is isolated — a failing zone shows its error state without affecting others.
 */
function ZoneBadge({ status }: { status: ZonePayload['zone_status'] }) {
  if (status === 'content' || status === 'empty') {
    return (
      <span className="inline-flex items-center gap-1 text-xs font-medium text-emerald-500">
        <CircleDot size={12} /> LIVE
      </span>
    );
  }
  if (status === 'error') {
    return (
      <span className="inline-flex items-center gap-1 text-xs font-medium text-amber-500">
        <AlertTriangle size={12} /> unavailable
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 text-xs text-text-muted">
      <Circle size={12} /> not yet active
    </span>
  );
}

export function ZoneSlot({ zone }: { zone: ZonePayload }) {
  const title = zone.title ?? zone.zone_id;
  const isStub = zone.zone_status === 'inactive';

  let body: React.ReactNode;
  switch (zone.zone_status) {
    case 'content':
      body = zone.component
        ? renderRegisteredComponent(zone.component, zone.props ?? {})
        : <SafeFallback reason="content-without-component" />;
      break;
    case 'empty':
      body = <p className="text-xs text-text-muted">{zone.detail ?? 'Nothing needs you right now.'}</p>;
      break;
    case 'error':
      body = (
        <p role="alert" className="text-xs text-text-muted">
          {zone.detail ?? 'This zone is temporarily unavailable.'}
        </p>
      );
      break;
    case 'inactive':
    default:
      body = <p className="text-xs text-text-muted">{zone.detail ?? 'Not yet active.'}</p>;
      break;
  }

  return (
    <section
      aria-label={title}
      data-zone={zone.zone_id}
      data-status={zone.zone_status}
      className={cn(
        'rounded-lg border border-surface-border bg-surface-card p-3',
        isStub && 'opacity-70',
      )}
    >
      <header className="mb-2 flex items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
        <ZoneBadge status={zone.zone_status} />
      </header>
      {body}
    </section>
  );
}
