import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/api'
import type {
  CommissionAdjustmentDecisionRequestDto,
  CommissionAdjustmentDto,
  CommissionAdjustmentRequestDto,
  CommissionScheduleDto,
  CommissionScheduleUpsertDto,
  CommissionSearchParams,
  ExpectedCommissionDetailDto,
  ExpectedCommissionDto,
  PaginatedResponse,
  ProducerSplitAssignmentDto,
  ProducerSplitAssignmentUpsertDto,
  RevenueAttributionRollupResponseDto,
  RollupGroupBy,
} from './types'

export function useExpectedCommissions(params: CommissionSearchParams = {}) {
  const query = new URLSearchParams()
  if (params.search) query.set('search', params.search)
  if (params.status && params.status !== 'All') query.set('status', params.status)
  if (params.exceptionState && params.exceptionState !== 'All') query.set('exceptionState', params.exceptionState)
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 20))

  return useQuery({
    queryKey: ['commissions', 'expected', params],
    queryFn: () => api.get<PaginatedResponse<ExpectedCommissionDto>>(`/expected-commissions?${query.toString()}`),
  })
}

export function useExpectedCommission(id: string | undefined) {
  return useQuery({
    queryKey: ['commissions', 'expected', id],
    queryFn: () => api.get<ExpectedCommissionDetailDto>(`/expected-commissions/${id}`),
    enabled: Boolean(id),
  })
}

export function useRevenueAttributionRollups(params: {
  startDate: string
  endDate: string
  groupBy: RollupGroupBy
  status?: string
  exceptionState?: string
}) {
  const query = new URLSearchParams()
  query.set('startDate', params.startDate)
  query.set('endDate', params.endDate)
  query.set('groupBy', params.groupBy)
  if (params.status && params.status !== 'All') query.set('status', params.status)
  if (params.exceptionState && params.exceptionState !== 'All') query.set('exceptionState', params.exceptionState)

  return useQuery({
    queryKey: ['commissions', 'rollups', params],
    queryFn: () => api.get<RevenueAttributionRollupResponseDto>(`/revenue-attribution/rollups?${query.toString()}`),
  })
}

export function useCreateCommissionSchedule() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: CommissionScheduleUpsertDto) => api.post<CommissionScheduleDto>('/commission-schedules', dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['commissions'] }),
  })
}

export function useUpsertProducerSplit() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: ProducerSplitAssignmentUpsertDto) => api.post<ProducerSplitAssignmentDto>('/producer-splits', dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['commissions'] }),
  })
}

export function useCalculateExpectedCommission(expectedCommissionId: string | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => api.post<ExpectedCommissionDto>(`/expected-commissions/${expectedCommissionId}/calculate`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['commissions'] })
      queryClient.invalidateQueries({ queryKey: ['commissions', 'expected', expectedCommissionId] })
    },
  })
}

export function useRequestCommissionAdjustment(expectedCommissionId: string | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: CommissionAdjustmentRequestDto) =>
      api.post<CommissionAdjustmentDto>(`/expected-commissions/${expectedCommissionId}/adjustments`, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['commissions'] })
      queryClient.invalidateQueries({ queryKey: ['commissions', 'expected', expectedCommissionId] })
    },
  })
}

export function useDecideCommissionAdjustment(expectedCommissionId: string | undefined) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ adjustmentId, dto }: { adjustmentId: string; dto: CommissionAdjustmentDecisionRequestDto }) =>
      api.post<CommissionAdjustmentDto>(`/commission-adjustments/${adjustmentId}/decision`, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['commissions'] })
      queryClient.invalidateQueries({ queryKey: ['commissions', 'expected', expectedCommissionId] })
    },
  })
}
