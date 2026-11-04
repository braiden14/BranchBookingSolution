import { CalendarDays, Clock3, X } from 'lucide-react'
import { durationMinutes, formatDate } from '../../../dateUtils'
import type { Appointment } from '../types/appointment.types'

interface CustomerAppointmentListProps {
  appointments: Appointment[]
  history: boolean
  onReschedule: (appointment: Appointment) => void
  onCancel: (appointment: Appointment) => void
}

export function CustomerAppointmentList({ appointments, history, onReschedule, onCancel }: CustomerAppointmentListProps) {
  if (!appointments.length) {
    return (
      <section className="empty-state">
        <CalendarDays size={28} />
        <h2>{history ? 'No appointment history' : 'No upcoming appointments'}</h2>
        <p>{history ? 'Completed and cancelled appointments will appear here.' : 'Book your first appointment when you are ready.'}</p>
      </section>
    )
  }

  return (
    <div className="appointment-list">
      {appointments.map(appointment => (
        <article className="section-card appointment-card" key={appointment.appointmentId}>
          <div>
            <span className="eyebrow">{appointment.status}</span>
            <h2>{appointment.serviceName}</h2>
            <p>{appointment.branchName} · {formatDate(appointment.startAt)}</p>
            <small><Clock3 size={14} /> {durationMinutes(appointment.startAt, appointment.endAt)} minutes</small>
          </div>
          {!history && (
            <div className="appointment-actions">
              <button className="button button--secondary" onClick={() => onReschedule(appointment)}>Reschedule</button>
              <button className="button button--secondary" onClick={() => onCancel(appointment)}><X size={15} /> Cancel</button>
            </div>
          )}
        </article>
      ))}
    </div>
  )
}
