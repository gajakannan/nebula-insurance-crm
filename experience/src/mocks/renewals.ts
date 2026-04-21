import type { TimelineEventDto } from '@/contracts/timeline';
import type {
  PaginatedResponse,
  RenewalAssignmentRequestDto,
  RenewalCreateDto,
  RenewalDto,
  RenewalListItemDto,
  RenewalLostReasonCode,
  RenewalStatus,
  RenewalTransitionRequestDto,
  RenewalUrgency,
  WorkflowTransitionRecordDto,
} from '@/features/renewals';
import type { UserSummaryDto } from '@/features/tasks';
import { brokerListFixture } from './brokers';
import { accountReferenceFixture, submissionUsersFixture } from './submissions';

interface MockPolicyRecord {
  id: string;
  accountId: string;
  brokerId: string;
  policyNumber: string;
  carrier: string | null;
  lineOfBusiness: string | null;
  effectiveDate: string | null;
  expirationDate: string;
  premium: number | null;
}

interface MockRenewalRecord {
  id: string;
  policyId: string;
  accountId: string;
  brokerId: string;
  currentStatus: RenewalStatus;
  lineOfBusiness: string | null;
  policyExpirationDate: string;
  targetOutreachDate: string;
  assignedToUserId: string;
  lostReasonCode: RenewalLostReasonCode | null;
  lostReasonDetail: string | null;
  boundPolicyId: string | null;
  renewalSubmissionId: string | null;
  rowVersion: number;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
  timeline: TimelineEventDto[];
}

interface MockMutationError {
  code: string;
  detail: string;
  errors?: Record<string, string[]>;
}

const TODAY = Date.parse('2026-04-11T00:00:00Z');

const accountPrimaryStateById = new Map<string, string>([
  ['account-1', 'CA'],
  ['account-2', 'WA'],
  ['account-3', 'CO'],
]);

const policyFixtures: MockPolicyRecord[] = [
  {
    id: 'policy-1',
    accountId: 'account-1',
    brokerId: 'broker-1',
    policyNumber: 'BH-2026-001',
    carrier: 'Lighthouse Specialty',
    lineOfBusiness: 'Property',
    effectiveDate: '2025-05-21',
    expirationDate: '2026-05-20',
    premium: 850000,
  },
  {
    id: 'policy-2',
    accountId: 'account-2',
    brokerId: 'broker-1',
    policyNumber: 'CM-2026-204',
    carrier: 'Nexus Cyber',
    lineOfBusiness: 'Cyber',
    effectiveDate: '2025-06-11',
    expirationDate: '2026-06-10',
    premium: 240000,
  },
  {
    id: 'policy-3',
    accountId: 'account-3',
    brokerId: 'broker-1',
    policyNumber: 'NC-2026-778',
    carrier: 'Summit Casualty',
    lineOfBusiness: 'GeneralLiability',
    effectiveDate: '2025-07-06',
    expirationDate: '2026-07-05',
    premium: 420000,
  },
  {
    id: 'policy-4',
    accountId: 'account-3',
    brokerId: 'broker-1',
    policyNumber: 'NC-2026-990',
    carrier: 'Atlas Excess',
    lineOfBusiness: 'Umbrella',
    effectiveDate: '2025-08-26',
    expirationDate: '2026-08-25',
    premium: 310000,
  },
];

const policyById = new Map(policyFixtures.map((policy) => [policy.id, policy] as const));
const userById = new Map(submissionUsersFixture.map((user) => [user.userId, user] as const));
const brokerById = new Map(brokerListFixture.map((broker) => [broker.id, broker] as const));
const accountById = new Map(accountReferenceFixture.map((account) => [account.id, account] as const));

const terminalStatuses = new Set<RenewalStatus>(['Completed', 'Lost']);

const transitionMap: Record<RenewalStatus, RenewalStatus[]> = {
  Identified: ['Outreach', 'Lost'],
  Outreach: ['InReview', 'Lost'],
  InReview: ['Quoted', 'Lost'],
  Quoted: ['Completed', 'Lost'],
  Completed: [],
  Lost: [],
};

const statusSortOrder: Record<RenewalStatus, number> = {
  Identified: 1,
  Outreach: 2,
  InReview: 3,
  Quoted: 4,
  Completed: 5,
  Lost: 6,
};

