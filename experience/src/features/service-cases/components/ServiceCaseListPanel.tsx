import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Plus } from 'lucide-react';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Skeleton } from '@/components/ui/Skeleton';
import { useServiceCases } from '../hooks';
import { ServiceCaseCreateModal } from './ServiceCaseCreateModal';
import { ServiceCasePriorityBadge, ServiceCaseStatusBadge } from './ServiceCaseBadges';

interface Props {
  accountId: string;
  policyId?: string | null;
  title?: string;
}

export function ServiceCaseListPanel({ accountId, policyId, title = 'Service cases' }: Props) {
  const navigate = useNavigate();
  const [createOpen, setCreateOpen] = useState(false);
  const query = useServiceCases(policyId ? { policyId, page: 1, pageSize: 10 } : { accountId, page: 1, pageSize: 10 });
  const cases = query.data?.data ?? [];

  return (
    <Card className="border border-surface-border bg-surface-card/35">
      <CardHeader>
        <div className="flex items-center justify-between gap-3">
          <CardTitle>{title}</CardTitle>
          <button type="button" onClick={() => setCreateOpen(true)} className="inline-flex items-center gap-1.5 rounded-lg bg-nebula-violet px-3 py-1.5 text-sm font-medium text-white hover:bg-nebula-violet/90">
            <Plus size={14} />
            New Case
          </button>
        </div>
      </CardHeader>

      <div className="space-y-3">
        {query.isLoading && <Skeleton className="h-28 w-full" />}
        {!query.isLoading && cases.length === 0 && (
          <div className="rounded-lg border border-surface-border px-3 py-4 text-sm text-text-muted">
            No service cases recorded.
          </div>
        )}
        {cases.map((serviceCase) => (
          <Link key={serviceCase.id} to={`/service-cases/${serviceCase.id}`} className="block rounded-lg border border-surface-border px-3 py-3 transition-colors hover:bg-surface-card">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div>
                <p className="text-sm font-medium text-text-primary">{serviceCase.caseNumber}</p>
                <p className="mt-1 text-sm text-text-secondary">{serviceCase.summary}</p>
              </div>
              <div className="flex gap-1.5">
                <ServiceCaseStatusBadge status={serviceCase.status} />
                <ServiceCasePriorityBadge priority={serviceCase.priority} />
              </div>
            </div>
            <p className="mt-2 text-xs text-text-muted">
              {serviceCase.dueDate ? `Due ${formatDate(serviceCase.dueDate)}` : 'No due date'} · {serviceCase.type}
            </p>
          </Link>
        ))}
      </div>

      <ServiceCaseCreateModal
        open={createOpen}
        accountId={accountId}
        policyId={policyId}
        onClose={() => setCreateOpen(false)}
        onCreated={(serviceCaseId) => navigate(`/service-cases/${serviceCaseId}`)}
      />
    </Card>
  );
}

function formatDate(value: string): string {
  return new Date(`${value}T00:00:00`).toLocaleDateString();
}
