import { Building2, ChevronLeft, ChevronRight, Plus, Search } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ConfirmationDialog } from '../../../components/ui/ConfirmationDialog'
import { useToast } from '../../../components/ui/ToastProvider'
import { useAuth } from '../../auth/providers/AuthProvider'
import { useBranches } from '../hooks/useBranches'
import type { Branch } from '../types/branch.types'
import { BranchTableRow } from './BranchTableRow'
import { CreateBranchDialog } from './CreateBranchDialog'

const pageSizeOptions = [5, 10, 20, 50]

export function BranchesPage() {
  const { branches, pageNumber, pageSize, searchTerm, totalCount, totalPages, isLoading, error, refresh, addBranch, disableBranch, goToPage, updatePageSize, updateSearchTerm } = useBranches()
  const navigate = useNavigate()
  const { hasRole } = useAuth()
  const { showToast } = useToast()
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [deactivatingBranchId, setDeactivatingBranchId] = useState<number | null>(null)
  const [branchToDeactivate, setBranchToDeactivate] = useState<Branch | null>(null)

  const deactivate = async (branch: Branch) => {
    setDeactivatingBranchId(branch.branchId)
    try {
      await disableBranch(branch.branchId)
      showToast('Branch deactivated and bookings disabled.')
      setBranchToDeactivate(null)
    } catch (deactivationError) {
      showToast(deactivationError instanceof Error ? deactivationError.message : 'We could not deactivate this branch.', 'error')
    } finally {
      setDeactivatingBranchId(null)
    }
  }

  const editBranch = (branch: Branch) => navigate(`/branches/${branch.branchId}/configuration`, { state: { branch } })
  const emptyMessage = searchTerm
    ? 'No branches match your search. Try a different term.'
    : hasRole(['admin']) ? 'Add your first branch to begin configuring bookings.' : 'No branches are available yet.'

  return <div className="branches-page">
    <div className="page-heading">
      <div>
        <span className="eyebrow">Workspace / branches</span>
        <h1>Your branches</h1>
        <p className="page-heading__description">Keep your locations organised and ready for every customer.</p>
      </div>
      {hasRole(['admin']) && <button className="button button--primary" onClick={() => setIsCreateOpen(true)}><Plus size={18} /> Add branch</button>}
    </div>
    <section className="section-card branch-table-card">
      <div className="section-card__heading">
        <div><h2>All branches</h2><p>{totalCount} locations in your network</p></div>
        <div className="filter-controls">
          <label className="search-box branch-search"><Search size={16} /><input value={searchTerm} placeholder="Search branches" aria-label="Search branches" onChange={event => updateSearchTerm(event.target.value)} /></label>
          <label className="filter-select"><span>Rows</span><select value={pageSize} aria-label="Branches per page" onChange={event => updatePageSize(Number(event.target.value))}>{pageSizeOptions.map(size => <option key={size} value={size}>{size}</option>)}</select></label>
        </div>
      </div>
      {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /><div className="skeleton-card" /></div>
        : error ? <div className="error-state"><p>{error}</p><button className="button button--secondary" onClick={() => void refresh()}>Try again</button></div>
        : branches.length ? <>
          <div className="branch-table-wrapper">
            <table className="branch-table">
              <thead><tr><th>Branch</th><th>Location</th><th>Status</th><th>Bookings</th><th><span className="sr-only">Actions</span></th></tr></thead>
              <tbody>{branches.map(branch => <BranchTableRow key={branch.branchId} branch={branch} isDeactivating={deactivatingBranchId === branch.branchId} onEdit={editBranch} onDeactivate={setBranchToDeactivate} />)}</tbody>
            </table>
          </div>
          <div className="table-pagination">
            <span>Page {pageNumber} of {totalPages || 1}</span>
            <div>
              <button type="button" className="icon-button" aria-label="Previous page" disabled={pageNumber <= 1} onClick={() => goToPage(pageNumber - 1)}><ChevronLeft size={17} /></button>
              <button type="button" className="icon-button" aria-label="Next page" disabled={pageNumber >= totalPages} onClick={() => goToPage(pageNumber + 1)}><ChevronRight size={17} /></button>
            </div>
          </div>
        </>
        : <div className="empty-state"><span className="empty-state__icon"><Building2 size={26} /></span><h3>No branches found</h3><p className="muted">{emptyMessage}</p></div>}
    </section>
    {isCreateOpen && <CreateBranchDialog onClose={() => setIsCreateOpen(false)} onSubmit={async request => { const branchId = await addBranch(request); navigate(`/branches/${branchId}/configuration`) }} />}
    {branchToDeactivate && <ConfirmationDialog title={`Deactivate ${branchToDeactivate.name}?`} description="This will stop the branch from accepting bookings." confirmLabel="Deactivate branch" isConfirming={deactivatingBranchId === branchToDeactivate.branchId} onConfirm={() => void deactivate(branchToDeactivate)} onClose={() => { if (!deactivatingBranchId) setBranchToDeactivate(null) }} />}
  </div>
}
