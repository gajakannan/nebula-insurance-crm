import type { TimelineEventDto } from '@/contracts/timeline';
import type {
  PaginatedResponse,
  PolicyAccountSummaryDto,
  PolicyCancelRequestDto,
  PolicyCoverageInputDto,
  PolicyCoverageLineDto,
  PolicyCreateRequestDto,
  PolicyDto,
  PolicyEndorsementDto,
  PolicyEndorsementRequestDto,
  PolicyImportRequestDto,
  PolicyImportResultDto,
  PolicyListItemDto,
  PolicyReinstateRequestDto,
  PolicyStatus,
  PolicySummaryDto,
  PolicyVersionDto,
} from '@/features/policies';
import { accountReferenceFixture } from './submissions';
import { brokerListFixture } from './brokers';

const SYSTEM_USER_ID = '00000000-0000-0000-0000-000000000000';
const now = '2026-04-22T00:00:00Z';

const carrierNames = new Map([
  ['17000000-0000-0000-0000-000000000001', 'Archway Specialty'],
  ['17000000-0000-0000-0000-000000000002', 'Blue Atlas Insurance'],
  ['17000000-0000-0000-0000-000000000003', 'Summit National'],
  ['17000000-0000-0000-0000-000000000004', 'Frontier Casualty'],
  ['17000000-0000-0000-0000-000000000005', 'Compass Mutual'],
]);

let rowVersionSeed = 10;

const policyState: PolicyDto[] = [
  buildPolicy({
    id: 'policy-1',
    policyNumber: 'NEB-GL-2026-000001',
    accountId: 'account-1',
    brokerOfRecordId: 'broker-1',
    carrierId: '17000000-0000-0000-0000-000000000001',
    lineOfBusiness: 'GeneralLiability',
    status: 'Issued',
    effectiveDate: '2026-01-01',
    expirationDate: '2027-01-01',
    totalPremium: 42000,
    versionCount: 2,
  }),
  buildPolicy({
    id: 'policy-2',
    policyNumber: 'NEB-CYBR-2026-000002',
    accountId: 'account-2',
    brokerOfRecordId: 'broker-1',
    carrierId: '17000000-0000-0000-0000-000000000002',
    lineOfBusiness: 'Cyber',
    status: 'Pending',
    effectiveDate: '2026-05-01',
    expirationDate: '2027-05-01',
    totalPremium: 18500,
  }),
  buildPolicy({
    id: 'policy-3',
    policyNumber: 'NEB-PROP-2025-000031',
    accountId: 'account-3',
    brokerOfRecordId: 'broker-1',
    carrierId: '17000000-0000-0000-0000-000000000003',
    lineOfBusiness: 'Property',
    status: 'Cancelled',
    effectiveDate: '2025-07-01',
    expirationDate: '2026-07-01',
    totalPremium: 61000,
    cancelledAt: '2026-03-15T18:30:00Z',
    cancellationEffectiveDate: '2026-03-20',
    cancellationReasonCode: 'InsuredRequest',
    reinstatementDeadline: '2026-04-19',
  }),
];

const coverageState = new Map<string, PolicyCoverageLineDto[]>(
  policyState.map((policy) => [policy.id, [
    {
      id: `${policy.id}-coverage-1`,
      policyId: policy.id,
      policyVersionId: policy.currentVersionId ?? `${policy.id}-version-1`,
      versionNumber: policy.versionCount,
      coverageCode: policy.lineOfBusiness,
      coverageName: policy.lineOfBusiness,
      limit: policy.totalPremium * 10,
      deductible: policy.totalPremium > 30000 ? 2500 : 1000,
      premium: policy.totalPremium,
      premiumCurrency: policy.premiumCurrency,
      exposureBasis: null,
      exposureQuantity: null,
      isCurrent: true,
      createdAt: policy.createdAt,
    },
  ]] as const),
);

const endorsementState = new Map<string, PolicyEndorsementDto[]>([
  ['policy-1', [
    {
      id: 'endorsement-1',
      policyId: 'policy-1',
      endorsementNumber: 1,
      policyVersionId: 'policy-1-version-2',
      versionNumber: 2,
      endorsementReasonCode: 'CoverageChange',
      endorsementReasonDetail: 'Increased liability limit.',
      effectiveDate: '2026-03-01',
      premiumDelta: 3500,
      premiumCurrency: 'USD',
      createdAt: '2026-03-01T15:00:00Z',
      createdByUserId: SYSTEM_USER_ID,
    },
  ]],
]);

