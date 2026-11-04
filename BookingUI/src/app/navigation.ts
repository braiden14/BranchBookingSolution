import { Building2, CalendarDays, LayoutDashboard, UserRoundCog, UsersRound, Wrench, type LucideIcon } from 'lucide-react'
import type { AuthUser, UserRole } from '../features/auth/types/auth.types'

export interface NavigationItem {
  key: string
  label: string
  icon: LucideIcon
  to: string
}

const navigationItems: NavigationItem[] = [
  { key: 'overview', label: 'Overview', icon: LayoutDashboard, to: '/overview' },
  { key: 'branches', label: 'Branches', icon: Building2, to: '/branches' },
  { key: 'appointments', label: 'Appointments', icon: CalendarDays, to: '/appointments' },
  { key: 'customers', label: 'Customers', icon: UsersRound, to: '/customers' },
  { key: 'staff', label: 'Staff', icon: UserRoundCog, to: '/staff' },
  { key: 'services', label: 'Services', icon: Wrench, to: '/services' },
]

const navigationByRole: Record<UserRole, string[]> = {
  admin: ['overview', 'branches', 'appointments', 'customers', 'staff', 'services'],
  'branch-manager': ['branches', 'appointments'],
  staff: ['appointments'],
  customer: ['appointments'],
}

const appointmentsHomeRoles: UserRole[] = ['customer', 'staff', 'branch-manager']

export const homeRouteFor = (role?: UserRole) => (role && appointmentsHomeRoles.includes(role) ? '/appointments' : '/overview')

// Unknown or missing roles get no navigation rather than a default menu.
export function getNavigation(user: Pick<AuthUser, 'role' | 'assignedBranchId'> | null | undefined): NavigationItem[] {
  const allowed = user ? navigationByRole[user.role] : undefined
  if (!user || !allowed) return []

  return navigationItems
    .filter(item => allowed.includes(item.key))
    .filter(item => !(user.role === 'branch-manager' && item.key === 'branches' && !user.assignedBranchId))
    .map(item => user.role === 'branch-manager' && item.key === 'branches'
      ? { ...item, label: 'Branch', to: `/branches/${user.assignedBranchId}/configuration` }
      : item)
}
