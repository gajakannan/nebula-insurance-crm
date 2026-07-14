import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowUpRight, Search } from 'lucide-react'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { Badge } from '@/components/ui/Badge'
import { Card, CardHeader, CardTitle } from '@/components/ui/Card'
import { ErrorFallback } from '@/components/ui/ErrorFallback'
import { Skeleton } from '@/components/ui/Skeleton'
import { useDebounce } from '@/hooks/useDebounce'
import { ApiError } from '@/services/api'
import { useExpectedCommissions, useRevenueAttributionRollups } from '@/features/commissions'
import type { ExpectedCommissionDto, RevenueAttributionRollupRowDto, RollupGroupBy } from '@/features/commissions'

const STATUS_OPTIONS = ['All', 'Draft', 'ReadyForReview', 'Calculated', 'Exception'] as const
const EXCEPTION_OPTIONS = ['All', 'None', 'MissingSchedule', 'MissingSplit', 'MissingPremium', 'StaleSource'] as const
const GROUP_OPTIONS: Array<{ label: string; value: RollupGroupBy }> = [
  { label: 'Producer', value: 'producer' },
  { label: 'Broker', value: 'broker' },
  { label: 'Territory', value: 'territory' },
  { label: 'Carrier', value: 'carrierMarket' },
  { label: 'Policy Period', value: 'policyPeriod' },
]

export default function CommissionsPage() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('All')
  const [exceptionState, setExceptionState] = useState('All')
  const [groupBy, setGroupBy] = useState<RollupGroupBy>('producer')
  const debouncedSearch = useDebounce(search)
  const dates = useMemo(() => defaultRollupDates(), [])

  const commissionsQuery = useExpectedCommissions({
    search: debouncedSearch,
    status,
    exceptionState,
    page: 1,
    pageSize: 30,
  })
  const rollupsQuery = useRevenueAttributionRollups({
    startDate: dates.startDate,
    endDate: dates.endDate,
    groupBy,
    status,
    exceptionState,
  })

  return (
    <DashboardLayout title="Commissions">
      <div className="grid gap-5 2xl:grid-cols-[minmax(0,1fr)_460px]">
        <section className="space-y-5" aria-labelledby="commission-workspace-heading">
          <Card>
            <CardHeader>
              <CardTitle>Commission Workspace</CardTitle>
            </CardHeader>
            <div className="grid gap-3 lg:grid-cols-[minmax(260px,1fr)_180px_190px]">
              <label className="relative block">
                <span className="sr-only">Search commission records</span>
                <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search policy, producer, broker, carrier"
                  className="h-11 w-full rounded-lg border border-surface-border bg-surface-card px-9 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
                />
              </label>
              <SelectInput label="Status" value={status} options={STATUS_OPTIONS} onChange={setStatus} />
              <SelectInput label="Exception" value={exceptionState} options={EXCEPTION_OPTIONS} onChange={setExceptionState} />
            </div>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Expected Commission Records</CardTitle>
              {commissionsQuery.data && (
                <span className="text-sm text-text-muted">{commissionsQuery.data.totalCount} records</span>
              )}
            </CardHeader>
            {commissionsQuery.isLoading && <ListSkeleton />}
            {commissionsQuery.isError && (
              commissionsQuery.error instanceof ApiError && commissionsQuery.error.status === 403
                ? <p className="py-10 text-center text-sm text-text-muted">You do not have permission to view commission records.</p>
                : <ErrorFallback message="Unable to load commission records." onRetry={() => commissionsQuery.refetch()} />
            )}
            {commissionsQuery.data && commissionsQuery.data.data.length === 0 && (
              <p className="py-10 text-center text-sm text-text-muted">No commission records match this search.</p>
            )}
            {commissionsQuery.data && commissionsQuery.data.data.length > 0 && (
              <div className="overflow-hidden rounded-lg border border-surface-border">
                <div className="hidden grid-cols-[1.3fr_1fr_1fr_120px_42px] gap-3 border-b border-surface-border bg-surface-highlight px-3 py-2 text-xs font-medium uppercase text-text-muted lg:grid">
                  <span>Policy</span>
                  <span>Expected</span>
                  <span>Adjusted</span>
                  <span>Status</span>
                  <span className="sr-only">Open</span>
                </div>
                {commissionsQuery.data.data.map((record) => (
                  <CommissionRecordRow key={record.id} record={record} />
                ))}
              </div>
            )}
          </Card>
        </section>

        <aside className="space-y-5" aria-labelledby="revenue-rollups-heading">
          <Card>
            <CardHeader>
              <CardTitle>Revenue Rollups</CardTitle>
            </CardHeader>
            <div className="mb-4">
              <SelectInput label="Group by" value={groupBy} options={GROUP_OPTIONS.map((option) => option.value)} onChange={(value) => setGroupBy(value as RollupGroupBy)} />
            </div>
            {rollupsQuery.isLoading && <ListSkeleton rows={4} />}
            {rollupsQuery.isError && <ErrorFallback message="Unable to load revenue rollups." onRetry={() => rollupsQuery.refetch()} />}
            {rollupsQuery.data && rollupsQuery.data.rows.length === 0 && (
              <p className="py-10 text-center text-sm text-text-muted">No revenue rollups are available for this scope.</p>
            )}
            {rollupsQuery.data && rollupsQuery.data.rows.length > 0 && (
              <div className="space-y-3">
                {rollupsQuery.data.rows.map((row) => (
                  <RollupRow key={row.groupKey} row={row} />
                ))}
              </div>
            )}
          </Card>
        </aside>
      </div>
    </DashboardLayout>
  )
}

