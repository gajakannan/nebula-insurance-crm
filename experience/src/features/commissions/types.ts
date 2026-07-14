export interface PaginatedResponse<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type CommissionStatus = 'Draft' | 'ReadyForReview' | 'Calculated' | 'Exception'
export type CommissionExceptionState = 'None' | 'MissingSchedule' | 'MissingSplit' | 'MissingPremium' | 'StaleSource'
export type CommissionBasis = 'PercentOfPremium' | 'Flat'
export type AdjustmentDecision = 'Approved' | 'Rejected'
export type RollupGroupBy = 'producer' | 'broker' | 'territory' | 'carrierMarket' | 'policyPeriod'

export interface CommissionSearchParams {
  search?: string
  status?: string
  exceptionState?: string
  page?: number
  pageSize?: number
}

export interface ExpectedCommissionDto {
  id: string
  policyId: string
  policyNumber: string | null
  accountDisplayName: string | null
  policyVersionId: string | null
  carrierMarketId: string
  carrierMarketName: string | null
  brokerName: string | null
  producerUserId: string | null
  producerDisplayName: string | null
  lineOfBusiness: string | null
  commissionScheduleId: string | null
  producerSplitAssignmentId: string | null
  premiumBasisAmount: number | null
  expectedGrossCommission: number | null
  approvedAdjustmentTotal: number
  adjustedExpectedCommission: number | null
  status: CommissionStatus | string
  exceptionState: CommissionExceptionState | string
  calculatedAt: string | null
  rowVersion: number
}

export interface CommissionScheduleUpsertDto {
  carrierMarketId: string
  lineOfBusiness: string
  state: string | null
  productCode: string | null
  basis: CommissionBasis
  ratePercent: number | null
  flatAmount: number | null
  effectiveFrom: string
  effectiveTo: string | null
  sourceNote: string
}

export interface CommissionScheduleDto extends CommissionScheduleUpsertDto {
  id: string
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface ProducerSplitParticipantUpsertDto {
  producerId: string
  splitPercent: number
}

export interface ProducerSplitAssignmentUpsertDto {
  policyId: string
  effectiveFrom: string
  effectiveTo: string | null
  reason: string
  participants: ProducerSplitParticipantUpsertDto[]
}

export interface ProducerSplitParticipantDto extends ProducerSplitParticipantUpsertDto {
  id: string
  producerDisplayName: string | null
  sourceOwnershipSnapshotJson: string | null
  rowVersion: number
}

export interface ProducerSplitAssignmentDto {
  id: string
  policyId: string
  effectiveFrom: string
  effectiveTo: string | null
  reason: string
  participants: ProducerSplitParticipantDto[]
  createdAt: string
  createdByUserId: string
  updatedAt: string
  updatedByUserId: string
  rowVersion: number
}

export interface CommissionAdjustmentRequestDto {
  amount: number
  effectiveDate: string
  reason: string
}

export interface CommissionAdjustmentDecisionRequestDto {
  decision: AdjustmentDecision
  decisionNote: string
}

export interface CommissionAdjustmentDto extends CommissionAdjustmentRequestDto {
  id: string
  expectedCommissionId: string
  status: 'Pending' | 'Approved' | 'Rejected' | string
  requestedByUserId: string
  requestedAt: string
  decidedByUserId: string | null
  decidedAt: string | null
  decisionNote: string | null
  rowVersion: number
}

export interface ExpectedCommissionDetailDto {
  commission: ExpectedCommissionDto
  schedules: CommissionScheduleDto[]
  splits: ProducerSplitAssignmentDto[]
  adjustments: CommissionAdjustmentDto[]
}

export interface RevenueAttributionRollupQuery {
  startDate: string
  endDate: string
  groupBy: RollupGroupBy | string
  producerId: string | null
  brokerId: string | null
  territoryId: string | null
  carrierMarketId: string | null
  status: string | null
  exceptionState: string | null
}

export interface RevenueAttributionRollupRowDto {
  groupKey: string
  groupLabel: string
  expectedGrossCommissionTotal: number
  approvedAdjustmentTotal: number
  adjustedExpectedCommissionTotal: number
  producerAllocationTotal: number
  recordCount: number
  exceptionCount: number
}

export interface RevenueAttributionRollupResponseDto {
  query: RevenueAttributionRollupQuery
  rows: RevenueAttributionRollupRowDto[]
}
