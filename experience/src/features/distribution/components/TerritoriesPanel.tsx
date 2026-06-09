import { useState, type FormEvent } from 'react';
import { Skeleton } from '@/components/ui/Skeleton';
import { ErrorFallback } from '@/components/ui/ErrorFallback';
import { ApiError } from '@/services/api';
import {
  useAssignTerritoryMember,
  useCreateTerritory,
  useTerritoryAssignmentForMember,
} from '../hooks/useTerritories';
import type { MemberType } from '../types';

interface TerritoriesPanelProps {
  memberType: MemberType;
  memberId: string;
}

function assignErrorMessage(error: unknown): string {
  const code = error instanceof ApiError ? error.code : undefined;
  switch (code) {
    case 'territory_assignment_overlap':
      return 'That assignment conflicts with an existing active one.';
    case 'territory_assignment_period_invalid':
      return 'The effective date must be after the current assignment’s start date.';
    case 'precondition_failed':
      return 'The territory changed since you loaded it. Refresh and retry.';
    case 'not_found':
      return 'That territory does not exist.';
    default:
      return 'Unable to assign the territory.';
  }
}

export function TerritoriesPanel({ memberType, memberId }: TerritoriesPanelProps) {
  const lookup = useTerritoryAssignmentForMember(memberType, memberId);
  const createTerritory = useCreateTerritory();
  const assignMember = useAssignTerritoryMember();
  const [name, setName] = useState('');
  const [region, setRegion] = useState('');
  const [territoryId, setTerritoryId] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState('');

  function onCreate(event: FormEvent) {
    event.preventDefault();
    createTerritory.mutate(
      { name: name.trim(), criteria: region.trim() ? { region: region.trim() } : {} },
      {
        onSuccess: (territory) => {
          setName('');
          setRegion('');
          setTerritoryId(territory.id);
        },
      },
    );
  }

  function onAssign(event: FormEvent) {
    event.preventDefault();
    assignMember.mutate(
      { territoryId: territoryId.trim(), request: { memberType, memberId, effectiveFrom } },
      { onSuccess: () => setEffectiveFrom('') },
    );
  }

  const assignment = lookup.data?.assignment ?? null;

  return (
    <section className="space-y-3" aria-label="Territories">
      <h4 className="text-xs font-medium uppercase tracking-wide text-text-muted">Territory</h4>

      {lookup.isLoading ? (
        <Skeleton className="h-10 w-full" />
      ) : lookup.isError ? (
        <ErrorFallback message="Unable to load territory assignment." onRetry={() => lookup.refetch()} />
      ) : assignment ? (
        <div className="rounded-lg border border-surface-border p-3 text-sm">
          <p className="text-text-secondary">
            Assigned to{' '}
            <span className="font-medium text-text-primary">{assignment.territoryName ?? assignment.territoryId}</span>
          </p>
          <p className="text-xs text-text-muted">
            Effective {assignment.effectiveFrom} → {assignment.effectiveTo ?? 'open'}
          </p>
        </div>
      ) : (
        <p className="py-3 text-center text-sm text-text-muted">Not assigned to a territory.</p>
      )}

      <form onSubmit={onCreate} className="flex flex-wrap items-end gap-2 border-t border-surface-border pt-3">
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          New territory name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="territory name"
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Region
          <input
            value={region}
            onChange={(e) => setRegion(e.target.value)}
            placeholder="e.g. Northeast"
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <button
          type="submit"
          disabled={createTerritory.isPending || !name.trim()}
          className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-primary disabled:opacity-50"
        >
          {createTerritory.isPending ? 'Creating…' : 'Create territory'}
        </button>
        {createTerritory.isError && (
          <p role="alert" className="w-full text-xs text-status-error">
            {(createTerritory.error instanceof ApiError && createTerritory.error.code === 'territory_duplicate_name')
              ? 'A territory with that name already exists.'
              : 'Unable to create the territory.'}
          </p>
        )}
      </form>

      <form onSubmit={onAssign} className="flex flex-wrap items-end gap-2">
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Assign to territory id
          <input
            value={territoryId}
            onChange={(e) => setTerritoryId(e.target.value)}
            placeholder="territory id"
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <label className="flex flex-col gap-1 text-xs text-text-muted">
          Effective from
          <input
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            className="rounded-md border border-surface-border bg-surface px-2 py-1 text-sm text-text-primary"
          />
        </label>
        <button
          type="submit"
          disabled={assignMember.isPending || !territoryId.trim() || !effectiveFrom}
          className="rounded-md border border-surface-border px-3 py-1 text-sm text-text-primary disabled:opacity-50"
        >
          {assignMember.isPending ? 'Assigning…' : 'Assign member'}
        </button>
        {assignMember.isError && (
          <p role="alert" className="w-full text-xs text-status-error">
            {assignErrorMessage(assignMember.error)}
          </p>
        )}
      </form>
    </section>
  );
}
