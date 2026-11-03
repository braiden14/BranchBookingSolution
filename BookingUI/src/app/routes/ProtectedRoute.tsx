import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../features/auth/providers/AuthProvider'
import type { UserRole } from '../../features/auth/types/auth.types'

export function ProtectedRoute({ roles }: { roles?: UserRole[] }) {
  const { isAuthenticated, isLoading, hasRole } = useAuth()
  const location = useLocation()

  if (isLoading) return <main className="route-loading" aria-live="polite"><div className="save-loader" role="status"><span className="save-loader__spinner" />Loading your workspace…</div></main>
  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />
  if (roles && !hasRole(roles)) return <Navigate to="/forbidden" replace />

  return <Outlet />
}
