import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginPage } from '@/pages/Login/ui/LoginPage'
import { RegisterPage } from '@/pages/Register/ui/RegisterPage'
import { ForgotPasswordPage } from '@/pages/ForgotPassword/ui/ForgotPasswordPage'
import { ResetPasswordPage } from '@/pages/ResetPassword/ui/ResetPasswordPage'
import { BlockListPage } from '@/pages/BlockList/ui/BlockListPage'
import { BlockDetailPage } from '@/pages/BlockDetail/ui/BlockDetailPage'
import { WordImportPage } from '@/pages/WordImport/ui/WordImportPage'
import { TestListPage } from '@/pages/TestList/ui/TestListPage'
import { TestCreatePage } from '@/pages/TestCreate/ui/TestCreatePage'
import { TestRunnerPage } from '@/pages/TestRunner/ui/TestRunnerPage'
import { TestResultsPage } from '@/pages/TestResults/ui/TestResultsPage'
import { ReviewSessionPage } from '@/pages/ReviewSession/ui/ReviewSessionPage'
import { DashboardPage } from '@/pages/Dashboard/ui/DashboardPage'
import { SearchResultsPage } from '@/pages/Search/ui/SearchResultsPage'
import { ProfilePage } from '@/pages/Profile/ui/ProfilePage'
import { AdminDashboardPage } from '@/pages/AdminDashboard/ui/AdminDashboardPage'
import { AdminUsersPage } from '@/pages/AdminUsers/ui/AdminUsersPage'
import { AdminAiMonitorPage } from '@/pages/AdminAiMonitor/ui/AdminAiMonitorPage'
import { AdminSettingsPage } from '@/pages/AdminSettings/ui/AdminSettingsPage'
import { AdminLanguagesPage } from '@/pages/AdminLanguages/ui/AdminLanguagesPage'
import { AdminAuditPage } from '@/pages/AdminAudit/ui/AdminAuditPage'
import { AdminLayout } from '@/app/layouts/AdminLayout'
import { UserLayout } from '@/app/layouts/UserLayout'
import { AuthGuard } from './guards/AuthGuard'
import { AdminGuard } from './guards/AdminGuard'

const router = createBrowserRouter([
  { path: ROUTES.LOGIN, element: <LoginPage /> },
  { path: ROUTES.REGISTER, element: <RegisterPage /> },
  { path: ROUTES.FORGOT_PASSWORD, element: <ForgotPasswordPage /> },
  { path: ROUTES.RESET_PASSWORD, element: <ResetPasswordPage /> },
  {
    element: <AuthGuard />,
    children: [
      {
        element: <UserLayout />,
        children: [
          { path: ROUTES.DASHBOARD, element: <DashboardPage /> },
          { path: ROUTES.BLOCKS, element: <BlockListPage /> },
          { path: '/blocks/:id', element: <BlockDetailPage /> },
          { path: '/blocks/:id/import', element: <WordImportPage /> },
          { path: ROUTES.TESTS, element: <TestListPage /> },
          { path: ROUTES.TEST_CREATE, element: <TestCreatePage /> },
          { path: '/tests/:id/run', element: <TestRunnerPage /> },
          { path: '/tests/:id/results', element: <TestResultsPage /> },
          { path: ROUTES.REVIEW, element: <ReviewSessionPage /> },
          { path: ROUTES.SEARCH, element: <SearchResultsPage /> },
          { path: ROUTES.PROFILE, element: <ProfilePage /> },
        ],
      },
      {
        element: <AdminGuard />,
        children: [
          {
            element: <AdminLayout />,
            children: [
              { path: ROUTES.ADMIN.DASHBOARD, element: <AdminDashboardPage /> },
              { path: ROUTES.ADMIN.USERS, element: <AdminUsersPage /> },
              { path: ROUTES.ADMIN.AI_MONITOR, element: <AdminAiMonitorPage /> },
              { path: ROUTES.ADMIN.SETTINGS, element: <AdminSettingsPage /> },
              { path: ROUTES.ADMIN.LANGUAGES, element: <AdminLanguagesPage /> },
              { path: ROUTES.ADMIN.AUDIT, element: <AdminAuditPage /> },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to={ROUTES.DASHBOARD} replace /> },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
