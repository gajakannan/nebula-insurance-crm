import { useMemo, useState } from 'react';
import { Card } from '@/components/ui/Card';
import { BrokerInsightCards } from './BrokerInsightCards';
import { BrokerInsightBenchmarkPanel } from './BrokerInsightBenchmarkPanel';
import { BrokerInsightSnapshotPanel } from './BrokerInsightSnapshotPanel';
import { BrokerInsightTrendDrawer } from './BrokerInsightTrendDrawer';
import {
  useBrokerInsightBenchmark,
  useBrokerInsightScorecards,
  useBrokerInsightSnapshot,
  useBrokerInsightTrend,
} from '../hooks';
import type { BrokerInsightMetricKey, BrokerInsightParams } from '../types';

const WINDOWS = [
  { key: '30d', label: '30d', days: 30 },
  { key: '90d', label: '90d', days: 90 },
  { key: 'qtd', label: 'QTD', days: 90 },
  { key: 'ytd', label: 'YTD', days: 365 },
] as const;

export function BrokerInsightsWorkspace() {
  const [windowKey, setWindowKey] = useState<typeof WINDOWS[number]['key']>('90d');
  const [brokerIdFilter, setBrokerIdFilter] = useState('');
  const [selectedMetric, setSelectedMetric] = useState<BrokerInsightMetricKey>('quoteCount');
  const params = useMemo(() => buildParams(windowKey, brokerIdFilter), [windowKey, brokerIdFilter]);
  const scorecards = useBrokerInsightScorecards(params);
  const selectedBroker = scorecards.data?.items[0] ?? null;
  const trend = useBrokerInsightTrend(selectedBroker?.brokerId, selectedMetric, params);
  const benchmark = useBrokerInsightBenchmark(selectedBroker?.brokerId, params);
  const snapshot = useBrokerInsightSnapshot(selectedBroker?.brokerId, params);

  return (
    <div className="space-y-4">
      <Card>
        <div className="flex flex-col gap-4 p-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h1 className="text-base font-semibold text-text-primary">Broker insights</h1>
            <p className="text-xs text-text-muted">
              Permission-filtered scorecards, trends, benchmarks, and review snapshots.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="text-xs text-text-muted" htmlFor="broker-filter">Broker ID</label>
            <input
              id="broker-filter"
              value={brokerIdFilter}
              onChange={(event) => setBrokerIdFilter(event.target.value)}
              className="h-9 w-72 max-w-full rounded-md border border-surface-border bg-surface-card px-3 text-sm text-text-primary"
              placeholder="Optional UUID filter"
            />
            <div className="flex rounded-md border border-surface-border bg-surface-card p-1">
              {WINDOWS.map((window) => (
                <button
                  key={window.key}
                  type="button"
                  onClick={() => setWindowKey(window.key)}
                  className={`h-7 min-w-12 rounded px-2 text-xs ${
                    windowKey === window.key ? 'bg-accent-primary text-white' : 'text-text-muted hover:bg-surface-card-hover'
                  }`}
                >
                  {window.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </Card>

      {scorecards.isLoading ? (
        <Card><p className="p-4 text-sm text-text-muted">Loading broker insights...</p></Card>
      ) : selectedBroker ? (
        <>
          <Card>
            <div className="space-y-3 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="text-sm font-semibold text-text-primary">{selectedBroker?.brokerName}</h2>
                  <p className="text-xs text-text-muted">
                    {params.periodStart} to {params.periodEnd}
                  </p>
                </div>
                {selectedBroker?.partialData && (
                  <span className="rounded-full border border-surface-border px-2 py-1 text-xs text-text-muted">Partial data</span>
                )}
              </div>
              <BrokerInsightCards
                metrics={selectedBroker.metrics}
                selectedMetric={selectedMetric}
                onSelectMetric={setSelectedMetric}
              />
            </div>
          </Card>

          <div className="grid gap-4 xl:grid-cols-[1.2fr_0.8fr]">
            <Card>
              <div className="space-y-3 p-4">
                <h2 className="text-sm font-semibold text-text-primary">Trend drilldown</h2>
                <BrokerInsightTrendDrawer trend={trend.data} />
              </div>
            </Card>
            <Card>
              <div className="space-y-3 p-4">
                <h2 className="text-sm font-semibold text-text-primary">Authorized benchmark</h2>
                <BrokerInsightBenchmarkPanel benchmark={benchmark.data} />
              </div>
            </Card>
          </div>

          <Card>
            <div className="space-y-3 p-4">
              <h2 className="text-sm font-semibold text-text-primary">Review snapshot</h2>
              <BrokerInsightSnapshotPanel snapshot={snapshot.data} />
            </div>
          </Card>
        </>
      ) : (
        <Card>
          <div className="p-4">
            <h2 className="text-sm font-semibold text-text-primary">No authorized broker insight data</h2>
            <p className="mt-1 text-sm text-text-muted">
              No scorecards match the selected broker and time window, or your current permissions hide every matching record.
            </p>
          </div>
        </Card>
      )}
    </div>
  );
}

function buildParams(windowKey: typeof WINDOWS[number]['key'], brokerIdFilter: string): BrokerInsightParams {
  const end = new Date();
  const selected = WINDOWS.find((window) => window.key === windowKey) ?? WINDOWS[1];
  const start = new Date(end);
  if (windowKey === 'qtd') {
    start.setMonth(Math.floor(end.getMonth() / 3) * 3, 1);
  } else if (windowKey === 'ytd') {
    start.setMonth(0, 1);
  } else {
    start.setDate(end.getDate() - selected.days);
  }

  return {
    brokerId: brokerIdFilter.trim() || undefined,
    periodStart: toDateOnly(start),
    periodEnd: toDateOnly(end),
    page: 1,
    pageSize: 25,
  };
}

function toDateOnly(date: Date): string {
  return date.toISOString().slice(0, 10);
}
