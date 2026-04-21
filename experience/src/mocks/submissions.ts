import type { TimelineEventDto } from '@/contracts/timeline';
import type { BrokerDto } from '@/features/brokers';
import type {
  AccountReferenceDto,
  PaginatedResponse,
  ProgramReferenceDto,
  SubmissionAssignmentRequestDto,
  SubmissionCreateDto,
  SubmissionDocumentCheckDto,
  SubmissionDto,
  SubmissionFieldCheckDto,
  SubmissionListItemDto,
  SubmissionStatus,
  SubmissionUpdateDto,
  WorkflowTransitionRecordDto,
  WorkflowTransitionRequestDto,
} from '@/features/submissions';
import type { UserSearchResponseDto, UserSummaryDto } from '@/features/tasks';
import { brokerListFixture } from './brokers';

interface MockBroker extends BrokerDto {
  regions: string[];
}

interface MockSubmissionRecord {
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
  accountName: string;
  accountRegion: string | null;
  accountIndustry: string | null;
  brokerName: string;
  brokerLicenseNumber: string | null;
  programName: string | null;
  assignedToDisplayName: string | null;
  isStale: boolean;
  rowVersion: number;
  createdAt: string;
  createdByUserId: string;
  updatedAt: string;
  updatedByUserId: string;
  timeline: TimelineEventDto[];
}

const brokerById = new Map(
  [
    {
      ...(brokerListFixture[0] as BrokerDto),
      regions: ['West'],
    },
    {
      ...({
        id: 'broker-4',
        legalName: 'Northstar Wholesale',
        licenseNumber: 'IL-443311',
        state: 'IL',
        status: 'Active',
        email: 'hello@northstar.test',
        phone: '+13125550101',
        createdAt: '2026-03-01T00:00:00Z',
        updatedAt: '2026-03-19T00:00:00Z',
        rowVersion: 2,
        isDeactivated: false,
      } satisfies BrokerDto),
      regions: ['Central'],
    },
  ].map((broker) => [broker.id, broker] satisfies [string, MockBroker]),
);

export const accountReferenceFixture: AccountReferenceDto[] = [
  {
    id: 'account-1',
    name: 'Blue Horizon Manufacturing',
    status: 'Active',
    industry: 'Manufacturing',
  },
  {
    id: 'account-2',
    name: 'Compass Markets Retail Group',
    status: 'Active',
    industry: 'Retail',
  },
  {
    id: 'account-3',
    name: 'Northstar Contractors Collective',
    status: 'Active',
    industry: 'Construction',
  },
];

const accountRegionById = new Map<string, string>([
  ['account-1', 'West'],
  ['account-2', 'West'],
  ['account-3', 'Central'],
]);

export const programReferenceFixture: ProgramReferenceDto[] = [
  { id: 'program-1', name: 'Construction Preferred', mgaId: 'mga-1' },
  { id: 'program-2', name: 'Cyber Growth', mgaId: 'mga-2' },
];

export const submissionUsersFixture: UserSummaryDto[] = [
  {
    userId: 'user-dist-manager',
    displayName: 'Sarah Chen',
    email: 'sarah.chen@nebula.local',
    roles: ['DistributionManager'],
    isActive: true,
  },
  {
    userId: 'user-underwriter-1',
    displayName: 'Nadia Brooks',
    email: 'nadia.brooks@nebula.local',
    roles: ['Underwriter'],
    isActive: true,
  },
  {
    userId: 'user-underwriter-2',
    displayName: 'Alex Kim',
    email: 'alex.kim@nebula.local',
    roles: ['Underwriter'],
    isActive: true,
  },
  {
    userId: 'user-distribution-1',
    displayName: 'Lisa Wong',
    email: 'lisa.wong@nebula.local',
    roles: ['DistributionUser'],
    isActive: true,
  },
];

const transitionMap: Record<SubmissionStatus, SubmissionStatus[]> = {
  Received: ['Triaging'],
  Triaging: ['WaitingOnBroker', 'ReadyForUWReview'],
  WaitingOnBroker: ['ReadyForUWReview'],
  ReadyForUWReview: ['InReview'],
  InReview: ['Quoted', 'Declined'],
  Quoted: ['BindRequested', 'Declined', 'Withdrawn'],
  BindRequested: ['Bound', 'Withdrawn'],
  Bound: [],
  Declined: [],
  Withdrawn: [],
};

