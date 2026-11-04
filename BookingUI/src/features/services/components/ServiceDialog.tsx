import { X } from 'lucide-react'
import { useState, type ChangeEvent, type FormEvent } from 'react'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
import { useToast } from '../../../components/ui/ToastProvider'
import type { OfferedService, OfferedServiceRequest } from '../services.service'

const emptyForm: OfferedServiceRequest = {
  code: '',
  name: '',
  serviceDurationMinutes: 30,
  isActive: true,
}

export function ServiceDialog({ service, onClose, onSubmit }: {
  service?: OfferedService
  onClose: () => void
  onSubmit: (request: OfferedServiceRequest) => Promise<void>
}) {
  const [form, setForm] = useState<OfferedServiceRequest>(service ? {
    code: service.code,
    name: service.name,
    serviceDurationMinutes: service.serviceDurationMinutes,
    isActive: service.isActive,
  } : emptyForm)
  const [isSaving, setIsSaving] = useState(false)
  const { showToast } = useToast()
  const update = (field: keyof OfferedServiceRequest) => (event: ChangeEvent<HTMLInputElement>) => setForm(current => ({
    ...current,
    [field]: event.target.type === 'checkbox' ? event.target.checked : event.target.type === 'number' ? Number(event.target.value) : event.target.value,
  }))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      await onSubmit(form)
      showToast(service ? 'Service updated successfully.' : 'Service created successfully.')
      onClose()
    } catch (saveError) {
      showToast(saveError instanceof Error ? saveError.message : `We could not ${service ? 'update' : 'create'} the service.`, 'error')
    } finally {
      setIsSaving(false)
    }
  }

  return <div className="dialog-backdrop" role="presentation" onMouseDown={event => event.target === event.currentTarget && !isSaving && onClose()}>
    <section className="dialog" role="dialog" aria-modal="true" aria-labelledby="service-dialog-title">
      <div className="dialog__header">
        <div><span className="eyebrow">Service catalogue</span><h2 id="service-dialog-title">{service ? 'Edit service' : 'Add service'}</h2><p className="muted">{service ? 'Update the details for this offered service.' : 'Add a service that branches can offer to customers.'}</p></div>
        <button type="button" className="icon-button" onClick={onClose} disabled={isSaving} aria-label="Close dialog"><X size={20} /></button>
      </div>
      <form onSubmit={submit}>
        <div className="form-grid">
          <label>Service code<input required maxLength={50} value={form.code} onChange={update('code')} placeholder="e.g. PASSPORT" /></label>
          <label>Service name<input required maxLength={200} value={form.name} onChange={update('name')} placeholder="e.g. Passport application" /></label>
          <label>Duration (minutes)<input required type="number" min="1" value={form.serviceDurationMinutes} onChange={update('serviceDurationMinutes')} /></label>
          <label className="checkbox-field"><input type="checkbox" checked={form.isActive ?? true} onChange={update('isActive')} /> Available for booking</label>
        </div>
        <div className="dialog__actions"><button type="button" className="button button--secondary" onClick={onClose} disabled={isSaving}>Cancel</button><button type="submit" className="button button--primary" disabled={isSaving}>{isSaving ? <LoadingOverlay label={service ? 'Updating…' : 'Creating…'} /> : service ? 'Update service' : 'Add service'}</button></div>
      </form>
    </section>
  </div>
}
