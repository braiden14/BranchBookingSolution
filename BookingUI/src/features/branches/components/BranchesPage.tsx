import { Building2, ChevronLeft, ChevronRight, Edit3, Power, Plus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useToast } from '../../../components/ui/ToastProvider'
import { useAuth } from '../../auth/providers/AuthProvider'
import { useBranches } from '../hooks/useBranches'
import type { Branch, CreateBranchRequest } from '../types/branch.types'
import { CreateBranchDialog } from './CreateBranchDialog'

export function BranchesPage() {
  const { branches, pageNumber, totalCount, totalPages, isLoading, error, refresh, addBranch, editBranch, disableBranch, goToPage } = useBranches()
  const navigate = useNavigate()
  const { hasRole } = useAuth()
  const { showToast } = useToast()
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [editingBranch, setEditingBranch] = useState<Branch | null>(null)
  const [deactivatingBranchId, setDeactivatingBranchId] = useState<number | null>(null)

  const submitEdit = async (request: CreateBranchRequest) => {
    if (!editingBranch) return
    await editBranch(editingBranch.branchId, request)
    setEditingBranch(null)
  }

  const deactivate = async (branch: Branch) => {
    if (!window.confirm(`Deactivate ${branch.name}? This will stop the branch from accepting bookings.`)) return
    setDeactivatingBranchId(branch.branchId)
    try {
      await disableBranch(branch.branchId)
      showToast('Branch deactivated and bookings disabled.')
    } catch (deactivationError) {
      showToast(deactivationError instanceof Error ? deactivationError.message : 'We could not deactivate this branch.', 'error')
    } finally {
      setDeactivatingBranchId(null)
    }
  }

  return <div className="branches-page">
    <div className="page-heading"><div><span className="eyebrow">Workspace / branches</span><h1>Your branches</h1><p className="page-heading__description">Keep your locations organised and ready for every customer.</p></div>{hasRole(['admin']) && <button className="button button--primary" onClick={() => setIsCreateOpen(true)}><Plus size={18} /> Add branch</button>}</div>
    <section className="section-card branch-table-card"><div className="section-card__heading"><div><h2>All branches</h2><p>{totalCount} locations in your network</p></div></div>
      {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /><div className="skeleton-card" /></div> : error ? <div className="error-state"><p>{error}</p><button className="button button--secondary" onClick={() => void refresh()}>Try again</button></div> : branches.length ? <>
        <div className="branch-table-wrapper"><table className="branch-table"><thead><tr><th>Branch</th><th>Location</th><th>Status</th><th>Bookings</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{branches.map((branch) => <tr key={branch.branchId}><td><div className="branch-table__name"><span className="branch-table__initials">{branch.name.split(' ').map((word) => word[0]).slice(0, 2).join('')}</span><div><strong>{branch.name}</strong><span>{branch.code}</span></div></div></td><td>{branch.addressLine1}, {branch.city}<small>{branch.province} {branch.postalCode}</small></td><td><StatusBadge tone={branch.status === 'Active' ? 'active' : 'inactive'}>{branch.status}</StatusBadge></td><td><span className={`booking-state ${branch.acceptsBookings ? 'booking-state--active' : ''}`}><span />{branch.acceptsBookings ? 'Accepting bookings' : 'Bookings paused'}</span></td><td><div className="table-actions"><button type="button" className="icon-button" title="Edit branch" aria-label={`Edit ${branch.name}`} onClick={() => setEditingBranch(branch)}><Edit3 size={16} /></button><button type="button" className="icon-button icon-button--danger" title="Deactivate branch" aria-label={`Deactivate ${branch.name}`} disabled={branch.status !== 'Active' || deactivatingBranchId === branch.branchId} onClick={() => void deactivate(branch)}><Power size={16} /></button></div></td></tr>)}</tbody></table></div>
        <div className="table-pagination"><span>Page {pageNumber} of {totalPages || 1}</span><div><button type="button" className="icon-button" aria-label="Previous page" disabled={pageNumber <= 1} onClick={() => goToPage(pageNumber - 1)}><ChevronLeft size={17} /></button><button type="button" className="icon-button" aria-label="Next page" disabled={pageNumber >= totalPages} onClick={() => goToPage(pageNumber + 1)}><ChevronRight size={17} /></button></div></div>
      </> : <div className="empty-state"><span className="empty-state__icon"><Building2 size={26} /></span><h3>No branches found</h3><p className="muted">Add your first branch to begin configuring bookings.</p></div>}
    </section>
    {isCreateOpen && <CreateBranchDialog onClose={() => setIsCreateOpen(false)} onSubmit={async (request) => { const branchId = await addBranch(request); navigate(`/branches/${branchId}/configuration`) }} />}
    {editingBranch && <CreateBranchDialog branch={editingBranch} onClose={() => setEditingBranch(null)} onSubmit={submitEdit} />}
  </div>
}