let renewalState = createInitialRenewalState();

export function resetRenewalMockState() {
  renewalState = createInitialRenewalState();
}

export function listRenewals(
  searchParams: URLSearchParams,
): PaginatedResponse<RenewalListItemDto> {
  const dueWindow = searchParams.get('dueWindow');
  const status = searchParams.get('status');
  const assignedToUserId = searchParams.get('assignedToUserId');
  const lineOfBusiness = searchParams.get('lineOfBusiness');
  const urgency = searchParams.get('urgency');
  const includeTerminal = searchParams.get('includeTerminal') === 'true';
  const sort = searchParams.get('sort') ?? 'policyExpirationDate';
  const sortDir = searchParams.get('sortDir') ?? 'asc';
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '25');

  const filtered = renewalState
    .filter((renewal) => includeTerminal || !terminalStatuses.has(renewal.currentStatus))
    .filter((renewal) => !status || renewal.currentStatus === status)
    .filter((renewal) => !assignedToUserId || renewal.assignedToUserId === assignedToUserId)
    .filter((renewal) => !lineOfBusiness || renewal.lineOfBusiness === lineOfBusiness)
    .filter((renewal) => !urgency || getRenewalUrgency(renewal) === urgency)
    .filter((renewal) => matchesDueWindow(renewal, dueWindow));

  filtered.sort((left, right) => {
    const direction = sortDir === 'desc' ? -1 : 1;
    const leftValue = sortValue(left, sort);
    const rightValue = sortValue(right, sort);
    return leftValue.localeCompare(rightValue) * direction;
  });

  const normalizedPage = Math.max(page, 1);
  const normalizedPageSize = Math.max(pageSize, 1);
  const offset = (normalizedPage - 1) * normalizedPageSize;

  return {
    data: filtered.slice(offset, offset + normalizedPageSize).map(toListItem),
    page: normalizedPage,
    pageSize: normalizedPageSize,
    totalCount: filtered.length,
    totalPages: Math.max(1, Math.ceil(filtered.length / normalizedPageSize)),
  };
}

export function getRenewal(renewalId: string): RenewalDto | null {
  const renewal = renewalState.find((record) => record.id === renewalId);
  return renewal ? toDetail(renewal) : null;
}

export function getRenewalTimeline(
  renewalId: string,
  page: number,
  pageSize: number,
): PaginatedResponse<TimelineEventDto> | null {
  const renewal = renewalState.find((record) => record.id === renewalId);
  if (!renewal) return null;

  const normalizedPage = Math.max(page, 1);
  const normalizedPageSize = Math.max(pageSize, 1);
  const offset = (normalizedPage - 1) * normalizedPageSize;

  return {
    data: renewal.timeline.slice(offset, offset + normalizedPageSize),
    page: normalizedPage,
    pageSize: normalizedPageSize,
    totalCount: renewal.timeline.length,
    totalPages: Math.max(1, Math.ceil(renewal.timeline.length / normalizedPageSize)),
  };
}

export function createRenewal(
  dto: RenewalCreateDto,
): RenewalDto | MockMutationError {
  const policy = policyById.get(dto.policyId);
  if (!policy) {
    return {
      code: 'not_found',
      detail: 'The requested policy was not found.',
    };
  }

  if (renewalState.some((renewal) => renewal.policyId === policy.id && !terminalStatuses.has(renewal.currentStatus))) {
    return {
      code: 'duplicate_renewal',
      detail: 'An active renewal already exists for this policy.',
    };
  }

  const assignee = resolveAssignableUser(dto.assignedToUserId ?? 'user-dist-manager', 'Identified');
  if ('code' in assignee) {
    return assignee;
  }

  const createdAt = nextTimestamp();
  const outreachDate = offsetIsoDate(policy.expirationDate, -45);
  const renewal: MockRenewalRecord = {
    id: `renewal-${renewalState.length + 1}`,
    policyId: policy.id,
    accountId: policy.accountId,
    brokerId: policy.brokerId,
    currentStatus: 'Identified',
    lineOfBusiness: dto.lineOfBusiness ?? policy.lineOfBusiness,
    policyExpirationDate: policy.expirationDate,
    targetOutreachDate: outreachDate,
    assignedToUserId: assignee.userId,
    lostReasonCode: null,
    lostReasonDetail: null,
    boundPolicyId: null,
    renewalSubmissionId: null,
    rowVersion: 1,
    createdAt,
    createdByUserId: assignee.userId,
    updatedAt: createdAt,
    updatedByUserId: assignee.userId,
    timeline: [
      buildTimelineEvent(
        `renewal-${renewalState.length + 1}`,
        1,
        'RenewalCreated',
        `Renewal created for policy ${policy.policyNumber}.`,
        assignee.displayName,
        createdAt,
      ),
    ],
  };

  renewalState = [renewal, ...renewalState];
  return toDetail(renewal);
}

