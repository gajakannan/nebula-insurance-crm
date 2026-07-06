import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/api'
import type {
  AdminConfigurationAuditResponse,
  AdminConfigurationDomain,
  AdminConfigurationDomainDetail,
  AdminConfigurationDraft,
  AdminConfigurationPublishedSet,
  AdminConfigurationValidationResult,
} from './types'

export interface AdminConfigurationAuditFilters {
  domainKey?: string
  action?: string
  outcome?: string
  actorUserId?: string
  from?: string
  to?: string
}

export function useAdminConfigurationDomains() {
  return useQuery({
    queryKey: ['admin-configuration', 'domains'],
    queryFn: () => api.get<AdminConfigurationDomain[]>('/admin/configuration-domains'),
  })
}

export function useAdminConfigurationDomain(domainKey: string | undefined) {
  return useQuery({
    queryKey: ['admin-configuration', 'domain', domainKey],
    queryFn: () => api.get<AdminConfigurationDomainDetail>(`/admin/configuration-domains/${domainKey}`),
    enabled: Boolean(domainKey),
  })
}

export function useAdminConfigurationAudit(filters: AdminConfigurationAuditFilters) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.set(key, value)
  })
  const suffix = params.toString()
  return useQuery({
    queryKey: ['admin-configuration', 'audit', filters],
    queryFn: () => api.get<AdminConfigurationAuditResponse>(`/admin/configuration-audit-events${suffix ? `?${suffix}` : ''}`),
  })
}

export function useAdminConfigurationMutations(domainKey: string | undefined) {
  const queryClient = useQueryClient()
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ['admin-configuration'] })
  }

  return {
    createDraft: useMutation({
      mutationFn: ({ key, reason }: { key: string; reason: string }) => api.post<AdminConfigurationDraft>(`/admin/configuration-domains/${key}/drafts`, { reason }),
      onSuccess: invalidate,
    }),
    updateDraft: useMutation({
      mutationFn: ({ draftId, payload, rowVersion, reason }: { draftId: string; payload: unknown; rowVersion: string; reason: string }) =>
        api.patch<AdminConfigurationDraft>(`/admin/configuration-drafts/${draftId}`, { payload, reason }, { 'If-Match': rowVersion }),
      onSuccess: invalidate,
    }),
    compareDraft: useMutation({
      mutationFn: (draftId: string) => api.get<AdminConfigurationValidationResult>(`/admin/configuration-drafts/${draftId}/comparison`),
    }),
    validateDraft: useMutation({
      mutationFn: (draftId: string) => api.post<AdminConfigurationValidationResult>(`/admin/configuration-drafts/${draftId}/validation`, {}),
      onSuccess: invalidate,
    }),
    publishDraft: useMutation({
      mutationFn: ({ draftId, reason }: { draftId: string; reason: string }) =>
        api.post<AdminConfigurationPublishedSet>(`/admin/configuration-drafts/${draftId}/publish`, { reason }),
      onSuccess: invalidate,
    }),
    rollback: useMutation({
      mutationFn: ({ targetPublishedVersion, reason }: { targetPublishedVersion: number; reason: string }) =>
        api.post<AdminConfigurationPublishedSet>(`/admin/configuration-domains/${domainKey}/rollback`, { targetPublishedVersion, reason }),
      onSuccess: invalidate,
    }),
  }
}
