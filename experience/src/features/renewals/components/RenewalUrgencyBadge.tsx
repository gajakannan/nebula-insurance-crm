import { Badge } from '@/components/ui/Badge';
import type { RenewalUrgency } from '../types';
import { getRenewalUrgencyLabel } from '../lib/constants';

interface RenewalUrgencyBadgeProps {
  urgency: RenewalUrgency;
}

export function RenewalUrgencyBadge({ urgency }: RenewalUrgencyBadgeProps) {
  const label = getRenewalUrgencyLabel(urgency);
  if (!label) return null;

  return (
    <Badge variant={urgency === 'overdue' ? 'error' : 'warning'}>
      {label}
    </Badge>
  );
}
