import { useState, useEffect } from 'react';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { useBrokerTimeline } from '../hooks/useBrokerTimeline';
import { formatRelativeTime } from '@/lib/format';

interface BrokerTimelineTabProps {
  brokerId: string;
}

export function BrokerTimelineTab({ brokerId }: BrokerTimelineTabProps) {
  const [page, setPage] = useState(1);
  useEffect(() => { setPage(1); }, [brokerId]);
  const { data: result, isLoading, isError, refetch } = useBrokerTimeline(brokerId, page);

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (isError) {
    return <ErrorFallback message="Unable to load timeline." onRetry={() => refetch()} />;
  }

  const events = result?.data ?? [];
  const totalPages = result?.totalPages ?? 1;

  if (events.length === 0 && page === 1) {
    return <p className="py-8 text-center text-sm text-text-muted">No activity recorded.</p>;
  }

  return (
    <div>
      <div className="space-y-3">
        {events.map((event) => (
          <div
            key={event.id}
            className="flex items-start gap-3 rounded-lg border border-surface-border p-3"
          >
            <div className="mt-1 h-2 w-2 flex-shrink-0 rounded-full bg-text-muted" />
            <div className="min-w-0 flex-1">
              <p className="text-sm text-text-secondary">{event.eventDescription}</p>
              <div className="mt-1 flex gap-2 text-xs text-text-muted">
                {event.actorDisplayName && <span>{event.actorDisplayName}</span>}
                <span>{formatRelativeTime(event.occurredAt)}</span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between text-xs text-text-muted">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded px-2 py-1 transition-colors hover:bg-surface-card-hover disabled:opacity-40"
          >
            ← Previous
          </button>
          <span>
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded px-2 py-1 transition-colors hover:bg-surface-card-hover disabled:opacity-40"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
}
