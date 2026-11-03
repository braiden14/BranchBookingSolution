import { ArrowLeft, Building2, CalendarDays, Check, Clock3, Save } from 'lucide-react'
import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
import { useToast } from '../../../components/ui/ToastProvider'
import { useBranchConfiguration } from '../hooks/useBranchConfiguration'
import { useOperationalSchedule } from '../hooks/useOperationalSchedule'
import { activateBranch, deactivateBranch, updateBranch } from '../services/branches.service'
import { BranchServicesConfiguration } from './BranchServicesConfiguration'
import { CapacityOverridesConfiguration } from './CapacityOverridesConfiguration'
import { DateOverridesConfiguration } from './DateOverridesConfiguration'
import type { CreateOperationalScheduleRequest, WeeklyOperatingHours } from '../types/operational-schedule.types'
import type { Branch, UpdateBranchRequest } from '../types/branch.types'

const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
const today = new Date().toISOString().slice(0, 10)
// PostgreSQL EXTRACT(DOW) and the schema use Sunday=1 ... Saturday=7.
const defaultHours = (): WeeklyOperatingHours[] => days.map((_, index) => ({ dayOfWeek: (index + 1) % 7 + 1, opensAt: index > 4 ? '09:00' : '08:00', closesAt: index > 4 ? '13:00' : '17:00', isClosed: index === 6, capacity: index === 6 ? 0 : 10 }))

type ConfigurationTab = 'details' | 'services' | 'schedule' | 'overrides'

