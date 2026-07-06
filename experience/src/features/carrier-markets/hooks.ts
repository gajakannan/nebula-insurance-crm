import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/api'
import type {
  CarrierAppointmentDto,
  CarrierAppointmentUpsertDto,
  CarrierAppetiteNoteDto,
  CarrierAppetiteNoteUpsertDto,
  CarrierMarketActivityLinkCreateDto,
  CarrierMarketActivityLinkDto,
  CarrierMarketContactDto,
  CarrierMarketContactUpsertDto,
  CarrierMarketCreateDto,
  CarrierMarketDetailDto,
  CarrierMarketDto,
  CarrierMarketUpdateDto,
  PaginatedResponse,
} from './types'

interface UseCarrierMarketsParams {
  search?: string
  status?: string
  marketType?: string
  page?: number
  pageSize?: number
}

export function useCarrierMarkets({ search, status, marketType, page = 1, pageSize = 20 }: UseCarrierMarketsParams = {}) {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  if (status && status !== 'All') params.set('status', status)
  if (marketType && marketType !== 'All') params.set('marketType', marketType)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))

  return useQuery({
    queryKey: ['carrier-markets', { search, status, marketType, page, pageSize }],
    queryFn: () => api.get<PaginatedResponse<CarrierMarketDto>>(`/carrier-markets?${params.toString()}`),
  })
}

export function useCarrierMarket(id: string | null) {
  return useQuery({
    queryKey: ['carrier-markets', id],
    queryFn: () => api.get<CarrierMarketDetailDto>(`/carrier-markets/${id}`),
    enabled: Boolean(id),
  })
}

export function useCreateCarrierMarket() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (dto: CarrierMarketCreateDto) => api.post<CarrierMarketDto>('/carrier-markets', dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['carrier-markets'] }),
  })
}

export function useUpdateCarrierMarket() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, dto, rowVersion }: { id: string; dto: CarrierMarketUpdateDto; rowVersion: number }) =>
      api.put<CarrierMarketDto>(`/carrier-markets/${id}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['carrier-markets'] })
      queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.id] })
    },
  })
}

export function useDeleteCarrierMarket() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => api.delete(`/carrier-markets/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['carrier-markets'] }),
  })
}

export function useAddCarrierContact() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, dto }: { marketId: string; dto: CarrierMarketContactUpsertDto }) =>
      api.post<CarrierMarketContactDto>(`/carrier-markets/${marketId}/contacts`, dto),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}

export function useUpdateCarrierContact() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, contactId, dto, rowVersion }: { marketId: string; contactId: string; dto: CarrierMarketContactUpsertDto; rowVersion: number }) =>
      api.put<CarrierMarketContactDto>(`/carrier-markets/${marketId}/contacts/${contactId}`, dto, { 'If-Match': `"${rowVersion}"` }),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}

export function useDeleteCarrierContact() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, contactId }: { marketId: string; contactId: string }) =>
      api.delete(`/carrier-markets/${marketId}/contacts/${contactId}`),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}

export function useAddCarrierAppetiteNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, dto }: { marketId: string; dto: CarrierAppetiteNoteUpsertDto }) =>
      api.post<CarrierAppetiteNoteDto>(`/carrier-markets/${marketId}/appetite-notes`, dto),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}

export function useAddCarrierAppointment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, dto }: { marketId: string; dto: CarrierAppointmentUpsertDto }) =>
      api.post<CarrierAppointmentDto>(`/carrier-markets/${marketId}/appointments`, dto),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}

export function useAddCarrierActivityLink() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ marketId, dto }: { marketId: string; dto: CarrierMarketActivityLinkCreateDto }) =>
      api.post<CarrierMarketActivityLinkDto>(`/carrier-markets/${marketId}/activity-links`, dto),
    onSuccess: (_data, variables) => queryClient.invalidateQueries({ queryKey: ['carrier-markets', variables.marketId] }),
  })
}
