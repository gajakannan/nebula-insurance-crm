import { useQuery } from '@tanstack/react-query'
import { api } from '@/services/api'
import type { LobSchemaBundleDto } from '../types'
import { CYBER_BUNDLE_IDENTITY } from '../lib/cyber'

export function useCyberSchemaBundle(enabled: boolean) {
  const params = new URLSearchParams({
    lineOfBusiness: CYBER_BUNDLE_IDENTITY.lineOfBusiness,
  })

  return useQuery({
    queryKey: ['lob-schemas', 'active', CYBER_BUNDLE_IDENTITY],
    queryFn: () => api.get<LobSchemaBundleDto>(
      [
        '/lob-schemas/active',
        CYBER_BUNDLE_IDENTITY.productKey,
        CYBER_BUNDLE_IDENTITY.productVersion,
        CYBER_BUNDLE_IDENTITY.schemaVersion,
      ].join('/') + `?${params.toString()}`,
    ),
    enabled,
    staleTime: 5 * 60 * 1000,
  })
}
