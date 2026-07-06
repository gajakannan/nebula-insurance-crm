import type { TaskDto } from '@/features/tasks';

export type ServiceCaseStatus = 'Intake' | 'InProgress' | 'Waiting' | 'Resolved' | 'Closed';
export type ServiceCasePriority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type ServiceCaseType = 'Service' | 'ClaimSupport' | 'Billing' | 'PolicyChange' | 'General';

export interface ServiceCaseClaimReferenceDto {
  carrierClaimNumber: string | null;
  dateOfLoss: string | null;
  claimantDisplayName: string | null;
  lossSummary: string | null;
  carrierContactReference: string | null;
  updatedByUserId: string | null;
  updatedAt: string | null;
}

export interface ServiceCaseCommunicationLinkDto {
  communicationEventId: string;
  linkType: string;
  createdByUserId: string;
  createdAt: string;
}

export interface ServiceCaseTaskLinkDto {
  taskId: string;
  relationship: string;
  createdByUserId: string;
  createdAt: string;
}

export interface ServiceCaseTransitionDto {
  fromStatus: ServiceCaseStatus | null;
  toStatus: ServiceCaseStatus;
  actorUserId: string;
  occurredAt: string;
  reasonCode: string | null;
  note: string | null;
}

export interface ServiceCaseDto {
  id: string;
  caseNumber: string;
  accountId: string;
  policyId: string | null;
  summary: string;
  description: string | null;
  type: ServiceCaseType;
  status: ServiceCaseStatus;
  priority: ServiceCasePriority;
  ownerUserId: string;
  ownerDisplayName: string | null;
  accountDisplayName: string | null;
  policyNumber: string | null;
  dueDate: string | null;
  followUpSummary: string | null;
  hasClaimReference: boolean;
  lastActivityAt: string | null;
  claimReference: ServiceCaseClaimReferenceDto | null;
  communicationLinks: ServiceCaseCommunicationLinkDto[];
  taskLinks: ServiceCaseTaskLinkDto[];
  transitions: ServiceCaseTransitionDto[];
  resolvedAt: string | null;
  closedAt: string | null;
  resolutionSummary: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string | null;
  rowVersion: number;
}

export interface ServiceCaseCreateRequestDto {
  accountId: string;
  policyId?: string | null;
  summary: string;
  description?: string | null;
  type: ServiceCaseType;
  priority: ServiceCasePriority;
  ownerUserId: string;
  dueDate?: string | null;
  followUpSummary?: string | null;
  claimReference?: ServiceCaseClaimReferenceUpdateRequestDto | null;
}

export interface ServiceCaseUpdateRequestDto {
  summary?: string;
  description?: string | null;
  priority?: ServiceCasePriority;
  ownerUserId?: string;
  dueDate?: string | null;
  followUpSummary?: string | null;
  resolutionSummary?: string | null;
  rowVersion?: number;
}

export interface ServiceCaseTransitionRequestDto {
  toStatus: ServiceCaseStatus;
  reasonCode?: string | null;
  note?: string | null;
  resolutionSummary?: string | null;
  rowVersion?: number;
}

export interface ServiceCaseClaimReferenceUpdateRequestDto {
  carrierClaimNumber?: string | null;
  dateOfLoss?: string | null;
  claimantDisplayName?: string | null;
  lossSummary?: string | null;
  carrierContactReference?: string | null;
  rowVersion?: number;
}

export interface ServiceCaseFollowUpTaskRequestDto {
  title: string;
  description?: string | null;
  assignedToUserId: string;
  dueDate?: string | null;
  priority?: 'Low' | 'Normal' | 'High' | null;
  rowVersion?: number;
}

export interface ServiceCaseCommunicationLinkRequestDto {
  communicationEventId: string;
  linkType?: 'Context' | 'Evidence' | 'FollowUp' | null;
  rowVersion?: number;
}

export interface ServiceCaseListQuery {
  accountId?: string;
  policyId?: string;
  ownerUserId?: string;
  status?: ServiceCaseStatus;
  priority?: ServiceCasePriority;
  q?: string;
  dueBefore?: string;
  dueAfter?: string;
  includeClosed?: boolean;
  page?: number;
  pageSize?: number;
}

export interface ServiceCaseListResponseDto {
  data: ServiceCaseDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type ServiceCaseFollowUpTaskResponse = TaskDto;
