import { ChevronDown, Search } from 'lucide-react'
import type { CustomerBranch } from '../types/appointment.types'

interface BranchPickerProps {
  branches: CustomerBranch[]
  selectedBranchId: number
  search: string
  isOpen: boolean
  onOpen: () => void
  onSearchChange: (value: string) => void
  onSelect: (branch: CustomerBranch) => void
}

export function BranchPicker({ branches, selectedBranchId, search, isOpen, onOpen, onSearchChange, onSelect }: BranchPickerProps) {
  return (
    <div className="branch-picker">
      <div className={`booking-input booking-input--with-icon ${isOpen ? 'booking-input--active' : ''}`}>
        <Search size={16} />
        <input value={search} placeholder="Search branches" role="combobox" aria-expanded={isOpen} aria-controls="branch-picker-options" aria-autocomplete="list" onFocus={onOpen} onChange={event => onSearchChange(event.target.value)} />
        <ChevronDown size={16} aria-hidden="true" />
      </div>
      {isOpen && (
        <div id="branch-picker-options" className="branch-picker__menu" role="listbox">
          {branches.length
            ? branches.map(branch => (
              <button type="button" role="option" aria-selected={branch.branchId === selectedBranchId} key={branch.branchId} onClick={() => onSelect(branch)}>
                <span>{branch.name}</span>
                <small>{branch.city} · {branch.code}</small>
              </button>
            ))
            : <p className="branch-picker__empty">No branches match that search.</p>}
        </div>
      )}
    </div>
  )
}