function CommissionRecordRow({ record }: { record: ExpectedCommissionDto }) {
  const title = record.policyNumber ?? shortId(record.policyId)
  const context = [
    record.accountDisplayName,
    record.carrierMarketName,
    record.producerDisplayName ? `Producer ${record.producerDisplayName}` : null,
  ].filter(Boolean).join(' · ')

  return (
    <Link
      to={`/commissions/${record.id}`}
      className="grid gap-3 border-b border-surface-border px-3 py-3 last:border-b-0 hover:bg-surface-card-hover lg:grid-cols-[1.3fr_1fr_1fr_120px_42px] lg:items-center"
    >
      <div className="min-w-0">
        <p className="truncate text-sm font-medium text-text-primary">{title}</p>
        <p className="mt-1 truncate text-xs text-text-muted">{context || `Policy ${shortId(record.policyId)}`}</p>
        <p className="mt-1 truncate font-mono text-[11px] text-text-muted">{record.policyId}</p>
      </div>
      <Metric label="Expected" value={money(record.expectedGrossCommission)} />
      <Metric label="Adjusted" value={money(record.adjustedExpectedCommission)} />
      <div className="flex flex-wrap gap-2">
        <StatusBadge status={record.status} />
        {record.exceptionState !== 'None' && <Badge variant="warning">{record.exceptionState}</Badge>}
      </div>
      <span className="flex h-9 w-9 items-center justify-center rounded-lg border border-surface-border text-text-muted" aria-hidden="true">
        <ArrowUpRight size={16} />
      </span>
    </Link>
  )
}

function RollupRow({ row }: { row: RevenueAttributionRollupRowDto }) {
  return (
    <div className="rounded-lg border border-surface-border bg-surface-card p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-medium text-text-primary">{row.groupLabel}</p>
          <p className="mt-1 text-xs text-text-muted">{row.recordCount} records</p>
        </div>
        {row.exceptionCount > 0 && <Badge variant="warning">{row.exceptionCount} exceptions</Badge>}
      </div>
      <div className="mt-3 grid grid-cols-2 gap-3 text-sm">
        <Metric label="Expected" value={money(row.expectedGrossCommissionTotal)} />
        <Metric label="Adjusted" value={money(row.adjustedExpectedCommissionTotal)} />
        <Metric label="Allocated" value={money(row.producerAllocationTotal)} />
        <Metric label="Approvals" value={money(row.approvedAdjustmentTotal)} />
      </div>
    </div>
  )
}

function SelectInput<T extends string>({ label, value, options, onChange }: { label: string; value: T | string; options: readonly T[]; onChange: (value: string) => void }) {
  return (
    <label className="grid gap-1 text-xs font-medium text-text-muted">
      {label}
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="h-11 rounded-lg border border-surface-border bg-surface-card px-3 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
      >
        {options.map((option) => <option key={option} value={option}>{option}</option>)}
      </select>
    </label>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-text-muted">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-text-primary">{value}</p>
    </div>
  )
}

function StatusBadge({ status }: { status: string }) {
  const variant = status === 'Calculated' ? 'success' : status === 'Exception' ? 'error' : 'info'
  return <Badge variant={variant}>{status}</Badge>
}

function ListSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }).map((_, index) => <Skeleton key={index} className="h-16 w-full rounded-lg" />)}
    </div>
  )
}

function money(value: number | null | undefined) {
  if (value === null || value === undefined) return 'Not calculated'
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function shortId(value: string) {
  return value.slice(0, 8)
}

function defaultRollupDates() {
  const end = new Date()
  const start = new Date()
  start.setMonth(start.getMonth() - 3)
  return {
    startDate: start.toISOString().slice(0, 10),
    endDate: end.toISOString().slice(0, 10),
  }
}
