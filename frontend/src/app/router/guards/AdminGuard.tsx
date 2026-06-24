import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/entities/user'
import { ROUTES } from '@/shared/config'

export function AdminGuard() {
  const role = useAuthStore((s) => s.user?.role)
  const allowed = role === 'admin' || role === 'moderator'
  return allowed ? <Outlet /> : <Navigate to={ROUTES.DASHBOARD} replace />
}
