import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useBillingInvoice, useReconciliationBacklog } from '@/features/billing/hooks'

const { get } = vi.hoisted(() => ({ get: vi.fn() }))

vi.mock('@/services/api', () => ({
  api: { get },
}))

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  })
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}

describe('F0026 billing query hooks', () => {
  it('loads the invoice evidence envelope and expanded backlog from their canonical routes', async () => {
    get.mockImplementation((path: string) => {
      if (path === '/billing-invoices/invoice-1') {
        return Promise.resolve({
          invoice: { id: 'invoice-1' },
          applications: [{ id: 'application-1' }],
          receipts: [{ id: 'receipt-1' }],
          exceptions: [],
          auditEvents: [{ id: 'event-1' }],
        })
      }
      if (path === '/reconciliation-backlog') {
        return Promise.resolve({
          openCount: 1,
          exactApplicationCount: 2,
          pendingCorrectionCount: 1,
          rejectedImportRowCount: 3,
          duplicateImportRowCount: 4,
          oldestOpenDays: 0,
          byType: [],
        })
      }
      return Promise.reject(new Error(`Unexpected API path: ${path}`))
    })

    const { result } = renderHook(() => ({
      detail: useBillingInvoice('invoice-1'),
      backlog: useReconciliationBacklog(),
    }), { wrapper })

    await waitFor(() => {
      expect(result.current.detail.isSuccess).toBe(true)
      expect(result.current.backlog.isSuccess).toBe(true)
    })

    expect(get).toHaveBeenCalledWith('/billing-invoices/invoice-1')
    expect(get).toHaveBeenCalledWith('/reconciliation-backlog')
    expect(result.current.detail.data?.applications).toHaveLength(1)
    expect(result.current.backlog.data?.pendingCorrectionCount).toBe(1)
  })
})
