import { X } from 'lucide-react'
import { useState, type ChangeEvent, type FormEvent } from 'react'
import type { CreateBranchRequest } from '../types/branch.types'

const emptyForm: CreateBranchRequest = { code: '', name: '', addressLine1: '', city: '', province: '', postalCode: '' }

export function CreateBranchDialog({ onClose, onSubmit }: { onClose: () => void; onSubmit: (request: CreateBranchRequest) => Promise<void> }) {
  const [form, setForm] = useState(emptyForm)
  const [isSaving, setIsSaving] = useState(false)
  const update = (field: keyof CreateBranchRequest) => (event: ChangeEvent<HTMLInputElement>) => setForm((current) => ({ ...current, [field]: event.target.value }))
  const submit = async (event: FormEvent) => { event.preventDefault(); setIsSaving(true); await onSubmit(form); setIsSaving(false); onClose() }

  return <div className="dialog-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="create-branch-title">
    <div className="dialog__header"><div><span className="eyebrow">Branch management</span><h2 id="create-branch-title">Add a new branch</h2><p className="muted">Set up the details for your new location.</p></div><button className="icon-button" onClick={onClose} aria-label="Close dialog"><X size={20} /></button></div>
    <form onSubmit={submit}><div className="form-grid"><label>Branch code<input required value={form.code} onChange={update('code')} placeholder="e.g. JHB02" /></label><label>Branch name<input required value={form.name} onChange={update('name')} placeholder="e.g. Rosebank" /></label><label className="form-grid__wide">Address<input required value={form.addressLine1} onChange={update('addressLine1')} placeholder="Street address" /></label><label>City<input required value={form.city} onChange={update('city')} placeholder="Johannesburg" /></label><label>Province<input required value={form.province} onChange={update('province')} placeholder="Gauteng" /></label><label>Postal code<input required value={form.postalCode} onChange={update('postalCode')} placeholder="2196" /></label></div><div className="dialog__actions"><button type="button" className="button button--secondary" onClick={onClose}>Cancel</button><button type="submit" className="button button--primary" disabled={isSaving}>{isSaving ? 'Creating…' : 'Create branch'}</button></div></form>
  </section></div>
}
