import { Skeleton } from '@/components/ui/Skeleton';
import { ActivityFeedItem } from '@/features/timeline/components/ActivityFeedItem';
import { useSubmissionTimeline } from '../hooks/useSubmissionTimeline';

interface SubmissionTimelineSectionProps {
  submissionId: string;
}

export function SubmissionTimelineSection({ submissionId }: SubmissionTimelineSectionProps) {
  const {
    data,
    isLoading,
    isError,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useSubmissionTimeline(submissionId);

  const events = data?.pages.flatMap((page) => page.data) ?? [];

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 3 }).map((_, index) => (
          <Skeleton key={index} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-3 text-sm text-text-secondary">
        Unable to load submission activity.
      </div>
    );
  }

  if (events.length === 0) {
    return (
      <div className="rounded-lg border border-surface-border bg-surface-card/60 px-3 py-3 text-sm text-text-muted">
        No activity recorded yet.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        {events.map((event, index) => (
          <ActivityFeedItem
            key={event.id}
            event={event}
            isLast={index === events.length - 1}
          />
        ))}
      </div>

      {hasNextPage && (
        <button
          type="button"
          onClick={() => fetchNextPage()}
          disabled={isFetchingNextPage}
          className="rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-card-hover hover:text-text-primary disabled:opacity-50"
        >
          {isFetchingNextPage ? 'Loading…' : 'Load more'}
        </button>
      )}
    </div>
  );
}