export function listPolicies(searchParams: URLSearchParams): PaginatedResponse<PolicyListItemDto> {
  const query = (searchParams.get('q') ?? '').toLowerCase();
  const status = searchParams.get('status') as PolicyStatus | null;
  const lineOfBusiness = searchParams.get('lineOfBusiness');
  const accountId = searchParams.get('accountId');
  const page = Number(searchParams.get('page') ?? '1');
  const pageSize = Number(searchParams.get('pageSize') ?? '25');

  let rows = policyState;
  if (query) {
    rows = rows.filter((policy) =>
      policy.policyNumber.toLowerCase().includes(query)
      || (policy.accountDisplayNameAtLink ?? '').toLowerCase().includes(query)
      || (policy.carrierName ?? '').toLowerCase().includes(query));
  }
  if (status) rows = rows.filter((policy) => policy.status === status);
  if (lineOfBusiness) rows = rows.filter((policy) => policy.lineOfBusiness === lineOfBusiness);
  if (accountId) rows = rows.filter((policy) => policy.accountId === accountId);

  const totalCount = rows.length;
  const start = (page - 1) * pageSize;
  return {
    data: rows.slice(start, start + pageSize).map(toListItem),
    page,
    pageSize,
    totalCount,
    totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
  };
}

export function listAccountPolicies(accountId: string, searchParams: URLSearchParams) {
  const params = new URLSearchParams(searchParams);
  params.set('accountId', accountId);
  return listPolicies(params);
}

export function getPolicy(policyId: string): PolicyDto | null {
  return policyState.find((policy) => policy.id === policyId) ?? null;
}

export function getPolicySummary(policyId: string): PolicySummaryDto | null {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  return {
    id: policy.id,
    policyNumber: policy.policyNumber,
    accountId: policy.accountId,
    accountDisplayName: policy.accountDisplayNameAtLink,
    accountStatus: policy.accountStatusAtRead,
    accountSurvivorId: policy.accountSurvivorId,
    brokerOfRecordId: policy.brokerOfRecordId,
    brokerName: findBroker(policy.brokerOfRecordId)?.legalName ?? null,
    carrierId: policy.carrierId,
    carrierName: policy.carrierName,
    lineOfBusiness: policy.lineOfBusiness,
    status: policy.status,
    effectiveDate: policy.effectiveDate,
    expirationDate: policy.expirationDate,
    boundAt: policy.boundAt,
    cancelledAt: policy.cancelledAt,
    cancellationEffectiveDate: policy.cancellationEffectiveDate,
    cancellationReasonCode: policy.cancellationReasonCode,
    reinstatementDeadline: policy.reinstatementDeadline,
    expiredAt: policy.expiredAt,
    predecessorPolicyId: policy.predecessorPolicyId,
    predecessorPolicyNumber: null,
    successorPolicyId: policy.successorPolicyId,
    successorPolicyNumber: null,
    totalPremium: policy.totalPremium,
    premiumCurrency: policy.premiumCurrency,
    currentVersionId: policy.currentVersionId,
    currentVersionNumber: policy.currentVersionNumber,
    versionCount: policy.versionCount,
    endorsementCount: endorsementState.get(policyId)?.length ?? 0,
    coverageLineCount: coverageState.get(policyId)?.length ?? 0,
    openRenewalCount: policy.id === 'policy-1' ? 1 : 0,
    producerUserId: policy.producerUserId,
    producerDisplayName: policy.producerDisplayName,
    importSource: policy.importSource,
    externalPolicyReference: policy.externalPolicyReference,
    availableTransitions: policy.availableTransitions,
    rowVersion: policy.rowVersion,
    createdAt: policy.createdAt,
    createdByUserId: policy.createdByUserId,
    updatedAt: policy.updatedAt,
    updatedByUserId: policy.updatedByUserId,
  };
}

export function createPolicy(dto: PolicyCreateRequestDto): PolicyDto {
  const policy = buildPolicy({
    id: `policy-${policyState.length + 1}`,
    policyNumber: `NEB-${dto.lineOfBusiness.slice(0, 4).toUpperCase()}-2026-${String(policyState.length + 1).padStart(6, '0')}`,
    accountId: dto.accountId,
    brokerOfRecordId: dto.brokerOfRecordId,
    carrierId: dto.carrierId,
    lineOfBusiness: dto.lineOfBusiness,
    status: dto.importMode === 'csv-import' ? 'Issued' : 'Pending',
    effectiveDate: dto.effectiveDate,
    expirationDate: dto.expirationDate,
    totalPremium: dto.totalPremium ?? dto.coverages?.reduce((sum, coverage) => sum + coverage.premium, 0) ?? 0,
    externalPolicyReference: dto.externalPolicyReference ?? null,
  });
  policyState.unshift(policy);
  coverageState.set(policy.id, toCoverageLines(policy, dto.coverages));
  return policy;
}

