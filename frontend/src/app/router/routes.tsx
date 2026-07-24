import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginPage } from '@/pages/Login/ui/LoginPage'
import { RegisterPage } from '@/pages/Register/ui/RegisterPage'
import { ForgotPasswordPage } from '@/pages/ForgotPassword/ui/ForgotPasswordPage'
import { ResetPasswordPage } from '@/pages/ResetPassword/ui/ResetPasswordPage'
import { CheckEmailPage } from '@/pages/CheckEmail/ui/CheckEmailPage'
import { VerifyEmailPage } from '@/pages/VerifyEmail/ui/VerifyEmailPage'
import { UnsubscribePage } from '@/pages/Unsubscribe/ui/UnsubscribePage'
import { BlockListPage } from '@/pages/BlockList/ui/BlockListPage'
import { BlockDetailPage } from '@/pages/BlockDetail/ui/BlockDetailPage'
import { SharedBlockPage } from '@/pages/SharedBlock/ui/SharedBlockPage'
import { WordImportPage } from '@/pages/WordImport/ui/WordImportPage'
import { TestListPage } from '@/pages/TestList/ui/TestListPage'
import { TestCreatePage } from '@/pages/TestCreate/ui/TestCreatePage'
import { TestRunnerPage } from '@/pages/TestRunner/ui/TestRunnerPage'
import { TestResultsPage } from '@/pages/TestResults/ui/TestResultsPage'
import { ReviewSessionPage } from '@/pages/ReviewSession/ui/ReviewSessionPage'
import { ConversationSetupPage } from '@/pages/Conversation/ui/ConversationSetupPage'
import { ConversationHistoryPage } from '@/pages/Conversation/ui/ConversationHistoryPage'
import { ConversationPage } from '@/pages/Conversation/ui/ConversationPage'
import { StatsPage } from '@/pages/Stats/ui/StatsPage'
import { DashboardPage } from '@/pages/Dashboard/ui/DashboardPage'
import { SearchResultsPage } from '@/pages/Search/ui/SearchResultsPage'
import { ProfilePage } from '@/pages/Profile/ui/ProfilePage'
import { FeedbackPage } from '@/pages/Feedback/ui/FeedbackPage'
import { AdminDashboardPage } from '@/pages/AdminDashboard/ui/AdminDashboardPage'
import { AdminUsersPage } from '@/pages/AdminUsers/ui/AdminUsersPage'
import { AdminAiMonitorPage } from '@/pages/AdminAiMonitor/ui/AdminAiMonitorPage'
import { AdminSettingsPage } from '@/pages/AdminSettings/ui/AdminSettingsPage'
import { AdminLanguagesPage } from '@/pages/AdminLanguages/ui/AdminLanguagesPage'
import { AdminAuditPage } from '@/pages/AdminAudit/ui/AdminAuditPage'
import { AdminFeedbackPage } from '@/pages/AdminFeedback/ui/AdminFeedbackPage'
import { NotFoundPage } from '@/pages/NotFound/ui/NotFoundPage'
import { AdminLayout } from '@/app/layouts/AdminLayout'
import { UserLayout } from '@/app/layouts/UserLayout'
import { AuthGuard } from './guards/AuthGuard'
import { AdminGuard } from './guards/AdminGuard'

const router = createBrowserRouter([
  { path: ROUTES.LOGIN, element: <LoginPage /> },
  { path: ROUTES.REGISTER, element: <RegisterPage /> },
  { path: ROUTES.FORGOT_PASSWORD, element: <ForgotPasswordPage /> },
  { path: ROUTES.RESET_PASSWORD, element: <ResetPasswordPage /> },
  // Outside AuthGuard: confirming an address is what gets you *to* a usable session.
  { path: ROUTES.CHECK_EMAIL, element: <CheckEmailPage /> },
  { path: ROUTES.VERIFY_EMAIL, element: <VerifyEmailPage /> },
  // Also outside AuthGuard: the link is opened from an inbox, usually on a signed-out device.
  { path: ROUTES.UNSUBSCRIBE, element: <UnsubscribePage /> },
  {
    element: <AuthGuard />,
    children: [
      {
        element: <UserLayout />,
        children: [
          { path: ROUTES.DASHBOARD, element: <DashboardPage /> },
          { path: ROUTES.BLOCKS, element: <BlockListPage /> },
          { path: '/blocks/:id', element: <BlockDetailPage /> },
          // Inside the guard: opening someone's share link requires an account, and the guard
          // remembers the link so signing in lands back here rather than on the dashboard.
          { path: ROUTES.SHARED_BLOCK_PATTERN, element: <SharedBlockPage /> },
          { path: '/blocks/:id/import', element: <WordImportPage /> },
          { path: ROUTES.TESTS, element: <TestListPage /> },
          { path: ROUTES.TEST_CREATE, element: <TestCreatePage /> },
          { path: '/tests/:id/run', element: <TestRunnerPage /> },
          { path: '/tests/:id/results', element: <TestResultsPage /> },
          { path: ROUTES.REVIEW, element: <ReviewSessionPage /> },
          { path: ROUTES.PRACTICE_CHAT, element: <ConversationSetupPage /> },
          { path: ROUTES.PRACTICE_CHAT_HISTORY, element: <ConversationHistoryPage /> },
          { path: '/practice/chat/:id', element: <ConversationPage /> },
          { path: ROUTES.STATS, element: <StatsPage /> },
          { path: ROUTES.SEARCH, element: <SearchResultsPage /> },
          { path: ROUTES.PROFILE, element: <ProfilePage /> },
          { path: ROUTES.FEEDBACK, element: <FeedbackPage /> },
          // Inside the layout so the sidebar stays available on a dead link.
          { path: '*', element: <NotFoundPage /> },
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
              { path: ROUTES.ADMIN.FEEDBACK, element: <AdminFeedbackPage /> },
            ],
          },
        ],
      },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
