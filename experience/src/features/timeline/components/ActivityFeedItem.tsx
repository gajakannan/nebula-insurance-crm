import type { TimelineEventDto } from '@/contracts/timeline';
import { formatRelativeTime } from '@/lib/format';
import { getEntityPath } from '@/lib/navigation';
import { Link } from 'react-router-dom';

interface ActivityFeedItemProps {
  event: TimelineEventDto;
  isLast?: boolean;
}

function eventAccent(entityType: string): string {
  switch (entityType.toLowerCase()) {
    case 'submission':
      return '#60a5fa';
    case 'renewal':
      return '#34d399';
    case 'broker':
      return '#f59e0b';
    default:
      return '#94a3b8';
  }
}

function shortType(eventType: string): string {
  return eventType
    .replace(/([A-Z])/g, ' $1')
    .trim()
    .replace(/\s+/g, ' ');
}

export function ActivityFeedItem({ event, isLast = false }: ActivityFeedItemProps) {
  const path =
    event.entityType && event.entityId
      ? getEntityPath(event.entityType, event.entityId)
      : null;
  const accent = eventAccent(event.entityType);

  return (
    <div className="group relative pl-8 pr-1">
      {isLast && <span className="sr-only">Last activity item</span>}

      <span
        aria-hidden="true"
        className="absolute left-[4px] top-3 h-2.5 w-2.5 rounded-full"
        style={{ backgroundColor: accent }}
      />

      <div className="rounded-md px-1 py-2 transition-colors group-hover:bg-surface-panel/40">
        <div className="mb-1 flex items-center gap-2">
          <span
            className="rounded bg-surface-main/55 px-1.5 py-0.5 text-xs font-medium uppercase tracking-wide text-text-muted"
            title={event.eventType}
          >
            {shortType(event.eventType)}
          </span>
          <span className="text-xs text-text-muted">
            {formatRelativeTime(event.occurredAt)}
          </span>
        </div>

        <p className="text-xs leading-5 text-text-secondary">
          {event.eventDescription ?? event.eventType}
        </p>

        <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-text-muted">
          {event.entityName && (
            <>
              {path ? (
                <Link to={path} className="font-medium text-text-secondary hover:text-nebula-violet">
                  {event.entityName}
                </Link>
              ) : (
                <span className="font-medium text-text-secondary">{event.entityName}</span>
              )}
              <span aria-hidden="true">·</span>
            </>
          )}

          {event.actorDisplayName && <span>{event.actorDisplayName}</span>}
        </div>
      </div>
    </div>
  );
}