let submissionState = createInitialSubmissionState();

export function resetSubmissionMockState() {
  submissionState = createInitialSubmissionState();
}

export function listSubmissions(
  searchParams: URLSearchParams,
): PaginatedResponse<SubmissionListItemDto> {
  const status = searchParams.get('status');
  const brokerId = searchParams.get('brokerId');
  const accountId = searchParams.get('accountId');
  const lineOfBusiness = searchParams.get('lineOfBusiness');
  const assignedToUserId = searchParams.get('assignedToUserId');
  const stale = searchParams.get('stale');
  const sort = searchParams.get('sort') ?? 'createdAt';
  const sortDir = searchParams.get('sortDir') ?? 'desc';
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '25');

  const filtered = submissionState
    .filter((submission) => !status || submission.currentStatus === status)
    .filter((submission) => !brokerId || submission.brokerId === brokerId)
    .filter((submission) => !accountId || submission.accountId === accountId)
    .filter((submission) => !lineOfBusiness || submission.lineOfBusiness === lineOfBusiness)
    .filter((submission) => !assignedToUserId || submission.assignedToUserId === assignedToUserId)
    .filter((submission) => (
      stale === 'true'
        ? submission.isStale
        : stale === 'false'
          ? !submission.isStale
          : true
    ));

  filtered.sort((left, right) => {
    const direction = sortDir === 'asc' ? 1 : -1;
    const leftValue = sortValue(left, sort);
    const rightValue = sortValue(right, sort);
    return leftValue.localeCompare(rightValue) * direction;
  });

  const offset = (Math.max(page, 1) - 1) * Math.max(pageSize, 1);
  const paged = filtered.slice(offset, offset + pageSize).map(toListItem);

  return {
    data: paged,
    page,
    pageSize,
    totalCount: filtered.length,
    totalPages: Math.max(1, Math.ceil(filtered.length / Math.max(pageSize, 1))),
  };
}

export function getSubmission(submissionId: string): SubmissionDto | null {
  const submission = submissionState.find((record) => record.id === submissionId);
  return submission ? toDetail(submission) : null;
}

export function getSubmissionTimeline(
  submissionId: string,
  page: number,
  pageSize: number,
): PaginatedResponse<TimelineEventDto> | null {
  const submission = submissionState.find((record) => record.id === submissionId);
  if (!submission) return null;

  const offset = (Math.max(page, 1) - 1) * Math.max(pageSize, 1);
  const paged = submission.timeline.slice(offset, offset + pageSize);

  return {
    data: paged,
    page,
    pageSize,
    totalCount: submission.timeline.length,
    totalPages: Math.max(1, Math.ceil(submission.timeline.length / Math.max(pageSize, 1))),
  };
}

