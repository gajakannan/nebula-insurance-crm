import { Select } from '@/components/ui/Select';
import type { GlobalSearchFacets } from '../types';

export interface SearchFilterState {
  objectType: string;
  status: string;
  region: string;
  lineOfBusiness: string;
  sort: string;
}

interface SearchFiltersProps {
  filters: SearchFilterState;
  facets?: GlobalSearchFacets;
  onChange: (key: keyof SearchFilterState, value: string) => void;
}

const SORT_OPTIONS = [
  { value: 'relevance', label: 'Relevance' },
  { value: 'updated', label: 'Last updated' },
  { value: 'title', label: 'Title (A–Z)' },
];

function toOptions(buckets: { key: string; label: string | null; count: number }[] | undefined, allLabel: string) {
  const opts = (buckets ?? []).map((b) => ({ value: b.key, label: `${b.label ?? b.key} (${b.count})` }));
  return [{ value: '', label: allLabel }, ...opts];
}

export function SearchFilters({ filters, facets, onChange }: SearchFiltersProps) {
  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
      <Select
        label="Object type"
        value={filters.objectType}
        onChange={(e) => onChange('objectType', e.target.value)}
        options={toOptions(facets?.objectTypes, 'All types')}
      />
      <Select
        label="Status"
        value={filters.status}
        onChange={(e) => onChange('status', e.target.value)}
        options={toOptions(facets?.statuses, 'Any status')}
      />
      <Select
        label="Region"
        value={filters.region}
        onChange={(e) => onChange('region', e.target.value)}
        options={toOptions(facets?.regions, 'Any region')}
      />
      <Select
        label="Line of business"
        value={filters.lineOfBusiness}
        onChange={(e) => onChange('lineOfBusiness', e.target.value)}
        options={toOptions(facets?.linesOfBusiness, 'Any LOB')}
      />
      <Select
        label="Sort"
        value={filters.sort}
        onChange={(e) => onChange('sort', e.target.value)}
        options={SORT_OPTIONS}
      />
    </div>
  );
}
