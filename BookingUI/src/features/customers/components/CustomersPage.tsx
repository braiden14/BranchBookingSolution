import { ChevronLeft, ChevronRight, UsersRound } from 'lucide-react'
import { useEffect, useState } from 'react'
import { getCustomers } from '../services/customer.service'
import type { Customer, PagedCustomers } from '../types/customer.types'

const formatDate = (value: string) => new Intl.DateTimeFormat('en-ZA', { dateStyle: 'medium' }).format(new Date(value))

export function CustomersPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const [page, setPage] = useState<PagedCustomers | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let isCurrent = true
    setIsLoading(true)
    getCustomers(pageNumber)
      .then(result => { if (isCurrent) { setPage(result); setError(null) } })
      .catch(loadError => { if (isCurrent) setError(loadError instanceof Error ? loadError.message : 'Could not load customers.') })
      .finally(() => { if (isCurrent) setIsLoading(false) })
    return () => { isCurrent = false }
  }, [pageNumber, reloadKey])

  return <div className="customers-page"><div className="page-heading"><div><span className="eyebrow">Admin / directory</span><h1>Customers</h1><p className="page-heading__description">View all registered customers and their contact details.</p></div></div>{error ? <div className="error-state" role="alert"><p>{error}</p><button className="button button--secondary" onClick={() => setReloadKey(value => value + 1)}>Try again</button></div> : isLoading ? <div className="table-loading" aria-label="Loading customers"><div className="skeleton-card" /><div className="skeleton-card" /></div> : <CustomerTable customers={page?.items ?? []} />}{!error && page && <div className="table-pagination"><span>{page.totalCount ? `Showing ${(page.pageNumber - 1) * page.pageSize + 1}–${Math.min(page.pageNumber * page.pageSize, page.totalCount)} of ${page.totalCount}` : 'No customers'}</span><div><button className="icon-button" disabled={page.pageNumber <= 1} onClick={() => setPageNumber(current => current - 1)} aria-label="Previous page"><ChevronLeft size={16} /></button><span>Page {page.pageNumber} of {Math.max(page.totalPages, 1)}</span><button className="icon-button" disabled={page.pageNumber >= page.totalPages} onClick={() => setPageNumber(current => current + 1)} aria-label="Next page"><ChevronRight size={16} /></button></div></div>}</div>
}

function CustomerTable({ customers }: { customers: Customer[] }) {
  if (!customers.length) return <section className="empty-state"><UsersRound size={28} /><h2>No customers found</h2><p>Registered customers will appear here.</p></section>
  return <div className="section-card branch-table-card"><div className="branch-table-wrapper"><table className="branch-table responsive-table"><thead><tr><th>Customer</th><th>Email</th><th>Phone</th><th>Joined</th><th>Status</th></tr></thead><tbody>{customers.map(customer => <tr key={customer.customerId}><td data-label="Customer"><div className="branch-table__name"><div><strong>{customer.firstName} {customer.lastName}</strong><span>Customer #{customer.customerId}</span></div></div></td><td data-label="Email">{customer.email}</td><td data-label="Phone">{customer.phoneNumber || '—'}</td><td data-label="Joined">{formatDate(customer.createdAtUtc)}</td><td data-label="Status"><span className={`status-badge ${customer.status === 1 ? 'status-badge--active' : 'status-badge--inactive'}`}>{customer.status === 1 ? 'Active' : 'Inactive'}</span></td></tr>)}</tbody></table></div></div>
}
