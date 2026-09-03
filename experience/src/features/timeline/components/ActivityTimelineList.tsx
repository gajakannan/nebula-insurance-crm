import type { TimelineEventDto } from '@/contracts/timeline';
import { ActivityFeedItem } from './ActivityFeedItem';

/** Shared semantic presentation for Broker and record timeline event lists. */
export function ActivityTimelineList({
  events,
  ariaLabel,
}: {
  events: TimelineEventDto[];
  ariaLabel: string;
}) {
  return (
    <ol aria-label={ariaLabel} className="space-y-0">
      {events.map((event, index) => (
        <li key={event.id}>
          <ActivityFeedItem event={event} isLast={index === events.length - 1} />
        </li>
      ))}
    </ol>
  );
}
