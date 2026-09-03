import type { TimelineEventDto } from '@/contracts/timeline';
import { ActivityTimelineList } from '@/features/timeline/components/ActivityTimelineList';

export interface BrokerActivityListProps {
  items: TimelineEventDto[];
}

/** Registered read-only Broker timeline list used by glance and replayed messages. */
export function BrokerActivityList({ props }: { props: Record<string, unknown> }) {
  const { items } = props as unknown as BrokerActivityListProps;
  return (
    <div data-testid="broker-activity-list" className="max-h-80 overflow-y-auto">
      <ActivityTimelineList events={items} ariaLabel="Recent Broker activity" />
    </div>
  );
}
