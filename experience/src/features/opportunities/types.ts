export interface OpportunityStatusCountDto {
  status: string;
  count: number;
  colorGroup: OpportunityColorGroup;
}

export type OpportunityColorGroup =
  | 'intake'
  | 'triage'
  | 'waiting'
  | 'review'
  | 'decision'
  | 'won'
  | 'lost';

export interface DashboardOpportunitiesDto {
  submissions: OpportunityStatusCountDto[];
  renewals: OpportunityStatusCountDto[];
}

export type OpportunityEntityType = 'submission' | 'renewal';

export interface OpportunityFlowNodeDto {
  status: string;
  label: string;
  isTerminal: boolean;
  displayOrder: number;
  colorGroup: OpportunityColorGroup;
  currentCount: number;
  inflowCount: number;
  outflowCount: number;
  avgDwellDays?: number | null;
  emphasis?: OpportunityFlowEmphasis | null;
}

export type OpportunityFlowEmphasis =
  | 'normal'
  | 'active'
  | 'blocked'
  | 'bottleneck';

export interface OpportunityFlowLinkDto {
  sourceStatus: string;
  targetStatus: string;
  count: number;
}

export interface OpportunityFlowDto {
  entityType: OpportunityEntityType;
  periodDays: number;
  windowStartUtc: string;
  windowEndUtc: string;
  nodes: OpportunityFlowNodeDto[];
  links: OpportunityFlowLinkDto[];
}

export interface OpportunityMiniCardDto {
  entityId: string;
  entityName: string;
  amount: number | null;
  daysInStatus: number;
  assignedUserInitials: string | null;
  assignedUserDisplayName: string | null;
}

export interface OpportunityItemsDto {
  items: OpportunityMiniCardDto[];
  totalCount: number;
}

export type OpportunityBreakdownGroupBy =
  | 'assignedUser'
  | 'broker'
  | 'program'
  | 'lineOfBusiness'
  | 'brokerState';

export interface OpportunityBreakdownGroupDto {
  key: string | null;
  label: string;
  count: number;
}

export interface OpportunityBreakdownDto {
  entityType: OpportunityEntityType;
  status: string;
  groupBy: OpportunityBreakdownGroupBy;
  periodDays: number;
  groups: OpportunityBreakdownGroupDto[];
  total: number;
}

// View mode for opportunities widget
export type OpportunityViewMode = 'pipeline' | 'heatmap' | 'treemap' | 'sunburst';

// Aging Heatmap DTOs (S0002)
export interface OpportunityAgingBucketDto {
  key: string;
  label: string;
  count: number;
}

export interface OpportunityAgingSlaDto {
  warningDays: number;
  targetDays: number;
  totalCount: number;
  onTimeCount: number;
  approachingCount: number;
  overdueCount: number;
}

export interface OpportunityAgingStatusDto {
  status: string;
  label: string;
  colorGroup: OpportunityColorGroup;
  displayOrder: number;
  sla?: OpportunityAgingSlaDto | null;
  buckets: OpportunityAgingBucketDto[];
  total: number;
}

export interface OpportunityAgingDto {
  entityType: OpportunityEntityType;
  periodDays: number;
  statuses: OpportunityAgingStatusDto[];
}

// Hierarchy DTOs (S0003/S0004 — Treemap & Sunburst)
export interface OpportunityHierarchyNodeDto {
  id: string;
  label: string;
  count: number;
  levelType?: string;
  colorGroup?: OpportunityColorGroup;
  children?: OpportunityHierarchyNodeDto[];
}

export interface OpportunityHierarchyDto {
  periodDays: number;
  root: OpportunityHierarchyNodeDto;
}

export type OpportunityBranchStyle = 'solid' | 'red_dashed' | 'gray_dotted';

export interface OpportunityOutcomeDto {
  key: string;
  label: string;
  branchStyle: OpportunityBranchStyle;
  count: number;
  percentOfTotal: number;
  averageDaysToExit: number | null;
}

export interface OpportunityOutcomesDto {
  periodDays: number;
  totalExits: number;
  outcomes: OpportunityOutcomeDto[];
}
