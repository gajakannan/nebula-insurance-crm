import { Badge } from '@/components/ui/Badge';
import { RENEWAL_STATUS_META, getRenewalStatusLabel } from '../lib/constants';

interface RenewalStatusBadgeProps {
  status: string;
}

export function RenewalStatusBadge({ status }: RenewalStatusBadgeProps) {
  const meta = RENEWAL_STATUS_META.find((entry) => entry.value === status);

  return (
    <Badge variant={meta?.tone ?? 'default'}>
      {getRenewalStatusLabel(status)}
    </Badge>
  );
}