export function importPolicies(dto: PolicyImportRequestDto): PolicyImportResultDto {
  const accepted: PolicyDto[] = [];
  const rejected = dto.policies.flatMap((policy, index) => {
    if (!policy.accountId || !policy.brokerOfRecordId || !policy.carrierId) {
      return [{ index, code: 'validation_error', message: 'Account, broker, and carrier are required.' }];
    }
    accepted.push(createPolicy({ ...policy, importMode: 'csv-import' }));
    return [];
  });
  return { accepted, rejected };
}

export function issuePolicy(policyId: string, rowVersion: string | null): PolicyDto | null | { code: string } {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  if (policy.rowVersion !== rowVersion) return { code: 'precondition_failed' };
  if (policy.status !== 'Pending') return { code: 'invalid_transition' };
  mutatePolicy(policy, 'Issued', { issuedAt: now, boundAt: now });
  return policy;
}

export function cancelPolicy(policyId: string, rowVersion: string | null, dto: PolicyCancelRequestDto): PolicyDto | null | { code: string } {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  if (policy.rowVersion !== rowVersion) return { code: 'precondition_failed' };
  if (policy.status !== 'Issued') return { code: 'invalid_transition' };
  mutatePolicy(policy, 'Cancelled', {
    cancelledAt: now,
    cancellationEffectiveDate: dto.cancellationEffectiveDate,
    cancellationReasonCode: dto.cancellationReasonCode,
    cancellationReasonDetail: dto.cancellationReasonDetail ?? null,
    reinstatementDeadline: addDays(dto.cancellationEffectiveDate, 30),
  });
  return policy;
}

export function reinstatePolicy(policyId: string, rowVersion: string | null, _dto: PolicyReinstateRequestDto): PolicyDto | null | { code: string } {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  if (policy.rowVersion !== rowVersion) return { code: 'precondition_failed' };
  if (policy.status !== 'Cancelled') return { code: 'invalid_transition' };
  mutatePolicy(policy, 'Issued', {
    cancelledAt: null,
    cancellationEffectiveDate: null,
    cancellationReasonCode: null,
    cancellationReasonDetail: null,
    reinstatementDeadline: null,
  });
  return policy;
}

export function endorsePolicy(policyId: string, rowVersion: string | null, dto: PolicyEndorsementRequestDto): PolicyEndorsementDto | null | { code: string } {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  if (policy.rowVersion !== rowVersion) return { code: 'precondition_failed' };
  if (policy.status !== 'Issued') return { code: 'invalid_transition' };
  const endorsementNumber = (endorsementState.get(policyId)?.length ?? 0) + 1;
  const versionNumber = policy.versionCount + 1;
  const versionId = `${policyId}-version-${versionNumber}`;
  const endorsement: PolicyEndorsementDto = {
    id: `${policyId}-endorsement-${endorsementNumber}`,
    policyId,
    endorsementNumber,
    policyVersionId: versionId,
    versionNumber,
    endorsementReasonCode: dto.endorsementReasonCode,
    endorsementReasonDetail: dto.endorsementReasonDetail ?? null,
    effectiveDate: dto.effectiveDate,
    premiumDelta: dto.premiumDelta ?? 0,
    premiumCurrency: policy.premiumCurrency,
    createdAt: now,
    createdByUserId: SYSTEM_USER_ID,
  };
  endorsementState.set(policyId, [endorsement, ...(endorsementState.get(policyId) ?? [])]);
  policy.totalPremium = dto.coverages.reduce((sum, coverage) => sum + coverage.premium, 0);
  policy.versionCount = versionNumber;
  policy.currentVersionNumber = versionNumber;
  policy.currentVersionId = versionId;
  policy.rowVersion = String(++rowVersionSeed);
  coverageState.set(policyId, toCoverageLines(policy, dto.coverages));
  return endorsement;
}

