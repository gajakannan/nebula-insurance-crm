export type WorkType = 'Task' | 'Submission' | 'Renewal' | 'Mixed';
export type QueueStatus = 'Active' | 'Inactive';
export type RuleStatus = 'Draft' | 'Active' | 'Inactive';
export type CoverageStatus = 'Scheduled' | 'Active' | 'Cancelled';
export type QueueItemStatus = 'Open' | 'Assigned' | 'InProgress' | 'Closed';

export interface WorkQueue {
  id: string;
  name: string;
  workType: WorkType;
  status: QueueStatus;
  isFallback: boolean;
  description?: string | null;
  activeMemberCount: number;
  openItemCount: number;
  rowVersion: number;
}

export interface QueueMemberUpsertRequest {
  userProfileId: string;
  role: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
}

export interface WorkQueueUpsertRequest {
  name: string;
  workType: WorkType;
  status: QueueStatus;
  description?: string | null;
  members: QueueMemberUpsertRequest[];
}

export interface AssignmentRule {
  id: string;
  workQueueId: string;
  ruleType: string;
  precedence: number;
  version: number;
  status: RuleStatus;
  conditionsJson: string;
  outcomeJson: string;
  rowVersion: number;
}

export interface AssignmentRuleUpsertRequest {
  workQueueId: string;
  ruleType: string;
  precedence: number;
  status: RuleStatus;
  conditionsJson: string;
  outcomeJson: string;
}

export interface CoverageWindow {
  id: string;
  coveredUserId: string;
  backupUserId: string;
  workQueueId?: string | null;
  startsAt: string;
  endsAt: string;
  status: CoverageStatus;
  reason?: string | null;
  rowVersion: number;
}

export interface CoverageWindowUpsertRequest {
  coveredUserId: string;
  backupUserId: string;
  workQueueId?: string | null;
  startsAt: string;
  endsAt: string;
  status: CoverageStatus;
  reason?: string | null;
}

export interface QueueWorkItem {
  id: string;
  workQueueId: string;
  sourceType: string;
  sourceId: string;
  assignedToUserId?: string | null;
  queueStatus: QueueItemStatus;
  routedAt: string;
  ruleVersion?: string | null;
  matchReason?: string | null;
  rowVersion: number;
}

export interface QueueReassignmentRequest {
  assignedToUserId: string;
  reason: string;
}

export interface QueueRebalanceRequest {
  strategy: string;
  maxItems?: number | null;
  reason: string;
}

export interface RoutingEvent {
  id: string;
  queueWorkItemId?: string | null;
  sourceType: string;
  sourceId: string;
  outcome: string;
  reasonCode: string;
  actorUserId?: string | null;
  occurredAt: string;
  decisionPayloadJson: string;
}

export interface ListResponse<T> {
  items: T[];
  totalCount: number;
}
