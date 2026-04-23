import { Link } from 'react-router-dom';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { ActivityFeedItem } from '@/features/timeline/components/ActivityFeedItem';
import type {
  PolicyCoverageLineDto,
  PolicyEndorsementDto,
  PolicySummaryDto,
  PolicyTimelineResponse,
  PolicyVersionDto,
  PaginatedResponse,
} from '../types';
import { formatPolicyCurrency, formatPolicyDate, formatPolicyDateTime } from '../lib/format';

interface PolicyRailsProps {
  summary?: PolicySummaryDto;
  versions?: PaginatedResponse<PolicyVersionDto>;
  endorsements?: PaginatedResponse<PolicyEndorsementDto>;
  coverages?: PolicyCoverageLineDto[];
  timeline?: PolicyTimelineResponse;
}

export function PolicyRails({ summary, versions, endorsements, coverages, timeline }: PolicyRailsProps) {
  return (
    <div className="grid gap-4 xl:grid-cols-[1.25fr_0.9fr]">
      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Coverage lines</CardTitle>
            <span className="text-xs text-text-muted">{summary?.coverageLineCount ?? coverages?.length ?? 0} current</span>
          </CardHeader>
          {coverages?.length ? (
            <div className="space-y-2">
              {coverages.map((line) => (
                <div key={line.id} className="grid gap-2 rounded-lg border border-surface-border px-3 py-2 md:grid-cols-[1fr_0.7fr_0.7fr_0.7fr] md:items-center">
                  <div>
                    <p className="text-sm font-medium text-text-primary">{line.coverageName ?? line.coverageCode}</p>
                    <p className="mt-1 text-xs text-text-muted">{line.coverageCode} - version {line.versionNumber}</p>
                  </div>
                  <Metric label="Limit" value={formatPolicyCurrency(line.limit, line.premiumCurrency)} />
                  <Metric label="Deductible" value={line.deductible ? formatPolicyCurrency(line.deductible, line.premiumCurrency) : '-'} />
                  <Metric label="Premium" value={formatPolicyCurrency(line.premium, line.premiumCurrency)} />
                </div>
              ))}
            </div>
          ) : (
            <EmptyRail message="No current coverage lines." />
          )}
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Versions</CardTitle>
            <span className="text-xs text-text-muted">{versions?.totalCount ?? 0} total</span>
          </CardHeader>
          {versions?.data.length ? (
            <div className="space-y-2">
              {versions.data.map((version) => (
                <div key={version.id} className="rounded-lg border border-surface-border px-3 py-2">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <p className="text-sm font-medium text-text-primary">Version {version.versionNumber}</p>
                    <span className="text-xs text-text-muted">{formatPolicyDateTime(version.createdAt)}</span>
                  </div>
                  <p className="mt-1 text-xs text-text-secondary">
                    {version.versionReason} - {formatPolicyCurrency(version.totalPremium, version.premiumCurrency)}
                  </p>
                </div>
              ))}
            </div>
          ) : (
            <EmptyRail message="No policy versions." />
          )}
        </Card>
      </div>

      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Policy 360</CardTitle>
          </CardHeader>
          <div className="grid gap-3">
            <Metric label="Open renewals" value={String(summary?.openRenewalCount ?? 0)} />
            <Metric label="Endorsements" value={String(summary?.endorsementCount ?? 0)} />
            <Metric label="Imported by" value={summary?.importSource ?? 'manual'} />
            {summary?.predecessorPolicyId && (
              <Metric label="Predecessor" value={summary.predecessorPolicyNumber ?? summary.predecessorPolicyId} />
            )}
            {summary?.successorPolicyId && (
              <Metric
                label="Successor"
                value={summary.successorPolicyNumber ?? summary.successorPolicyId}
                to={`/policies/${summary.successorPolicyId}`}
              />
            )}
          </div>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Endorsements</CardTitle>
            <span className="text-xs text-text-muted">{endorsements?.totalCount ?? 0} total</span>
          </CardHeader>
          {endorsements?.data.length ? (
            <div className="space-y-2">
              {endorsements.data.map((endorsement) => (
                <div key={endorsement.id} className="rounded-lg border border-surface-border px-3 py-2">
                  <p className="text-sm font-medium text-text-primary">
                    Endorsement {endorsement.endorsementNumber}
                  </p>
                  <p className="mt-1 text-xs text-text-secondary">
                    {endorsement.endorsementReasonCode} - effective {formatPolicyDate(endorsement.effectiveDate)}
                  </p>
                  <p className="mt-1 text-xs text-text-muted">
                    Delta {formatPolicyCurrency(endorsement.premiumDelta, endorsement.premiumCurrency)}
                  </p>
                </div>
              ))}
            </div>
          ) : (
            <EmptyRail message="No endorsements recorded." />
          )}
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Activity</CardTitle>
          </CardHeader>
          {timeline?.data.length ? (
            <div className="space-y-3">
              {timeline.data.map((event) => <ActivityFeedItem key={event.id} event={event} />)}
            </div>
          ) : (
            <EmptyRail message="No activity yet." />
          )}
        </Card>
      </div>
    </div>
  );
}

function Metric({ label, value, to }: { label: string; value: string; to?: string }) {
  const content = (
    <>
      <span className="block text-xs font-medium uppercase text-text-muted">{label}</span>
      <span className="mt-1 block text-sm text-text-primary">{value}</span>
    </>
  );

  if (to) {
    return (
      <Link to={to} className="rounded-lg border border-surface-border px-3 py-2 hover:bg-surface-card">
        {content}
      </Link>
    );
  }

  return (
    <div className="rounded-lg border border-surface-border px-3 py-2">
      {content}
    </div>
  );
}

function EmptyRail({ message }: { message: string }) {
  return (
    <div className="rounded-lg border border-dashed border-surface-border px-3 py-6 text-center text-sm text-text-muted">
      {message}
    </div>
  );
}