export function listPolicyVersions(policyId: string): PaginatedResponse<PolicyVersionDto> | null {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  const rows = Array.from({ length: policy.versionCount }).map((_, index) => {
    const versionNumber = policy.versionCount - index;
    return {
      id: `${policyId}-version-${versionNumber}`,
      policyId,
      versionNumber,
      versionReason: versionNumber === 1 ? 'IssuedInitial' : 'Endorsement',
      endorsementId: versionNumber === 1 ? null : `${policyId}-endorsement-${versionNumber - 1}`,
      effectiveDate: policy.effectiveDate,
      expirationDate: policy.expirationDate,
      totalPremium: policy.totalPremium,
      premiumCurrency: policy.premiumCurrency,
      profileSnapshot: null,
      coverageSnapshot: null,
      premiumSnapshot: null,
      createdAt: policy.updatedAt,
      createdByUserId: policy.updatedByUserId,
    } satisfies PolicyVersionDto;
  });
  return paginate(rows);
}

export function listPolicyEndorsements(policyId: string): PaginatedResponse<PolicyEndorsementDto> | null {
  if (!getPolicy(policyId)) return null;
  return paginate(endorsementState.get(policyId) ?? []);
}

export function listPolicyCoverages(policyId: string): PolicyCoverageLineDto[] | null {
  if (!getPolicy(policyId)) return null;
  return coverageState.get(policyId) ?? [];
}

export function listPolicyTimeline(policyId: string): PaginatedResponse<TimelineEventDto> | null {
  const policy = getPolicy(policyId);
  if (!policy) return null;
  return paginate([
    {
      id: `${policyId}-timeline-created`,
      entityType: 'Policy',
      entityId: policyId,
      eventType: 'PolicyCreated',
      eventDescription: `Policy ${policy.policyNumber} created`,
      entityName: policy.policyNumber,
      actorDisplayName: 'System',
      occurredAt: policy.createdAt,
    },
  ]);
}

export function getPolicyAccountSummary(accountId: string): PolicyAccountSummaryDto {
  const rows = policyState.filter((policy) => policy.accountId === accountId);
  const issued = rows.filter((policy) => policy.status === 'Issued');
  const next = issued.slice().sort((a, b) => a.expirationDate.localeCompare(b.expirationDate))[0];
  return {
    accountId,
    activePolicyCount: issued.length,
    expiredPolicyCount: rows.filter((policy) => policy.status === 'Expired').length,
    cancelledPolicyCount: rows.filter((policy) => policy.status === 'Cancelled').length,
    pendingPolicyCount: rows.filter((policy) => policy.status === 'Pending').length,
    nextExpiringDate: next?.expirationDate ?? null,
    nextExpiringPolicyId: next?.id ?? null,
    nextExpiringPolicyNumber: next?.policyNumber ?? null,
    totalCurrentPremium: issued.reduce((sum, policy) => sum + policy.totalPremium, 0),
    premiumCurrency: 'USD',
    computedAt: now,
  };
}

function buildPolicy(input: {
  id: string;
  policyNumber: string;
  accountId: string;
  brokerOfRecordId: string;
  carrierId: string;
  lineOfBusiness: string;
  status: PolicyStatus;
  effectiveDate: string;
  expirationDate: string;
  totalPremium: number;
  versionCount?: number;
  cancelledAt?: string | null;
  cancellationEffectiveDate?: string | null;
  cancellationReasonCode?: string | null;
  reinstatementDeadline?: string | null;
  externalPolicyReference?: string | null;
}): PolicyDto {
  const account = accountReferenceFixture.find((record) => record.id === input.accountId);
  return {
    id: input.id,
    accountId: input.accountId,
    brokerOfRecordId: input.brokerOfRecordId,
    policyNumber: input.policyNumber,
    lineOfBusiness: input.lineOfBusiness,
    carrierId: input.carrierId,
    carrierName: carrierNames.get(input.carrierId) ?? 'Legacy Carrier',
    status: input.status,
    effectiveDate: input.effectiveDate,
    expirationDate: input.expirationDate,
    boundAt: input.status === 'Issued' ? input.effectiveDate : null,
    issuedAt: input.status === 'Issued' ? input.effectiveDate : null,
    cancelledAt: input.cancelledAt ?? null,
    cancellationEffectiveDate: input.cancellationEffectiveDate ?? null,
    cancellationReasonCode: input.cancellationReasonCode ?? null,
    cancellationReasonDetail: null,
    reinstatementDeadline: input.reinstatementDeadline ?? null,
    expiredAt: input.status === 'Expired' ? input.expirationDate : null,
    predecessorPolicyId: null,
    successorPolicyId: null,
    currentVersionId: `${input.id}-version-${input.versionCount ?? 1}`,
    currentVersionNumber: input.versionCount ?? 1,
    versionCount: input.versionCount ?? 1,
    totalPremium: input.totalPremium,
    premiumCurrency: 'USD',
    producerUserId: null,
    producerDisplayName: null,
    importSource: 'manual',
    externalPolicyReference: input.externalPolicyReference ?? null,
    accountDisplayNameAtLink: account?.name ?? input.accountId,
    accountStatusAtRead: account?.status ?? null,
    accountSurvivorId: null,
    availableTransitions: availableTransitions(input.status, input.reinstatementDeadline),
    rowVersion: String(++rowVersionSeed),
    createdAt: now,
    createdByUserId: SYSTEM_USER_ID,
    updatedAt: now,
    updatedByUserId: SYSTEM_USER_ID,
  };
}