export function assignRenewal(
  renewalId: string,
  rowVersion: string | null,
  dto: RenewalAssignmentRequestDto,
): RenewalDto | MockMutationError | null {
  const renewal = renewalState.find((record) => record.id === renewalId);
  if (!renewal) return null;

  const rowVersionError = validateRowVersion(renewal, rowVersion);
  if (rowVersionError) return rowVersionError;

  if (terminalStatuses.has(renewal.currentStatus)) {
    return {
      code: 'assignment_not_allowed_in_terminal_state',
      detail: 'Completed and lost renewals cannot be reassigned.',
    };
  }

  const assignee = resolveAssignableUser(dto.assignedToUserId, renewal.currentStatus);
  if ('code' in assignee) {
    return assignee;
  }

  renewal.assignedToUserId = assignee.userId;
  renewal.rowVersion += 1;
  renewal.updatedAt = nextTimestamp();
  renewal.updatedByUserId = assignee.userId;
  renewal.timeline.unshift(
    buildTimelineEvent(
      renewal.id,
      renewal.timeline.length + 1,
      'RenewalAssigned',
      `Renewal assigned to ${assignee.displayName}.`,
      'Sarah Chen',
      renewal.updatedAt,
    ),
  );

  return toDetail(renewal);
}

export function transitionRenewal(
  renewalId: string,
  rowVersion: string | null,
  dto: RenewalTransitionRequestDto,
): WorkflowTransitionRecordDto | MockMutationError | null {
  const renewal = renewalState.find((record) => record.id === renewalId);
  if (!renewal) return null;

  const rowVersionError = validateRowVersion(renewal, rowVersion);
  if (rowVersionError) return rowVersionError;

  if (!transitionMap[renewal.currentStatus].includes(dto.toState)) {
    return {
      code: 'invalid_transition',
      detail: 'That transition is not allowed from the current renewal state.',
    };
  }

  if (dto.toState === 'Lost' && !dto.reasonCode) {
    return {
      code: 'missing_transition_prerequisite',
      detail: 'Loss reason is required when marking a renewal as lost.',
      errors: {
        reasonCode: ['Loss reason is required when marking a renewal as lost.'],
      },
    };
  }

  if (dto.toState === 'Lost' && dto.reasonCode === 'Other' && !dto.reasonDetail?.trim()) {
    return {
      code: 'missing_transition_prerequisite',
      detail: 'Loss reason detail is required when the reason is Other.',
      errors: {
        reasonDetail: ['Loss reason detail is required when the reason is Other.'],
      },
    };
  }

  if (dto.toState === 'Completed' && !dto.boundPolicyId?.trim() && !dto.renewalSubmissionId?.trim()) {
    return {
      code: 'missing_transition_prerequisite',
      detail: 'Bound policy ID or renewal submission ID is required to complete a renewal.',
      errors: {
        boundPolicyId: ['Bound policy ID or renewal submission ID is required to complete a renewal.'],
      },
    };
  }

  const previousState = renewal.currentStatus;
  renewal.currentStatus = dto.toState;
  renewal.lostReasonCode = dto.toState === 'Lost' ? dto.reasonCode ?? null : null;
  renewal.lostReasonDetail = dto.toState === 'Lost' ? dto.reasonDetail?.trim() || null : null;
  renewal.boundPolicyId = dto.toState === 'Completed' ? dto.boundPolicyId?.trim() || null : null;
  renewal.renewalSubmissionId = dto.toState === 'Completed' ? dto.renewalSubmissionId?.trim() || null : null;
  renewal.rowVersion += 1;
  renewal.updatedAt = nextTimestamp();
  renewal.updatedByUserId = renewal.assignedToUserId;

  const eventDescription = dto.toState === 'Completed'
    ? `Renewal completed${renewal.boundPolicyId ? ` with bound policy ${renewal.boundPolicyId}` : ''}.`
    : dto.toState === 'Lost'
      ? `Renewal marked as lost${renewal.lostReasonCode ? ` (${renewal.lostReasonCode})` : ''}.`
      : `Renewal advanced to ${dto.toState}.`;

  renewal.timeline.unshift(
    buildTimelineEvent(
      renewal.id,
      renewal.timeline.length + 1,
      'RenewalTransitioned',
      eventDescription,
      'Sarah Chen',
      renewal.updatedAt,
    ),
  );

  return {
    id: `${renewal.id}-transition-${renewal.rowVersion}`,
    workflowType: 'Renewal',
    entityId: renewal.id,
    fromState: previousState,
    toState: dto.toState,
    reason: dto.reason?.trim() || null,
    occurredAt: renewal.updatedAt,
  };
}

