import { FormEvent, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowUpRight, FileUp, Plus, Search } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import { useDebounce } from '@/hooks/useDebounce'
import {
  useBillingInvoices,
  useCreateBillingInvoice,
  useCreatePaymentReceipt,
  useImportPaymentReceipts,
} from '@/features/billing'
import type { BillingInvoiceCreateRequest, PaymentReceiptCreateRequest } from '@/features/billing'
import {
  MutationFeedback,
  SelectField,
  TextField,
} from '@/features/billing/components/BillingUi'
import { money, primaryButtonClass } from '@/features/billing/presentation'

const today = new Date().toISOString().slice(0, 10)

export default function BillingPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('All')
  const [exceptionFilter, setExceptionFilter] = useState('All')
  const query = useBillingInvoices({
    q: useDebounce(search),
    status: status as 'Outstanding' | 'Reconciled' | 'All',
    hasOpenException: exceptionFilter === 'All' ? undefined : exceptionFilter === 'Open exception',
    pageSize: 30,
  })

  return (
    <DashboardLayout title="Billing">
      <div className="space-y-5">
        <Card>
          <CardHeader>
            <CardTitle>Agency-Bill Workspace</CardTitle>
            <Link to="/billing/reconciliation" className="inline-flex min-h-11 items-center gap-2 text-sm font-medium text-text-secondary hover:text-text-primary">
              Reconciliation backlog <ArrowUpRight size={16} />
            </Link>
          </CardHeader>
          <div className="grid gap-3 lg:grid-cols-[minmax(260px,1fr)_180px_180px]">
            <label className="relative block">
              <span className="sr-only">Search invoices</span>
              <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" />
              <input
                type="search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Invoice, policy, account, or receipt reference"
                className="h-11 w-full rounded-lg border border-surface-border bg-surface-card px-9 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
              />
            </label>
            <SelectField label="Invoice status" value={status} options={['All', 'Outstanding', 'Reconciled']} onChange={setStatus} />
            <SelectField label="Exception state" value={exceptionFilter} options={['All', 'Open exception', 'No open exception']} onChange={setExceptionFilter} />
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Invoices</CardTitle>
            {query.data && <span className="text-sm text-text-muted">{query.data.totalCount} authorized records</span>}
          </CardHeader>
          {query.isLoading && <ListSkeleton />}
          {query.isError && <ErrorFallback message="Unable to load authorized invoices." onRetry={() => query.refetch()} />}
          {query.data?.data.length === 0 && (
            <div className="py-10 text-center">
              <p className="text-sm text-text-muted">No invoices match the current source-authorized filters.</p>
              <button type="button" onClick={() => { setSearch(''); setStatus('All'); setExceptionFilter('All') }} className="mt-3 text-sm font-medium text-nebula-violet">Clear filters</button>
            </div>
          )}
          {query.data && query.data.data.length > 0 && (
            <div className="overflow-hidden rounded-lg border border-surface-border">
              <div className="hidden grid-cols-[1.2fr_1fr_1fr_1fr_42px] gap-3 border-b border-surface-border bg-surface-highlight px-3 py-2 text-xs font-medium uppercase text-text-muted lg:grid">
                <span>Invoice</span><span>Policy</span><span>Outstanding</span><span>Status</span><span className="sr-only">Open</span>
              </div>
              {query.data.data.map((invoice) => (
                <Link key={invoice.id} to={`/billing/invoices/${invoice.id}`} className="grid gap-3 border-b border-surface-border px-3 py-3 last:border-b-0 hover:bg-surface-card-hover lg:grid-cols-[1.2fr_1fr_1fr_1fr_42px] lg:items-center">
                  <div><p className="text-sm font-medium text-text-primary">{invoice.invoiceNumber}</p><p className="mt-1 text-xs text-text-muted">Due {invoice.dueDate}</p></div>
                  <div><p className="text-sm text-text-primary">{invoice.policyId.slice(0, 8)}</p><p className="mt-1 text-xs text-text-muted">Version {invoice.policyVersionId.slice(0, 8)}</p></div>
                  <p className="text-sm font-medium text-text-primary">{money(invoice.outstandingAmount, invoice.currency)}</p>
                  <Badge variant={invoice.status === 'Reconciled' ? 'success' : 'warning'}>{invoice.status}</Badge>
                  <span className="flex h-9 w-9 items-center justify-center rounded-lg border border-surface-border text-text-muted" aria-hidden="true"><ArrowUpRight size={16} /></span>
                </Link>
              ))}
            </div>
          )}
        </Card>

        <div className="grid gap-5 xl:grid-cols-2">
          <InvoiceCreateCard />
          <ReceiptCaptureCard />
        </div>
      </div>
    </DashboardLayout>
  )
}

