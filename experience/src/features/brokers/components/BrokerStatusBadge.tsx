import { Badge } from '@/components/ui/Badge';
import type { BrokerStatus } from '../types';

const statusVariant: Record<BrokerStatus, 'success' | 'default' | 'warning'> = {
  Active: 'success',
  Inactive: 'default',
  Pending: 'warning',
};

interface BrokerStatusBadgeProps {
  status: BrokerStatus;
}

export function BrokerStatusBadge({ status }: BrokerStatusBadgeProps) {
  return <Badge variant={statusVariant[status]}>{status}</Badge>;
}
