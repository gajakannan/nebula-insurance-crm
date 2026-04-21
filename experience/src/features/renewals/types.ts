export type RenewalStatus =
  | 'Identified'
  | 'Outreach'
  | 'InReview'
  | 'Quoted'
  | 'Completed'
  | 'Lost';

export type RenewalUrgency = 'overdue' | 'approaching' | null;

export type RenewalLostReasonCode =
  | 'NonRenewal'
  | 'CompetitiveLoss'
  | 'BusinessClosed'
  | 'CoverageNoLongerNeeded'
  | 'PricingDeclined'
  | 'Other';

export interface RenewalListItemDto {
  id: string;
  accountId: string;
  accountDisplayName: string;
  accountStatus: string;
  accountSurvivorId: string | null;
  accountName: string;
  accountIndustry: string;
  accountPrimaryState: string;
  brokerName: string;
  brokerLicenseNumber: string;
  brokerState: string;
  policyNumber: string;
  carrier: string | null;
  lineOfBusiness: string | null;
  currentStatus: RenewalStatus;
  policyExpirationDate: string;
  targetOutreachDate: string;
  assignedToUserId: string;
  assignedUserDisplayName: string | null;
  urgency: RenewalUrgency;
  rowVersion: string;
}

export interface RenewalDto {
  id: string;
  accountId: string;
  brokerId: string;
  policyId: string;
  currentStatus: RenewalStatus;
  lineOfBusiness: string | null;
  policyExpirationDate: string;
  targetOutreachDate: string;
  assignedToUserId: string;
  lostReasonCode: RenewalLostReasonCode | null;
  lostReasonDetail: string | null;
  boundPolicyId: string | null;
  renewalSubmissionId: string | null;
  urgency: RenewalUrgency;
  availableTransitions: RenewalStatus[];
  assignedUserDisplayName: string | null;
  accountDisplayName: string | null;
  accountStatus: string;
  accountSurvivorId: string | null;
  accountName: string | null;
  accountIndustry: string | null;
  accountPrimaryState: string | null;
  brokerName: string | null;
  brokerLicenseNumber: string | null;
  brokerState: string | null;
  policyNumber: string | null;
  policyCarrier: string | null;
  policyEffectiveDate: string | null;
  policyPremium: number | null;
  rowVersion: string;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
}

export interface RenewalCreateDto {
  policyId: string;
  assignedToUserId?: string | null;
  lineOfBusiness?: string | null;
}

export interface RenewalAssignmentRequestDto {
  assignedToUserId: string;
}

export interface RenewalTransitionRequestDto {
  toState: RenewalStatus;
  reason?: string | null;
  reasonCode?: RenewalLostReasonCode | null;
  reasonDetail?: string | null;
  boundPolicyId?: string | null;
  renewalSubmissionId?: string | null;
}

export interface WorkflowTransitionRecordDto {
  id: string;
  workflowType: string;
  entityId: string;
  fromState: RenewalStatus | null;
  toState: RenewalStatus;
  reason: string | null;
  occurredAt: string;
}

export interface RenewalListQuery {
  dueWindow?: '45' | '60' | '90' | 'overdue';
  status?: string;
  assignedToUserId?: string;
  lineOfBusiness?: string;
  accountId?: string;
  brokerId?: string;
  urgency?: 'overdue' | 'approaching';
  sort?: 'policyExpirationDate' | 'accountName' | 'currentStatus' | 'assignedToUserId';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
  includeTerminal?: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
