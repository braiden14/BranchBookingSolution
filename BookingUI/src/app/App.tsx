import { BrowserRouter, Link, Navigate, Route, Routes } from 'react-router-dom'
import type { ReactNode } from 'react'
import { AppLayout } from '../components/layout/AppLayout'
import { LoginPage } from '../features/auth/components/LoginPage'
import { RegisterPage } from '../features/auth/components/RegisterPage'
import { AcceptInvitationPage } from '../features/auth/components/AcceptInvitationPage'
import { AuthProvider } from '../features/auth/providers/AuthProvider'
import { BranchesPage } from '../features/branches/components/BranchesPage'
import { BranchConfigurationWorkspace } from '../features/branches/components/BranchConfigurationWorkspace'
import { OverviewPage } from '../features/overview/components/OverviewPage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { AppointmentsPage } from '../features/appointments/components/AppointmentsPage'
import { CustomersPage } from '../features/customers/components/CustomersPage'
import { useAuth } from '../features/auth/providers/AuthProvider'
import { StaffPage } from '../features/staff/components/StaffPage'

function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={user?.role === 'customer' ? '/appointments' : '/overview'} replace />
}

function ForbiddenPage() {
  const { user } = useAuth()
  const home = user?.role === 'customer' ? '/appointments' : '/overview'
  return <section className="empty-state"><span className="empty-state__icon">!</span><h1>Access restricted</h1><p className="muted">You do not have permission to view this page.</p><Link className="button button--primary" to={home}>Return to your workspace</Link></section>
}

function AdminOnly({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  return user?.role === 'admin' ? <>{children}</> : <Navigate to="/forbidden" replace />
}

export default function App() {
  return <AuthProvider><BrowserRouter><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route path="/accept-invitation" element={<AcceptInvitationPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<AppLayout />}>
        <Route index element={<HomeRedirect />} />
        <Route path="overview" element={<ProtectedRoute roles={['admin', 'branch-manager', 'staff']} />}>
          <Route index element={<OverviewPage />} />
        </Route>
        <Route element={<ProtectedRoute roles={['admin', 'branch-manager', 'staff']} />}>
          <Route path="branches" element={<BranchesPage />} />
          <Route path="branches/:branchId/configuration" element={<BranchConfigurationWorkspace />} />
        </Route>
         <Route path="appointments" element={<AppointmentsPage />} />
         <Route path="customers" element={<AdminOnly><CustomersPage /></AdminOnly>} />
         <Route path="staff" element={<AdminOnly><StaffPage /></AdminOnly>} />
        <Route path="forbidden" element={<ForbiddenPage />} />
        <Route path="*" element={<HomeRedirect />} />
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/login" replace />} />
  </Routes></BrowserRouter></AuthProvider>
}
