import { CalendarPlus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useToast } from '../../../components/ui/ToastProvider'
import type { DateOverride, DateOverrideRequest } from '../types/branch-configuration.types'

interface Props {
  scheduleId: number | null
  overrides: DateOverride[]
  isDisabled?: boolean
  onAdd: (request: DateOverrideRequest) => Promise<number>
  onDelete: (overrideId: number) => Promise<void>
}

export function DateOverridesConfiguration({ scheduleId, overrides, isDisabled, onAdd, onDelete }: Props) {
  const { showToast } = useToast()
  const [overrideDate, setOverrideDate] = useState('')
  const [reason, setReason] = useState('')
  const [isClosed, setIsClosed] = useState(true)
  const [opensAt, setOpensAt] = useState('09:00')
  const [closesAt, setClosesAt] = useState('13:00')
  const [capacity, setCapacity] = useState('10')
  const [isSaving, setIsSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!scheduleId) return
    setIsSaving(true)
    try {
      await onAdd({
        scheduleId,
        overrideDate,
        reason: reason || null,
        isClosed,
        opensAt: isClosed ? null : opensAt,
        closesAt: isClosed ? null : closesAt,
        capacity: isClosed ? null : Number(capacity),
      })
      setOverrideDate('')
      setReason('')
      showToast('Date override saved successfully.')
    } catch {
      showToast('We could not save this date override.', 'error')
    } finally {
      setIsSaving(false)
    }
  }

  const remove = async (overrideId: number) => {
    try {
      await onDelete(overrideId)
      showToast('Date override removed.')
    } catch {
      showToast('We could not remove this date override.', 'error')
    }
  }

  return <section className="section-card configuration-card">
    <div className="configuration-card__title"><span className="configuration-icon"><CalendarPlus size={20} /></span><div><h2>Date overrides</h2><p>Close the branch or set special hours for a specific date.</p></div></div>
    {!scheduleId && <p className="form-hint">Save the operational schedule before adding date overrides.</p>}
    <form className="override-form" onSubmit={submit}>
      <label>Date<input required type="date" value={overrideDate} disabled={isDisabled || !scheduleId} onChange={(event) => setOverrideDate(event.target.value)} /></label>
      <label>Reason <span className="optional">Optional</span><input value={reason} disabled={isDisabled || !scheduleId} onChange={(event) => setReason(event.target.value)} placeholder="Public holiday" /></label>
      <label className="closed-toggle override-form__closed"><input type="checkbox" checked={isClosed} disabled={isDisabled || !scheduleId} onChange={(event) => setIsClosed(event.target.checked)} /><span />Closed</label>
      {!isClosed && <><label>Opens<input required type="time" value={opensAt} disabled={isDisabled || !scheduleId} onChange={(event) => setOpensAt(event.target.value)} /></label><label>Closes<input required type="time" value={closesAt} disabled={isDisabled || !scheduleId} onChange={(event) => setClosesAt(event.target.value)} /></label><label>Capacity<input required min="0" type="number" value={capacity} disabled={isDisabled || !scheduleId} onChange={(event) => setCapacity(event.target.value)} /></label></>}
      <button type="submit" className="button button--primary override-form__action" disabled={isDisabled || !scheduleId || isSaving}>{isSaving ? 'Saving...' : 'Add override'}</button>
    </form>
    <div className="configuration-list">{overrides.length ? overrides.map((override) => <div className="configuration-list__row" key={override.overrideId}><div><strong>{override.overrideDate}</strong><span>{override.reason || 'Special operating date'} · {override.isClosed ? 'Closed' : `${override.opensAt?.slice(0, 5)} - ${override.closesAt?.slice(0, 5)} · capacity ${override.capacity ?? 'default'}`}</span></div><button type="button" className="icon-button" aria-label={`Remove override for ${override.overrideDate}`} onClick={() => void remove(override.overrideId)}><Trash2 size={16} /></button></div>) : <p className="muted">No date overrides configured.</p>}</div>
  </section>
}