export function createSubmission(dto: SubmissionCreateDto): SubmissionDto | { code: string; detail: string } {
  const account = accountReferenceFixture.find((record) => record.id === dto.accountId);
  if (!account) {
    return { code: 'invalid_account', detail: 'The selected account is invalid.' };
  }

  const broker = brokerById.get(dto.brokerId);
  if (!broker || broker.status !== 'Active') {
    return { code: 'invalid_broker', detail: 'The selected broker is invalid.' };
  }

  if (!broker.regions.includes(accountRegionById.get(dto.accountId) ?? '')) {
    return { code: 'region_mismatch', detail: 'The selected account and broker are not region-aligned.' };
  }

  if (dto.programId && !programReferenceFixture.some((program) => program.id === dto.programId)) {
    return { code: 'invalid_program', detail: 'The selected program is invalid.' };
  }

  const createdAt = new Date().toISOString();
  const id = `submission-${submissionState.length + 1}`;
  const expirationDate = dto.expirationDate ?? addMonths(dto.effectiveDate, 12);
  const assignedUser = submissionUsersFixture[0];
  const program = dto.programId
    ? programReferenceFixture.find((record) => record.id === dto.programId) ?? null
    : null;

  const record: MockSubmissionRecord = {
    id,
    accountId: dto.accountId,
    brokerId: dto.brokerId,
    programId: dto.programId ?? null,
    lineOfBusiness: dto.lineOfBusiness ?? null,
    currentStatus: 'Received',
    effectiveDate: isoDate(dto.effectiveDate),
    expirationDate: expirationDate ? isoDate(expirationDate) : null,
    premiumEstimate: dto.premiumEstimate ?? null,
    description: dto.description ?? null,
    assignedToUserId: assignedUser.userId,
    accountName: account.name,
    accountRegion: accountRegionById.get(account.id) ?? null,
    accountIndustry: account.industry,
    brokerName: broker.legalName,
    brokerLicenseNumber: broker.licenseNumber,
    programName: program?.name ?? null,
    assignedToDisplayName: assignedUser.displayName,
    isStale: false,
    rowVersion: 1,
    createdAt,
    createdByUserId: assignedUser.userId,
    updatedAt: createdAt,
    updatedByUserId: assignedUser.userId,
    timeline: [
      {
        id: `${id}-timeline-1`,
        entityType: 'Submission',
        entityId: id,
        eventType: 'SubmissionCreated',
        eventDescription: `Submission created for ${account.name} via ${broker.legalName}`,
        entityName: null,
        actorDisplayName: assignedUser.displayName,
        occurredAt: createdAt,
      },
    ],
  };

  submissionState = [record, ...submissionState];
  return toDetail(record);
}

export function updateSubmission(
  submissionId: string,
  rowVersion: string | null,
  dto: SubmissionUpdateDto,
): SubmissionDto | { code: string; detail: string } | null {
  const hasField = (field: keyof SubmissionUpdateDto) => Object.prototype.hasOwnProperty.call(dto, field);
  const submission = submissionState.find((record) => record.id === submissionId);
  if (!submission) return null;

  if (!rowVersion || submission.rowVersion !== Number(rowVersion)) {
    return { code: 'precondition_failed', detail: 'The submission was modified by another user. Refresh and retry.' };
  }

  if (hasField('programId')) {
    submission.programId = dto.programId ?? null;
    submission.programName = submission.programId
      ? programReferenceFixture.find((record) => record.id === submission.programId)?.name ?? null
      : null;
  }

  if (hasField('lineOfBusiness')) {
    submission.lineOfBusiness = dto.lineOfBusiness ?? null;
  }

  if (hasField('effectiveDate') && dto.effectiveDate) {
    submission.effectiveDate = isoDate(dto.effectiveDate);
  }

  if (hasField('expirationDate')) {
    submission.expirationDate = dto.expirationDate ? isoDate(dto.expirationDate) : null;
  }

  if (hasField('premiumEstimate')) {
    submission.premiumEstimate = dto.premiumEstimate ?? null;
  }

  if (hasField('description')) {
    submission.description = dto.description ?? null;
  }

  submission.updatedAt = new Date().toISOString();
  submission.updatedByUserId = 'user-dist-manager';
  submission.rowVersion += 1;
  submission.timeline.unshift({
    id: `${submission.id}-timeline-${submission.timeline.length + 1}`,
    entityType: 'Submission',
    entityId: submission.id,
    eventType: 'SubmissionUpdated',
    eventDescription: 'Submission updated',
    entityName: null,
    actorDisplayName: 'Sarah Chen',
    occurredAt: submission.updatedAt,
  });

  return toDetail(submission);
}

