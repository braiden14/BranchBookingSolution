import { CalendarDays, Check, ChevronDown, Clock3, Plus, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { ConfirmationDialog } from '../../../components/ui/ConfirmationDialog'
import { useToast } from '../../../components/ui/ToastProvider'
import { useAuth } from '../../auth/providers/AuthProvider'
import { appointmentService } from '../services/appointment.service'
import type { Appointment, AvailableSlot, CustomerBranch, CustomerService } from '../types/appointment.types'
import { OperationalAppointmentsPage } from './OperationalAppointmentsPage'

const localDate = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
const today = localDate(new Date())
const formatDate = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
const formatTime = (value: string) => new Intl.DateTimeFormat('en-ZA', { timeStyle: 'short' }).format(new Date(value))

export function AppointmentsPage() {
  const { user } = useAuth()
  return user?.role === 'customer' ? <CustomerAppointmentsPage /> : <OperationalAppointmentsPage />
}

function CustomerAppointmentsPage() {
  const { showToast } = useToast()
  const { user } = useAuth()
  const [history, setHistory] = useState(false)
  const [appointments, setAppointments] = useState<Appointment[]>([])
  const [branches, setBranches] = useState<CustomerBranch[]>([])
  const [services, setServices] = useState<CustomerService[]>([])
  const [slots, setSlots] = useState<AvailableSlot[]>([])
  const [branchId, setBranchId] = useState(0)
  const [branchSearch, setBranchSearch] = useState('')
  const [isBranchPickerOpen, setIsBranchPickerOpen] = useState(false)
  const [serviceId, setServiceId] = useState(0)
  const [date, setDate] = useState(today)
  const [startAt, setStartAt] = useState('')
  const [rescheduling, setRescheduling] = useState<Appointment | null>(null)
  const [showBooking, setShowBooking] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [cancellingAppointment, setCancellingAppointment] = useState<Appointment | null>(null)
  const [isCancelling, setIsCancelling] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const filteredBranches = useMemo(() => {
    const query = branchSearch.trim().toLowerCase()
    if (!query) return branches
    return branches.filter(branch => `${branch.name} ${branch.city} ${branch.code}`.toLowerCase().includes(query))
  }, [branchSearch, branches])

  const loadAppointments = async () => {
    setIsLoading(true)
    try { setAppointments(await appointmentService.getAppointments(history)); setError(null) }
    catch (loadError) { setError(loadError instanceof Error ? loadError.message : 'Could not load appointments.') }
    finally { setIsLoading(false) }
  }

  useEffect(() => {
    // AppLayout remains mounted while the auth context changes. Do not retain
    // the previous customer's filter or appointments after a new login.
    setHistory(false)
    setAppointments([])
  }, [user?.id])
  useEffect(() => { void loadAppointments() }, [history, user?.id])
  useEffect(() => { appointmentService.getBranches().then(setBranches).catch(() => setError('Could not load branches.')) }, [])
  useEffect(() => {
    setServices([]); setServiceId(0); setSlots([]); setStartAt('')
    if (branchId) void appointmentService.getServices(branchId).then(result => { setServices(result); setError(null) }).catch(error => setError(error instanceof Error ? error.message : 'Could not load services.'))
  }, [branchId])
  useEffect(() => {
    setSlots([]); setStartAt('')
    if (branchId && serviceId && date) void appointmentService.getSlots(branchId, serviceId, date, rescheduling?.appointmentId).then(result => { setSlots(result); setError(null) }).catch(error => setError(error instanceof Error ? error.message : 'Could not load available slots.'))
  }, [branchId, serviceId, date, rescheduling])

  useEffect(() => {
    if (!showBooking) return
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape') setShowBooking(false) }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [showBooking])

  const openBooking = (appointment?: Appointment) => {
    setRescheduling(appointment ?? null)
    setBranchId(appointment?.branchId ?? 0)
    const appointmentBranch = branches.find(branch => branch.branchId === appointment?.branchId)
    setBranchSearch(appointmentBranch ? `${appointmentBranch.name} — ${appointmentBranch.city}` : '')
    setIsBranchPickerOpen(false)
    setServiceId(appointment?.serviceId ?? 0)
    setDate(appointment ? appointment.startAt.slice(0, 10) : today)
    setStartAt('')
    setShowBooking(true)
  }

  const save = async () => {
    if (!branchId || !serviceId || !startAt) return
    setIsSaving(true)
    try {
      if (rescheduling) await appointmentService.reschedule(rescheduling.appointmentId, startAt)
      else await appointmentService.create(branchId, serviceId, startAt)
      showToast(rescheduling ? 'Appointment rescheduled.' : 'Appointment booked successfully.')
      setShowBooking(false); setRescheduling(null); await loadAppointments()
    } catch (saveError) { showToast(saveError instanceof Error ? saveError.message : 'The appointment could not be saved.', 'error') }
    finally { setIsSaving(false) }
  }

  const cancel = async () => {
    if (!cancellingAppointment) return
    setIsCancelling(true)
    try { await appointmentService.cancel(cancellingAppointment.appointmentId); showToast('Appointment cancelled.'); setCancellingAppointment(null); await loadAppointments() }
    catch (cancelError) { showToast(cancelError instanceof Error ? cancelError.message : 'The appointment could not be cancelled.', 'error') }
    finally { setIsCancelling(false) }
  }

  return <div className="appointments-page">
    <div className="page-heading"><div><span className="eyebrow">Customer / appointments</span><h1>{history ? 'Appointment history' : 'Your appointments'}</h1><p className="page-heading__description">Book and manage your branch appointments.</p></div>{!history && <button className="button button--primary" onClick={() => openBooking()}><Plus size={17} /> Book appointment</button>}</div>
    <div className="configuration-tabs" role="tablist"><button className={`configuration-tab ${!history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(false)}>Upcoming</button><button className={`configuration-tab ${history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(true)}>History</button></div>
    {error && <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => void loadAppointments()}>Try again</button></div>}
     {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /></div> : appointments.length ? <div className="appointment-list">{appointments.map(appointment => <article className="section-card appointment-card" key={appointment.appointmentId}><div><span className="eyebrow">{appointment.status}</span><h2>{appointment.serviceName}</h2><p>{appointment.branchName} · {formatDate(appointment.startAt)}</p><small><Clock3 size={14} /> {Math.round((new Date(appointment.endAt).getTime() - new Date(appointment.startAt).getTime()) / 60000)} minutes</small></div>{!history && <div className="appointment-actions"><button className="button button--secondary" onClick={() => openBooking(appointment)}>Reschedule</button><button className="button button--secondary" onClick={() => setCancellingAppointment(appointment)}><X size={15} /> Cancel</button></div>}</article>)}</div> : <section className="empty-state"><CalendarDays size={28} /><h2>{history ? 'No appointment history' : 'No upcoming appointments'}</h2><p>{history ? 'Completed and cancelled appointments will appear here.' : 'Book your first appointment when you are ready.'}</p></section>}
      {showBooking && <div className="dialog-backdrop"><section className="dialog dialog--booking" role="dialog" aria-modal="true" aria-labelledby="booking-dialog-title"><div className="dialog__header"><div><span className="eyebrow">{rescheduling ? 'Reschedule appointment' : 'New appointment'}</span><h2 id="booking-dialog-title">{rescheduling ? 'Choose another slot' : 'Book an appointment'}</h2><p className="muted">Choose a branch, service, and time that works for you.</p></div><button className="icon-button" onClick={() => setShowBooking(false)} aria-label="Close booking dialog"><X size={20} /></button></div><div className="login-form booking-form"><label>Branch<div className="branch-picker"><div className={`booking-input booking-input--with-icon ${isBranchPickerOpen ? 'booking-input--active' : ''}`}><Search size={16} /><input value={branchSearch} placeholder="Search by branch or city" onFocus={() => setIsBranchPickerOpen(true)} onChange={event => { setBranchSearch(event.target.value); setBranchId(0); setIsBranchPickerOpen(true) }} /><ChevronDown size={16} /></div>{isBranchPickerOpen && <div className="branch-picker__menu" role="listbox">{filteredBranches.length ? filteredBranches.map(branch => <button type="button" role="option" aria-selected={branch.branchId === branchId} key={branch.branchId} onClick={() => { setBranchId(branch.branchId); setBranchSearch(`${branch.name} — ${branch.city}`); setIsBranchPickerOpen(false) }}><span>{branch.name}</span><small>{branch.city} · {branch.code}</small></button>) : <p className="branch-picker__empty">No branches match that search.</p>}</div>}</div></label><label>Service<div className="booking-select"><select value={serviceId} onChange={event => setServiceId(Number(event.target.value))} disabled={!branchId}><option value={0}>{branchId ? 'Select a service' : 'Select a branch first'}</option>{services.map(service => <option key={service.serviceId} value={service.serviceId}>{service.name} ({service.durationMinutes} min)</option>)}</select><ChevronDown size={16} /></div></label><label>Date<input className="booking-input" type="date" min={today} value={date} onChange={event => setDate(event.target.value)} /></label>{serviceId && <label>Available slot<div className="booking-select"><select value={startAt} onChange={event => setStartAt(event.target.value)}><option value="">{slots.length ? 'Select a 15-minute slot' : 'No slots available for this date'}</option>{slots.map(slot => <option key={slot.startAt} value={slot.startAt}>{formatDate(slot.startAt)} – {formatTime(slot.endAt)}</option>)}</select><ChevronDown size={16} /></div></label>}<div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => setShowBooking(false)}>Cancel</button><button type="button" className="button button--primary" aria-busy={isSaving} disabled={isSaving || !startAt} onClick={() => void save()}>{isSaving ? 'Saving...' : <><Check size={16} />{rescheduling ? 'Reschedule' : 'Confirm booking'}</>}</button></div></div></section></div>}
     {cancellingAppointment && <ConfirmationDialog title="Cancel appointment?" description={`Cancel your ${cancellingAppointment.serviceName} appointment at ${cancellingAppointment.branchName}?`} confirmLabel="Cancel appointment" isConfirming={isCancelling} onConfirm={() => void cancel()} onClose={() => { if (!isCancelling) setCancellingAppointment(null) }} />}
   </div>
}
