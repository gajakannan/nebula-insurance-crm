import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, SlidersHorizontal } from 'lucide-react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Card } from '@/components/ui/Card';
import { Skeleton } from '@/components/ui/Skeleton';
import {
  ServiceCasePriorityBadge,
  ServiceCaseCreateModal,
  ServiceCaseStatusBadge,
  useServiceCases,
  type ServiceCasePriority,
  type ServiceCaseStatus,
} from '@/features/service-cases';
import { AssigneePicker, type UserSummaryDto } from '@/features/tasks';

const STATUSES: Array<ServiceCaseStatus | ''> = ['', 'Intake', 'InProgress', 'Waiting', 'Resolved', 'Closed'];
const PRIORITIES: Array<ServiceCasePriority | ''> = ['', 'Low', 'Medium', 'High', 'Urgent'];
const DUE_FILTERS = ['', 'overdue', 'next7'] as const;

export default function ServiceCasesPage() {
  const [status, setStatus] = useState<ServiceCaseStatus | ''>('');
  const [priority, setPriority] = useState<ServiceCasePriority | ''>('');
  const [search, setSearch] = useState('');
  const [dueFilter, setDueFilter] = useState<(typeof DUE_FILTERS)[number]>('');
  const [owner, setOwner] = useState<UserSummaryDto | null>(null);
  const [includeClosed, setIncludeClosed] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [showFilters, setShowFilters] = useState(false);
  const today = toDateOnly(new Date());
  const nextWeek = toDateOnly(addDays(new Date(), 7));
  const query = useServiceCases({
    status: status || undefined,
    priority: priority || undefined,
    ownerUserId: owner?.userId,
    q: search.trim() || undefined,
    dueBefore: dueFilter === 'overdue' ? today : dueFilter === 'next7' ? nextWeek : undefined,
    dueAfter: dueFilter === 'next7' ? today : undefined,
    includeClosed,
    page: 1,
    pageSize: 50,
  });
  const cases = query.data?.data ?? [];

  return (
    <DashboardLayout title="Service Cases">
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold text-text-primary">Service Cases</h1>
            <p className="mt-1 text-sm text-text-muted">Track servicing requests and claim-support follow-up.</p>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setShowFilters((value) => !value)}
              aria-pressed={showFilters}
              className="inline-flex items-center gap-1.5 rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-secondary hover:bg-surface-card-hover hover:text-text-primary"
            >
              <SlidersHorizontal size={14} />
              Filters
            </button>
            <button type="button" onClick={() => setCreateOpen(true)} className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-2 text-sm font-medium text-white hover:bg-nebula-violet/90">
              <Plus size={14} />
              New Case
            </button>
          </div>
        </div>

        <div className="max-w-xl">
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search case, account, policy, claim, or summary"
            className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-nebula-violet"
          />
        </div>

        {showFilters && (
          <Card className="border border-surface-border bg-surface-card/35">
            <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-4">
              <FilterSelect label="Status" value={status} onChange={(value) => setStatus(value as ServiceCaseStatus | '')} options={STATUSES} />
              <FilterSelect label="Priority" value={priority} onChange={(value) => setPriority(value as ServiceCasePriority | '')} options={PRIORITIES} />
              <FilterSelect label="Due" value={dueFilter} onChange={(value) => setDueFilter(value as (typeof DUE_FILTERS)[number])} options={[...DUE_FILTERS]} labels={{ overdue: 'Overdue', next7: 'Next 7 days' }} />
              <AssigneePicker label="Owner" selectedUser={owner} onSelect={setOwner} />
              <label className="flex items-center gap-2 pt-6 text-sm text-text-secondary">
                <input type="checkbox" checked={includeClosed} onChange={(event) => setIncludeClosed(event.target.checked)} className="h-4 w-4 rounded border-surface-border bg-surface-card accent-nebula-violet" />
                Include closed
              </label>
            </div>
          </Card>
        )}

        <Card>
          {query.isLoading && <Skeleton className="h-72 w-full" />}
          {query.isError && (
            <div className="rounded-lg border border-status-error/35 bg-status-error/10 px-3 py-3 text-sm text-text-secondary">
              Unable to load service cases.
            </div>
          )}
          {!query.isLoading && !query.isError && cases.length === 0 && (
            <div className="rounded-lg border border-surface-border px-3 py-6 text-center text-sm text-text-muted">
              No service cases match the current view.
            </div>
          )}
          {!query.isLoading && !query.isError && cases.length > 0 && (
            <div className="overflow-hidden rounded-lg border border-surface-border">
              <table className="w-full text-left text-sm">
                <thead className="border-b border-surface-border bg-surface-card">
                  <tr className="text-xs uppercase text-text-muted">
                    <th className="px-3 py-2 font-medium">Case</th>
                    <th className="px-3 py-2 font-medium">Account</th>
                    <th className="px-3 py-2 font-medium">Policy</th>
                    <th className="px-3 py-2 font-medium">Owner</th>
                    <th className="px-3 py-2 font-medium">Status</th>
                    <th className="px-3 py-2 font-medium">Priority</th>
                    <th className="px-3 py-2 font-medium">Due</th>
                    <th className="px-3 py-2 font-medium">Claim</th>
                    <th className="px-3 py-2 font-medium">Activity</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-border">
                  {cases.map((serviceCase) => (
                    <tr key={serviceCase.id} className="hover:bg-surface-card">
                      <td className="px-3 py-3">
                        <Link to={`/service-cases/${serviceCase.id}`} className="font-medium text-text-primary hover:text-nebula-violet">
                          {serviceCase.caseNumber}
                        </Link>
                        <p className="mt-1 max-w-[18rem] truncate text-xs text-text-muted">{serviceCase.summary}</p>
                      </td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.accountDisplayName ?? 'Account'}</td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.policyNumber ?? 'Not linked'}</td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.ownerDisplayName ?? serviceCase.ownerUserId}</td>
                      <td className="px-3 py-3"><ServiceCaseStatusBadge status={serviceCase.status} /></td>
                      <td className="px-3 py-3"><ServiceCasePriorityBadge priority={serviceCase.priority} /></td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.dueDate ? formatDate(serviceCase.dueDate) : 'Unscheduled'}</td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.hasClaimReference ? 'Linked' : 'None'}</td>
                      <td className="px-3 py-3 text-text-secondary">{serviceCase.lastActivityAt ? formatDateTime(serviceCase.lastActivityAt) : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
        <ServiceCaseCreateModal open={createOpen} onClose={() => setCreateOpen(false)} onCreated={() => setCreateOpen(false)} />
      </div>
    </DashboardLayout>
  );
}

function FilterSelect({ label, value, onChange, options, labels = {} }: { label: string; value: string; onChange: (value: string) => void; options: string[]; labels?: Record<string, string> }) {
  return (
    <label className="block space-y-1.5">
      <span className="block text-xs font-medium text-text-secondary">{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)} className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet">
        {options.map((option) => (
          <option key={option || 'all'} value={option}>{option ? labels[option] ?? option : 'All'}</option>
        ))}
      </select>
    </label>
  );
}

function formatDate(value: string): string {
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleDateString();
}

function addDays(value: Date, days: number): Date {
  const date = new Date(value);
  date.setDate(date.getDate() + days);
  return date;
}

function toDateOnly(value: Date): string {
  return value.toISOString().slice(0, 10);
}
