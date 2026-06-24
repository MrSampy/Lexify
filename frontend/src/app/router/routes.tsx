import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { ROUTES } from '@/shared/config'
import { LoginPage } from '@/pages/Login/ui/LoginPage'
import { RegisterPage } from '@/pages/Register/ui/RegisterPage'
import { BlockListPage } from '@/pages/BlockList/ui/BlockListPage'
import { BlockDetailPage } from '@/pages/BlockDetail/ui/BlockDetailPage'
import { WordImportPage } from '@/pages/WordImport/ui/WordImportPage'
import { AuthGuard } from './guards/AuthGuard'
import { AdminGuard } from './guards/AdminGuard'

const Placeholder = ({ title }: { title: string }) => (
  <div className="flex min-h-screen items-center justify-center">
    <p className="text-muted-foreground">{title} — coming soon</p>
  </div>
)

const router = createBrowserRouter([
  { path: ROUTES.LOGIN, element: <LoginPage /> },
  { path: ROUTES.REGISTER, element: <RegisterPage /> },
  {
    element: <AuthGuard />,
    children: [
      { path: ROUTES.DASHBOARD, element: <Placeholder title="Dashboard" /> },
      { path: ROUTES.BLOCKS, element: <BlockListPage /> },
      { path: '/blocks/:id', element: <BlockDetailPage /> },
      { path: '/blocks/:id/import', element: <WordImportPage /> },
      { path: ROUTES.TESTS, element: <Placeholder title="Tests" /> },
      { path: ROUTES.REVIEW, element: <Placeholder title="Review" /> },
      {
        element: <AdminGuard />,
        children: [
          { path: ROUTES.ADMIN.DASHBOARD, element: <Placeholder title="Admin Dashboard" /> },
          { path: ROUTES.ADMIN.USERS, element: <Placeholder title="Admin Users" /> },
          { path: ROUTES.ADMIN.AI_MONITOR, element: <Placeholder title="AI Monitor" /> },
          { path: ROUTES.ADMIN.SETTINGS, element: <Placeholder title="Admin Settings" /> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to={ROUTES.DASHBOARD} replace /> },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
