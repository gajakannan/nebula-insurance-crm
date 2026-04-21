import { useState } from 'react';
import { useDashboardNudges } from '../hooks/useDashboardNudges';
import { NudgeCard } from './NudgeCard';

interface NudgeCardsSectionProps {
  excludeTypes?: string[];
}

export function NudgeCardsSection({ excludeTypes = [] }: NudgeCardsSectionProps) {
  const { data, isError } = useDashboardNudges();
  const [dismissed, setDismissed] = useState<Set<string>>(new Set());

  // Silently omit entire section on error or no data
  if (isError || !data) return null;

  const visible = data.nudges.filter(
    (n) => !dismissed.has(`${n.nudgeType}-${n.linkedEntityId}`) && !excludeTypes.includes(n.nudgeType),
  );

  if (visible.length === 0) return null;

  function handleDismiss(nudgeType: string, linkedEntityId: string) {
    setDismissed((prev) => {
      const next = new Set(prev);
      next.add(`${nudgeType}-${linkedEntityId}`);
      return next;
    });
  }

  return (
    <div className="canvas-section canvas-zone-tight" aria-label="Nudge zone">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        {visible.slice(0, 3).map((nudge) => (
          <NudgeCard
            key={`${nudge.nudgeType}-${nudge.linkedEntityId}`}
            nudge={nudge}
            onDismiss={() => handleDismiss(nudge.nudgeType, nudge.linkedEntityId)}
          />
        ))}
      </div>
    </div>
  );
}
