import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/entities/user'
import { ROUTES } from '@/shared/config'

export function AuthGuard() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  return isAuthenticated ? <Outlet /> : <Navigate to={ROUTES.LOGIN} replace />
}
