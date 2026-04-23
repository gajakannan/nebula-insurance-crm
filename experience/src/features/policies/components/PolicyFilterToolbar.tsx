import { Search, SlidersHorizontal } from 'lucide-react';
import { TextInput } from '@/components/ui/TextInput';
import { LINE_OF_BUSINESS_OPTIONS } from '@/features/submissions';
import type { PolicyStatus } from '../types';

const STATUS_OPTIONS: Array<{ value: '' | PolicyStatus; label: string }> = [
  { value: '', label: 'All statuses' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Issued', label: 'Issued' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: 'Expired', label: 'Expired' },
];

const SORT_OPTIONS = [
  { value: 'expirationDate:asc', label: 'Expiration ascending' },
  { value: 'expirationDate:desc', label: 'Expiration descending' },
  { value: 'effectiveDate:asc', label: 'Effective ascending' },
  { value: 'status:asc', label: 'Status' },
  { value: 'premium:desc', label: 'Premium high to low' },
];

interface PolicyFilterToolbarProps {
  query: string;
  status: '' | PolicyStatus;
  lineOfBusiness: string;
  sort: string;
  onQueryChange: (value: string) => void;
  onStatusChange: (value: '' | PolicyStatus) => void;
  onLineOfBusinessChange: (value: string) => void;
  onSortChange: (value: string) => void;
}

export function PolicyFilterToolbar({
  query,
  status,
  lineOfBusiness,
  sort,
  onQueryChange,
  onStatusChange,
  onLineOfBusinessChange,
  onSortChange,
}: PolicyFilterToolbarProps) {
  return (
    <div className="grid gap-3 lg:grid-cols-[minmax(220px,1.4fr)_repeat(3,minmax(160px,1fr))]">
      <div className="space-y-1.5">
        <span className="flex items-center gap-1.5 text-xs font-medium text-text-secondary">
          <Search size={14} />
          Search
        </span>
        <TextInput
          label="Search"
          aria-label="Search policies"
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
          placeholder="Policy, account, carrier"
        />
      </div>

      <label className="space-y-1.5">
        <span className="block text-xs font-medium text-text-secondary">Status</span>
        <select
          aria-label="Filter policies by status"
          value={status}
          onChange={(event) => onStatusChange(event.target.value as '' | PolicyStatus)}
          className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
        >
          {STATUS_OPTIONS.map((option) => (
            <option key={option.label} value={option.value}>{option.label}</option>
          ))}
        </select>
      </label>

      <label className="space-y-1.5">
        <span className="block text-xs font-medium text-text-secondary">Line</span>
        <select
          aria-label="Filter policies by line of business"
          value={lineOfBusiness}
          onChange={(event) => onLineOfBusinessChange(event.target.value)}
          className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
        >
          <option value="">All lines</option>
          {LINE_OF_BUSINESS_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
      </label>

      <label className="space-y-1.5">
        <span className="flex items-center gap-1.5 text-xs font-medium text-text-secondary">
          <SlidersHorizontal size={14} />
          Sort
        </span>
        <select
          aria-label="Sort policies"
          value={sort}
          onChange={(event) => onSortChange(event.target.value)}
          className="w-full rounded-lg border border-surface-border bg-surface-card px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-1 focus:ring-nebula-violet"
        >
          {SORT_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
      </label>
    </div>
  );
}
