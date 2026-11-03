import { useEffect, useState, type FormEvent } from 'react'
import { ConfirmationDialog } from '../../../components/ui/ConfirmationDialog'
import { useToast } from '../../../components/ui/ToastProvider'
import { getBranches } from '../../branches/services/branches.service'
import type { Branch } from '../../branches/types/branch.types'
import { staffService } from '../services/staff.service'
import type { InviteStaffRequest, PagedStaff, StaffMember, StaffRole, StaffStatus } from '../types/staff.types'

const statusLabels: Record<StaffStatus, string> = { 0: 'Pending', 1: 'Active', 2: 'Locked', 3: 'Disabled', 4: 'Invited' }
const roleLabel = (member: StaffMember) => member.roles.some(role => role.toLowerCase() === 'admin') ? 'Admin' : member.roles.some(role => role.toLowerCase().replace(' ', '') === 'branchmanager') ? 'Branch manager' : 'Staff'

export function StaffPage() {
  const { showToast } = useToast()
  const [page, setPage] = useState<PagedStaff | null>(null)
  const [pageNumber, setPageNumber] = useState(1)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const [isInviteOpen, setIsInviteOpen] = useState(false)
  const [memberToChange, setMemberToChange] = useState<StaffMember | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [invitationLink, setInvitationLink] = useState<string | null>(null)

  const load = async () => {
    setIsLoading(true)
    try { setPage(await staffService.getStaff(pageNumber)); setError(null) }
    catch (loadError) { setError(loadError instanceof Error ? loadError.message : 'Could not load staff.') }
    finally { setIsLoading(false) }
  }

  useEffect(() => { void load() }, [pageNumber, reloadKey])

  const updateStatus = async () => {
    if (!memberToChange) return
    setIsSaving(true)
    try {
      const status: StaffStatus = memberToChange.status === 1 ? 3 : 1
      await staffService.updateStatus(memberToChange.userId, status)
      showToast(status === 1 ? 'Staff member activated.' : 'Staff member deactivated.')
      setMemberToChange(null)
      await load()
    } catch (saveError) { showToast(saveError instanceof Error ? saveError.message : 'The staff status could not be updated.', 'error') }
    finally { setIsSaving(false) }
  }

  return <div className="staff-page">
    <div className="page-heading"><div><span className="eyebrow">Admin / directory</span><h1>Staff</h1><p className="page-heading__description">Invite and manage the people responsible for branch operations.</p></div><button className="button button--primary" onClick={() => setIsInviteOpen(true)}>Invite staff</button></div>
    {error ? <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => setReloadKey(value => value + 1)}>Try again</button></div> : isLoading ? <div className="table-loading" aria-label="Loading staff"><div className="skeleton-card" /><div className="skeleton-card" /></div> : <>
      <section className="section-card branch-table-card"><div className="branch-table-wrapper"><table className="branch-table responsive-table"><thead><tr><th>Staff member</th><th>Role</th><th>Branch</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{page?.items.map(member => <tr key={member.userId}><td data-label="Staff member"><div className="branch-table__name"><div><strong>{member.firstName} {member.lastName}</strong><span>{member.email}</span></div></div></td><td data-label="Role">{roleLabel(member)}</td><td data-label="Branch">{member.branchName ?? '—'}</td><td data-label="Status"><span className={`status-badge ${member.status === 1 ? 'status-badge--active' : 'status-badge--inactive'}`}>{statusLabels[member.status]}</span></td><td data-label="Actions"><button type="button" className="button button--secondary" onClick={() => setMemberToChange(member)}>{member.status === 1 ? 'Deactivate' : 'Activate'}</button></td></tr>)}</tbody></table></div>{!page?.items.length && <div className="empty-state"><h2>No staff found</h2><p>Invite a staff member to begin.</p></div>}</section>
      {page && <div className="table-pagination"><span>{page.totalCount} staff members</span><div><button className="icon-button" disabled={page.pageNumber <= 1} onClick={() => setPageNumber(value => value - 1)} aria-label="Previous page">‹</button><span>Page {page.pageNumber} of {Math.max(page.totalPages, 1)}</span><button className="icon-button" disabled={page.pageNumber >= page.totalPages} onClick={() => setPageNumber(value => value + 1)} aria-label="Next page">›</button></div></div>}
    </>}
    {isInviteOpen && <InviteStaffDialog onClose={() => setIsInviteOpen(false)} onInvited={token => { setInvitationLink(`${window.location.origin}/accept-invitation?token=${encodeURIComponent(token)}`); setIsInviteOpen(false); void load() }} />}
    {invitationLink && <InvitationLinkDialog link={invitationLink} onClose={() => setInvitationLink(null)} />}
    {memberToChange && <ConfirmationDialog title={`${memberToChange.status === 1 ? 'Deactivate' : 'Activate'} ${memberToChange.firstName} ${memberToChange.lastName}?`} description={memberToChange.status === 1 ? 'This will prevent the staff member from signing in and revoke active sessions.' : 'This will allow the staff member to sign in again.'} confirmLabel={memberToChange.status === 1 ? 'Deactivate' : 'Activate'} isConfirming={isSaving} onConfirm={() => void updateStatus()} onClose={() => { if (!isSaving) setMemberToChange(null) }} />}
  </div>
}

