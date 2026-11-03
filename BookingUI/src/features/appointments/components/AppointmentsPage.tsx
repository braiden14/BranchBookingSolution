import { CalendarDays, Check, ChevronDown, Clock3, Plus, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useToast } from '../../../components/ui/ToastProvider'
import { useAuth } from '../../auth/providers/AuthProvider'
import { appointmentService } from '../services/appointment.service'
import type { Appointment, AvailableSlot, CustomerBranch, CustomerService } from '../types/appointment.types'

const today = new Date().toISOString().slice(0, 10)
const formatDate = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export function AppointmentsPage() {
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
    if (branchId) void appointmentService.getServices(branchId).then(setServices).catch(error => setError(error instanceof Error ? error.message : 'Could not load services.'))
  }, [branchId])
  useEffect(() => {
    setSlots([]); setStartAt('')
    if (branchId && serviceId && date) void appointmentService.getSlots(branchId, serviceId, date, rescheduling?.appointmentId).then(setSlots).catch(error => setError(error instanceof Error ? error.message : 'Could not load available slots.'))
  }, [branchId, serviceId, date, rescheduling])

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

  const cancel = async (appointment: Appointment) => {
    if (!window.confirm('Cancel this appointment?')) return
    try { await appointmentService.cancel(appointment.appointmentId); showToast('Appointment cancelled.'); await loadAppointments() }
    catch (cancelError) { showToast(cancelError instanceof Error ? cancelError.message : 'The appointment could not be cancelled.', 'error') }
  }

  return <div className="appointments-page">
    <div className="page-heading"><div><span className="eyebrow">Customer / appointments</span><h1>{history ? 'Appointment history' : 'Your appointments'}</h1><p className="page-heading__description">Book and manage your branch appointments.</p></div>{!history && <button className="button button--primary" onClick={() => openBooking()}><Plus size={17} /> Book appointment</button>}</div>
    <div className="configuration-tabs" role="tablist"><button className={`configuration-tab ${!history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(false)}>Upcoming</button><button className={`configuration-tab ${history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(true)}>History</button></div>
    {error && <div className="error-state"><p>{error}</p></div>}
    {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /></div> : appointments.length ? <div className="appointment-list">{appointments.map(appointment => <article className="section-card appointment-card" key={appointment.appointmentId}><div><span className="eyebrow">{appointment.status}</span><h2>{appointment.serviceName}</h2><p>{appointment.branchName} · {formatDate(appointment.startAt)}</p><small><Clock3 size={14} /> {Math.round((new Date(appointment.endAt).getTime() - new Date(appointment.startAt).getTime()) / 60000)} minutes</small></div>{!history && <div className="appointment-actions"><button className="button button--secondary" onClick={() => openBooking(appointment)}>Reschedule</button><button className="button button--secondary" onClick={() => void cancel(appointment)}><X size={15} /> Cancel</button></div>}</article>)}</div> : <section className="empty-state"><CalendarDays size={28} /><h2>{history ? 'No appointment history' : 'No upcoming appointments'}</h2><p>{history ? 'Completed and cancelled appointments will appear here.' : 'Book your first appointment when you are ready.'}</p></section>}
     {showBooking && <div className="dialog-backdrop"><section className="dialog dialog--booking"><div className="dialog__header"><div><span className="eyebrow">{rescheduling ? 'Reschedule appointment' : 'New appointment'}</span><h2>{rescheduling ? 'Choose another slot' : 'Book an appointment'}</h2><p className="muted">Choose a branch, service, and time that works for you.</p></div><button className="icon-button" onClick={() => setShowBooking(false)} aria-label="Close"><X size={20} /></button></div><div className="login-form booking-form"><label>Branch<div className="branch-picker"><div className={`booking-input booking-input--with-icon ${isBranchPickerOpen ? 'booking-input--active' : ''}`}><Search size={16} /><input value={branchSearch} placeholder="Search by branch or city" onFocus={() => setIsBranchPickerOpen(true)} onChange={event => { setBranchSearch(event.target.value); setBranchId(0); setIsBranchPickerOpen(true) }} /><ChevronDown size={16} /></div>{isBranchPickerOpen && <div className="branch-picker__menu" role="listbox">{filteredBranches.length ? filteredBranches.map(branch => <button type="button" role="option" key={branch.branchId} onClick={() => { setBranchId(branch.branchId); setBranchSearch(`${branch.name} — ${branch.city}`); setIsBranchPickerOpen(false) }}><span>{branch.name}</span><small>{branch.city} · {branch.code}</small></button>) : <p className="branch-picker__empty">No branches match that search.</p>}</div>}</div></label><label>Service<div className="booking-select"><select value={serviceId} onChange={event => setServiceId(Number(event.target.value))} disabled={!branchId}><option value={0}>{branchId ? 'Select a service' : 'Select a branch first'}</option>{services.map(service => <option key={service.serviceId} value={service.serviceId}>{service.name} ({service.durationMinutes} min)</option>)}</select><ChevronDown size={16} /></div></label><label>Date<input className="booking-input" type="date" min={today} value={date} onChange={event => setDate(event.target.value)} /></label>{serviceId && <label>Available slot<div className="booking-select"><select value={startAt} onChange={event => setStartAt(event.target.value)}><option value="">Select a 15-minute slot</option>{slots.map(slot => <option key={slot.startAt} value={slot.startAt}>{formatDate(slot.startAt)} – {formatDate(slot.endAt).slice(-5)}</option>)}</select><ChevronDown size={16} /></div></label>}<div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => setShowBooking(false)}>Cancel</button><button type="button" className="button button--primary" disabled={isSaving || !startAt} onClick={() => void save()}>{isSaving ? 'Saving...' : <><Check size={16} />{rescheduling ? 'Reschedule' : 'Confirm booking'}</>}</button></div></div></section></div>}
  </div>
}
