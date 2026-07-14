import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { ThemeContext, useThemeProvider } from './hooks/useTheme'
import { useAuthEventHandler, ProtectedRoute } from './features/auth'
import DashboardPage from './pages/DashboardPage'
import BrokerListPage from './pages/BrokerListPage'
import CreateBrokerPage from './pages/CreateBrokerPage'
import BrokerDetailPage from './pages/BrokerDetailPage'
import CarrierMarketsPage from './pages/CarrierMarketsPage'
import AccountsPage from './pages/AccountsPage'
import CreateAccountPage from './pages/CreateAccountPage'
import AccountDetailPage from './pages/AccountDetailPage'
import SubmissionsPage from './pages/SubmissionsPage'
import CreateSubmissionPage from './pages/CreateSubmissionPage'
import SubmissionDetailPage from './pages/SubmissionDetailPage'
import RenewalsPage from './pages/RenewalsPage'
import RenewalDetailPage from './pages/RenewalDetailPage'
import PoliciesPage from './pages/PoliciesPage'
import CreatePolicyPage from './pages/CreatePolicyPage'
import PolicyImportPage from './pages/PolicyImportPage'
import PolicyDetailPage from './pages/PolicyDetailPage'
import CommissionsPage from './pages/CommissionsPage'
import CommissionDetailPage from './pages/CommissionDetailPage'
import TaskCenterPage from './pages/TaskCenterPage'
import ServiceCasesPage from './pages/ServiceCasesPage'
import ServiceCaseDetailPage from './pages/ServiceCaseDetailPage'
import WorkQueuesPage from './pages/WorkQueuesPage'
import SearchResultsPage from './pages/SearchResultsPage'
import OperationalReportsPage from './pages/OperationalReportsPage'
import BrokerInsightsPage from './pages/BrokerInsightsPage'
import AdminConfigurationPage from './pages/AdminConfigurationPage'
import NotFoundPage from './pages/NotFoundPage'
import { DocumentDetailView, DocumentTemplatesLibrary } from './features/documents'
import { UnauthorizedPage } from './pages/UnauthorizedPage'
import { LoginPage } from './pages/LoginPage'
import { AuthCallbackPage } from './pages/AuthCallbackPage'
import { SessionContinuityProvider } from './features/session-continuity'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
})

/**
 * AppInner is rendered inside BrowserRouter so hooks that need useNavigate
 * (useAuthEventHandler -> useSessionTeardown) are valid here.
 *
 * useAuthEventHandler subscribes to the auth event bus and triggers
 * session teardown when the API 401 interceptor fires.
 */
function AppInner() {
  useAuthEventHandler()

  return (
    <Routes>
      {/* Public routes — no session required */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />

      {/* Protected routes — valid OIDC session required */}
      <Route path="/" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
      <Route path="/accounts" element={<ProtectedRoute><AccountsPage /></ProtectedRoute>} />
      <Route path="/accounts/new" element={<ProtectedRoute><CreateAccountPage /></ProtectedRoute>} />
      <Route path="/accounts/:accountId" element={<ProtectedRoute><AccountDetailPage /></ProtectedRoute>} />
      <Route path="/submissions" element={<ProtectedRoute><SubmissionsPage /></ProtectedRoute>} />
      <Route path="/submissions/new" element={<ProtectedRoute><CreateSubmissionPage /></ProtectedRoute>} />
      <Route path="/submissions/:submissionId" element={<ProtectedRoute><SubmissionDetailPage /></ProtectedRoute>} />
      <Route path="/renewals" element={<ProtectedRoute><RenewalsPage /></ProtectedRoute>} />
      <Route path="/renewals/:renewalId" element={<ProtectedRoute><RenewalDetailPage /></ProtectedRoute>} />
      <Route path="/policies" element={<ProtectedRoute><PoliciesPage /></ProtectedRoute>} />
      <Route path="/policies/new" element={<ProtectedRoute><CreatePolicyPage /></ProtectedRoute>} />
      <Route path="/policies/import" element={<ProtectedRoute><PolicyImportPage /></ProtectedRoute>} />
      <Route path="/policies/:policyId" element={<ProtectedRoute><PolicyDetailPage /></ProtectedRoute>} />
      <Route path="/commissions" element={<ProtectedRoute><CommissionsPage /></ProtectedRoute>} />
      <Route path="/commissions/:expectedCommissionId" element={<ProtectedRoute><CommissionDetailPage /></ProtectedRoute>} />
      <Route path="/documents/:documentId" element={<ProtectedRoute><DocumentDetailView /></ProtectedRoute>} />
      <Route path="/document-templates" element={<ProtectedRoute><DocumentTemplatesLibrary /></ProtectedRoute>} />
      <Route path="/brokers" element={<ProtectedRoute><BrokerListPage /></ProtectedRoute>} />
      <Route path="/brokers/new" element={<ProtectedRoute><CreateBrokerPage /></ProtectedRoute>} />
      <Route path="/brokers/:brokerId" element={<ProtectedRoute><BrokerDetailPage /></ProtectedRoute>} />
      <Route path="/carrier-markets" element={<ProtectedRoute><CarrierMarketsPage /></ProtectedRoute>} />
      <Route path="/tasks" element={<ProtectedRoute><TaskCenterPage /></ProtectedRoute>} />
      <Route path="/tasks/:taskId" element={<ProtectedRoute><TaskCenterPage /></ProtectedRoute>} />
      <Route path="/service-cases" element={<ProtectedRoute><ServiceCasesPage /></ProtectedRoute>} />
      <Route path="/service-cases/:serviceCaseId" element={<ProtectedRoute><ServiceCaseDetailPage /></ProtectedRoute>} />
      <Route path="/work-queues" element={<ProtectedRoute><WorkQueuesPage /></ProtectedRoute>} />
      <Route path="/search" element={<ProtectedRoute><SearchResultsPage /></ProtectedRoute>} />
      <Route path="/operational-reports" element={<ProtectedRoute><OperationalReportsPage /></ProtectedRoute>} />
      <Route path="/broker-insights" element={<ProtectedRoute><BrokerInsightsPage /></ProtectedRoute>} />
      <Route path="/admin/configuration" element={<ProtectedRoute><AdminConfigurationPage /></ProtectedRoute>} />

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

function App() {
  const themeValue = useThemeProvider()

  return (
    <ThemeContext.Provider value={themeValue}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <SessionContinuityProvider>
            <AppInner />
          </SessionContinuityProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeContext.Provider>
  )
}

export default App
