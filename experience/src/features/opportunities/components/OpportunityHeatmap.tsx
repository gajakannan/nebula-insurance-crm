import type {
  OpportunityEntityType,
  OpportunityAgingStatusDto,
} from '../types';
import { useOpportunityAging } from '../hooks/useOpportunityAging';
import { opportunityHex } from '../lib/opportunity-colors';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';

interface OpportunityHeatmapProps {
  entityType: OpportunityEntityType;
  periodDays: number;
  label: string;
}

function cellIntensity(count: number, maxCount: number): number {
  if (maxCount === 0 || count === 0) return 0;
  return Math.min(count / maxCount, 1);
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}

export function OpportunityHeatmap({
  entityType,
  periodDays,
  label,
}: OpportunityHeatmapProps) {
  const { data, isLoading, isError, refetch } = useOpportunityAging(
    entityType,
    periodDays,
  );

  if (isLoading) {
    return <Skeleton className="h-40 w-full" />;
  }

  if (isError || !data) {
    return (
      <ErrorFallback
        message={`Unable to load ${label.toLowerCase()} aging data`}
        onRetry={() => refetch()}
      />
    );
  }

  const activeStatuses = data.statuses.filter((s) => s.total > 0);

  if (activeStatuses.length === 0) {
    return (
      <div className="py-4 text-center text-sm text-text-muted">
        No aging data for {label.toLowerCase()}
      </div>
    );
  }

  const maxCount = Math.max(
    ...activeStatuses.flatMap((s) => s.buckets.map((b) => b.count)),
    1,
  );

  const bucketLabels = activeStatuses[0]?.buckets.map((b) => b.label) ?? [];

  return (
    <section aria-label={`${label} aging heatmap`}>
      <h3 className="mb-3 text-sm font-semibold text-text-secondary">
        {label}
      </h3>
      <div className="overflow-x-auto">
        <table className="w-full text-xs" role="grid">
          <thead>
            <tr>
              <th className="px-2 py-1 text-left font-medium text-text-muted">
                Status
              </th>
              {bucketLabels.map((bl) => (
                <th
                  key={bl}
                  className="px-2 py-1 text-center font-medium text-text-muted"
                >
                  {bl}
                </th>
              ))}
              <th className="px-2 py-1 text-center font-medium text-text-muted">
                Total
              </th>
            </tr>
          </thead>
          <tbody>
            {activeStatuses.map((status) => (
              <HeatmapRow
                key={status.status}
                status={status}
                maxCount={maxCount}
              />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

interface HeatmapRowProps {
  status: OpportunityAgingStatusDto;
  maxCount: number;
}

function HeatmapRow({ status, maxCount }: HeatmapRowProps) {
  const hex = opportunityHex(status.colorGroup);

  return (
    <tr>
      <td className="whitespace-nowrap px-2 py-1.5 text-text-primary">
        {formatStatus(status.status)}
      </td>
      {status.buckets.map((bucket) => {
        const intensity = cellIntensity(bucket.count, maxCount);
        return (
          <td
            key={bucket.key}
            className="px-2 py-1.5 text-center tabular-nums"
            title={`${formatStatus(status.status)} / ${bucket.label}: ${bucket.count}`}
            style={{
              backgroundColor:
                intensity > 0 ? `${hex}${Math.round(intensity * 40 + 15).toString(16).padStart(2, '0')}` : undefined,
            }}
          >
            {bucket.count}
          </td>
        );
      })}
      <td className="px-2 py-1.5 text-center font-semibold tabular-nums text-text-primary">
        {status.total}
      </td>
    </tr>
  );
}
