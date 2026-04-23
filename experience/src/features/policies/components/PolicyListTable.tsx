import { Link } from 'react-router-dom';
import { ArrowRight, FileText } from 'lucide-react';
import { getLineOfBusinessLabel } from '@/features/submissions';
import type { PolicyListItemDto } from '../types';
import { formatPolicyCurrency, formatPolicyDate } from '../lib/format';
import { PolicyStatusBadge } from './PolicyStatusBadge';

interface PolicyListTableProps {
  policies: PolicyListItemDto[];
}

export function PolicyListTable({ policies }: PolicyListTableProps) {
  if (policies.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-surface-border px-4 py-10 text-center">
        <FileText className="mx-auto h-8 w-8 text-text-muted" aria-hidden="true" />
        <p className="mt-3 text-sm text-text-secondary">No policies match the current filters.</p>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-surface-border">
      <div className="hidden grid-cols-[1.25fr_1.2fr_1fr_0.9fr_0.9fr_0.7fr_40px] gap-3 border-b border-surface-border bg-surface-card/70 px-4 py-2 text-xs font-medium uppercase text-text-muted lg:grid">
        <span>Policy</span>
        <span>Account</span>
        <span>Carrier</span>
        <span>Term</span>
        <span>Premium</span>
        <span>Status</span>
        <span />
      </div>
      <div className="divide-y divide-surface-border">
        {policies.map((policy) => (
          <Link
            key={policy.id}
            to={`/policies/${policy.id}`}
            className="grid gap-3 px-4 py-3 transition-colors hover:bg-surface-card/70 lg:grid-cols-[1.25fr_1.2fr_1fr_0.9fr_0.9fr_0.7fr_40px] lg:items-center"
          >
            <div>
              <p className="text-sm font-semibold text-text-primary">{policy.policyNumber}</p>
              <p className="mt-1 text-xs text-text-muted">{getLineOfBusinessLabel(policy.lineOfBusiness)}</p>
            </div>
            <div>
              <p className="text-sm text-text-primary">{policy.accountDisplayName ?? 'Unlinked account'}</p>
              {policy.accountStatus && <p className="mt-1 text-xs text-text-muted">{policy.accountStatus}</p>}
            </div>
            <p className="text-sm text-text-secondary">{policy.carrierName ?? 'Carrier unavailable'}</p>
            <p className="text-sm text-text-secondary">
              {formatPolicyDate(policy.effectiveDate)} - {formatPolicyDate(policy.expirationDate)}
            </p>
            <p className="text-sm font-medium text-text-primary">
              {formatPolicyCurrency(policy.totalPremium, policy.premiumCurrency)}
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <PolicyStatusBadge status={policy.status} />
              {policy.hasOpenRenewal && (
                <span className="rounded-full border border-status-info/35 bg-status-info/15 px-2 py-0.5 text-xs text-text-secondary">
                  Renewal
                </span>
              )}
            </div>
            <ArrowRight size={16} className="hidden justify-self-end text-text-muted lg:block" aria-hidden="true" />
          </Link>
        ))}
      </div>
    </div>
  );
}
