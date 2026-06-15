import { useState } from 'react';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { Skeleton } from '@/components/ui/Skeleton';
import {
  ScorecardPanel,
  TrendsChart,
  WindowSelector,
  useBrokerScorecard,
  useBrokerTrends,
} from '@/features/broker-insights';

interface BrokerInsightsTabContentProps {
  brokerId: string;
}

export default function BrokerInsightsTabContent({
  brokerId,
}: BrokerInsightsTabContentProps) {
  const [windowDays, setWindowDays] = useState(90);
  const {
    data: scorecardData,
    isLoading: scorecardLoading,
    error: scorecardError,
    refetch: refetchScorecard,
  } = useBrokerScorecard(brokerId, windowDays);
  const {
    data: trendsData,
    isLoading: trendsLoading,
    error: trendsError,
    refetch: refetchTrends,
  } = useBrokerTrends(brokerId, windowDays);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <span className="text-xs uppercase tracking-widest text-text-muted">
          Broker Analytics
        </span>
        <WindowSelector value={windowDays} onChange={setWindowDays} />
      </div>

      {scorecardLoading && <Skeleton className="h-48 w-full" />}
      {scorecardError && (
        <ErrorFallback
          message="Unable to load broker scorecard."
          onRetry={() => refetchScorecard()}
        />
      )}
      {scorecardData && <ScorecardPanel scorecard={scorecardData} />}

      {trendsLoading && <Skeleton className="h-56 w-full" />}
      {trendsError && (
        <ErrorFallback
          message="Unable to load broker trends."
          onRetry={() => refetchTrends()}
        />
      )}
      {trendsData && trendsData.points.length > 0 && (
        <TrendsChart trends={trendsData} />
      )}
      {trendsData && trendsData.points.length === 0 && (
        <p className="py-8 text-center text-sm text-text-muted">
          No trend data available for this window.
        </p>
      )}
    </div>
  );
}