export function assignSubmission(
  submissionId: string,
  rowVersion: string | null,
  dto: SubmissionAssignmentRequestDto,
): SubmissionDto | { code: string; detail: string } | null {
  const submission = submissionState.find((record) => record.id === submissionId);
  if (!submission) return null;

  if (!rowVersion || submission.rowVersion !== Number(rowVersion)) {
    return { code: 'precondition_failed', detail: 'The submission was modified by another user. Refresh and retry.' };
  }

  const user = submissionUsersFixture.find((record) => record.userId === dto.assignedToUserId && record.isActive);
  if (!user) {
    return { code: 'invalid_assignee', detail: 'The specified assignee is invalid.' };
  }

  if (submission.currentStatus === 'ReadyForUWReview' && !user.roles.includes('Underwriter')) {
    return {
      code: 'invalid_assignee',
      detail: 'Submission in ReadyForUWReview must be assigned to a user with Underwriter role.',
    };
  }

  if (submission.assignedToUserId === user.userId) {
    return toDetail(submission);
  }

  const previousAssignee = submission.assignedToDisplayName ?? 'Unknown assignee';
  submission.assignedToUserId = user.userId;
  submission.assignedToDisplayName = user.displayName;
  submission.updatedAt = new Date().toISOString();
  submission.updatedByUserId = 'user-dist-manager';
  submission.rowVersion += 1;
  submission.timeline.unshift({
    id: `${submission.id}-timeline-${submission.timeline.length + 1}`,
    entityType: 'Submission',
    entityId: submission.id,
    eventType: 'SubmissionAssigned',
    eventDescription: `Submission reassigned from ${previousAssignee} to ${user.displayName}`,
    entityName: null,
    actorDisplayName: 'Sarah Chen',
    occurredAt: submission.updatedAt,
  });

  return toDetail(submission);
}

export function transitionSubmission(
  submissionId: string,
  rowVersion: string | null,
  dto: WorkflowTransitionRequestDto,
): WorkflowTransitionRecordDto | { code: string; detail: string } | null {
  const submission = submissionState.find((record) => record.id === submissionId);
  if (!submission) return null;

  if (!rowVersion || submission.rowVersion !== Number(rowVersion)) {
    return { code: 'precondition_failed', detail: 'The submission was modified by another user. Refresh and retry.' };
  }

  if (!transitionMap[submission.currentStatus].includes(dto.toState)) {
    return { code: 'invalid_transition', detail: 'Transition is not valid from the current submission state.' };
  }

  if (dto.toState === 'ReadyForUWReview') {
    const completeness = buildCompleteness(submission);
    if (!completeness.isComplete) {
      return {
        code: 'missing_transition_prerequisite',
        detail: completeness.missingItems.join(', '),
      };
    }
  }

  const previous = submission.currentStatus;
  submission.currentStatus = dto.toState;
  submission.isStale = false;
  submission.updatedAt = new Date().toISOString();
  submission.updatedByUserId = 'user-dist-manager';
  submission.rowVersion += 1;
  const eventDescription = dto.reason?.trim()
    ? `Submission moved from ${previous} to ${dto.toState}. Note: ${dto.reason.trim()}`
    : `Submission moved from ${previous} to ${dto.toState}`;
  submission.timeline.unshift({
    id: `${submission.id}-timeline-${submission.timeline.length + 1}`,
    entityType: 'Submission',
    entityId: submission.id,
    eventType: 'SubmissionTransitioned',
    eventDescription,
    entityName: null,
    actorDisplayName: 'Sarah Chen',
    occurredAt: submission.updatedAt,
  });

  return {
    id: `${submission.id}-transition-${submission.timeline.length}`,
    workflowType: 'Submission',
    entityId: submission.id,
    fromState: previous,
    toState: dto.toState,
    reason: dto.reason ?? null,
    occurredAt: submission.updatedAt,
  };
}

export function searchUsers(query: string): UserSearchResponseDto {
  const normalized = query.trim().toLowerCase();
  return {
    users: submissionUsersFixture.filter((user) =>
      user.displayName.toLowerCase().includes(normalized)
      || user.email.toLowerCase().includes(normalized)),
  };
}

