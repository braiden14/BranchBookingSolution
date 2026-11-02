import { ArrowUpRight, MapPin } from 'lucide-react'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import type { Branch } from '../types/branch.types'

export function BranchCard({ branch }: { branch: Branch }) {
  const initials = branch.name.split(' ').map((word) => word[0]).slice(0, 2).join('')
  return <article className="branch-card">
    <div className="branch-card__top"><span className="branch-card__initials">{initials}</span><StatusBadge tone={branch.status === 'Active' ? 'active' : 'inactive'}>{branch.status}</StatusBadge><button className="card-arrow" aria-label={`Open ${branch.name}`}><ArrowUpRight size={17} /></button></div>
    <div className="branch-card__body"><span className="branch-code">{branch.code}</span><h3>{branch.name}</h3><p><MapPin size={15} />{branch.addressLine1}, {branch.city}</p></div>
    <div className="branch-card__footer"><span>{branch.acceptsBookings ? 'Accepting bookings' : 'Bookings paused'}</span><span className={branch.acceptsBookings ? 'booking-dot booking-dot--active' : 'booking-dot'} /></div>
  </article>
}
