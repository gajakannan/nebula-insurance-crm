import { useTimelineEvents } from '../hooks/useTimelineEvents';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { ActivityFeedItem } from './ActivityFeedItem';

export function ActivityFeed() {
  const { data, isLoading, isError, refetch } = useTimelineEvents('Broker', 12);

  return (
    <section
      className="glass-card operational-panel flex min-h-0 flex-col rounded-xl p-4 md:p-5"
      aria-label="Activity section"
    >
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-text-primary">Activity</h2>
        {/* <span className="text-xs uppercase tracking-wider text-text-muted">Timeline</span> */}
      </div>

      {isLoading && (
        <div className="flex-1 space-y-2 py-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-8 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <div className="flex-1 py-4">
          <ErrorFallback
            message="Unable to load activity feed"
            onRetry={() => refetch()}
          />
        </div>
      )}

      {data && (
        <>
          {data.length === 0 ? (
            <p className="flex-1 py-6 text-center text-sm text-text-muted">
              No recent broker activity.
            </p>
          ) : (
            <div className="timeline-scrollbar min-h-0 flex-1 overflow-y-scroll py-2">
              {data.map((event, index) => (
                <ActivityFeedItem
                  key={event.id}
                  event={event}
                  isLast={index === data.length - 1}
                />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  );
}
