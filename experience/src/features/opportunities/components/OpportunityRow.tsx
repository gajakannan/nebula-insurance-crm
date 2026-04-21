import type { OpportunityEntityType, OpportunityStatusCountDto } from '../types';
import { OpportunityPill } from './OpportunityPill';

interface OpportunityRowProps {
  label: string;
  entityType: OpportunityEntityType;
  statuses: OpportunityStatusCountDto[];
}

export function OpportunityRow({ label, entityType, statuses }: OpportunityRowProps) {
  return (
    <div>
      <h3 className="mb-2 text-xs font-medium text-text-muted">{label}</h3>
      <div className="flex flex-wrap gap-2">
        {statuses.length === 0 ? (
          <p className="text-xs text-text-muted">No opportunities in this stage</p>
        ) : (
          statuses.map((s) => (
            <OpportunityPill
              key={s.status}
              status={s.status}
              count={s.count}
              colorGroup={s.colorGroup}
              entityType={entityType}
            />
          ))
        )}
      </div>
    </div>
  );
}
