import type { TimelineEventDto } from '@/contracts/timeline';

export type AccountStatus = 'Active' | 'Inactive' | 'Merged' | 'Deleted';

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AccountListItemDto {
  id: string;
  displayName: string;
  legalName: string | null;
  taxId: string | null;
  status: AccountStatus;
  brokerOfRecordId: string | null;
  brokerOfRecordName: string | null;
  territoryCode: string | null;
  region: string | null;
  primaryLineOfBusiness: string | null;
  lastActivityAt: string | null;
  activePolicyCount: number | null;
  openSubmissionCount: number | null;
  renewalDueCount: number | null;
  rowVersion: string;
}

export interface AccountDto {
  id: string;
  displayName: string;
  stableDisplayName: string;
  legalName: string | null;
  taxId: string | null;
  industry: string | null;
  primaryLineOfBusiness: string | null;
  status: AccountStatus;
  brokerOfRecordId: string | null;
  brokerOfRecordName: string | null;
  primaryProducerUserId: string | null;
  primaryProducerDisplayName: string | null;
  territoryCode: string | null;
  region: string | null;
  address1: string | null;
  address2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  country: string | null;
  mergedIntoAccountId: string | null;
  survivorAccountId: string | null;
  deleteReasonCode: string | null;
  deleteReasonDetail: string | null;
  removedAt: string | null;
  rowVersion: string;
  createdAt: string;
  createdByUserId: string | null;
  updatedAt: string;
  updatedByUserId: string | null;
}

export interface AccountSummaryDto {
  id: string;
  displayName: string;
  status: AccountStatus;
  brokerOfRecordName: string | null;
  primaryProducerDisplayName: string | null;
  territoryCode: string | null;
  region: string | null;
  activePolicyCount: number;
  openSubmissionCount: number;
  renewalDueCount: number;
  lastActivityAt: string | null;
  rowVersion: string;
}

export interface AccountCreateRequestDto {
  displayName: string;
  legalName?: string | null;
  taxId?: string | null;
  industry?: string | null;
  primaryLineOfBusiness?: string | null;
  brokerOfRecordId?: string | null;
  primaryProducerUserId?: string | null;
  territoryCode?: string | null;
  region?: string | null;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  linkFromSubmissionId?: string | null;
  linkFromPolicyId?: string | null;
}

export interface AccountUpdateRequestDto {
  displayName?: string | null;
  legalName?: string | null;
  taxId?: string | null;
  industry?: string | null;
  primaryLineOfBusiness?: string | null;
  territoryCode?: string | null;
  region?: string | null;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface AccountLifecycleRequestDto {
  toState: AccountStatus;
  reasonCode?: string | null;
  reasonDetail?: string | null;
}

export interface AccountMergeRequestDto {
  survivorAccountId: string;
  notes?: string | null;
}

export interface AccountRelationshipRequestDto {
  relationshipType: 'BrokerOfRecord' | 'PrimaryProducer' | 'Territory';
  newValue: string;
  notes?: string | null;
}

export interface AccountContactDto {
  id: string;
  accountId: string;
  fullName: string;
  role: string | null;
  email: string | null;
  phone: string | null;
  isPrimary: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string;
}

export interface AccountContactRequestDto {
  fullName: string;
  role?: string | null;
  email?: string | null;
  phone?: string | null;
  isPrimary: boolean;
}

export interface AccountPolicyListItemDto {
  id: string;
  policyNumber: string;
  accountId?: string;
  accountDisplayName?: string | null;
  accountStatus?: string | null;
  accountSurvivorId?: string | null;
  brokerOfRecordId?: string;
  brokerName?: string | null;
  carrierId?: string;
  carrierName: string | null;
  lineOfBusiness: string;
  status: string;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: number;
  premiumCurrency: string;
  versionCount?: number;
  endorsementCount?: number;
  hasOpenRenewal?: boolean;
  reinstatementDeadline?: string | null;
  rowVersion?: string;
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

export interface AccountListQuery {
  query?: string;
  status?: string;
  territoryCode?: string;
  region?: string;
  brokerOfRecordId?: string;
  primaryLineOfBusiness?: string;
  includeSummary?: boolean;
  includeRemoved?: boolean;
  sort?: 'displayName' | 'status' | 'territoryCode';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface AccountProblemDetails {
  stableDisplayName?: string;
  removedAt?: string;
  reasonCode?: string;
}

export type AccountTimelineResponse = PaginatedResponse<TimelineEventDto>;
