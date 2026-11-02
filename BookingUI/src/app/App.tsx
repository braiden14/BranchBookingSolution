import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../components/layout/AppLayout'
import { LoginPage } from '../features/auth/components/LoginPage'
import { AuthProvider } from '../features/auth/providers/AuthProvider'
import { BranchesPage } from '../features/branches/components/BranchesPage'
import { BranchConfigurationWorkspace } from '../features/branches/components/BranchConfigurationWorkspace'
import { ProtectedRoute } from './routes/ProtectedRoute'

function ComingSoon({ title }: { title: string }) {
  return <section className="empty-state"><span className="empty-state__icon">✦</span><h1>{title}</h1><p className="muted">This workspace is ready for the next feature.</p></section>
}

export default function App() {
  return <AuthProvider><BrowserRouter><Routes>
    <Route path="/login" element={<LoginPage />} />
    <Route element={<ProtectedRoute />}>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/branches" replace />} />
        <Route path="branches" element={<BranchesPage />} />
        <Route path="branches/:branchId/configuration" element={<BranchConfigurationWorkspace />} />
        <Route path="appointments" element={<ComingSoon title="Appointments" />} />
        <Route path="customers" element={<ComingSoon title="Customers" />} />
        <Route path="forbidden" element={<ComingSoon title="Access restricted" />} />
      </Route>
    </Route>
    <Route path="*" element={<Navigate to="/branches" replace />} />
  </Routes></BrowserRouter></AuthProvider>
}