function toListItem(policy: PolicyDto): PolicyListItemDto {
  return {
    id: policy.id,
    policyNumber: policy.policyNumber,
    accountId: policy.accountId,
    accountDisplayName: policy.accountDisplayNameAtLink,
    accountStatus: policy.accountStatusAtRead,
    accountSurvivorId: policy.accountSurvivorId,
    brokerOfRecordId: policy.brokerOfRecordId,
    brokerName: findBroker(policy.brokerOfRecordId)?.legalName ?? null,
    carrierId: policy.carrierId,
    carrierName: policy.carrierName,
    lineOfBusiness: policy.lineOfBusiness,
    status: policy.status,
    effectiveDate: policy.effectiveDate,
    expirationDate: policy.expirationDate,
    totalPremium: policy.totalPremium,
    premiumCurrency: policy.premiumCurrency,
    producerUserId: policy.producerUserId,
    producerDisplayName: policy.producerDisplayName,
    versionCount: policy.versionCount,
    endorsementCount: endorsementState.get(policy.id)?.length ?? 0,
    hasOpenRenewal: policy.id === 'policy-1',
    reinstatementDeadline: policy.reinstatementDeadline,
    rowVersion: policy.rowVersion,
  };
}

function mutatePolicy(policy: PolicyDto, status: PolicyStatus, patch: Partial<PolicyDto>) {
  Object.assign(policy, patch, {
    status,
    updatedAt: now,
    updatedByUserId: SYSTEM_USER_ID,
    rowVersion: String(++rowVersionSeed),
  });
  policy.availableTransitions = availableTransitions(policy.status, policy.reinstatementDeadline);
}

function toCoverageLines(policy: PolicyDto, coverages: PolicyCoverageInputDto[] | null | undefined): PolicyCoverageLineDto[] {
  const rows = coverages?.length ? coverages : [{
    coverageCode: policy.lineOfBusiness,
    coverageName: policy.lineOfBusiness,
    limit: policy.totalPremium * 10,
    premium: policy.totalPremium,
  }];

  return rows.map((coverage, index) => ({
    id: `${policy.id}-coverage-${index + 1}`,
    policyId: policy.id,
    policyVersionId: policy.currentVersionId ?? `${policy.id}-version-${policy.versionCount}`,
    versionNumber: policy.versionCount,
    coverageCode: coverage.coverageCode,
    coverageName: coverage.coverageName ?? coverage.coverageCode,
    limit: coverage.limit,
    deductible: coverage.deductible ?? null,
    premium: coverage.premium,
    premiumCurrency: policy.premiumCurrency,
    exposureBasis: coverage.exposureBasis ?? null,
    exposureQuantity: coverage.exposureQuantity ?? null,
    isCurrent: true,
    createdAt: now,
  }));
}

function availableTransitions(status: PolicyStatus, reinstatementDeadline?: string | null): string[] {
  if (status === 'Pending') return ['Issue'];
  if (status === 'Issued') return ['Endorse', 'Cancel'];
  if (status === 'Cancelled' && reinstatementDeadline && reinstatementDeadline >= '2026-04-22') return ['Reinstate'];
  return [];
}

function findBroker(brokerId: string) {
  return brokerListFixture.find((broker) => broker.id === brokerId);
}

function paginate<T>(data: T[], page = 1, pageSize = 25): PaginatedResponse<T> {
  return {
    data,
    page,
    pageSize,
    totalCount: data.length,
    totalPages: Math.max(1, Math.ceil(data.length / pageSize)),
  };
}

function addDays(dateText: string, days: number): string {
  const date = new Date(`${dateText}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}
