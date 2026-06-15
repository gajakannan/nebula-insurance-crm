export interface LeaderboardEntry {
  rank: number;
  broker_id: string;
  legal_name: string;
  state: string;
  submission_count: number;
  renewal_count: number;
  total_premium: number;
}

export interface LeaderboardResponse {
  entries: LeaderboardEntry[];
  total_brokers: number;
}

export interface BrokerScorecard {
  broker_id: string;
  legal_name: string;
  window_days: number;
  total_submissions: number;
  quoted_submissions: number;
  bound_submissions: number;
  declined_submissions: number;
  quote_rate: number;
  bind_rate: number;
  total_renewals: number;
  completed_renewals: number;
  lost_renewals: number;
  retention_rate: number;
  total_premium_estimate: number;
  activity_count: number;
  computed_at: string;
}

export interface TrendPoint {
  period_label: string;
  submissions: number;
  bound: number;
  renewals_completed: number;
  premium: number;
}

export interface BrokerTrends {
  broker_id: string;
  window_days: number;
  granularity: string;
  points: TrendPoint[];
}

