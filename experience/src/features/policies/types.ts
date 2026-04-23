import type { TimelineEventDto } from '@/contracts/timeline';

export type PolicyStatus = 'Pending' | 'Issued' | 'Cancelled' | 'Expired';

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PolicyCoverageInputDto {
  coverageCode: string;
  coverageName?: string | null;
  limit: number;
  deductible?: number | null;
  premium: number;
  exposureBasis?: string | null;
  exposureQuantity?: number | null;
}

export interface PolicyCreateRequestDto {
  accountId: string;
  brokerOfRecordId: string;
  lineOfBusiness: string;
  carrierId: string;
  effectiveDate: string;
  expirationDate: string;
  predecessorPolicyId?: string | null;
  producerUserId?: string | null;
  totalPremium?: number | null;
  premiumCurrency?: string | null;
  importMode?: string | null;
  externalPolicyReference?: string | null;
  coverages?: PolicyCoverageInputDto[] | null;
}

export interface PolicyUpdateRequestDto {
  lineOfBusiness?: string | null;
  carrierId?: string | null;
  effectiveDate?: string | null;
  expirationDate?: string | null;
  producerUserId?: string | null;
  totalPremium?: number | null;
  externalPolicyReference?: string | null;
}

export interface PolicyIssueRequestDto {
  issuedAt?: string | null;
}

export interface PolicyEndorsementRequestDto {
  endorsementReasonCode: string;
  endorsementReasonDetail?: string | null;
  effectiveDate: string;
  premiumDelta?: number | null;
  coverages: PolicyCoverageInputDto[];
}

export interface PolicyCancelRequestDto {
  cancellationReasonCode: string;
  cancellationReasonDetail?: string | null;
  cancellationEffectiveDate: string;
}

export interface PolicyReinstateRequestDto {
  reinstatementReason: string;
  reinstatementDetail?: string | null;
}

export interface PolicyImportRequestDto {
  policies: PolicyCreateRequestDto[];
}

export interface PolicyImportRejectedRowDto {
  index: number;
  code: string;
  message: string;
}

export interface PolicyImportResultDto {
  accepted: PolicyDto[];
  rejected: PolicyImportRejectedRowDto[];
}

export interface PolicyListItemDto {
  id: string;
  policyNumber: string;
  accountId: string;
  accountDisplayName: string | null;
  accountStatus: string | null;
  accountSurvivorId: string | null;
  brokerOfRecordId: string;
  brokerName: string | null;
  carrierId: string;
  carrierName: string | null;
  lineOfBusiness: string;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: number;
  premiumCurrency: string;
  producerUserId: string | null;
  producerDisplayName: string | null;
  versionCount: number;
  endorsementCount: number;
  hasOpenRenewal: boolean;
  reinstatementDeadline: string | null;
  rowVersion: string;
}

export interface PolicyDto {
  id: string;
  accountId: string;
  brokerOfRecordId: string;
  policyNumber: string;
  lineOfBusiness: string;
  carrierId: string;
  carrierName: string | null;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  boundAt: string | null;
  issuedAt: string | null;
  cancelledAt: string | null;
  cancellationEffectiveDate: string | null;
  cancellationReasonCode: string | null;
  cancellationReasonDetail: string | null;
  reinstatementDeadline: string | null;
  expiredAt: string | null;
  predecessorPolicyId: string | null;
  successorPolicyId: string | null;
  currentVersionId: string | null;
  currentVersionNumber: number;
  versionCount: number;
  totalPremium: number;
  premiumCurrency: string;
  producerUserId: string | null;
  producerDisplayName: string | null;
  importSource: string | null;
  externalPolicyReference: string | null;
  accountDisplayNameAtLink: string | null;
  accountStatusAtRead: string | null;
  accountSurvivorId: string | null;
  availableTransitions: string[];
  rowVersion: string;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
}

export interface PolicySummaryDto {
  id: string;
  policyNumber: string;
  accountId: string;
  accountDisplayName: string | null;
  accountStatus: string | null;
  accountSurvivorId: string | null;
  brokerOfRecordId: string;
  brokerName: string | null;
  carrierId: string;
  carrierName: string | null;
  lineOfBusiness: string;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  boundAt: string | null;
  cancelledAt: string | null;
  cancellationEffectiveDate: string | null;
  cancellationReasonCode: string | null;
  reinstatementDeadline: string | null;
  expiredAt: string | null;
  predecessorPolicyId: string | null;
  predecessorPolicyNumber: string | null;
  successorPolicyId: string | null;
  successorPolicyNumber: string | null;
  totalPremium: number;
  premiumCurrency: string;
  currentVersionId: string | null;
  currentVersionNumber: number;
  versionCount: number;
  endorsementCount: number;
  coverageLineCount: number;
  openRenewalCount: number;
  producerUserId: string | null;
  producerDisplayName: string | null;
  importSource: string | null;
  externalPolicyReference: string | null;
  availableTransitions: string[];
  rowVersion: string;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
}

export interface PolicyVersionDto {
  id: string;
  policyId: string;
  versionNumber: number;
  versionReason: string;
  endorsementId: string | null;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: number;
  premiumCurrency: string;
  profileSnapshot: unknown;
  coverageSnapshot: unknown;
  premiumSnapshot: unknown;
  createdAt: string;
  createdByUserId: string;
}

export interface PolicyEndorsementDto {
  id: string;
  policyId: string;
  endorsementNumber: number;
  policyVersionId: string;
  versionNumber: number | null;
  endorsementReasonCode: string;
  endorsementReasonDetail: string | null;
  effectiveDate: string;
  premiumDelta: number;
  premiumCurrency: string;
  createdAt: string;
  createdByUserId: string;
}

export interface PolicyCoverageLineDto {
  id: string;
  policyId: string;
  policyVersionId: string;
  versionNumber: number;
  coverageCode: string;
  coverageName: string | null;
  limit: number;
  deductible: number | null;
  premium: number;
  premiumCurrency: string;
  exposureBasis: string | null;
  exposureQuantity: number | null;
  isCurrent: boolean;
  createdAt: string;
}

export interface PolicyAccountSummaryDto {
  accountId: string;
  activePolicyCount: number;
  expiredPolicyCount: number;
  cancelledPolicyCount: number;
  pendingPolicyCount: number;
  nextExpiringDate: string | null;
  nextExpiringPolicyId: string | null;
  nextExpiringPolicyNumber: string | null;
  totalCurrentPremium: number;
  premiumCurrency: string;
  computedAt: string;
}

export type PolicyTimelineResponse = PaginatedResponse<TimelineEventDto>;
