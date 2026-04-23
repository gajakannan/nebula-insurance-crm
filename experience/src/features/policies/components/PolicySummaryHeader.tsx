import { Link } from 'react-router-dom';
import { Ban, CheckCircle2, FilePenLine, RotateCcw, ShieldCheck, type LucideIcon } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { getLineOfBusinessLabel } from '@/features/submissions';
import type { PolicyDto, PolicySummaryDto } from '../types';
import { formatPolicyCurrency, formatPolicyDate } from '../lib/format';
import { PolicyStatusBadge } from './PolicyStatusBadge';

interface PolicySummaryHeaderProps {
  policy: PolicyDto | PolicySummaryDto;
  onIssue?: () => void;
  onEndorse?: () => void;
  onCancel?: () => void;
  onReinstate?: () => void;
}

export function PolicySummaryHeader({
  policy,
  onIssue,
  onEndorse,
  onCancel,
  onReinstate,
}: PolicySummaryHeaderProps) {
  const accountName = 'accountDisplayName' in policy
    ? policy.accountDisplayName
    : policy.accountDisplayNameAtLink;
  const brokerName = 'brokerName' in policy ? policy.brokerName : null;

  return (
    <Card>
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <PolicyStatusBadge status={policy.status} />
            <span className="rounded-full border border-surface-border bg-surface-card px-2 py-0.5 text-xs text-text-muted">
              Version {policy.versionCount}
            </span>
            {policy.reinstatementDeadline && (
              <span className="rounded-full border border-status-warning/35 bg-status-warning/15 px-2 py-0.5 text-xs text-text-secondary">
                Reinstate by {formatPolicyDate(policy.reinstatementDeadline)}
              </span>
            )}
          </div>
          <div>
            <h2 className="text-2xl font-semibold tracking-normal text-text-primary">{policy.policyNumber}</h2>
            <p className="mt-1 text-sm text-text-secondary">
              {getLineOfBusinessLabel(policy.lineOfBusiness)} with {policy.carrierName ?? 'carrier unavailable'}
            </p>
          </div>
          <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-text-muted">
            <span>{formatPolicyDate(policy.effectiveDate)} - {formatPolicyDate(policy.expirationDate)}</span>
            <span>{formatPolicyCurrency(policy.totalPremium, policy.premiumCurrency)}</span>
            {accountName && (
              <Link to={`/accounts/${policy.accountId}`} className="text-nebula-violet hover:underline">
                {accountName}
              </Link>
            )}
            {brokerName && <span>{brokerName}</span>}
          </div>
        </div>

        <div className="flex flex-wrap gap-2 lg:justify-end">
          {policy.availableTransitions.includes('Issue') && (
            <ActionButton icon={ShieldCheck} label="Issue" onClick={onIssue} tone="primary" />
          )}
          {policy.availableTransitions.includes('Endorse') && (
            <ActionButton icon={FilePenLine} label="Endorse" onClick={onEndorse} />
          )}
          {policy.availableTransitions.includes('Cancel') && (
            <ActionButton icon={Ban} label="Cancel" onClick={onCancel} tone="danger" />
          )}
          {policy.availableTransitions.includes('Reinstate') && (
            <ActionButton icon={RotateCcw} label="Reinstate" onClick={onReinstate} />
          )}
          {policy.status === 'Issued' && !policy.availableTransitions.length && (
            <span className="inline-flex items-center gap-1 rounded-lg border border-surface-border bg-surface-card px-3 py-1.5 text-sm text-text-secondary">
              <CheckCircle2 size={15} />
              In force
            </span>
          )}
        </div>
      </div>
    </Card>
  );
}

interface ActionButtonProps {
  icon: LucideIcon;
  label: string;
  onClick?: () => void;
  tone?: 'default' | 'primary' | 'danger';
}

function ActionButton({ icon: Icon, label, onClick, tone = 'default' }: ActionButtonProps) {
  const toneClass = tone === 'primary'
    ? 'border-nebula-violet/35 bg-nebula-violet px-3 py-1.5 text-white hover:bg-nebula-violet/90'
    : tone === 'danger'
      ? 'border-status-error/35 bg-status-error/10 px-3 py-1.5 text-text-secondary hover:bg-status-error/20 hover:text-text-primary'
      : 'border-surface-border bg-surface-card px-3 py-1.5 text-text-secondary hover:bg-surface-card-hover hover:text-text-primary';

  return (
    <button
      type="button"
      onClick={onClick}
      className={`inline-flex items-center gap-1.5 rounded-lg border text-sm font-medium transition-colors ${toneClass}`}
    >
      <Icon size={15} />
      {label}
    </button>
  );
}