function createInitialSubmissionState(): MockSubmissionRecord[] {
  return [
    buildSubmission({
      id: 'submission-1',
      accountId: 'account-1',
      brokerId: 'broker-1',
      programId: 'program-2',
      lineOfBusiness: 'Cyber',
      currentStatus: 'Triaging',
      premiumEstimate: 175000,
      description: 'Cyber renewal risk expanding into three states.',
      assignedToUserId: 'user-underwriter-1',
      isStale: true,
      createdAt: '2026-03-18T14:00:00Z',
    }),
    buildSubmission({
      id: 'submission-2',
      accountId: 'account-2',
      brokerId: 'broker-1',
      programId: null,
      lineOfBusiness: null,
      currentStatus: 'Received',
      premiumEstimate: 92000,
      description: 'Waiting on intake classification from the broker.',
      assignedToUserId: 'user-distribution-1',
      isStale: true,
      createdAt: '2026-03-22T13:00:00Z',
    }),
    buildSubmission({
      id: 'submission-3',
      accountId: 'account-3',
      brokerId: 'broker-4',
      programId: 'program-1',
      lineOfBusiness: 'Property',
      currentStatus: 'ReadyForUWReview',
      premiumEstimate: 240000,
      description: 'Construction property schedule ready for underwriting handoff.',
      assignedToUserId: 'user-underwriter-2',
      isStale: false,
      createdAt: '2026-03-25T09:30:00Z',
    }),
  ];
}

function buildSubmission({
  id,
  accountId,
  brokerId,
  programId,
  lineOfBusiness,
  currentStatus,
  premiumEstimate,
  description,
  assignedToUserId,
  isStale,
  createdAt,
}: {
  id: string;
  accountId: string;
  brokerId: string;
  programId: string | null;
  lineOfBusiness: string | null;
  currentStatus: SubmissionStatus;
  premiumEstimate: number | null;
  description: string;
  assignedToUserId: string;
  isStale: boolean;
  createdAt: string;
}): MockSubmissionRecord {
  const account = accountReferenceFixture.find((record) => record.id === accountId)!;
  const broker = brokerById.get(brokerId)!;
  const program = programId ? programReferenceFixture.find((record) => record.id === programId) : null;
  const assignedTo = submissionUsersFixture.find((record) => record.userId === assignedToUserId)!;

  return {
    id,
    accountId,
    brokerId,
    programId,
    lineOfBusiness,
    currentStatus,
    effectiveDate: createdAt.slice(0, 10),
    expirationDate: addMonths(createdAt.slice(0, 10), 12),
    premiumEstimate,
    description,
    assignedToUserId,
    accountName: account.name,
    accountRegion: accountRegionById.get(accountId) ?? null,
    accountIndustry: account.industry,
    brokerName: broker.legalName,
    brokerLicenseNumber: broker.licenseNumber,
    programName: program?.name ?? null,
    assignedToDisplayName: assignedTo.displayName,
    isStale,
    rowVersion: 1,
    createdAt,
    createdByUserId: 'user-dist-manager',
    updatedAt: createdAt,
    updatedByUserId: 'user-dist-manager',
    timeline: [
      {
        id: `${id}-timeline-3`,
        entityType: 'Submission',
        entityId: id,
        eventType: 'SubmissionUpdated',
        eventDescription: 'Submission details were refreshed.',
        entityName: null,
        actorDisplayName: 'Sarah Chen',
        occurredAt: new Date(new Date(createdAt).getTime() + 2 * 60 * 60 * 1000).toISOString(),
      },
      {
        id: `${id}-timeline-2`,
        entityType: 'Submission',
        entityId: id,
        eventType: 'SubmissionTransitioned',
        eventDescription: `Submission moved into ${currentStatus}`,
        entityName: null,
        actorDisplayName: 'Sarah Chen',
        occurredAt: new Date(new Date(createdAt).getTime() + 60 * 60 * 1000).toISOString(),
      },
      {
        id: `${id}-timeline-1`,
        entityType: 'Submission',
        entityId: id,
        eventType: 'SubmissionCreated',
        eventDescription: `Submission created for ${account.name} via ${broker.legalName}`,
        entityName: null,
        actorDisplayName: 'Sarah Chen',
        occurredAt: createdAt,
      },
    ],
  };
}

function toListItem(record: MockSubmissionRecord): SubmissionListItemDto {
  return {
    id: record.id,
    accountId: record.accountId,
    accountDisplayName: record.accountName,
    accountStatus: 'Active',
    accountSurvivorId: null,
    accountName: record.accountName,
    brokerName: record.brokerName,
    lineOfBusiness: record.lineOfBusiness,
    currentStatus: record.currentStatus,
    effectiveDate: record.effectiveDate,
    assignedToUserId: record.assignedToUserId,
    assignedToDisplayName: record.assignedToDisplayName,
    createdAt: record.createdAt,
    isStale: record.isStale,
  };
}