export function BranchConfigurationWorkspace() {
  const { branchId: branchIdParam } = useParams()
  const branchId = Number(branchIdParam)
  const location = useLocation()
  const navigate = useNavigate()
  const branch = (location.state as { branch?: Branch } | null)?.branch
  const { schedule, isLoading: isScheduleLoading, error: scheduleError, save: saveSchedule } = useOperationalSchedule(branchId)
  const configuration = useBranchConfiguration(branchId)
  const { showToast } = useToast()
  const [effectiveFrom, setEffectiveFrom] = useState(today)
  const [effectiveTo, setEffectiveTo] = useState('')
  const [weeklyHours, setWeeklyHours] = useState(defaultHours)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<ConfigurationTab>(branch ? 'details' : 'services')
  const [branchForm, setBranchForm] = useState<UpdateBranchRequest>(branch ? {
    code: branch.code,
    name: branch.name,
    addressLine1: branch.addressLine1,
    city: branch.city,
    province: branch.province,
    postalCode: branch.postalCode,
  } : { code: '', name: '', addressLine1: '', city: '', province: '', postalCode: '' })
  const [isBranchSaving, setIsBranchSaving] = useState(false)
  const [branchSaveError, setBranchSaveError] = useState<string | null>(null)
  const [isActivating, setIsActivating] = useState(false)
  const [isActivated, setIsActivated] = useState(branch?.status === 'Active')
  const [activationError, setActivationError] = useState<string | null>(null)

  useEffect(() => {
    if (!schedule) return
    setEffectiveFrom(schedule.effectiveFrom)
    setEffectiveTo(schedule.effectiveTo ?? '')
    setWeeklyHours(schedule.weeklyHours.map((hours) => ({ ...hours, opensAt: hours.opensAt?.slice(0, 5) ?? null, closesAt: hours.closesAt?.slice(0, 5) ?? null })))
  }, [schedule])

  const updateDay = (index: number, field: keyof WeeklyOperatingHours, value: string | boolean | number | null) => setWeeklyHours((current) => current.map((hours, currentIndex) => currentIndex === index ? { ...hours, [field]: value } : hours))
  const toggleClosed = (index: number, isClosed: boolean) => setWeeklyHours((current) => current.map((hours, currentIndex) => currentIndex === index ? { ...hours, isClosed, opensAt: isClosed ? null : hours.opensAt ?? '08:00', closesAt: isClosed ? null : hours.closesAt ?? '17:00', capacity: isClosed ? 0 : hours.capacity || 10 } : hours))
  const updateTime = (index: number, field: 'opensAt' | 'closesAt') => (event: ChangeEvent<HTMLInputElement>) => updateDay(index, field, event.target.value)

  const updateBranchField = (field: keyof UpdateBranchRequest) => (event: ChangeEvent<HTMLInputElement>) => setBranchForm((current) => ({ ...current, [field]: event.target.value }))

  const submitBranchDetails = async (event: FormEvent) => {
    event.preventDefault()
    setIsBranchSaving(true)
    setBranchSaveError(null)
    try {
      await updateBranch(branchId, branchForm)
      showToast('Branch details saved successfully.')
    } catch {
      const message = 'We could not save the branch details. Please try again.'
      setBranchSaveError(message)
      showToast(message, 'error')
    } finally {
      setIsBranchSaving(false)
    }
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    setSaveError(null)
    const request: CreateOperationalScheduleRequest = { effectiveFrom, effectiveTo: effectiveTo || null, weeklyHours }
    try {
      await saveSchedule(request)
      setActiveTab('overrides')
      showToast('Operating hours saved successfully.')
    } catch {
      const message = 'We could not save this configuration. Please try again.'
      setSaveError(message)
      showToast(message, 'error')
    } finally {
      setIsSaving(false)
    }
  }

  const activate = async () => {
    if (isActivated && !window.confirm('Deactivate this branch? This will stop accepting bookings.')) return
    setIsActivating(true)
    setActivationError(null)
    try {
      if (isActivated) {
        await deactivateBranch(branchId)
        setIsActivated(false)
        showToast('Branch deactivated and bookings disabled.')
      } else {
        await activateBranch(branchId)
        setIsActivated(true)
        showToast('Branch activated and accepting bookings.')
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : `The branch could not be ${isActivated ? 'deactivated' : 'activated'}.`
      setActivationError(message)
      showToast(message, 'error')
    } finally {
      setIsActivating(false)
  }
  }

  if (isScheduleLoading || configuration.isLoading) return <div className="configuration-page"><div className="skeleton-card configuration-loading" /></div>
  if (scheduleError || configuration.error) return <div className="configuration-page"><div className="error-state"><p>{scheduleError || configuration.error}</p><button className="button button--secondary" onClick={() => navigate('/branches')}>Back to branches</button></div></div>

  return <div className="configuration-page">
    <button className="back-link" onClick={() => navigate('/branches')}><ArrowLeft size={16} /> Back to branches</button>
      <div className="page-heading page-heading--configuration"><div><span className="eyebrow">Branch setup / configuration</span><h1>Configure your branch</h1><p className="page-heading__description">Set up services, operating hours, and booking availability.</p></div><button type="button" className="button button--primary activation-button" disabled={isActivating} onClick={() => void activate()}>{isActivating ? (isActivated ? 'Deactivating...' : 'Activating...') : isActivated ? 'Deactivate branch' : 'Activate & accept bookings'}</button></div>
    {activationError && <p className="form-error activation-error">{activationError}</p>}

    <div className="configuration-tabs" role="tablist" aria-label="Branch configuration sections">
      <button type="button" role="tab" aria-selected={activeTab === 'details'} className={`configuration-tab ${activeTab === 'details' ? 'configuration-tab--active' : ''}`} disabled={!branch} onClick={() => setActiveTab('details')}>Branch details</button>
      <button type="button" role="tab" aria-selected={activeTab === 'services'} className={`configuration-tab ${activeTab === 'services' ? 'configuration-tab--active' : ''}`} onClick={() => setActiveTab('services')}>Services</button>
      <button type="button" role="tab" aria-selected={activeTab === 'schedule'} className={`configuration-tab ${activeTab === 'schedule' ? 'configuration-tab--active' : ''}`} onClick={() => setActiveTab('schedule')}>Operating schedule</button>
      <button type="button" role="tab" aria-selected={activeTab === 'overrides'} className={`configuration-tab ${activeTab === 'overrides' ? 'configuration-tab--active' : ''}`} disabled={!schedule} onClick={() => setActiveTab('overrides')}>Overrides {!schedule && <span className="configuration-tab__lock">Save schedule first</span>}</button>
    </div>

    {activeTab === 'details' && branch && <form onSubmit={submitBranchDetails}>
      <section className="section-card configuration-card"><div className="configuration-card__title"><span className="configuration-icon"><Building2 size={20} /></span><div><h2>Branch details</h2><p>Update the information customers and staff use to identify this branch.</p></div></div><div className="form-grid"><label>Branch code<input required value={branchForm.code} onChange={updateBranchField('code')} /></label><label>Branch name<input required value={branchForm.name} onChange={updateBranchField('name')} /></label><label className="form-grid__wide">Address<input required value={branchForm.addressLine1} onChange={updateBranchField('addressLine1')} /></label><label>City<input required value={branchForm.city} onChange={updateBranchField('city')} /></label><label>Province<input required value={branchForm.province} onChange={updateBranchField('province')} /></label><label>Postal code<input required value={branchForm.postalCode} onChange={updateBranchField('postalCode')} /></label></div></section>
      {branchSaveError && <p className="form-error">{branchSaveError}</p>}<div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => navigate('/branches')} disabled={isBranchSaving}>Back to branches</button><button type="submit" className="button button--primary" disabled={isBranchSaving}>{isBranchSaving ? <LoadingOverlay label="Saving…" /> : <><Save size={16} />Save branch details<Check size={15} /></>}</button></div>
    </form>}

    {activeTab === 'services' && <>
      <BranchServicesConfiguration
        offeredServices={configuration.offeredServices}
        branchServices={configuration.branchServices}
        onSave={(serviceId, isActive) => configuration.saveBranchService({ serviceId, isActive })}
      />
      <div className="configuration-actions configuration-actions--back"><button type="button" className="button button--secondary" onClick={() => navigate('/branches')}>Back to branches</button></div>
    </>}

    {activeTab === 'schedule' && <form onSubmit={submit}>
      <section className="section-card configuration-card"><div className="configuration-card__title"><span className="configuration-icon"><CalendarDays size={20} /></span><div><h2>Operational schedule</h2><p>Choose the period this weekly schedule will apply.</p></div></div><div className="form-grid schedule-dates"><label>Effective from<input required type="date" value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} /></label><label>Effective to <span className="optional">[Optional]</span><input type="date" min={effectiveFrom} value={effectiveTo} onChange={(event) => setEffectiveTo(event.target.value)} /></label></div></section>
      <section className="section-card configuration-card"><div className="configuration-card__title"><span className="configuration-icon"><Clock3 size={20} /></span><div><h2>Weekly operating hours</h2><p>These hours will repeat for every day in the selected period.</p></div></div><div className="weekly-hours-list">{weeklyHours.map((hours, index) => <div className={`weekly-row ${hours.isClosed ? 'weekly-row--closed' : ''}`} key={hours.dayOfWeek}><strong>{days[index]}</strong><label className="closed-toggle"><input type="checkbox" checked={hours.isClosed} onChange={(event) => toggleClosed(index, event.target.checked)} /><span />Closed</label>{hours.isClosed ? <span className="closed-label">This branch will not accept bookings</span> : <><label>Opens<input required type="time" value={hours.opensAt ?? ''} onChange={updateTime(index, 'opensAt')} /></label><span className="time-separator">to</span><label>Closes<input required type="time" value={hours.closesAt ?? ''} onChange={updateTime(index, 'closesAt')} /></label><label>Daily capacity<input required min="1" type="number" value={hours.capacity ?? ''} onChange={(event) => updateDay(index, 'capacity', Number(event.target.value))} /></label></>}</div>)}</div></section>
      {saveError && <p className="form-error">{saveError}</p>}<div className="configuration-actions"><button type="button" className="button button--secondary" onClick={() => navigate('/branches')} disabled={isSaving}>Back to branches</button><button type="submit" className="button button--primary" disabled={isSaving}>{isSaving ? <LoadingOverlay label="Saving..." /> : <><Save size={16} />Save operating schedule<Check size={15} /></>}</button></div>
    </form>}

    {activeTab === 'overrides' && schedule && <>
      <DateOverridesConfiguration
        scheduleId={schedule.id}
        overrides={configuration.dateOverrides}
        onAdd={configuration.addDateOverride}
        onDelete={configuration.removeDateOverride}
      />
      <CapacityOverridesConfiguration
        overrides={configuration.capacityOverrides}
        onSave={configuration.saveCapacityOverride}
        onDelete={configuration.removeCapacityOverride}
      />
      <div className="configuration-actions configuration-actions--back"><button type="button" className="button button--secondary" onClick={() => navigate('/branches')}>Back to branches</button></div>
    </>}
  </div>
}
