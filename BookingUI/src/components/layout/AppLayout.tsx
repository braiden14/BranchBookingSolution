import { Building2, CalendarDays, ChevronDown, LayoutDashboard, LogOut, Menu, UsersRound, UserRoundCog, Wrench, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../../features/auth/providers/AuthProvider'

const navigation = [
  { label: 'Overview', icon: LayoutDashboard, to: '/overview' },
  { label: 'Branches', icon: Building2, to: '/branches' },
  { label: 'Appointments', icon: CalendarDays, to: '/appointments' },
  { label: 'Customers', icon: UsersRound, to: '/customers' },
  { label: 'Staff', icon: UserRoundCog, to: '/staff' },
  { label: 'Services', icon: Wrench, to: '/services' },
]

export function AppLayout() {
  const [isOpen, setIsOpen] = useState(false)
  const { user, signOut } = useAuth()
  const initials = user?.name.split(' ').filter(Boolean).map(part => part[0]).join('').slice(0, 2).toUpperCase() ?? 'B'
  const roleLabel = user?.role === 'branch-manager' ? 'Branch manager' : user?.role ? user.role.charAt(0).toUpperCase() + user.role.slice(1) : ''
  const hour = new Date().getHours()
  const greeting = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening'

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape') setIsOpen(false) }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [])
  const roleNavigation = user?.role === 'branch-manager' && user.assignedBranchId
    ? navigation.map(item => item.to === '/branches' ? { ...item, label: 'Branch', to: `/branches/${user.assignedBranchId}/configuration` } : item)
    : navigation
  const visibleNavigation = user?.role === 'customer'
    ? navigation.filter(({ to }) => to === '/appointments')
    : user?.role === 'staff'
      ? roleNavigation.filter(({ to }) => to === '/appointments')
    : user?.role === 'branch-manager'
      ? roleNavigation.filter(({ to }) => to === '/appointments' || to.startsWith('/branches/'))
    : user?.role === 'admin'
      ? roleNavigation
    : roleNavigation

  return (
    <div className="app-shell">
      <aside className={`sidebar ${isOpen ? 'sidebar--open' : ''}`} aria-label="Primary navigation">
        <div className="sidebar__brand"><span className="brand-mark">B</span><span>BranchBook</span></div>
        <div className="sidebar__section-label">Workspace</div>
        <nav className="sidebar__nav">
          {visibleNavigation.map(({ label, icon: Icon, to }) => <NavLink key={to} to={to} onClick={() => setIsOpen(false)} className={({ isActive }) => `nav-item ${isActive ? 'nav-item--active' : ''}`}><Icon size={19} /><span>{label}</span></NavLink>)}
        </nav>
        <div className="sidebar__footer"><a className="help-card" href="mailto:support@branchbook.example"><span className="help-card__dot" />Need a hand?<strong>Contact support</strong></a><button className="user-menu" onClick={signOut} aria-label={`Sign out ${user?.name ?? 'user'}`}><span className="avatar">{initials}</span><span className="user-menu__text"><strong>{user?.name}</strong><small>{roleLabel}</small></span><LogOut size={16} /></button></div>
      </aside>
      {isOpen && <button className="sidebar-backdrop" onClick={() => setIsOpen(false)} aria-label="Close navigation" />}
      <div className="main-area">
        <header className="mobile-header"><button className="icon-button" onClick={() => setIsOpen(!isOpen)} aria-label={isOpen ? 'Close navigation' : 'Open navigation'} aria-expanded={isOpen}>{isOpen ? <X /> : <Menu />}</button><span className="sidebar__brand"><span className="brand-mark">B</span>BranchBook</span></header>
        <header className="topbar"><div><h2>{greeting}, {user?.name.split(' ')[0]}</h2></div><div className="profile-chip" aria-label={`${user?.name}, ${roleLabel}`}><span className="avatar avatar--small">{initials}</span><span>{roleLabel}</span><ChevronDown size={15} /></div></header>
        <main className="page-content"><Outlet /></main>
      </div>
    </div>
  )
}
