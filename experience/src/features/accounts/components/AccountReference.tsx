import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/Badge';
import type { AccountStatus } from '../types';

interface AccountReferenceProps {
  accountId: string | null;
  displayName: string;
  status: AccountStatus | string;
  survivorAccountId?: string | null;
  survivorName?: string | null;
  className?: string;
}

export function AccountReference({
  accountId,
  displayName,
  status,
  survivorAccountId,
  survivorName,
  className,
}: AccountReferenceProps) {
  const normalizedStatus = status as AccountStatus;
  const label = normalizedStatus === 'Merged' && survivorName
    ? `${displayName} → ${survivorName}`
    : displayName;

  if (normalizedStatus === 'Deleted') {
    return (
      <span className={className}>
        <span className="font-medium text-text-primary">{displayName}</span>
        <Badge variant="error" className="ml-2">Deleted</Badge>
      </span>
    );
  }

  if (normalizedStatus === 'Merged' && survivorAccountId) {
    return (
      <span className={className}>
        <Link to={`/accounts/${survivorAccountId}`} className="font-medium text-text-primary hover:text-nebula-violet">
          {label}
        </Link>
        <Badge variant="warning" className="ml-2">Merged</Badge>
      </span>
    );
  }

  if (accountId) {
    return (
      <Link to={`/accounts/${accountId}`} className={className ?? 'font-medium text-text-primary hover:text-nebula-violet'}>
        {displayName}
      </Link>
    );
  }

  return <span className={className}>{displayName}</span>;
}

export function AccountStatusBadge({ status }: { status: AccountStatus | string }) {
  const normalizedStatus = status as AccountStatus;
  const variant = normalizedStatus === 'Active'
    ? 'success'
    : normalizedStatus === 'Inactive'
      ? 'default'
      : normalizedStatus === 'Merged'
        ? 'warning'
        : 'error';

  return <Badge variant={variant}>{normalizedStatus}</Badge>;
}
