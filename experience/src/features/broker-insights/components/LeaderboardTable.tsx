import { Link } from 'react-router-dom';
import type { LeaderboardEntry } from '../types';
import { formatCurrency } from '../utils';

interface LeaderboardTableProps {
  entries: LeaderboardEntry[];
  totalBrokers: number;
}

const MEDALS: Record<number, string> = {
  1: '🥇',
  2: '🥈',
  3: '🥉',
};

export default function LeaderboardTable({ entries, totalBrokers }: LeaderboardTableProps) {
  return (
    <div className="glass-card gradient-accent-top overflow-hidden rounded-xl">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] text-sm">
          <thead>
            <tr
              className="border-b text-left text-xs uppercase tracking-widest text-text-muted"
              style={{ borderColor: 'var(--surface-border)' }}
            >
              <th className="px-5 py-4 font-semibold">Rank</th>
              <th className="px-5 py-4 font-semibold">Broker</th>
              <th className="px-5 py-4 font-semibold">State</th>
              <th className="px-5 py-4 text-right font-semibold">Submissions</th>
              <th className="px-5 py-4 text-right font-semibold">Renewals</th>
              <th className="px-5 py-4 text-right font-semibold">Premium</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr
                key={entry.broker_id}
                className="transition-colors duration-150"
                onMouseEnter={(event) => {
                  event.currentTarget.style.background = 'var(--surface-card-hover)';
                }}
                onMouseLeave={(event) => {
                  event.currentTarget.style.background = 'transparent';
                }}
              >
                <td className="px-5 py-4 text-text-secondary">
                  {MEDALS[entry.rank] ?? entry.rank}
                </td>
                <td className="px-5 py-4">
                  <Link
                    to={`/brokers/${entry.broker_id}`}
                    className="font-medium text-text-primary hover:text-nebula-violet hover:underline"
                  >
                    {entry.legal_name}
                  </Link>
                </td>
                <td className="px-5 py-4">
                  <span className="rounded-full border border-surface-border bg-surface-card px-2 py-1 text-xs text-text-secondary">
                    {entry.state}
                  </span>
                </td>
                <td className="px-5 py-4 text-right text-text-secondary">
                  {entry.submission_count}
                </td>
                <td className="px-5 py-4 text-right text-text-secondary">
                  {entry.renewal_count}
                </td>
                <td
                  className="px-5 py-4 text-right font-semibold"
                  style={{ color: 'var(--accent-primary)' }}
                >
                  {formatCurrency(entry.total_premium)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="px-5 pb-4 text-right text-xs text-text-muted">
        Showing {entries.length} of {totalBrokers} active brokers
      </p>
    </div>
  );
}

