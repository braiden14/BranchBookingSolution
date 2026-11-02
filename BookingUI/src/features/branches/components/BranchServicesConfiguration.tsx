import { Check, Clock3 } from 'lucide-react'
import { useState } from 'react'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
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
  const configured = new Map(branchServices.map((service) => [service.serviceId, service.isActive]))

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
    <div className="configuration-card__title"><span className="configuration-icon"><Check size={20} /></span><div><h2>Branch services</h2><p>Select the services customers can book at this branch.</p></div></div>
    {!offeredServices.length ? <p className="muted">No offered services are available yet.</p> : <div className="configuration-list">
      {offeredServices.map((service) => {
        const isActive = configured.get(service.id) ?? false
        const isSaving = savingServiceId === service.id
        return <div className={`configuration-list__row ${isActive ? 'configuration-list__row--active' : ''}`} key={service.id}>
          <div><strong>{service.name}</strong><span>{service.code} <Clock3 size={13} /> {service.serviceDurationMinutes} minutes</span></div>
          <button type="button" className={`button ${isActive ? 'button--primary' : 'button--secondary'}`} disabled={isDisabled || isSaving} onClick={() => void toggle(service.id)}>
            {isSaving ? <LoadingOverlay label="Saving..." /> : isActive ? 'Enabled' : 'Enable'}
          </button>
        </div>
      })}
    </div>}
  </section>
}
