import { fireEvent, render, screen } from '@testing-library/react'
import { axe } from 'jest-axe'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import BillingPage from '@/pages/BillingPage'
import BillingInvoiceDetailPage from '@/pages/BillingInvoiceDetailPage'
import BillingReconciliationPage from '@/pages/BillingReconciliationPage'

const { applyExact, correctReference, decideCorrection, mutation } = vi.hoisted(() => ({
  applyExact: vi.fn(),
  correctReference: vi.fn(),
  decideCorrection: vi.fn(),
  mutation: () => ({ mutate: vi.fn(), isPending: false, isSuccess: false, isError: false, error: null, data: undefined }),
}))

vi.mock('@/components/layout/DashboardLayout', () => ({
  DashboardLayout: ({ children }: { children: React.ReactNode }) => <main>{children}</main>,
}))

vi.mock('@/features/commissions', () => ({
  useExpectedCommissions: () => ({
    data: {
      data: [{
        id: 'commission-1', producerDisplayName: 'Alex Producer', status: 'Calculated', exceptionState: 'None',
        adjustedExpectedCommission: 25,
      }],
      page: 1, pageSize: 20, totalCount: 1, totalPages: 1,
    },
    isLoading: false,
    isError: false,
    refetch: vi.fn(),
  }),
}))

