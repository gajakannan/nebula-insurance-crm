import type { BrokerInsightBenchmark } from '../types';

export function BrokerInsightBenchmarkPanel({ benchmark }: { benchmark: BrokerInsightBenchmark | undefined }) {
  if (!benchmark) {
    return <p className="text-sm text-text-muted">Benchmark data appears after a visible broker is selected.</p>;
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-3 text-xs text-text-muted">
        <span>Peer set: {benchmark.peerSet.type}</span>
        <span>Visible peers: {benchmark.peerSet.visiblePeerCount}</span>
        <span>Status: {benchmark.peerSet.status}</span>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full text-sm">
          <thead className="text-left text-xs text-text-muted">
            <tr>
              <th className="py-2 pr-3">Metric</th>
              <th className="py-2 pr-3">Broker</th>
              <th className="py-2 pr-3">Median</th>
              <th className="py-2 pr-3">Rank</th>
              <th className="py-2 pr-3">Variance</th>
              <th className="py-2 pr-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-surface-border">
            {benchmark.metrics.map((metric) => (
              <tr key={metric.metricKey}>
                <td className="py-2 pr-3 text-text-primary">{metric.metricKey}</td>
                <td className="py-2 pr-3 text-text-secondary">{metric.brokerValue ?? 'N/A'}</td>
                <td className="py-2 pr-3 text-text-secondary">{metric.peerMedian ?? 'N/A'}</td>
                <td className="py-2 pr-3 text-text-secondary">{metric.rank ?? 'Suppressed'}</td>
                <td className="py-2 pr-3 text-text-secondary">{metric.variance ?? 'N/A'}</td>
                <td className="py-2 pr-3 text-text-muted">{metric.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
