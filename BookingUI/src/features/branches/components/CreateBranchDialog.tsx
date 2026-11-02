import { X } from 'lucide-react'
import { useState, type ChangeEvent, type FormEvent } from 'react'
import { LoadingOverlay } from '../../../components/ui/LoadingOverlay'
import { useToast } from '../../../components/ui/ToastProvider'
import type { Branch, CreateBranchRequest } from '../types/branch.types'

const emptyForm: CreateBranchRequest = { code: '', name: '', addressLine1: '', city: '', province: '', postalCode: '' }

export function CreateBranchDialog({ onClose, onSubmit, branch }: { onClose: () => void; onSubmit: (request: CreateBranchRequest) => Promise<void>; branch?: Branch }) {
  const [form, setForm] = useState<CreateBranchRequest>(branch ? {
    code: branch.code,
    name: branch.name,
    addressLine1: branch.addressLine1,
    city: branch.city,
    province: branch.province,
    postalCode: branch.postalCode,
  } : emptyForm)
  const [isSaving, setIsSaving] = useState(false)
  const { showToast } = useToast()
  const update = (field: keyof CreateBranchRequest) => (event: ChangeEvent<HTMLInputElement>) => setForm((current) => ({ ...current, [field]: event.target.value }))
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    try {
      await onSubmit(form)
      showToast(branch ? 'Branch updated successfully.' : 'Branch created successfully.')
      onClose()
    } catch {
      showToast(`We could not ${branch ? 'update' : 'create'} the branch. Please try again.`, 'error')
    } finally {
      setIsSaving(false)
    }
  }

  return <div className="dialog-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="create-branch-title">
    <div className="dialog__header"><div><span className="eyebrow">Branch management</span><h2 id="create-branch-title">{branch ? 'Edit branch' : 'Add a new branch'}</h2><p className="muted">{branch ? 'Update the details for this location.' : 'Set up the details for your new location.'}</p></div><button className="icon-button" onClick={onClose} aria-label="Close dialog"><X size={20} /></button></div>
    <form onSubmit={submit}><div className="form-grid"><label>Branch code<input required value={form.code} onChange={update('code')} placeholder="e.g. JHB02" /></label><label>Branch name<input required value={form.name} onChange={update('name')} placeholder="e.g. Rosebank" /></label><label className="form-grid__wide">Address<input required value={form.addressLine1} onChange={update('addressLine1')} placeholder="Street address" /></label><label>City<input required value={form.city} onChange={update('city')} placeholder="Johannesburg" /></label><label>Province<input required value={form.province} onChange={update('province')} placeholder="Gauteng" /></label><label>Postal code<input required value={form.postalCode} onChange={update('postalCode')} placeholder="2196" /></label></div><div className="dialog__actions"><button type="button" className="button button--secondary" onClick={onClose} disabled={isSaving}>Cancel</button><button type="submit" className="button button--primary" disabled={isSaving}>{isSaving ? <LoadingOverlay label={branch ? 'Updating…' : 'Creating…'} /> : branch ? 'Update branch' : 'Create branch'}</button></div></form>
  </section></div>
}
