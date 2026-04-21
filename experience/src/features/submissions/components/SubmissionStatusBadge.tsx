import { Badge } from '@/components/ui/Badge';
import { SUBMISSION_STATUS_META, getSubmissionStatusLabel } from '../lib/constants';

interface SubmissionStatusBadgeProps {
  status: string;
}

export function SubmissionStatusBadge({ status }: SubmissionStatusBadgeProps) {
  const meta = SUBMISSION_STATUS_META.find((entry) => entry.value === status);

  return (
    <Badge variant={meta?.tone ?? 'default'}>
      {getSubmissionStatusLabel(status)}
    </Badge>
  );
}
