import { Building2, CheckCircle2, Plus, Search, SlidersHorizontal, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAuth } from '../../auth/providers/AuthProvider'
import { BranchCard } from './BranchCard'
import { CreateBranchDialog } from './CreateBranchDialog'
import { useBranches } from '../hooks/useBranches'

export function BranchesPage() {
  const { branches, isLoading, error, refresh, addBranch } = useBranches()
  const { hasRole } = useAuth()
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'All' | 'Active' | 'Inactive'>('All')
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const filteredBranches = useMemo(() => branches.filter((branch) => {
    const matchesSearch = `${branch.name} ${branch.code} ${branch.city}`.toLowerCase().includes(search.toLowerCase())
    return matchesSearch && (statusFilter === 'All' || branch.status === statusFilter)
  }), [branches, search, statusFilter])
  const activeCount = branches.filter((branch) => branch.status === 'Active').length

  return <div className="branches-page">
    <div className="page-heading"><div><span className="eyebrow">Workspace / branches</span><h1>Your branches</h1><p className="page-heading__description">Keep your locations organised and ready for every customer.</p></div>{hasRole(['admin']) && <button className="button button--primary" onClick={() => setIsCreateOpen(true)}><Plus size={18} /> Add branch</button>}</div>
    <div className="stats-row"><div className="stat-card"><span className="stat-card__icon stat-card__icon--blue"><Building2 size={20} /></span><div><span>Total branches</span><strong>{branches.length}</strong></div></div><div className="stat-card"><span className="stat-card__icon stat-card__icon--green"><CheckCircle2 size={20} /></span><div><span>Active locations</span><strong>{activeCount}</strong></div></div><div className="stat-card stat-card--accent"><div><span>Booking coverage</span><strong>{branches.length ? Math.round((activeCount / branches.length) * 100) : 0}%</strong></div><span className="stat-card__trend">+12.5% <small>this month</small></span></div></div>
    <section className="section-card"><div className="section-card__heading"><div><h2>All branches</h2><p>{filteredBranches.length} locations in your network</p></div><div className="filter-controls"><div className="search-box"><Search size={17} /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search branches" aria-label="Search branches" />{search && <button onClick={() => setSearch('')} aria-label="Clear search"><X size={15} /></button>}</div><div className="filter-select"><SlidersHorizontal size={16} /><select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as typeof statusFilter)} aria-label="Filter by status"><option>All</option><option>Active</option><option>Inactive</option></select></div></div></div>
      {isLoading ? <div className="loading-grid">{[1, 2, 3].map((item) => <div className="skeleton-card" key={item} />)}</div> : error ? <div className="error-state"><p>{error}</p><button className="button button--secondary" onClick={() => void refresh()}>Try again</button></div> : filteredBranches.length ? <div className="branch-grid">{filteredBranches.map((branch) => <BranchCard branch={branch} key={branch.branchId} />)}</div> : <div className="empty-state"><span className="empty-state__icon">⌕</span><h3>No branches found</h3><p className="muted">Try changing your search or filter.</p></div>}
    </section>
    {isCreateOpen && <CreateBranchDialog onClose={() => setIsCreateOpen(false)} onSubmit={addBranch} />}
  </div>
}
