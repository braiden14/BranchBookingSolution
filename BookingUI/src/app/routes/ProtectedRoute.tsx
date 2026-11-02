import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../features/auth/providers/AuthProvider'
import type { UserRole } from '../../features/auth/types/auth.types'

export function ProtectedRoute({ roles }: { roles?: UserRole[] }) {
  const { isAuthenticated, isLoading, hasRole } = useAuth()
  const location = useLocation()

  if (isLoading) return null
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />
  if (roles && !hasRole(roles)) return <Navigate to="/forbidden" replace />

  return <Outlet />
}
