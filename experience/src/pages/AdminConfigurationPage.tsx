import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { AdminConfigurationWorkspace } from '@/features/admin-configuration'

export default function AdminConfigurationPage() {
  return (
    <DashboardLayout>
      <AdminConfigurationWorkspace />
    </DashboardLayout>
  )
}