function createInitialRenewalState(): MockRenewalRecord[] {
  return [
    createRenewalRecord({
      id: 'renewal-1',
      policyId: 'policy-1',
      currentStatus: 'Identified',
      lineOfBusiness: 'Property',
      targetOutreachDate: '2026-04-01',
      assignedToUserId: 'user-distribution-1',
      rowVersion: 4,
      createdAt: '2026-01-15T14:00:00Z',
      updatedAt: '2026-04-01T09:30:00Z',
      createdByUserId: 'user-dist-manager',
      updatedByUserId: 'user-dist-manager',
      timeline: [
        buildTimelineEvent(
          'renewal-1',
          2,
          'RenewalAssigned',
          'Renewal assigned to Lisa Wong.',
          'Sarah Chen',
          '2026-02-15T09:00:00Z',
        ),
        buildTimelineEvent(
          'renewal-1',
          1,
          'RenewalCreated',
          'Renewal identified from the active policy.',
          'Sarah Chen',
          '2026-01-15T14:00:00Z',
        ),
      ],
    }),
    createRenewalRecord({
      id: 'renewal-2',
      policyId: 'policy-2',
      currentStatus: 'Quoted',
      lineOfBusiness: 'Cyber',
      targetOutreachDate: '2026-05-01',
      assignedToUserId: 'user-underwriter-1',
      rowVersion: 7,
      createdAt: '2026-01-20T10:15:00Z',
      updatedAt: '2026-03-30T16:45:00Z',
      createdByUserId: 'user-dist-manager',
      updatedByUserId: 'user-underwriter-1',
      timeline: [
        buildTimelineEvent(
          'renewal-2',
          4,
          'RenewalTransitioned',
          'Renewal advanced to Quoted.',
          'Nadia Brooks',
          '2026-03-30T16:45:00Z',
        ),
        buildTimelineEvent(
          'renewal-2',
          3,
          'RenewalTransitioned',
          'Renewal advanced to InReview.',
          'Sarah Chen',
          '2026-03-17T11:20:00Z',
        ),
        buildTimelineEvent(
          'renewal-2',
          2,
          'RenewalTransitioned',
          'Renewal advanced to Outreach.',
          'Sarah Chen',
          '2026-02-24T13:10:00Z',
        ),
        buildTimelineEvent(
          'renewal-2',
          1,
          'RenewalCreated',
          'Renewal created from the policy book.',
          'Sarah Chen',
          '2026-01-20T10:15:00Z',
        ),
      ],
    }),
    createRenewalRecord({
      id: 'renewal-3',
      policyId: 'policy-3',
      currentStatus: 'Identified',
      lineOfBusiness: 'GeneralLiability',
      targetOutreachDate: '2026-04-20',
      assignedToUserId: 'user-dist-manager',
      rowVersion: 2,
      createdAt: '2026-02-05T08:45:00Z',
      updatedAt: '2026-02-10T09:15:00Z',
      createdByUserId: 'user-dist-manager',
      updatedByUserId: 'user-dist-manager',
      timeline: [
        buildTimelineEvent(
          'renewal-3',
          1,
          'RenewalCreated',
          'Renewal created for the upcoming contractors renewal cycle.',
          'Sarah Chen',
          '2026-02-05T08:45:00Z',
        ),
      ],
    }),
  ];
}

