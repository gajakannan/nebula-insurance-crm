import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/services/api'
import type {
  BillingCorrection,
  BillingInvoice,
  BillingInvoiceCreateRequest,
  BillingInvoiceDetail,
  BillingInvoiceSearchParams,
  PaginatedResponse,
  PaymentApplication,
  PaymentReceipt,
  PaymentReceiptCreateRequest,
  PaymentReceiptImportResult,
  PaymentReceiptSearchParams,
  PolicyBillingSummary,
  ReconciliationBacklog,
  ReconciliationException,
  ReconciliationExceptionType,
} from './types'

function pageQuery(params: { page?: number; pageSize?: number }) {
  const query = new URLSearchParams()
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 25))
  return query
}

export function useBillingInvoices(params: BillingInvoiceSearchParams = {}) {
  const query = pageQuery(params)
  if (params.q) query.set('q', params.q)
  if (params.status && params.status !== 'All') query.set('status', params.status)
  if (params.hasOpenException !== undefined) query.set('hasOpenException', String(params.hasOpenException))
  return useQuery({
    queryKey: ['billing', 'invoices', params],
    queryFn: () => api.get<PaginatedResponse<BillingInvoice>>(`/billing-invoices?${query.toString()}`),
  })
}

export function useBillingInvoice(invoiceId: string | undefined) {
  return useQuery({
    queryKey: ['billing', 'invoice', invoiceId],
    queryFn: () => api.get<BillingInvoiceDetail>(`/billing-invoices/${invoiceId}`),
    enabled: Boolean(invoiceId),
  })
}

export function usePolicyBillingSummary(policyId: string | undefined) {
  return useQuery({
    queryKey: ['billing', 'policy-summary', policyId],
    queryFn: () => api.get<PolicyBillingSummary>(`/policies/${policyId}/billing-summary`),
    enabled: Boolean(policyId),
  })
}

export function usePaymentReceipts(params: PaymentReceiptSearchParams = {}) {
  const query = pageQuery(params)
  if (params.applicationStatus && params.applicationStatus !== 'All') query.set('applicationStatus', params.applicationStatus)
  if (params.externalReference) query.set('externalReference', params.externalReference)
  if (params.currency) query.set('currency', params.currency)
  return useQuery({
    queryKey: ['billing', 'receipts', params],
    queryFn: () => api.get<PaginatedResponse<PaymentReceipt>>(`/payment-receipts?${query.toString()}`),
  })
}

export function useReconciliationExceptions(params: {
  status?: 'Open' | 'Resolved' | 'All'
  type?: ReconciliationExceptionType | 'All'
  page?: number
  pageSize?: number
} = {}) {
  const query = pageQuery(params)
  if (params.status && params.status !== 'All') query.set('status', params.status)
  if (params.type && params.type !== 'All') query.set('type', params.type)
  return useQuery({
    queryKey: ['billing', 'exceptions', params],
    queryFn: () => api.get<PaginatedResponse<ReconciliationException>>(`/reconciliation-exceptions?${query.toString()}`),
  })
}

export function useReconciliationBacklog() {
  return useQuery({
    queryKey: ['billing', 'backlog'],
    queryFn: () => api.get<ReconciliationBacklog>('/reconciliation-backlog'),
  })
}

function useBillingMutation<TData, TVariables>(mutationFn: (variables: TVariables) => Promise<TData>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billing'] }),
  })
}

export function useCreateBillingInvoice() {
  return useBillingMutation((dto: BillingInvoiceCreateRequest) => api.post<BillingInvoice>('/billing-invoices', dto))
}

export function useCreatePaymentReceipt() {
  return useBillingMutation((dto: PaymentReceiptCreateRequest) => api.post<PaymentReceipt>('/payment-receipts', dto))
}

export function useImportPaymentReceipts() {
  return useBillingMutation((file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.postMultipart<PaymentReceiptImportResult>('/payment-receipt-imports', form)
  })
}

export function useApplyExactPayment() {
  return useBillingMutation((input: { invoiceId: string; invoiceRowVersion: string; receipt: PaymentReceipt }) =>
    api.post<PaymentApplication>('/payment-applications', {
      billingInvoiceId: input.invoiceId,
      paymentReceiptId: input.receipt.id,
      paymentReceiptRowVersion: input.receipt.rowVersion,
    }, { 'If-Match': `"${input.invoiceRowVersion}"` }))
}

export function useCorrectReconciliationReference() {
  return useBillingMutation((input: {
    exceptionId: string
    rowVersion: string
    billingInvoiceId: string
    resolutionCode: string
    resolutionNote: string
  }) => api.patch<ReconciliationException>(`/reconciliation-exceptions/${input.exceptionId}/reference`, {
    billingInvoiceId: input.billingInvoiceId,
    resolutionCode: input.resolutionCode,
    resolutionNote: input.resolutionNote,
  }, { 'If-Match': `"${input.rowVersion}"` }))
}

export function useRequestBillingCorrection() {
  return useBillingMutation((input: {
    exceptionId: string
    rowVersion: string
    correctionAmount: number
    proposedOutstandingAmount: number
    reason: string
    evidenceNote: string
  }) => api.post<BillingCorrection>(`/reconciliation-exceptions/${input.exceptionId}/corrections`, {
    correctionAmount: input.correctionAmount,
    proposedOutstandingAmount: input.proposedOutstandingAmount,
    reason: input.reason,
    evidenceNote: input.evidenceNote,
  }, { 'If-Match': `"${input.rowVersion}"` }))
}

export function useDecideBillingCorrection() {
  return useBillingMutation((input: {
    correctionId: string
    rowVersion: string
    decision: 'Approve' | 'Reject'
    decisionNote: string
  }) => api.post<BillingCorrection>(`/billing-corrections/${input.correctionId}/decision`, {
    decision: input.decision,
    decisionNote: input.decisionNote,
  }, { 'If-Match': `"${input.rowVersion}"` }))
}
