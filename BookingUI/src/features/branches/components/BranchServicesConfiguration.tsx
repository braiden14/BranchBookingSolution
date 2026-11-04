import { Check, Clock3, Search } from 'lucide-react'
import { useState } from 'react'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useToast } from '../../../components/ui/ToastProvider'
import type { BranchService, OfferedService } from '../types/branch-configuration.types'

interface Props {
  offeredServices: OfferedService[]
  branchServices: BranchService[]
  isDisabled?: boolean
  onSave: (serviceId: number, isActive: boolean) => Promise<void>
}

export function BranchServicesConfiguration({ offeredServices, branchServices, isDisabled, onSave }: Props) {
  const { showToast } = useToast()
  const [savingServiceId, setSavingServiceId] = useState<number | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [availability, setAvailability] = useState<'all' | 'available' | 'unavailable'>('all')
  const configured = new Map(branchServices.map((service) => [service.serviceId, service.isActive]))
  const normalizedSearch = searchTerm.trim().toLowerCase()
  const visibleServices = offeredServices.filter((service) => {
    const matchesSearch = !normalizedSearch || `${service.code} ${service.name}`.toLowerCase().includes(normalizedSearch)
    const matchesAvailability = availability === 'all' || (availability === 'available' ? service.isActive : !service.isActive)
    return matchesSearch && matchesAvailability
  })
  const activeCount = branchServices.filter(service => service.isActive).length

  const toggle = async (serviceId: number) => {
    const isActive = !(configured.get(serviceId) ?? false)
    setSavingServiceId(serviceId)
    try {
      await onSave(serviceId, isActive)
      showToast(isActive ? 'Service enabled for this branch.' : 'Service disabled for this branch.')
    } catch {
      showToast('We could not update this branch service.', 'error')
    } finally {
      setSavingServiceId(null)
    }
  }

  return <section className="section-card configuration-card">
    <div className="configuration-card__title"><span className="configuration-icon"><Check size={20} /></span><div><h2>Branch services</h2><p>{activeCount} of {offeredServices.length} services enabled at this branch.</p></div></div>
    {offeredServices.length > 0 && <div className="configuration-filters">
      <label className="search-box" htmlFor="branch-service-search"><Search size={16} aria-hidden="true" /><input id="branch-service-search" value={searchTerm} placeholder="Search services" aria-label="Search branch services" onChange={event => setSearchTerm(event.target.value)} /></label>
      <label className="filter-select" htmlFor="branch-service-availability"><span>Catalogue</span><select id="branch-service-availability" value={availability} onChange={event => setAvailability(event.target.value as typeof availability)}><option value="all">All services</option><option value="available">Available</option><option value="unavailable">Unavailable</option></select></label>
    </div>}
    {!offeredServices.length ? <p className="muted">No offered services are available yet.</p> : !visibleServices.length ? <div className="empty-state empty-state--compact"><h3>No matching services</h3><p className="muted">Try a different search or catalogue filter.</p></div> : <div className="configuration-list">
      {visibleServices.map((service) => {
        const isActive = configured.get(service.id) ?? false
        const isSaving = savingServiceId === service.id
        const isUnavailable = !service.isActive
        return <div className={`configuration-list__row ${isActive ? 'configuration-list__row--active' : ''}`} key={service.id}>
          <div><strong>{service.name}</strong><span>{service.code} <Clock3 size={13} /> {service.serviceDurationMinutes} minutes</span><StatusBadge tone={isUnavailable ? 'inactive' : 'active'}>{isUnavailable ? 'Unavailable in catalogue' : 'Available in catalogue'}</StatusBadge></div>
          <button type="button" className={`button ${isActive ? 'button--primary' : 'button--secondary'}`} disabled={isDisabled || isSaving || isUnavailable} onClick={() => void toggle(service.id)} aria-pressed={isActive} aria-label={`${isActive ? 'Disable' : 'Enable'} ${service.name} at this branch`}>
            {isSaving ? <LoadingOverlay label="Saving..." /> : isUnavailable ? 'Unavailable' : isActive ? 'Enabled' : 'Enable'}
          </button>
        </div>
      })}
    </div>}
  </section>
}