function createRenewalRecord({
  id,
  policyId,
  currentStatus,
  lineOfBusiness,
  targetOutreachDate,
  assignedToUserId,
  rowVersion,
  createdAt,
  updatedAt,
  createdByUserId,
  updatedByUserId,
  timeline,
}: {
  id: string;
  policyId: string;
  currentStatus: RenewalStatus;
  lineOfBusiness: string | null;
  targetOutreachDate: string;
  assignedToUserId: string;
  rowVersion: number;
  createdAt: string;
  updatedAt: string;
  createdByUserId: string;
  updatedByUserId: string;
  timeline: TimelineEventDto[];
}): MockRenewalRecord {
  const policy = policyById.get(policyId);
  if (!policy) {
    throw new Error(`Unknown policy fixture: ${policyId}`);
  }

  return {
    id,
    policyId,
    accountId: policy.accountId,
    brokerId: policy.brokerId,
    currentStatus,
    lineOfBusiness,
    policyExpirationDate: policy.expirationDate,
    targetOutreachDate,
    assignedToUserId,
    lostReasonCode: null,
    lostReasonDetail: null,
    boundPolicyId: null,
    renewalSubmissionId: null,
    rowVersion,
    createdAt,
    createdByUserId,
    updatedAt,
    updatedByUserId,
    timeline,
  };
}

function toListItem(renewal: MockRenewalRecord): RenewalListItemDto {
  const policy = policyById.get(renewal.policyId);
  const account = accountById.get(renewal.accountId);
  const broker = brokerById.get(renewal.brokerId);
  const assignee = userById.get(renewal.assignedToUserId);

  return {
    id: renewal.id,
    accountId: renewal.accountId,
    accountDisplayName: account?.name ?? 'Unknown account',
    accountStatus: 'Active',
    accountSurvivorId: null,
    accountName: account?.name ?? 'Unknown account',
    accountIndustry: account?.industry ?? 'Unknown industry',
    accountPrimaryState: accountPrimaryStateById.get(renewal.accountId) ?? 'Unknown state',
    brokerName: broker?.legalName ?? 'Unknown broker',
    brokerLicenseNumber: broker?.licenseNumber ?? 'Unknown license',
    brokerState: broker?.state ?? 'Unknown state',
    policyNumber: policy?.policyNumber ?? 'Unknown policy',
    carrier: policy?.carrier ?? null,
    lineOfBusiness: renewal.lineOfBusiness,
    currentStatus: renewal.currentStatus,
    policyExpirationDate: renewal.policyExpirationDate,
    targetOutreachDate: renewal.targetOutreachDate,
    assignedToUserId: renewal.assignedToUserId,
    assignedUserDisplayName: assignee?.displayName ?? null,
    urgency: getRenewalUrgency(renewal),
    rowVersion: String(renewal.rowVersion),
  };
}

function toDetail(renewal: MockRenewalRecord): RenewalDto {
  const policy = policyById.get(renewal.policyId);
  const account = accountById.get(renewal.accountId);
  const broker = brokerById.get(renewal.brokerId);
  const assignee = userById.get(renewal.assignedToUserId);

  return {
    id: renewal.id,
    accountId: renewal.accountId,
    brokerId: renewal.brokerId,
    policyId: renewal.policyId,
    currentStatus: renewal.currentStatus,
    lineOfBusiness: renewal.lineOfBusiness,
    policyExpirationDate: renewal.policyExpirationDate,
    targetOutreachDate: renewal.targetOutreachDate,
    assignedToUserId: renewal.assignedToUserId,
    lostReasonCode: renewal.lostReasonCode,
    lostReasonDetail: renewal.lostReasonDetail,
    boundPolicyId: renewal.boundPolicyId,
    renewalSubmissionId: renewal.renewalSubmissionId,
    urgency: getRenewalUrgency(renewal),
    availableTransitions: transitionMap[renewal.currentStatus],
    assignedUserDisplayName: assignee?.displayName ?? null,
    accountDisplayName: account?.name ?? null,
    accountStatus: 'Active',
    accountSurvivorId: null,
    accountName: account?.name ?? null,
    accountIndustry: account?.industry ?? null,
    accountPrimaryState: accountPrimaryStateById.get(renewal.accountId) ?? null,
    brokerName: broker?.legalName ?? null,
    brokerLicenseNumber: broker?.licenseNumber ?? null,
    brokerState: broker?.state ?? null,
    policyNumber: policy?.policyNumber ?? null,
    policyCarrier: policy?.carrier ?? null,
    policyEffectiveDate: policy?.effectiveDate ?? null,
    policyPremium: policy?.premium ?? null,
    rowVersion: String(renewal.rowVersion),
    createdAt: renewal.createdAt,
    createdByUserId: renewal.createdByUserId,
    updatedAt: renewal.updatedAt,
    updatedByUserId: renewal.updatedByUserId,
  };
}

