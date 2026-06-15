import type {
  BrokerScorecard,
  BrokerTrends,
  LeaderboardEntry,
  LeaderboardResponse,
  TrendPoint,
} from './types';

type RateColor = 'success' | 'warning' | 'danger';

interface LeaderboardWireEntry {
  rank: number;
  broker_id?: string;
  brokerId?: string;
  legal_name?: string;
  legalName?: string;
  state: string;
  submission_count?: number;
  submissionCount?: number;
  renewal_count?: number;
  renewalCount?: number;
  total_premium?: number | string;
  totalPremium?: number | string;
}

interface LeaderboardWireResponse {
  entries: LeaderboardWireEntry[];
  total_brokers?: number;
  totalBrokers?: number;
}

interface BrokerScorecardWire {
  broker_id?: string;
  brokerId?: string;
  legal_name?: string;
  legalName?: string;
  window_days?: number;
  windowDays?: number;
  total_submissions?: number;
  totalSubmissions?: number;
  quoted_submissions?: number;
  quotedSubmissions?: number;
  bound_submissions?: number;
  boundSubmissions?: number;
  declined_submissions?: number;
  declinedSubmissions?: number;
  quote_rate?: number;
  quoteRate?: number;
  bind_rate?: number;
  bindRate?: number;
  total_renewals?: number;
  totalRenewals?: number;
  completed_renewals?: number;
  completedRenewals?: number;
  lost_renewals?: number;
  lostRenewals?: number;
  retention_rate?: number;
  retentionRate?: number;
  total_premium_estimate?: number | string;
  totalPremiumEstimate?: number | string;
  activity_count?: number;
  activityCount?: number;
  computed_at?: string;
  computedAt?: string;
}

interface TrendPointWire {
  period_label?: string;
  periodLabel?: string;
  submissions: number;
  bound: number;
  renewals_completed?: number;
  renewalsCompleted?: number;
  premium: number | string;
}

interface BrokerTrendsWire {
  broker_id?: string;
  brokerId?: string;
  window_days?: number;
  windowDays?: number;
  granularity: string;
  points: TrendPointWire[];
}

export function formatPercent(rate: number): string {
  return `${(rate * 100).toFixed(1)}%`;
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(amount);
}

export function formatRelativeTime(iso: string): string {
  const value = new Date(iso).getTime();
  if (Number.isNaN(value)) return 'just now';

  const elapsedSeconds = Math.max(0, Math.round((Date.now() - value) / 1000));
  const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
    ['year', 60 * 60 * 24 * 365],
    ['month', 60 * 60 * 24 * 30],
    ['day', 60 * 60 * 24],
    ['hour', 60 * 60],
    ['minute', 60],
  ];
  const formatter = new Intl.RelativeTimeFormat('en-US', { numeric: 'auto' });

  for (const [unit, secondsPerUnit] of units) {
    if (elapsedSeconds >= secondsPerUnit) {
      return formatter.format(-Math.floor(elapsedSeconds / secondsPerUnit), unit);
    }
  }

  return 'just now';
}

export function getRateColor(rate: number): RateColor {
  if (rate >= 0.7) return 'success';
  if (rate >= 0.4) return 'warning';
  return 'danger';
}

export function normalizeLeaderboardResponse(
  response: LeaderboardResponse | LeaderboardWireResponse,
): LeaderboardResponse {
  return {
    entries: response.entries.map(normalizeLeaderboardEntry),
    total_brokers: 'total_brokers' in response
      ? response.total_brokers ?? response.entries.length
      : response.totalBrokers ?? response.entries.length,
  };
}

export function normalizeBrokerScorecard(
  scorecard: BrokerScorecard | BrokerScorecardWire,
): BrokerScorecard {
  const wire = scorecard as BrokerScorecardWire;
  return {
    broker_id: wire.broker_id ?? wire.brokerId ?? '',
    legal_name: wire.legal_name ?? wire.legalName ?? '',
    window_days: wire.window_days ?? wire.windowDays ?? 90,
    total_submissions: wire.total_submissions ?? wire.totalSubmissions ?? 0,
    quoted_submissions: wire.quoted_submissions ?? wire.quotedSubmissions ?? 0,
    bound_submissions: wire.bound_submissions ?? wire.boundSubmissions ?? 0,
    declined_submissions: wire.declined_submissions ?? wire.declinedSubmissions ?? 0,
    quote_rate: wire.quote_rate ?? wire.quoteRate ?? 0,
    bind_rate: wire.bind_rate ?? wire.bindRate ?? 0,
    total_renewals: wire.total_renewals ?? wire.totalRenewals ?? 0,
    completed_renewals: wire.completed_renewals ?? wire.completedRenewals ?? 0,
    lost_renewals: wire.lost_renewals ?? wire.lostRenewals ?? 0,
    retention_rate: wire.retention_rate ?? wire.retentionRate ?? 0,
    total_premium_estimate: toNumber(
      wire.total_premium_estimate ?? wire.totalPremiumEstimate ?? 0,
    ),
    activity_count: wire.activity_count ?? wire.activityCount ?? 0,
    computed_at: wire.computed_at ?? wire.computedAt ?? new Date().toISOString(),
  };
}

export function normalizeBrokerTrends(trends: BrokerTrends | BrokerTrendsWire): BrokerTrends {
  const wire = trends as BrokerTrendsWire;
  return {
    broker_id: wire.broker_id ?? wire.brokerId ?? '',
    window_days: wire.window_days ?? wire.windowDays ?? 90,
    granularity: wire.granularity,
    points: wire.points.map(normalizeTrendPoint),
  };
}

function normalizeLeaderboardEntry(entry: LeaderboardEntry | LeaderboardWireEntry): LeaderboardEntry {
  const wire = entry as LeaderboardWireEntry;
  return {
    rank: wire.rank,
    broker_id: wire.broker_id ?? wire.brokerId ?? '',
    legal_name: wire.legal_name ?? wire.legalName ?? '',
    state: wire.state,
    submission_count: wire.submission_count ?? wire.submissionCount ?? 0,
    renewal_count: wire.renewal_count ?? wire.renewalCount ?? 0,
    total_premium: toNumber(wire.total_premium ?? wire.totalPremium ?? 0),
  };
}

function normalizeTrendPoint(point: TrendPoint | TrendPointWire): TrendPoint {
  const wire = point as TrendPointWire;
  return {
    period_label: wire.period_label ?? wire.periodLabel ?? '',
    submissions: wire.submissions,
    bound: wire.bound,
    renewals_completed: wire.renewals_completed ?? wire.renewalsCompleted ?? 0,
    premium: toNumber(wire.premium),
  };
}

function toNumber(value: number | string): number {
  if (typeof value === 'number') return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

