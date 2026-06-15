import type { LobAttributeEnvelopeDto } from '@/features/lob-attributes'

export type SubmissionStatus =
  | 'Received'
  | 'Triaging'
  | 'WaitingOnBroker'
  | 'ReadyForUWReview'
  | 'InReview'
  | 'Quoted'
  | 'BindRequested'
  | 'Bound'
  | 'Declined'
  | 'Withdrawn';

export type SubmissionFieldCheckStatus = 'pass' | 'missing';
export type SubmissionDocumentCheckStatus = 'pass' | 'missing' | 'unavailable';

export interface SubmissionListItemDto {
  id: string;
  accountId: string;
  accountDisplayName: string;
  accountStatus: string;
  accountSurvivorId: string | null;
  accountName: string;
  brokerName: string;
  lineOfBusiness: string | null;
  currentStatus: SubmissionStatus;
  effectiveDate: string;
  assignedToUserId: string;
  assignedToDisplayName: string | null;
  createdAt: string;
  isStale: boolean;
  ageDaysInState: number;
  approvalStatus: string;
  approvalPending: boolean;
  isArchived: boolean;
  stuckFlag: boolean;
}

export interface SubmissionFieldCheckDto {
  field: string;
  required: boolean;
  status: SubmissionFieldCheckStatus;
}

export interface SubmissionDocumentCheckDto {
  category: string;
  required: boolean;
  status: SubmissionDocumentCheckStatus;
}

export interface SubmissionCompletenessDto {
  isComplete: boolean;
  fieldChecks: SubmissionFieldCheckDto[];
  documentChecks: SubmissionDocumentCheckDto[];
  missingItems: string[];
}

export interface SubmissionDto {
  id: string;
  accountId: string;
  brokerId: string;
  programId: string | null;
  lineOfBusiness: string | null;
  currentStatus: SubmissionStatus;
  effectiveDate: string;
  expirationDate: string | null;
  premiumEstimate: number | null;
  description: string | null;
  lobAttributes: LobAttributeEnvelopeDto | null;
  assignedToUserId: string;
  accountDisplayName: string;
  accountStatus: string;
  accountSurvivorId: string | null;
  accountName: string;
  accountRegion: string | null;
  accountIndustry: string | null;
  brokerName: string;
  brokerLicenseNumber: string | null;
  programName: string | null;
  assignedToDisplayName: string | null;
  isStale: boolean;
  ageDaysInState: number;
  approvalStatus: string;
  approvalPending: boolean;
  isArchived: boolean;
  archivedAt: string | null;
  archivedByUserId: string | null;
  completeness: SubmissionCompletenessDto;
  quotePacket: SubmissionQuotePacketDto;
  approvalDecisions: SubmissionApprovalDecisionDto[];
  bindHandoff: SubmissionBindHandoffDto | null;
  availableTransitions: SubmissionStatus[];
  rowVersion: string;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
}

export interface WorkflowTransitionRecordDto {
  id: string;
  workflowType: string;
  entityId: string;
  fromState: SubmissionStatus | null;
  toState: SubmissionStatus;
  reason: string | null;
  occurredAt: string;
}

export interface SubmissionCreateDto {
  accountId: string;
  brokerId: string;
  effectiveDate: string;
  programId?: string | null;
  lineOfBusiness?: string | null;
  premiumEstimate?: number | null;
  expirationDate?: string | null;
  description?: string | null;
  lobAttributes?: LobAttributeEnvelopeDto | null;
}

export interface SubmissionUpdateDto {
  programId?: string | null;
  lineOfBusiness?: string | null;
  effectiveDate?: string | null;
  expirationDate?: string | null;
  premiumEstimate?: number | null;
  description?: string | null;
  lobAttributes?: LobAttributeEnvelopeDto | null;
}

export interface SubmissionAssignmentRequestDto {
  assignedToUserId: string;
}

export interface WorkflowTransitionRequestDto {
  toState: SubmissionStatus;
  reason?: string | null;
  reasonCode?: string | null;
  reasonDetail?: string | null;
}

export interface SubmissionQuotePacketDto {
  id: string | null;
  submissionId: string;
  status: string;
  linkedDocumentRefs: string[];
  recordedPremiumAmount: number | null;
  recordedLimits: string | null;
  recordedDeductibles: string | null;
  effectiveDate: string | null;
  carrierMarket: string | null;
  readinessState: string;
  readyAt: string | null;
  readyByUserId: string | null;
  approvedAt: string | null;
  approvedByUserId: string | null;
  rowVersion: string;
}

export interface SubmissionQuotePacketUpdateDto {
  linkedDocumentRefs?: string[] | null;
  recordedPremiumAmount?: number | null;
  recordedLimits?: string | null;
  recordedDeductibles?: string | null;
  effectiveDate?: string | null;
  carrierMarket?: string | null;
  markReady?: boolean;
}

export interface SubmissionApprovalDecisionDto {
  id: string;
  submissionId: string;
  decision: string;
  approverUserId: string;
  reason: string;
  blockingConditions: string[];
  decidedAt: string;
}

export interface SubmissionApprovalRequestDto {
  decision: 'Granted' | 'Declined';
  reason: string;
  blockingConditions?: string[];
}

export interface SubmissionBindRequestDto {
  idempotencyKey?: string | null;
}

export interface SubmissionBindHandoffDto {
  id: string;
  submissionId: string;
  idempotencyKey: string;
  status: string;
  correlationId: string;
  requestedAt: string;
  completedAt: string | null;
}

export interface SubmissionArchiveRequestDto {
  reason: string;
}

export interface SubmissionListQuery {
  status?: string;
  brokerId?: string;
  accountId?: string;
  lineOfBusiness?: string;
  assignedToUserId?: string;
  stale?: boolean;
  approvalPending?: boolean;
  stuckOnly?: boolean;
  includeArchived?: boolean;
  sort?: 'createdAt' | 'effectiveDate' | 'accountName' | 'brokerName' | 'currentStatus';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AccountReferenceDto {
  id: string;
  name: string;
  status: string;
  industry: string | null;
}

export interface ProgramReferenceDto {
  id: string;
  name: string;
  mgaId: string;
}