function InvoiceCreateCard() {
  const mutation = useCreateBillingInvoice()
  const [form, setForm] = useState<BillingInvoiceCreateRequest>({
    invoiceNumber: '', policyId: '', policyVersionId: '', accountId: '', currency: 'USD', originalAmount: 0,
    invoiceDate: today, dueDate: today,
  })
  function submit(event: FormEvent) {
    event.preventDefault()
    mutation.mutate(form, { onSuccess: () => setForm((current) => ({ ...current, invoiceNumber: '', originalAmount: 0 })) })
  }
  return (
    <Card>
      <CardHeader><CardTitle>Create Agency-Bill Invoice</CardTitle><Badge variant="info">Finance operations</Badge></CardHeader>
      <form className="space-y-3" onSubmit={submit}>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label="Invoice number" value={form.invoiceNumber} onChange={(value) => setForm({ ...form, invoiceNumber: value })} required />
          <TextField label="Policy ID" value={form.policyId} onChange={(value) => setForm({ ...form, policyId: value })} required />
          <TextField label="Policy version ID" value={form.policyVersionId} onChange={(value) => setForm({ ...form, policyVersionId: value })} required />
          <TextField label="Account ID" value={form.accountId} onChange={(value) => setForm({ ...form, accountId: value })} required />
          <TextField label="Currency" value={form.currency} onChange={(value) => setForm({ ...form, currency: value.toUpperCase() })} required />
          <TextField label="Original amount" type="number" min={0.01} step="0.01" value={form.originalAmount || ''} onChange={(value) => setForm({ ...form, originalAmount: Number(value) })} required />
          <TextField label="Invoice date" type="date" value={form.invoiceDate} onChange={(value) => setForm({ ...form, invoiceDate: value })} required />
          <TextField label="Due date" type="date" value={form.dueDate} onChange={(value) => setForm({ ...form, dueDate: value })} required />
        </div>
        <button type="submit" disabled={mutation.isPending} className={primaryButtonClass}><Plus size={16} />{mutation.isPending ? 'Creating invoice' : 'Create invoice'}</button>
        <MutationFeedback isPending={mutation.isPending} isSuccess={mutation.isSuccess} error={mutation.error} pending="Creating the invoice…" success="Invoice created and available in the workspace." failure="Unable to create the invoice." />
      </form>
    </Card>
  )
}

function ReceiptCaptureCard() {
  const create = useCreatePaymentReceipt()
  const upload = useImportPaymentReceipts()
  const [file, setFile] = useState<File | null>(null)
  const [form, setForm] = useState<PaymentReceiptCreateRequest>({ externalReference: '', receivedDate: today, currency: 'USD', amount: 0, invoiceReference: null, memo: null })
  function submit(event: FormEvent) {
    event.preventDefault()
    create.mutate(form, { onSuccess: () => setForm((current) => ({ ...current, externalReference: '', amount: 0, invoiceReference: null, memo: null })) })
  }
  return (
    <Card>
      <CardHeader><CardTitle>Record Payment Receipt</CardTitle><Badge variant="info">Manual or mock CSV</Badge></CardHeader>
      <form className="space-y-3" onSubmit={submit}>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label="External reference" value={form.externalReference} onChange={(value) => setForm({ ...form, externalReference: value })} required />
          <TextField label="Received date" type="date" value={form.receivedDate} onChange={(value) => setForm({ ...form, receivedDate: value })} required />
          <TextField label="Currency" value={form.currency} onChange={(value) => setForm({ ...form, currency: value.toUpperCase() })} required />
          <TextField label="Amount" type="number" min={0.01} step="0.01" value={form.amount || ''} onChange={(value) => setForm({ ...form, amount: Number(value) })} required />
          <TextField label="Invoice reference (optional)" value={form.invoiceReference ?? ''} onChange={(value) => setForm({ ...form, invoiceReference: value || null })} />
          <TextField label="Memo (optional)" value={form.memo ?? ''} onChange={(value) => setForm({ ...form, memo: value || null })} />
        </div>
        <button type="submit" disabled={create.isPending} className={primaryButtonClass}><Plus size={16} />{create.isPending ? 'Recording receipt' : 'Record manual receipt'}</button>
        <MutationFeedback isPending={create.isPending} isSuccess={create.isSuccess} error={create.error} pending="Recording the receipt…" success="Receipt recorded as unapplied." failure="Unable to record the receipt." />
      </form>
      <div className="mt-5 border-t border-surface-border pt-4">
        <label className="grid gap-1 text-xs font-medium text-text-muted">
          mock-payment-receipt-row-v1 CSV
          <input type="file" accept=".csv,text/csv" onChange={(event) => setFile(event.target.files?.[0] ?? null)} className="block min-h-11 w-full rounded-lg border border-surface-border bg-surface-card p-2 text-sm text-text-primary file:mr-3 file:rounded-md file:border-0 file:bg-surface-highlight file:px-3 file:py-1 file:text-text-primary" />
        </label>
        <button type="button" onClick={() => file && upload.mutate(file)} disabled={!file || upload.isPending} className={`${primaryButtonClass} mt-3`}><FileUp size={16} />{upload.isPending ? 'Importing CSV' : 'Import mock CSV'}</button>
        <MutationFeedback isPending={upload.isPending} isSuccess={upload.isSuccess} error={upload.error} pending="Validating and importing rows…" success={upload.data ? `Import completed: ${upload.data.createdCount} created, ${upload.data.duplicateCount} duplicate, ${upload.data.rejectedCount} rejected.` : 'Import completed.'} failure="Unable to import the CSV." />
      </div>
    </Card>
  )
}

function ListSkeleton() {
  return <div className="space-y-3">{Array.from({ length: 5 }).map((_, index) => <Skeleton key={index} className="h-16 w-full rounded-lg" />)}</div>
}
