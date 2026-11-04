import { ChevronLeft, ChevronRight } from 'lucide-react'
import { formatDay, formatDayNumber, formatTime } from '../../../dateUtils'
import type { AvailableSlot } from '../types/appointment.types'

interface WeekSlotPickerProps {
  weekDates: string[]
  slotsByDate: Record<string, AvailableSlot[]>
  selectedDate: string
  selectedStartAt: string
  today: string
  allowPastDays: boolean
  canGoToPreviousWeek: boolean
  isLoading: boolean
  error: string | null
  onPreviousWeek: () => void
  onNextWeek: () => void
  onSelectDay: (day: string) => void
  onSelectSlot: (day: string, startAt: string) => void
}

export function WeekSlotPicker({
  weekDates, slotsByDate, selectedDate, selectedStartAt, today, allowPastDays, canGoToPreviousWeek, isLoading, error,
  onPreviousWeek, onNextWeek, onSelectDay, onSelectSlot,
}: WeekSlotPickerProps) {
  return (
    <fieldset className="booking-slots">
      <legend>Choose a day and available time</legend>
      <div className="week-navigation">
        <button type="button" className="icon-button" onClick={onPreviousWeek} disabled={!canGoToPreviousWeek} aria-label="Previous week"><ChevronLeft size={18} /></button>
        <strong>{formatDayNumber(weekDates[0])} – {formatDayNumber(weekDates[6])}</strong>
        <button type="button" className="icon-button" onClick={onNextWeek} aria-label="Next week"><ChevronRight size={18} /></button>
      </div>
      {isLoading ? <p className="booking-status">Checking availability...</p>
        : error ? <p className="booking-status booking-status--error" role="alert">{error}</p>
        : (
          <div className="week-grid">
            {weekDates.map(day => {
              const daySlots = slotsByDate[day] ?? []
              const unavailable = !allowPastDays && day < today
              return (
                <div className={`week-day ${selectedDate === day ? 'week-day--selected' : ''} ${unavailable ? 'week-day--unavailable' : ''}`} key={day}>
                  <button type="button" className="week-day__header" disabled={unavailable} onClick={() => onSelectDay(day)}>
                    <strong>{formatDay(day)}</strong>
                    <span>{formatDayNumber(day)}</span>
                  </button>
                  <div className="week-day__slots">
                    {daySlots.length
                      ? daySlots.map(slot => (
                        <button type="button" className={`slot-button ${selectedStartAt === slot.startAt ? 'slot-button--selected' : ''}`} key={slot.startAt} aria-pressed={selectedStartAt === slot.startAt} onClick={() => onSelectSlot(day, slot.startAt)}>
                          {formatTime(slot.startAt)}
                        </button>
                      ))
                      : <span className="week-day__empty">{unavailable ? 'Past' : 'No times'}</span>}
                  </div>
                </div>
              )
            })}
          </div>
        )}
    </fieldset>
  )
}
