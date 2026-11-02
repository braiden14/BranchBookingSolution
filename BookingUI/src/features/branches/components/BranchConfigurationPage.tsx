import { ArrowLeft, CalendarDays, Check, Clock3, Save } from 'lucide-react'
import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
import { useToast } from '../../../components/ui/ToastProvider'
import { useOperationalSchedule } from '../hooks/useOperationalSchedule'
import type { CreateOperationalScheduleRequest, WeeklyOperatingHours } from '../types/operational-schedule.types'

const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
const today = new Date().toISOString().slice(0, 10)
const defaultHours = (): WeeklyOperatingHours[] => days.map((_, index) => ({ dayOfWeek: index + 1, opensAt: index > 4 ? '09:00' : '08:00', closesAt: index > 4 ? '13:00' : '17:00', isClosed: index === 6, capacity: index === 6 ? 0 : 10 }))

export function BranchConfigurationPage() {
  const { branchId: branchIdParam } = useParams()
  const branchId = Number(branchIdParam)
  const navigate = useNavigate()
  const { schedule, isLoading, error, save } = useOperationalSchedule(branchId)
  const { showToast } = useToast()
  const [effectiveFrom, setEffectiveFrom] = useState(today)
  const [effectiveTo, setEffectiveTo] = useState('')
  const [weeklyHours, setWeeklyHours] = useState(defaultHours)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  useEffect(() => {
    if (!schedule) return
    setEffectiveFrom(schedule.effectiveFrom)
    setEffectiveTo(schedule.effectiveTo ?? '')
    setWeeklyHours(schedule.weeklyHours.map((hours) => ({ ...hours, opensAt: hours.opensAt?.slice(0, 5) ?? null, closesAt: hours.closesAt?.slice(0, 5) ?? null })))
  }, [schedule])

  const updateDay = (index: number, field: keyof WeeklyOperatingHours, value: string | boolean | number | null) => setWeeklyHours((current) => current.map((hours, currentIndex) => currentIndex === index ? { ...hours, [field]: value } : hours))
  const toggleClosed = (index: number, isClosed: boolean) => setWeeklyHours((current) => current.map((hours, currentIndex) => currentIndex === index ? { ...hours, isClosed, opensAt: isClosed ? null : hours.opensAt ?? '08:00', closesAt: isClosed ? null : hours.closesAt ?? '17:00', capacity: isClosed ? 0 : hours.capacity || 10 } : hours))
  const updateTime = (index: number, field: 'opensAt' | 'closesAt') => (event: ChangeEvent<HTMLInputElement>) => updateDay(index, field, event.target.value)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    setSaveError(null)
    const request: CreateOperationalScheduleRequest = { effectiveFrom, effectiveTo: effectiveTo || null, weeklyHours }
    try { await save(request); showToast('Operating hours saved successfully.'); navigate('/branches') } catch { const message = 'We could not save this configuration. Please try again.'; setSaveError(message); showToast(message, 'error') } finally { setIsSaving(false) }
  }

  if (isLoading) return <div className="configuration-page"><div className="skeleton-card configuration-loading" /></div>
  if (error) return <div className="configuration-page"><div className="error-state"><p>{error}</p><button className="button button--secondary" onClick={() => navigate('/branches')}>Back to branches</button></div></div>

  return <div className="configuration-page">
    <button className="back-link" onClick={() => navigate('/branches')}><ArrowLeft size={16} /> Back to branches</button>
    <div className="page-heading page-heading--configuration"><div><span className="eyebrow">Branch setup / operating hours</span><h1>Configure your hours</h1><p className="page-heading__description">Set when this branch is available for customer bookings.</p></div><span className="setup-step"><span>2</span> of 2 · Branch setup</span></div>
    <form onSubmit={submit}>
      <section className="section-card configuration-card"><div className="configuration-card__title"><span className="configuration-icon"><CalendarDays size={20} /></span><div><h2>Operational schedule</h2><p>Choose the period this weekly schedule will apply.</p></div></div><div className="form-grid schedule-dates"><label>Effective from<input required type="date" value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} /></label><label>Effective to <span className="optional">Optional</span><input type="date" min={effectiveFrom} value={effectiveTo} onChange={(event) => setEffectiveTo(event.target.value)} /></label></div></section>
      <section className="section-card configuration-card"><div className="configuration-card__title"><span className="configuration-icon"><Clock3 size={20} /></span><div><h2>Weekly operating hours</h2><p>These hours will repeat for every day in the selected period.</p></div></div><div className="weekly-hours-list">{weeklyHours.map((hours, index) => <div className={`weekly-row ${hours.isClosed ? 'weekly-row--closed' : ''}`} key={hours.dayOfWeek}><strong>{days[index]}</strong><label className="closed-toggle"><input type="checkbox" checked={hours.isClosed} onChange={(event) => toggleClosed(index, event.target.checked)} /><span />Closed</label>{hours.isClosed ? <span className="closed-label">This branch will not accept bookings</span> : <><label>Opens<input required type="time" value={hours.opensAt ?? ''} onChange={updateTime(index, 'opensAt')} /></label><span className="time-separator">to</span><label>Closes<input required type="time" value={hours.closesAt ?? ''} onChange={updateTime(index, 'closesAt')} /></label><label>Daily capacity<input required min="1" type="number" value={hours.capacity ?? ''} onChange={(event) => updateDay(index, 'capacity', Number(event.target.value))} /></label></>}</div>)}</div></section>
      {saveError && <p className="form-error">{saveError}</p>}<div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => navigate('/branches')} disabled={isSaving}>Cancel</button><button type="submit" className="button button--primary" disabled={isSaving}>{isSaving ? <LoadingOverlay label="Saving…" /> : <><Save size={16} />Save configuration<Check size={15} /></>}</button></div>
    </form>
  </div>
}
