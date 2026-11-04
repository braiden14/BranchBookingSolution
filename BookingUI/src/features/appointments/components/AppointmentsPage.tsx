import { ChevronDown, Plus, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { ConfirmationDialog } from '../../../components/ui/ConfirmationDialog'
import { useToast } from '../../../components/ui/ToastProvider'
import { addDays, formatDayNumber, formatTime, getToday, startOfWeek } from '../../../dateUtils'
import { useAuth } from '../../auth/providers/AuthProvider'
import { appointmentService } from '../services/appointment.service'
import type { Appointment, AvailableSlot, CustomerBranch, CustomerService } from '../types/appointment.types'
import { BranchPicker } from './BranchPicker'
import { CustomerAppointmentList } from './CustomerAppointmentList'
import { OperationalAppointmentsPage } from './OperationalAppointmentsPage'
import { WeekSlotPicker } from './WeekSlotPicker'

export function AppointmentsPage() {
  const { user } = useAuth()
  return user?.role === 'customer' ? <CustomerAppointmentsPage /> : <OperationalAppointmentsPage />
}

function CustomerAppointmentsPage() {
  const today = getToday()
  const { showToast } = useToast()
  const { user } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const isBookingRoute = location.pathname === '/appointments/book' || location.pathname.endsWith('/reschedule')
  const [history, setHistory] = useState(false)
  const [appointments, setAppointments] = useState<Appointment[]>([])
  const [branches, setBranches] = useState<CustomerBranch[]>([])
  const [services, setServices] = useState<CustomerService[]>([])
  const [slotsByDate, setSlotsByDate] = useState<Record<string, AvailableSlot[]>>({})
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
  const [isLoadingSlots, setIsLoadingSlots] = useState(false)
  const [weekStart, setWeekStart] = useState(startOfWeek(today))
  const [bookingError, setBookingError] = useState<string | null>(null)
  const [validationMessage, setValidationMessage] = useState<string | null>(null)
  const [showBookingConfirmation, setShowBookingConfirmation] = useState(false)
  const [cancellingAppointment, setCancellingAppointment] = useState<Appointment | null>(null)
  const [isCancelling, setIsCancelling] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const filteredBranches = useMemo(() => {
    const query = branchSearch.trim().toLowerCase().replace(/[—–-]/g, ' ').replace(/\s+/g, ' ')
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
    setServices([]); setSlotsByDate({}); setStartAt('')
    if (branchId) void appointmentService.getServices(branchId).then(result => { setServices(result); setBookingError(null) }).catch(error => setBookingError(error instanceof Error ? error.message : 'Could not load services.'))
  }, [branchId])
  const weekDates = useMemo(() => Array.from({ length: 7 }, (_, index) => addDays(weekStart, index)), [weekStart])
  useEffect(() => {
    setSlotsByDate({}); setStartAt('')
    if (!branchId || !serviceId) return
    setIsLoadingSlots(true); setBookingError(null)
      void appointmentService.getWeekSlots(branchId, serviceId, weekDates[0], weekDates[6], rescheduling?.appointmentId)
       .then(results => setSlotsByDate(Object.fromEntries(results.map(day => [day.date, day.slots]))))
      .catch(error => setBookingError(error instanceof Error ? error.message : 'Could not load available slots.'))
      .finally(() => setIsLoadingSlots(false))
  }, [branchId, serviceId, weekDates, rescheduling])

  useEffect(() => { setShowBooking(isBookingRoute) }, [isBookingRoute])

  const openBooking = (appointment?: Appointment) => {
    setRescheduling(appointment ?? null)
    setBranchId(appointment?.branchId ?? 0)
    const appointmentBranch = branches.find(branch => branch.branchId === appointment?.branchId)
    setBranchSearch(appointmentBranch ? `${appointmentBranch.name} — ${appointmentBranch.city}` : '')
    setIsBranchPickerOpen(false)
    setServiceId(appointment?.serviceId ?? 0)
    setDate(appointment ? appointment.startAt.slice(0, 10) : today)
    setWeekStart(startOfWeek(appointment ? appointment.startAt.slice(0, 10) : today))
    setStartAt('')
    setBookingError(null); setValidationMessage(null)
    setShowBooking(true)
    navigate(appointment ? `/appointments/${appointment.appointmentId}/reschedule` : '/appointments/book')
  }

  const save = async () => {
    if (!branchId || !serviceId || !startAt) {
      setValidationMessage(!branchId ? 'Select a branch to continue.' : !serviceId ? 'Select a service to continue.' : 'Choose an available time to continue.')
      return
    }
    setValidationMessage(null); setBookingError(null)
    setIsSaving(true)
    try {
      if (rescheduling) await appointmentService.reschedule(rescheduling.appointmentId, startAt)
      else await appointmentService.create(branchId, serviceId, startAt)
      showToast(rescheduling ? 'Appointment rescheduled.' : 'Appointment booked successfully.')
       setShowBookingConfirmation(false); navigate('/appointments'); setRescheduling(null); await loadAppointments()
    } catch (saveError) { const message = saveError instanceof Error ? saveError.message : 'The appointment could not be saved.'; setBookingError(message); showToast(message, 'error') }
    finally { setIsSaving(false) }
  }

  const cancel = async () => {
    if (!cancellingAppointment) return
    setIsCancelling(true)
    try { await appointmentService.cancel(cancellingAppointment.appointmentId); showToast('Appointment cancelled.'); setCancellingAppointment(null); await loadAppointments() }
    catch (cancelError) { showToast(cancelError instanceof Error ? cancelError.message : 'The appointment could not be cancelled.', 'error') }
    finally { setIsCancelling(false) }
  }

  const bookingSummary = `${rescheduling ? 'Reschedule' : 'Book'} ${services.find(service => service.serviceId === serviceId)?.name ?? 'your selected service'} at ${branches.find(branch => branch.branchId === branchId)?.name ?? 'your selected branch'} on ${formatDayNumber(date)} at ${startAt ? formatTime(startAt) : 'the selected time'}?`

  return <div className={`appointments-page ${isBookingRoute ? 'appointments-page--booking' : ''}`}>
    {!isBookingRoute && <>
    <div className="page-heading">
      <div>
        <span className="eyebrow">Customer / appointments</span>
        <h1>{history ? 'Appointment history' : 'Your appointments'}</h1>
        <p className="page-heading__description">Book and manage your branch appointments.</p>
      </div>
      {!history && <button className="button button--primary" onClick={() => openBooking()}><Plus size={17} /> Book appointment</button>}
    </div>
    <div className="configuration-tabs" role="tablist">
      <button className={`configuration-tab ${!history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(false)}>Upcoming</button>
      <button className={`configuration-tab ${history ? 'configuration-tab--active' : ''}`} onClick={() => setHistory(true)}>History</button>
    </div>
    {error && <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => void loadAppointments()}>Try again</button></div>}
    {isLoading
      ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /></div>
      : <CustomerAppointmentList appointments={appointments} history={history} onReschedule={openBooking} onCancel={setCancellingAppointment} />}</>}
      {showBooking && <div className={isBookingRoute ? 'booking-page' : 'dialog-backdrop'}>
        {isBookingRoute && <div className="page-heading">
          <div>
            <span className="eyebrow">Customer / appointments</span>
            <h1>{rescheduling ? 'Reschedule appointment' : 'Book an appointment'}</h1>
            <p className="page-heading__description">Choose a branch, service, date, and available time.</p>
          </div>
          <button type="button" className="button button--secondary" onClick={() => navigate('/appointments')}>Back to appointments</button>
        </div>}
        <section className={isBookingRoute ? 'booking-screen' : 'dialog dialog--booking'} role="dialog" aria-modal="true" aria-labelledby="booking-dialog-title">
        {!isBookingRoute && <div className="dialog__header">
          <div>
            <span className="eyebrow">New appointment</span>
            <h2 id="booking-dialog-title">Book an appointment</h2>
            <p className="muted">Choose a branch, service, date, and available time.</p>
          </div>
          <button className="icon-button" onClick={() => navigate('/appointments')} aria-label="Return to appointments"><X size={20} /></button>
        </div>}
         <div className="login-form booking-form">
           <div className="booking-fields">
             <label>Branch<span className="booking-field__hint">Search by branch name, city, or code</span>
               <BranchPicker
                 branches={filteredBranches}
                 selectedBranchId={branchId}
                 search={branchSearch}
                 isOpen={isBranchPickerOpen}
                 onOpen={() => setIsBranchPickerOpen(true)}
                 onSearchChange={value => { setBranchSearch(value); setBranchId(0); setServiceId(0); setIsBranchPickerOpen(true) }}
                 onSelect={branch => { setBranchId(branch.branchId); setServiceId(0); setBranchSearch(`${branch.name} — ${branch.city}`); setIsBranchPickerOpen(false) }}
               />
             </label>
             <label>Service<span className="booking-field__hint">Choose the service you need</span>
               <div className="booking-select">
                 <select value={serviceId} onChange={event => setServiceId(Number(event.target.value))} disabled={!branchId} aria-label="Service">
                   <option value={0}>{branchId ? 'Select a service' : 'Select a branch first'}</option>
                   {services.map(service => <option key={service.serviceId} value={service.serviceId}>{service.name} ({service.durationMinutes} min)</option>)}
                 </select>
                 <ChevronDown size={16} />
               </div>
             </label>
           </div>
           {serviceId > 0 && <WeekSlotPicker
             weekDates={weekDates}
             slotsByDate={slotsByDate}
             selectedDate={date}
             selectedStartAt={startAt}
             today={today}
             allowPastDays={Boolean(rescheduling)}
             canGoToPreviousWeek={Boolean(rescheduling) || weekStart > startOfWeek(today)}
             isLoading={isLoadingSlots}
             error={bookingError}
             onPreviousWeek={() => setWeekStart(current => addDays(current, -7))}
             onNextWeek={() => setWeekStart(current => addDays(current, 7))}
             onSelectDay={day => { setDate(day); setStartAt('') }}
             onSelectSlot={(day, slotStart) => { setDate(day); setStartAt(slotStart); setShowBookingConfirmation(true) }}
           />}
          {bookingError && !serviceId && <p className="booking-status booking-status--error" role="alert">{bookingError}</p>}
          {validationMessage && <p className="booking-status booking-status--error" role="alert">{validationMessage}</p>}
          {!isBookingRoute && <div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => navigate('/appointments')}>Cancel</button></div>}
        </div>
      </section></div>}
      {showBookingConfirmation && <ConfirmationDialog title={rescheduling ? 'Confirm rescheduling' : 'Confirm appointment'} description={bookingSummary} confirmLabel={rescheduling ? 'Reschedule appointment' : 'Book appointment'} tone="primary" isConfirming={isSaving} onConfirm={() => void save()} onClose={() => setShowBookingConfirmation(false)} />}
      {cancellingAppointment && <ConfirmationDialog title="Cancel appointment?" description={`Cancel your ${cancellingAppointment.serviceName} appointment at ${cancellingAppointment.branchName}?`} confirmLabel="Cancel appointment" isConfirming={isCancelling} onConfirm={() => void cancel()} onClose={() => { if (!isCancelling) setCancellingAppointment(null) }} />}
  </div>
}
