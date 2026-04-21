import { Badge } from '@/components/ui/Badge';
import type { SubmissionCompletenessDto } from '../types';

interface SubmissionCompletenessPanelProps {
  completeness: SubmissionCompletenessDto;
}

function statusBadge(status: string) {
  switch (status) {
    case 'pass':
      return <Badge variant="success">Pass</Badge>;
    case 'missing':
      return <Badge variant="error">Missing</Badge>;
    default:
      return <Badge variant="warning">Unavailable</Badge>;
  }
}

function fieldLabel(field: string) {
  switch (field) {
    case 'AccountId':
      return 'Account';
    case 'BrokerId':
      return 'Broker';
    case 'EffectiveDate':
      return 'Effective date';
    case 'LineOfBusiness':
      return 'Line of business';
    case 'AssignedToUserId':
      return 'Assigned underwriter';
    default:
      return field;
  }
}

export function SubmissionCompletenessPanel({ completeness }: SubmissionCompletenessPanelProps) {
  const allDocumentsUnavailable =
    completeness.documentChecks.length > 0
    && completeness.documentChecks.every((check) => check.status === 'unavailable');

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant={completeness.isComplete ? 'success' : 'warning'}>
          {completeness.isComplete ? 'Ready for handoff' : 'Needs follow-up'}
        </Badge>
        {!completeness.isComplete && completeness.missingItems.length > 0 && (
          <p className="text-xs text-text-muted">
            Missing: {completeness.missingItems.join(', ')}
          </p>
        )}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <section className="space-y-2">
          <h3 className="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">
            Required Fields
          </h3>
          <div className="space-y-2">
            {completeness.fieldChecks.map((check) => (
              <div
                key={check.field}
                className="flex items-center justify-between rounded-lg border border-surface-border bg-surface-card/60 px-3 py-2"
              >
                <span className="text-sm text-text-primary">{fieldLabel(check.field)}</span>
                {statusBadge(check.status)}
              </div>
            ))}
          </div>
        </section>

        <section className="space-y-2">
          <h3 className="text-xs font-semibold uppercase tracking-[0.18em] text-text-muted">
            Document Checks
          </h3>

          {allDocumentsUnavailable && (
            <div className="rounded-lg border border-status-warning/35 bg-status-warning/15 px-3 py-2 text-sm text-text-secondary">
              Document management not yet configured.
            </div>
          )}

          <div className="space-y-2">
            {completeness.documentChecks.map((check) => (
              <div
                key={check.category}
                className="flex items-center justify-between rounded-lg border border-surface-border bg-surface-card/60 px-3 py-2"
              >
                <span className="text-sm text-text-primary">{check.category}</span>
                {statusBadge(check.status)}
              </div>
            ))}
            {completeness.documentChecks.length === 0 && (
              <div className="rounded-lg border border-surface-border bg-surface-card/60 px-3 py-2 text-sm text-text-muted">
                No document checks configured.
              </div>
            )}
          </div>
        </section>
      </div>
    </div>
  );
}
