import type { OpportunityFlowNodeDto, OpportunityOutcomeDto } from '../types';

export interface StageAnchor {
  status: string;
  label: string;
  avgDwellDays?: number | null;
  emphasis?: OpportunityFlowNodeDto['emphasis'];
}

export interface OutcomeAnchor {
  key: string;
  label: string;
  branchStyle: OpportunityOutcomeDto['branchStyle'];
  count: number;
  percentOfTotal: number;
}