function toDetail(record: MockSubmissionRecord): SubmissionDto {
  const completeness = buildCompleteness(record);

  return {
    id: record.id,
    accountId: record.accountId,
    brokerId: record.brokerId,
    programId: record.programId,
    lineOfBusiness: record.lineOfBusiness,
    currentStatus: record.currentStatus,
    effectiveDate: record.effectiveDate,
    expirationDate: record.expirationDate,
    premiumEstimate: record.premiumEstimate,
    description: record.description,
    assignedToUserId: record.assignedToUserId,
    accountDisplayName: record.accountName,
    accountStatus: 'Active',
    accountSurvivorId: null,
    accountName: record.accountName,
    accountRegion: record.accountRegion,
    accountIndustry: record.accountIndustry,
    brokerName: record.brokerName,
    brokerLicenseNumber: record.brokerLicenseNumber,
    programName: record.programName,
    assignedToDisplayName: record.assignedToDisplayName,
    isStale: record.isStale,
    completeness,
    availableTransitions: transitionMap[record.currentStatus],
    rowVersion: String(record.rowVersion),
    createdAt: record.createdAt,
    createdByUserId: record.createdByUserId,
    updatedAt: record.updatedAt,
    updatedByUserId: record.updatedByUserId,
  };
}

function buildCompleteness(record: MockSubmissionRecord) {
  const assignedUser = submissionUsersFixture.find((user) => user.userId === record.assignedToUserId);
  const fieldChecks: SubmissionFieldCheckDto[] = [
    { field: 'AccountId', required: true, status: record.accountId ? 'pass' : 'missing' },
    { field: 'BrokerId', required: true, status: record.brokerId ? 'pass' : 'missing' },
    { field: 'EffectiveDate', required: true, status: record.effectiveDate ? 'pass' : 'missing' },
    { field: 'LineOfBusiness', required: true, status: record.lineOfBusiness ? 'pass' : 'missing' },
    {
      field: 'AssignedToUserId',
      required: true,
      status: assignedUser?.roles.includes('Underwriter') ? 'pass' : 'missing',
    },
  ];
  const documentChecks: SubmissionDocumentCheckDto[] = [
    {
      category: 'Submission packet',
      required: true,
      status: 'unavailable',
    },
  ];

  const missingItems = [
    ...fieldChecks
      .filter((check) => check.status === 'missing')
      .map((check) => mapMissingField(check.field)),
    ...documentChecks
      .filter((check) => check.status === 'missing')
      .map((check) => check.category),
  ];

  const isComplete = fieldChecks.every((check) => check.status === 'pass')
    && documentChecks.every((check) => check.status === 'pass' || check.status === 'unavailable');

  return {
    isComplete,
    fieldChecks,
    documentChecks,
    missingItems,
  };
}

function mapMissingField(field: string) {
  switch (field) {
    case 'AccountId':
      return 'Account';
    case 'BrokerId':
      return 'Broker';
    case 'EffectiveDate':
      return 'Effective date';
    case 'LineOfBusiness':
      return 'Line of business';
    case 'AssignedToUserId':
      return 'Assigned underwriter';
    default:
      return field;
  }
}

function sortValue(record: MockSubmissionRecord, sort: string) {
  switch (sort) {
    case 'effectiveDate':
      return record.effectiveDate;
    case 'accountName':
      return record.accountName;
    case 'brokerName':
      return record.brokerName;
    case 'currentStatus':
      return record.currentStatus;
    default:
      return record.createdAt;
  }
}

function isoDate(value: string) {
  if (value.includes('T')) return value;
  return `${value}T00:00:00.000Z`;
}

function addMonths(dateOnly: string, months: number) {
  const date = new Date(`${dateOnly}T00:00:00.000Z`);
  date.setUTCMonth(date.getUTCMonth() + months);
  return date.toISOString();
}
