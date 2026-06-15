import { useState } from 'react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import {
  LeaderboardTable,
  WindowSelector,
  useBrokerLeaderboard,
} from '@/features/broker-insights';

export default function BrokerInsightsPage() {
  const [limit, setLimit] = useState(10);
  const { data, isLoading, error, refetch } = useBrokerLeaderboard(limit);

  return (
    <DashboardLayout>
      <div className="space-y-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-xl font-semibold text-text-primary">
              Broker Insights
            </h1>
            <p className="mt-0.5 text-sm text-text-secondary">
              Production rankings and performance overview
            </p>
          </div>
          <WindowSelector
            value={limit}
            onChange={setLimit}
            options={[10, 25, 50]}
            formatLabel={(value) => value.toString()}
            ariaLabel="Select leaderboard size"
          />
        </div>

        {isLoading && <Skeleton className="h-96 w-full" />}
        {error && (
          <ErrorFallback
            message="Unable to load broker insights."
            onRetry={() => refetch()}
          />
        )}
        {data && (
          <LeaderboardTable
            entries={data.entries}
            totalBrokers={data.total_brokers}
          />
        )}
      </div>
    </DashboardLayout>
  );
}

