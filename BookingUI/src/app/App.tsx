import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../components/layout/AppLayout'
import { LoginPage } from '../features/auth/components/LoginPage'
import { RegisterPage } from '../features/auth/components/RegisterPage'
import { AuthProvider } from '../features/auth/providers/AuthProvider'
import { BranchesPage } from '../features/branches/components/BranchesPage'
import { BranchConfigurationWorkspace } from '../features/branches/components/BranchConfigurationWorkspace'
import { OverviewPage } from '../features/overview/components/OverviewPage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { AppointmentsPage } from '../features/appointments/components/AppointmentsPage'

function ComingSoon({ title }: { title: string }) {
  return <section className="empty-state"><span className="empty-state__icon">✦</span><h1>{title}</h1><p className="muted">This workspace is ready for the next feature.</p></section>
}

export default function App() {
  return <AuthProvider><BrowserRouter><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route path="/register" element={<RegisterPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<AppLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="branches" element={<BranchesPage />} />
        <Route path="branches/:branchId/configuration" element={<BranchConfigurationWorkspace />} />
         <Route path="appointments" element={<AppointmentsPage />} />
        <Route path="customers" element={<ComingSoon title="Customers" />} />
        <Route path="forbidden" element={<ComingSoon title="Access restricted" />} />
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/branches" replace />} />
  </Routes></BrowserRouter></AuthProvider>
}