function InvitationLinkDialog({ link, onClose }: { link: string; onClose: () => void }) {
  const { showToast } = useToast()
  const copy = async () => {
    try { await navigator.clipboard.writeText(link); showToast('Invitation link copied.') }
    catch { showToast('Copy failed. Select and copy the link manually.', 'error') }
  }
  return <ConfirmationDialog title="Invitation created" description={<span className="invitation-link-content">Print or share this development invitation link: <input className="invitation-link" readOnly value={link} aria-label="Invitation link" /><button type="button" className="button button--secondary" onClick={() => void copy()}>Copy link</button></span>} confirmLabel="Close" onConfirm={onClose} onClose={onClose} />
}

function InviteStaffDialog({ onClose, onInvited }: { onClose: () => void; onInvited: (token: string) => void }) {
  const { showToast } = useToast()
  const [branches, setBranches] = useState<Branch[]>([])
  const [form, setForm] = useState<InviteStaffRequest>({ email: '', firstName: '', lastName: '', phoneNumber: null, roleCode: 'Staff', branchId: null })
  const [isSaving, setIsSaving] = useState(false)
  useEffect(() => { getBranches(1, 200).then(result => setBranches(result.items)).catch(() => showToast('Could not load branches.', 'error')) }, [])
  const update = (field: keyof InviteStaffRequest, value: string | number | null) => setForm(current => ({ ...current, [field]: value }))
  const isAdmin = form.roleCode === 'Admin'
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSaving(true)
    try { const result = await staffService.invite(form); onInvited(result.invitationToken) }
    catch (error) { showToast(error instanceof Error ? error.message : 'The invitation could not be created.', 'error') }
    finally { setIsSaving(false) }
  }
  return <div className="dialog-backdrop"><section className="dialog" role="dialog" aria-modal="true" aria-labelledby="invite-staff-title"><div className="dialog__header"><div><span className="eyebrow">Admin / staff</span><h2 id="invite-staff-title">Invite staff member</h2></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close invitation dialog">×</button></div><form className="login-form" onSubmit={submit}><label>Email<input required type="email" autoComplete="email" value={form.email} onChange={event => update('email', event.target.value)} /></label><label>First name<input required autoComplete="given-name" value={form.firstName} onChange={event => update('firstName', event.target.value)} /></label><label>Last name<input required autoComplete="family-name" value={form.lastName} onChange={event => update('lastName', event.target.value)} /></label><label>Phone number<input type="tel" autoComplete="tel" value={form.phoneNumber ?? ''} onChange={event => update('phoneNumber', event.target.value || null)} /></label><label>Role<select className="modal-select" value={form.roleCode} onChange={event => { const role = event.target.value as StaffRole; update('roleCode', role); if (role === 'Admin') update('branchId', null) }}><option value="Staff">Staff</option><option value="BranchManager">Branch manager</option><option value="Admin">Admin</option></select></label><label>Branch<select className="modal-select" required={!isAdmin} disabled={isAdmin} value={form.branchId ?? ''} onChange={event => update('branchId', event.target.value ? Number(event.target.value) : null)}><option value="">{isAdmin ? 'Not required for Admin' : 'Select a branch'}</option>{branches.map(branch => <option key={branch.branchId} value={branch.branchId}>{branch.name}</option>)}</select></label><div className="configuration-actions"><button type="button" className="button button--secondary" onClick={onClose}>Cancel</button><button type="submit" className="button button--primary" disabled={isSaving || (!isAdmin && !form.branchId)} aria-busy={isSaving}>{isSaving ? 'Creating…' : 'Create invitation'}</button></div></form></section></div>
}
