import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { BrokerInsightsWorkspace } from '@/features/broker-insights';

export default function BrokerInsightsPage() {
  return (
    <DashboardLayout title="Broker insights">
      <BrokerInsightsWorkspace />
    </DashboardLayout>
  );
}
