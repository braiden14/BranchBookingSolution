import { Building2, CalendarDays, ChevronDown, CircleUserRound, LayoutDashboard, LogOut, Menu, UsersRound, X } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../../features/auth/providers/AuthProvider'

const navigation = [
  { label: 'Overview', icon: LayoutDashboard, to: '/' },
  { label: 'Branches', icon: Building2, to: '/branches' },
  { label: 'Appointments', icon: CalendarDays, to: '/appointments' },
  { label: 'Customers', icon: UsersRound, to: '/customers' },
]

export function AppLayout() {
  const [isOpen, setIsOpen] = useState(false)
  const { user, signOut } = useAuth()
  const visibleNavigation = user?.role === 'customer'
    ? navigation.filter(({ to }) => to === '/appointments')
    : navigation

  return (
    <div className="app-shell">
      <aside className={`sidebar ${isOpen ? 'sidebar--open' : ''}`}>
        <div className="sidebar__brand"><span className="brand-mark">B</span><span>BranchBook</span></div>
        <div className="sidebar__section-label">Workspace</div>
        <nav className="sidebar__nav">
          {visibleNavigation.map(({ label, icon: Icon, to }) => <NavLink key={to} to={to} onClick={() => setIsOpen(false)} className={({ isActive }) => `nav-item ${isActive ? 'nav-item--active' : ''}`}><Icon size={19} /><span>{label}</span></NavLink>)}
        </nav>
        <div className="sidebar__footer"><div className="help-card"><span className="help-card__dot" />Need a hand?<strong>Visit help centre</strong></div><button className="user-menu" onClick={signOut}><span className="avatar">TM</span><span className="user-menu__text"><strong>{user?.name}</strong><small>Administrator</small></span><LogOut size={16} /></button></div>
      </aside>
      {isOpen && <button className="sidebar-backdrop" onClick={() => setIsOpen(false)} aria-label="Close navigation" />}
      <div className="main-area">
        <header className="mobile-header"><button className="icon-button" onClick={() => setIsOpen(!isOpen)} aria-label="Toggle navigation">{isOpen ? <X /> : <Menu />}</button><span className="sidebar__brand"><span className="brand-mark">B</span>BranchBook</span></header>
        <header className="topbar"><div><h2>Good morning, {user?.name.split(' ')[0]}</h2></div><button className="profile-chip"><span className="avatar avatar--small">TM</span><span>My profile</span><ChevronDown size={15} /></button></header>
        <main className="page-content"><Outlet /></main>
      </div>
    </div>
  )
}
