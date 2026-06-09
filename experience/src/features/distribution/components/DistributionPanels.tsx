import type { ReactNode } from 'react';
import { HierarchyPanel } from './HierarchyPanel';
import { OwnershipPanel } from './OwnershipPanel';
import { TerritoriesPanel } from './TerritoriesPanel';
import type { MemberType, ScopeType } from '../types';

interface DistributionPanelsProps {
  /** Distribution-hierarchy node id for the entity being viewed (e.g. the broker's node). */
  nodeId: string;
  /** Ownership scope. Defaults to a broker-relationship scope keyed on the node id. */
  scopeType?: ScopeType;
  scopeId?: string;
  /** Territory member. Defaults to the node treated as a Broker member. */
  memberType?: MemberType;
  memberId?: string;
}

function PanelCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="rounded-xl border border-surface-border bg-surface p-4">
      <h3 className="mb-3 text-sm font-semibold text-text-primary">{title}</h3>
      {children}
    </div>
  );
}

export function DistributionPanels({
  nodeId,
  scopeType = 'BrokerRelationship',
  scopeId,
  memberType = 'Broker',
  memberId,
}: DistributionPanelsProps) {
  return (
    <div className="space-y-4">
      <PanelCard title="Hierarchy">
        <HierarchyPanel nodeId={nodeId} />
      </PanelCard>
      <PanelCard title="Ownership">
        <OwnershipPanel scopeType={scopeType} scopeId={scopeId ?? nodeId} />
      </PanelCard>
      <PanelCard title="Territories">
        <TerritoriesPanel memberType={memberType} memberId={memberId ?? nodeId} />
      </PanelCard>
    </div>
  );
}