function matchesDueWindow(
  renewal: MockRenewalRecord,
  dueWindow: string | null,
): boolean {
  if (!dueWindow) return true;
  if (dueWindow === 'overdue') return getRenewalUrgency(renewal) === 'overdue';
  if (!['45', '60', '90'].includes(dueWindow)) return true;
  return daysUntil(renewal.policyExpirationDate) <= Number(dueWindow);
}

function getRenewalUrgency(renewal: MockRenewalRecord): RenewalUrgency {
  if (terminalStatuses.has(renewal.currentStatus) || renewal.currentStatus === 'Quoted' || renewal.currentStatus === 'InReview') {
    return null;
  }

  const dayDelta = daysUntil(renewal.targetOutreachDate);
  if (dayDelta < 0) return 'overdue';
  if (dayDelta <= 14) return 'approaching';
  return null;
}

function daysUntil(isoDate: string): number {
  const target = Date.parse(`${isoDate}T00:00:00Z`);
  return Math.floor((target - TODAY) / 86_400_000);
}

function sortValue(renewal: MockRenewalRecord, sort: string): string {
  switch (sort) {
    case 'accountName':
      return (accountById.get(renewal.accountId)?.name ?? '').toLowerCase();
    case 'currentStatus':
      return String(statusSortOrder[renewal.currentStatus]).padStart(2, '0');
    case 'assignedToUserId':
      return (userById.get(renewal.assignedToUserId)?.displayName ?? renewal.assignedToUserId).toLowerCase();
    case 'policyExpirationDate':
    default:
      return renewal.policyExpirationDate;
  }
}

function resolveAssignableUser(
  userId: string,
  status: RenewalStatus,
): UserSummaryDto | MockMutationError {
  const user = userById.get(userId);
  if (!user) {
    return {
      code: 'invalid_assignee',
      detail: 'The selected assignee was not found.',
    };
  }

  if (!user.isActive) {
    return {
      code: 'inactive_assignee',
      detail: 'The selected assignee is inactive.',
    };
  }

  const allowedRoles = status === 'Identified' || status === 'Outreach'
    ? ['DistributionUser', 'DistributionManager', 'Admin']
    : ['Underwriter', 'Admin'];

  if (!allowedRoles.some((role) => user.roles.includes(role))) {
    return {
      code: 'invalid_assignee',
      detail: 'The selected assignee is not eligible for the current renewal stage.',
    };
  }

  return user;
}

function validateRowVersion(
  renewal: MockRenewalRecord,
  rowVersion: string | null,
): MockMutationError | null {
  if (!rowVersion || renewal.rowVersion !== Number(rowVersion)) {
    return {
      code: 'precondition_failed',
      detail: 'This renewal changed. Refresh and retry.',
    };
  }

  return null;
}

function buildTimelineEvent(
  renewalId: string,
  sequence: number,
  eventType: string,
  eventDescription: string,
  actorDisplayName: string,
  occurredAt: string,
): TimelineEventDto {
  return {
    id: `${renewalId}-timeline-${sequence}`,
    entityType: 'Renewal',
    entityId: renewalId,
    eventType,
    eventDescription,
    entityName: null,
    actorDisplayName,
    occurredAt,
  };
}

function nextTimestamp(): string {
  return new Date(TODAY + (renewalState.length + 1) * 60_000).toISOString();
}

function offsetIsoDate(isoDate: string, offsetDays: number): string {
  return new Date(Date.parse(`${isoDate}T00:00:00Z`) + offsetDays * 86_400_000)
    .toISOString()
    .slice(0, 10);
}
