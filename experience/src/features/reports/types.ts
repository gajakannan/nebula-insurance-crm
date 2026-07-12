import type { GlobalSearchResult } from '@/features/search/types';

export interface CountByKey {
  key: string;
  label: string | null;
  count: number;
}

export interface AgingBand {
  ageBand: string;
  count: number;
}

export interface OperationalWorkloadReport {
  totalOpen: number;
  dueToday: number;
  overdue: number;
  unassigned: number;
  byOwner: CountByKey[];
  byStatus: CountByKey[];
  byWorkflowType: CountByKey[];
  dueTodayDrilldown: GlobalSearchResult[];
  overdueDrilldown: GlobalSearchResult[];
  asOf: string;
  generatedAt: string;
}

export interface WorkflowAgingReport {
  totalOpen: number;
  byAgeBand: AgingBand[];
  byWorkflowType: CountByKey[];
  byStatus: CountByKey[];
  backlogDrilldown: GlobalSearchResult[];
  asOf: string;
  generatedAt: string;
}

export interface OperationalReportParams {
  region?: string;
  lineOfBusiness?: string;
  ownerUserId?: string;
  rootNodeId?: string;
  territoryId?: string;
  producerUserId?: string;
  workflowType?: string;
  asOf?: string;
  drilldownLimit?: number;
}

export interface DistributionScopeEcho {
  rootNodeId: string | null;
  territoryId: string | null;
  producerUserId: string | null;
}

export interface DistributionRollupMetricSet {
  recordCount: number;
  productionCount: number;
  workflowOpen: number;
  workflowOverdue: number;
  activityCount: number;
}

export interface DistributionRollupRow {
  groupKey: string;
  groupLabel: string;
  groupType: string;
  metrics: DistributionRollupMetricSet;
  drilldownUrl: string | null;
  unavailableReason: string | null;
}

export interface DistributionRollupReport {
  groupBy: string;
  metricFamily: string;
  asOf: string;
  generatedAt: string;
  scope: DistributionScopeEcho | null;
  totals: DistributionRollupMetricSet;
  rows: DistributionRollupRow[];
}

export interface DistributionRollupParams extends OperationalReportParams {
  groupBy?: 'Hierarchy' | 'Territory' | 'Producer';
  metricFamily?: 'Production' | 'Workflow' | 'Activity';
}
