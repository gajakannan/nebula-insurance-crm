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
  completeness: SubmissionCompletenessDto;
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
}

export interface SubmissionUpdateDto {
  programId?: string | null;
  lineOfBusiness?: string | null;
  effectiveDate?: string | null;
  expirationDate?: string | null;
  premiumEstimate?: number | null;
  description?: string | null;
}

export interface SubmissionAssignmentRequestDto {
  assignedToUserId: string;
}

export interface WorkflowTransitionRequestDto {
  toState: SubmissionStatus;
  reason?: string | null;
}

export interface SubmissionListQuery {
  status?: string;
  brokerId?: string;
  accountId?: string;
  lineOfBusiness?: string;
  assignedToUserId?: string;
  stale?: boolean;
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
