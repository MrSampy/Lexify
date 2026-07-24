import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/entities/user'
import { ROUTES } from '@/shared/config'

export function AuthGuard() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const location = useLocation()

  // Carry the attempted URL through the sign-in detour. Without it, following a share link while
  // signed out drops you on the dashboard with no way back to the link you were sent.
  return isAuthenticated ? (
    <Outlet />
  ) : (
    <Navigate to={ROUTES.LOGIN} state={{ from: location }} replace />
  )
}
