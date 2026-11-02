import { Gauge, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useToast } from '../../../components/ui/ToastProvider'
import type { CapacityOverride, CapacityOverrideRequest } from '../types/branch-configuration.types'

interface Props {
  overrides: CapacityOverride[]
  isDisabled?: boolean
  onSave: (request: CapacityOverrideRequest) => Promise<void>
  onDelete: (overrideId: number) => Promise<void>
}

export function CapacityOverridesConfiguration({ overrides, isDisabled, onSave, onDelete }: Props) {
  const { showToast } = useToast()
  const [overrideDate, setOverrideDate] = useState('')
  const [capacity, setCapacity] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      await onSave({ overrideDate, capacity: Number(capacity) })
      setOverrideDate('')
      setCapacity('')
      showToast('Capacity override saved successfully.')
    } catch {
      showToast('We could not save this capacity override.', 'error')
    } finally {
      setIsSaving(false)
    }
  }

  const remove = async (overrideId: number) => {
    try {
      await onDelete(overrideId)
      showToast('Capacity override removed.')
    } catch {
      showToast('We could not remove this capacity override.', 'error')
    }
  }

  return <section className="section-card configuration-card">
    <div className="configuration-card__title"><span className="configuration-icon"><Gauge size={20} /></span><div><h2>Capacity overrides</h2><p>Change the daily booking capacity without changing opening hours.</p></div></div>
    <form className="override-form override-form--capacity" onSubmit={submit}>
      <label>Date<input required type="date" value={overrideDate} disabled={isDisabled} onChange={(event) => setOverrideDate(event.target.value)} /></label>
      <label>Daily capacity<input required min="0" type="number" value={capacity} disabled={isDisabled} onChange={(event) => setCapacity(event.target.value)} /></label>
      <button type="submit" className="button button--primary override-form__action" disabled={isDisabled || isSaving}>{isSaving ? 'Saving...' : 'Save capacity'}</button>
    </form>
    <div className="configuration-list">{overrides.length ? overrides.map((override) => <div className="configuration-list__row" key={override.capacityOverrideId}><div><strong>{override.overrideDate}</strong><span>Daily capacity: {override.capacity}</span></div><button type="button" className="icon-button" aria-label={`Remove capacity override for ${override.overrideDate}`} onClick={() => void remove(override.capacityOverrideId)}><Trash2 size={16} /></button></div>) : <p className="muted">No capacity overrides configured.</p>}</div>
  </section>
}
