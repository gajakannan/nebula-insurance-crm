import type { BrokerScorecard } from '../types';
import { formatCurrency, formatPercent, formatRelativeTime, getRateColor } from '../utils';
import MetricCard from './MetricCard';

interface ScorecardPanelProps {
  scorecard: BrokerScorecard;
}

export default function ScorecardPanel({ scorecard }: ScorecardPanelProps) {
  return (
    <section>
      <h2 className="mb-3 text-xs uppercase tracking-widest text-text-muted">
        Performance Scorecard
      </h2>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        <MetricCard
          label="Total Submissions"
          value={scorecard.total_submissions.toString()}
        />
        <MetricCard
          label="Quoted"
          value={scorecard.quoted_submissions.toString()}
          color="primary"
        />
        <MetricCard
          label="Bound"
          value={scorecard.bound_submissions.toString()}
          color="success"
        />
        <MetricCard
          label="Declined"
          value={scorecard.declined_submissions.toString()}
          color="danger"
        />
        <MetricCard
          label="Quote Rate"
          value={formatPercent(scorecard.quote_rate)}
          color={getRateColor(scorecard.quote_rate)}
        />
        <MetricCard
          label="Bind Rate"
          value={formatPercent(scorecard.bind_rate)}
          color={getRateColor(scorecard.bind_rate)}
        />
        <MetricCard
          label="Retention Rate"
          value={formatPercent(scorecard.retention_rate)}
          color={getRateColor(scorecard.retention_rate)}
        />
        <MetricCard
          label="Est. Premium"
          value={formatCurrency(scorecard.total_premium_estimate)}
          color="primary"
        />
        <MetricCard
          label="Activity"
          value={scorecard.activity_count.toString()}
        />
        <MetricCard
          label="Window"
          value={`${scorecard.window_days} days`}
        />
      </div>
      <p className="mt-2 text-right text-xs text-text-muted">
        Computed {formatRelativeTime(scorecard.computed_at)}
      </p>
    </section>
  );
}

