import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { NudgeCardsSection } from '@/features/nudges';
import { RenewalPipelineNudgeCard } from '@/features/renewals';
import { OpportunitiesSummary } from '@/features/opportunities';
import { StaleSubmissionNudgeCard } from '@/features/submissions';
import { MyTasksWidget } from '@/features/tasks';
import { ActivityFeed } from '@/features/timeline';

export default function DashboardPage() {
  return (
    <DashboardLayout title="Dashboard" flatCanvas>
      <div
        className="mx-auto w-full"
        style={{
          maxWidth:
            'min(100%, calc(100vw - var(--sidebar-width, 0rem) - var(--chat-panel-width, 0rem) - 3rem))',
        }}
      >
        <div className="canvas-section canvas-zone-default">
        <p className="text-sm text-text-muted">Your opportunities at a glance</p>
        </div>

        <StaleSubmissionNudgeCard />
        <RenewalPipelineNudgeCard />
        <NudgeCardsSection excludeTypes={['StaleSubmission', 'UpcomingRenewal']} />
        <OpportunitiesSummary />

        <div className="canvas-section canvas-zone-break">
          <ActivityFeed />
        </div>

        <div className="canvas-section canvas-zone-break">
          <MyTasksWidget />
        </div>
      </div>
    </DashboardLayout>
  );
}
