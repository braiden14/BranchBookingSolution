import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../auth/providers/AuthProvider'
import { getOperationalAppointments } from '../services/operational-appointment.service'
import type { OperationalAppointment, PagedOperationalAppointments } from '../types/operational-appointment.types'

const formatDate = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export function OperationalAppointmentsPage() {
  const { user } = useAuth()
  const canViewHistory = user?.role === 'admin' || user?.role === 'branch-manager'
  const [history, setHistory] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [page, setPage] = useState<PagedOperationalAppointments | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [retryKey, setRetryKey] = useState(0)

  useEffect(() => {
    if (!canViewHistory && history) setHistory(false)
    setPageNumber(1)
  }, [history, user?.id, canViewHistory])

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    getOperationalAppointments(history, pageNumber)
      .then(result => { if (isCurrent) { setPage(result); setError(null) } })
      .catch(loadError => { if (isCurrent) setError(loadError instanceof Error ? loadError.message : 'Could not load appointments.') })
      .finally(() => { if (isCurrent) setIsLoading(false) })
    return () => { isCurrent = false }
  }, [history, pageNumber, user?.id, retryKey])

  return <div className="appointments-page">
    <div className="page-heading"><div><span className="eyebrow">Operations / appointments</span><h1>{history ? 'Appointment history' : 'Upcoming appointments'}</h1><p className="page-heading__description">{user?.role === 'admin' ? 'Review appointments across every branch.' : 'Review appointments for your assigned branch.'}</p></div></div>
     <div className="configuration-tabs" role="tablist"><button className={`configuration-tab ${!history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(false)}>Upcoming</button>{canViewHistory && <button className={`configuration-tab ${history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(true)}>History</button>}</div>
     {error && <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => setRetryKey(current => current + 1)}>Try again</button></div>}
     {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /></div> : !error && <AppointmentTable appointments={page?.items ?? []} history={history} />}
    {page && <div className="table-pagination"><span>{page.totalCount ? `Showing ${(page.pageNumber - 1) * page.pageSize + 1}–${Math.min(page.pageNumber * page.pageSize, page.totalCount)} of ${page.totalCount}` : 'No appointments'}</span><div><button className="icon-button" disabled={page.pageNumber <= 1} onClick={() => setPageNumber(current => current - 1)} aria-label="Previous page"><ChevronLeft size={16} /></button><span>Page {page.pageNumber} of {Math.max(page.totalPages, 1)}</span><button className="icon-button" disabled={page.pageNumber >= page.totalPages} onClick={() => setPageNumber(current => current + 1)} aria-label="Next page"><ChevronRight size={16} /></button></div></div>}
  </div>
}

function AppointmentTable({ appointments, history }: { appointments: OperationalAppointment[]; history: boolean }) {
  if (!appointments.length) return <section className="empty-state"><CalendarDays size={28} /><h2>{history ? 'No appointment history' : 'No upcoming appointments'}</h2><p>No appointments match this view.</p></section>
   return <div className="section-card branch-table-card"><div className="branch-table-wrapper"><table className="branch-table operational-table"><thead><tr><th>Customer</th><th>Branch</th><th>Service</th><th>Date and time</th><th>Status</th></tr></thead><tbody>{appointments.map(appointment => <tr key={appointment.appointmentId}><td><div className="branch-table__name"><div><strong>{appointment.customerName}</strong><span>{appointment.customerEmail}</span></div></div></td><td>{appointment.branchName}</td><td>{appointment.serviceName}</td><td>{formatDate(appointment.startAt)}</td><td><span className={`status-badge ${appointment.status === 'Confirmed' ? 'status-badge--active' : 'status-badge--inactive'}`}>{appointment.status === 'Confirmed' ? 'Confirmed' : appointment.status}</span></td></tr>)}</tbody></table></div></div>
}