vi.mock('@/features/billing', async (importActual) => {
  const actual = await importActual<typeof import('@/features/billing')>()
  const invoice = {
    id: 'invoice-1', invoiceNumber: 'INV-100', policyId: 'policy-1', policyVersionId: 'version-1', accountId: 'account-1',
    currency: 'USD', originalAmount: 250, outstandingAmount: 250, invoiceDate: '2026-07-19', dueDate: '2026-08-19',
    status: 'Outstanding', createdAt: '2026-07-19T10:00:00Z', createdByUserId: 'user-1', rowVersion: '3',
  }
  return {
    ...actual,
    useBillingInvoices: () => ({ data: { data: [invoice], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 }, isLoading: false, isError: false, refetch: vi.fn() }),
    useBillingInvoice: () => ({ data: {
      invoice,
      applications: [{ id: 'application-1', billingInvoiceId: 'invoice-1', paymentReceiptId: 'receipt-1', currency: 'USD', appliedAmount: 250, invoiceOutstandingBefore: 250, invoiceOutstandingAfter: 0, appliedAt: '2026-07-19T11:00:00Z', appliedByUserId: 'user-2' }],
      receipts: [{ id: 'receipt-1', source: 'Manual', externalReference: 'PAY-100', receivedDate: '2026-07-19', currency: 'USD', amount: 250, invoiceReference: 'INV-100', memo: null, importBatchId: null, importRowNumber: null, applicationStatus: 'Applied', rowVersion: '6' }],
      exceptions: [{ id: 'detail-exception-1', type: 'AmountMismatch', billingInvoiceId: 'invoice-1', paymentReceiptId: 'receipt-1', importBatchId: null, importRowOutcomeId: null, status: 'Resolved', openedAt: '2026-07-19T10:30:00Z', openedByUserId: 'user-1', resolvedAt: '2026-07-19T10:45:00Z', resolvedByUserId: 'user-2', resolutionCode: 'Verified', resolutionNote: 'Source checked.', pendingCorrection: null, rowVersion: '8' }],
      auditEvents: [{ id: 'event-1', entityType: 'BillingInvoice', entityId: 'invoice-1', eventType: 'ExactPaymentApplied', eventDescription: 'Exact payment receipt applied', entityName: null, actorDisplayName: 'Finance User', occurredAt: '2026-07-19T11:00:00Z' }],
    }, isLoading: false, isError: false, refetch: vi.fn() }),
    usePolicyBillingSummary: () => ({ data: { policyId: 'policy-1', currency: 'USD', invoiceCount: 1, outstandingInvoiceCount: 1, outstandingAmount: 250, nextDueDate: '2026-08-19', asOf: '2026-07-19T10:00:00Z' }, isLoading: false, isError: false, refetch: vi.fn() }),
    usePaymentReceipts: () => ({ data: { data: [{ id: 'receipt-1', source: 'Manual', externalReference: 'PAY-100', receivedDate: '2026-07-19', currency: 'USD', amount: 250, invoiceReference: 'INV-100', memo: null, importBatchId: null, importRowNumber: null, applicationStatus: 'Unapplied', rowVersion: '5' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 }, isLoading: false, isError: false, refetch: vi.fn() }),
    useReconciliationBacklog: () => ({ data: { openCount: 1, exactApplicationCount: 4, pendingCorrectionCount: 1, rejectedImportRowCount: 2, duplicateImportRowCount: 3, oldestOpenDays: 2, byType: [{ type: 'InvoiceReferenceConflict', count: 1 }] }, isLoading: false, isError: false, refetch: vi.fn() }),
    useReconciliationExceptions: () => ({ data: { data: [{ id: 'exception-1', type: 'InvoiceReferenceConflict', billingInvoiceId: 'invoice-1', paymentReceiptId: 'receipt-1', importBatchId: null, importRowOutcomeId: null, status: 'Open', openedAt: '2026-07-19T10:00:00Z', openedByUserId: 'user-1', resolvedAt: null, resolvedByUserId: null, resolutionCode: null, resolutionNote: null, pendingCorrection: { id: 'correction-1', reconciliationExceptionId: 'exception-1', billingInvoiceId: 'invoice-1', beforeOutstandingAmount: 250, correctionAmount: -25, proposedOutstandingAmount: 225, reason: 'Source adjustment', evidenceNote: 'Reviewed source record', status: 'Pending', requestedByUserId: 'user-1', requestedAt: '2026-07-19T10:30:00Z', decisionByUserId: null, decisionAt: null, decisionNote: null, rowVersion: '9' }, rowVersion: '7' }], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 }, isLoading: false, isError: false, refetch: vi.fn() }),
    useCreateBillingInvoice: mutation,
    useCreatePaymentReceipt: mutation,
    useImportPaymentReceipts: mutation,
    useApplyExactPayment: () => ({ ...mutation(), mutate: applyExact }),
    useCorrectReconciliationReference: () => ({ ...mutation(), mutate: correctReference }),
    useRequestBillingCorrection: mutation,
    useDecideBillingCorrection: () => ({ ...mutation(), mutate: decideCorrection }),
  }
})

describe('F0026 billing UI', () => {
  it('renders accessible source-authorized invoices and capture workflows', async () => {
    const { container } = render(<MemoryRouter><BillingPage /></MemoryRouter>)

    expect(screen.getByText('INV-100')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create invoice/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /record manual receipt/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /import mock csv/i })).toBeDisabled()
    await expect(axe(container)).resolves.toHaveNoViolations()
  })

  it('wires explicit exact application with selected receipt context', () => {
    render(
      <MemoryRouter initialEntries={['/billing/invoices/invoice-1']}>
        <Routes><Route path="/billing/invoices/:invoiceId" element={<BillingInvoiceDetailPage />} /></Routes>
      </MemoryRouter>,
    )

    fireEvent.click(screen.getByRole('button', { name: /apply exact payment/i }))

    expect(applyExact).toHaveBeenCalledWith(expect.objectContaining({ invoiceId: 'invoice-1', invoiceRowVersion: '3' }))
    expect(screen.getByText('ExactPaymentApplied')).toBeInTheDocument()
    expect(screen.getAllByText('PAY-100')).toHaveLength(2)
    expect(screen.getByText('Alex Producer')).toBeInTheDocument()
  })

  it('wires reference correction and manager decision as separate flows', () => {
    render(<MemoryRouter><BillingReconciliationPage /></MemoryRouter>)

    fireEvent.change(screen.getByLabelText('Resolution note'), { target: { value: 'Verified against source record.' } })
    fireEvent.click(screen.getByRole('button', { name: /correct reference/i }))
    expect(correctReference).toHaveBeenCalledWith(expect.objectContaining({ exceptionId: 'exception-1', rowVersion: '7' }))

    fireEvent.change(screen.getByLabelText('Decision note'), { target: { value: 'Independent manager review complete.' } })
    fireEvent.click(screen.getByRole('button', { name: /approve correction/i }))
    expect(decideCorrection).toHaveBeenCalledWith(expect.objectContaining({ correctionId: 'correction-1', rowVersion: '9', decision: 'Approve' }))
  })
})
