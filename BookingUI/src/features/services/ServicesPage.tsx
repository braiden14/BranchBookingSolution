import { ChevronLeft, ChevronRight, Edit3, Plus, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/ToastProvider'
import { ServiceDialog } from './components/ServiceDialog'
import { useServices, type ServiceAvailability } from './hooks/useServices'
import type { OfferedService } from './services.service'

export function ServicesPage() {
  const { services, pageNumber, pageSize, searchTerm, availability, totalCount, totalPages, isLoading, error, refresh, addService, editService, removeService, goToPage, updatePageSize, updateSearchTerm, updateAvailability } = useServices()
  const { showToast } = useToast()
  const [dialogService, setDialogService] = useState<OfferedService | undefined>()
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [deletingId, setDeletingId] = useState<number | null>(null)
  const openCreate = () => { setDialogService(undefined); setIsDialogOpen(true) }
  const openEdit = (service: OfferedService) => { setDialogService(service); setIsDialogOpen(true) }
  const closeDialog = () => { setIsDialogOpen(false); setDialogService(undefined) }
  const save = async (request: Parameters<typeof addService>[0]) => { if (dialogService) await editService(dialogService.id, request); else await addService(request) }
  const remove = async (service: OfferedService) => { if (!window.confirm(`Delete ${service.name}?`)) return; setDeletingId(service.id); try { await removeService(service.id); showToast('Service deleted successfully.') } catch (deleteError) { showToast(deleteError instanceof Error ? deleteError.message : 'We could not delete the service.', 'error') } finally { setDeletingId(null) } }

  return <div className="services-page">
    <div className="page-heading"><div><span className="eyebrow">Admin / catalogue</span><h1>Offered services</h1><p className="page-heading__description">Manage the services available to your branches.</p></div><button type="button" className="button button--primary" onClick={openCreate}><Plus size={18} /> Add service</button></div>
    <section className="section-card branch-table-card"><div className="section-card__heading"><div><h2>Service catalogue</h2><p>{totalCount} services in the catalogue</p></div><div className="filter-controls"><label className="search-box branch-search"><Search size={16} /><input value={searchTerm} placeholder="Search services" aria-label="Search services" onChange={event => updateSearchTerm(event.target.value)} /></label><label className="filter-select"><span>Availability</span><select value={availability} aria-label="Service availability" onChange={event => updateAvailability(event.target.value as ServiceAvailability)}><option value="all">All</option><option value="available">Available</option><option value="unavailable">Unavailable</option></select></label><label className="filter-select"><span>Rows</span><select value={pageSize} aria-label="Services per page" onChange={event => updatePageSize(Number(event.target.value))}><option value={5}>5</option><option value={10}>10</option><option value={20}>20</option><option value={50}>50</option></select></label></div></div>
      {isLoading ? <div className="table-loading"><div className="skeleton-card" /><div className="skeleton-card" /><div className="skeleton-card" /></div> : error ? <div className="error-state"><p>{error}</p><button type="button" className="button button--secondary" onClick={() => void refresh()}>Try again</button></div> : services.length ? <><div className="branch-table-wrapper"><table className="branch-table"><thead><tr><th>Code</th><th>Service</th><th>Duration</th><th>Status</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{services.map(service => <tr key={service.id}><td>{service.code}</td><td><strong>{service.name}</strong></td><td>{service.serviceDurationMinutes} minutes</td><td><StatusBadge tone={service.isActive ? 'active' : 'inactive'}>{service.isActive ? 'Available' : 'Unavailable'}</StatusBadge></td><td><div className="table-actions"><button type="button" className="icon-button" title="Edit service" aria-label={`Edit ${service.name}`} onClick={() => openEdit(service)}><Edit3 size={16} /></button><button type="button" className="icon-button icon-button--danger" title="Delete service" aria-label={`Delete ${service.name}`} disabled={deletingId === service.id} onClick={() => void remove(service)}><Trash2 size={16} /></button></div></td></tr>)}</tbody></table></div><div className="table-pagination"><span>Page {pageNumber} of {totalPages || 1}</span><div><button type="button" className="icon-button" aria-label="Previous page" disabled={pageNumber <= 1} onClick={() => goToPage(pageNumber - 1)}><ChevronLeft size={17} /></button><button type="button" className="icon-button" aria-label="Next page" disabled={pageNumber >= totalPages} onClick={() => goToPage(pageNumber + 1)}><ChevronRight size={17} /></button></div></div></> : <div className="empty-state"><h3>No services found</h3><p className="muted">Try changing the search or availability filter, or add a new service.</p></div>}
    </section>
    {isDialogOpen && <ServiceDialog service={dialogService} onClose={closeDialog} onSubmit={save} />}
  </div>
}
