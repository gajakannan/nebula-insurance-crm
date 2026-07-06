import type { GlobalSearchResult } from '@/features/search/types';

export type BrokerInsightMetricKey =
  | 'quoteCount'
  | 'bindCount'
  | 'quoteToBindRate'
  | 'retentionRate'
  | 'openPipelineCount'
  | 'activityCount'
  | 'productionAmount';

export interface BrokerInsightParams {
  brokerId?: string;
  periodStart: string;
  periodEnd: string;
  producerId?: string;
  territoryId?: string;
  programId?: string;
  lineOfBusiness?: string;
  region?: string;
  page?: number;
  pageSize?: number;
}

export interface BrokerInsightMetricCard {
  metricKey: BrokerInsightMetricKey;
  label: string;
  value: number | null;
  comparisonValue: number | null;
  unit: 'count' | 'percentage' | 'currency';
  denominator: number;
  sourceRecordCount: number;
  status: 'Available' | 'NoData' | 'Partial' | 'Unavailable';
  drilldownAvailable: boolean;
  lastRefreshedAt: string;
}

export interface BrokerInsightScorecard {
  brokerId: string;
  brokerName: string;
  periodStart: string;
  periodEnd: string;
  metrics: BrokerInsightMetricCard[];
  partialData: boolean;
  generatedAt: string;
}

export interface BrokerInsightScorecardResponse {
  items: BrokerInsightScorecard[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface BrokerInsightTrendPoint {
  bucketStart: string;
  bucketEnd: string;
  value: number | null;
  denominator: number;
  sourceRecordCount: number;
  status: string;
}

export interface BrokerInsightTrend {
  brokerId: string;
  metricKey: BrokerInsightMetricKey;
  bucket: string;
  periodStart: string;
  periodEnd: string;
  points: BrokerInsightTrendPoint[];
  sourceRows: GlobalSearchResult[];
  partialData: boolean;
  generatedAt: string;
}

export interface BrokerInsightBenchmarkMetric {
  metricKey: BrokerInsightMetricKey;
  brokerValue: number | null;
  denominator: number;
  peerMedian: number | null;
  rank: number | null;
  percentile: number | null;
  variance: number | null;
  status: 'Available' | 'Suppressed' | 'NoData';
}

export interface BrokerInsightBenchmark {
  brokerId: string;
  periodStart: string;
  periodEnd: string;
  peerSet: {
    type: string;
    visiblePeerCount: number;
    minimumPeerCount: number;
    status: 'Available' | 'InsufficientPeers' | 'NoData';
  };
  metrics: BrokerInsightBenchmarkMetric[];
  generatedAt: string;
}

export interface BrokerInsightSnapshot {
  brokerId: string;
  brokerName: string;
  periodStart: string;
  periodEnd: string;
  highlights: Array<{ label: string; value: string; sourceRecordCount: number }>;
  risks: Array<{ label: string; value: string; sourceRecordCount: number }>;
  activitySummary: string | null;
  opportunitySummary: string | null;
  sourceLinks: GlobalSearchResult[];
  partialData: boolean;
  generatedAt: string;
}
