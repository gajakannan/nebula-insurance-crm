import { Badge } from '@/components/ui/Badge';
import type { PolicyStatus } from '../types';
import { POLICY_STATUS_META } from '../lib/format';

interface PolicyStatusBadgeProps {
  status: PolicyStatus | string;
}

export function PolicyStatusBadge({ status }: PolicyStatusBadgeProps) {
  const meta = POLICY_STATUS_META[status as PolicyStatus] ?? { label: status, tone: 'default' as const };

  return (
    <Badge variant={meta.tone}>
      {meta.label}
    </Badge>
  );
}
