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
  workflowType?: string;
  asOf?: string;
  drilldownLimit?: number;
}
