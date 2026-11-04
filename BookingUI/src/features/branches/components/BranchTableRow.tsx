import { Edit3, Power } from 'lucide-react'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import type { Branch } from '../types/branch.types'

interface BranchTableRowProps {
  branch: Branch
  isDeactivating: boolean
  onEdit: (branch: Branch) => void
  onDeactivate: (branch: Branch) => void
}

const getInitials = (name: string) => name.split(' ').map(word => word[0]).slice(0, 2).join('')

export function BranchTableRow({ branch, isDeactivating, onEdit, onDeactivate }: BranchTableRowProps) {
  return (
    <tr>
      <td>
        <div className="branch-table__name">
          <span className="branch-table__initials">{getInitials(branch.name)}</span>
          <div><strong>{branch.name}</strong><span>{branch.code}</span></div>
        </div>
      </td>
      <td>{branch.addressLine1}, {branch.city}<small>{branch.province} {branch.postalCode}</small></td>
      <td><StatusBadge tone={branch.status === 'Active' ? 'active' : 'inactive'}>{branch.status}</StatusBadge></td>
      <td>
        <span className={`booking-state ${branch.acceptsBookings ? 'booking-state--active' : ''}`}>
          <span />{branch.acceptsBookings ? 'Accepting bookings' : 'Bookings paused'}
        </span>
      </td>
      <td>
        <div className="table-actions">
          <button type="button" className="icon-button" title="Edit branch" aria-label={`Edit ${branch.name}`} onClick={() => onEdit(branch)}><Edit3 size={16} /></button>
          <button type="button" className="icon-button icon-button--danger" title="Deactivate branch" aria-label={`Deactivate ${branch.name}`} disabled={branch.status !== 'Active' || isDeactivating} onClick={() => onDeactivate(branch)}><Power size={16} /></button>
        </div>
      </td>
    </tr>
  )
}
