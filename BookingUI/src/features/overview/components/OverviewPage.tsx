import { Building2, CheckCircle2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { getBranches } from '../../branches/services/branches.service'
import type { Branch } from '../../branches/types/branch.types'

export function OverviewPage() {
  const [branches, setBranches] = useState<Branch[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    void getBranches(1, 200).then((page) => {
      if (isMounted) { setBranches(page.items); setError(null) }
    }).catch(loadError => {
      if (isMounted) setError(loadError instanceof Error ? loadError.message : 'Could not load overview data.')
    }).finally(() => {
      if (isMounted) setIsLoading(false)
    })
    return () => { isMounted = false }
  }, [reloadKey])

  const activeCount = branches.filter((branch) => branch.status === 'Active').length
  const bookingCoverage = branches.length ? Math.round((branches.filter((branch) => branch.acceptsBookings).length / branches.length) * 100) : 0

  return <div className="overview-page">
    <div className="page-heading"><div><span className="eyebrow">Workspace / overview</span><h1>Overview</h1><p className="page-heading__description">A quick view of your branch network and booking readiness.</p></div></div>
    {error ? <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => setReloadKey(value => value + 1)}>Try again</button></div> : <div className="stats-row"><div className="stat-card"><span className="stat-card__icon stat-card__icon--blue"><Building2 size={20} /></span><div><span>Total branches</span><strong>{isLoading ? '—' : branches.length}</strong></div></div><div className="stat-card"><span className="stat-card__icon stat-card__icon--green"><CheckCircle2 size={20} /></span><div><span>Active locations</span><strong>{isLoading ? '—' : activeCount}</strong></div></div><div className="stat-card stat-card--accent"><div><span>Booking coverage</span><strong>{isLoading ? '—' : `${bookingCoverage}%`}</strong></div><span className="stat-card__trend">Ready to book</span></div></div>}
  </div>
}
