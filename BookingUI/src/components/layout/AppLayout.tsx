import { ChevronDown, LogOut, Menu, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { getNavigation, type NavigationItem } from '../../app/navigation'
import { useAuth } from '../../features/auth/providers/AuthProvider'

const getInitials = (name?: string) => name?.split(' ').filter(Boolean).map(part => part[0]).join('').slice(0, 2).toUpperCase() ?? 'B'

const getRoleLabel = (role?: string) => {
  if (role === 'branch-manager') return 'Branch manager'
  return role ? role.charAt(0).toUpperCase() + role.slice(1) : ''
}

const getGreeting = () => {
  const hour = new Date().getHours()
  return hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening'
}

function SidebarNavItem({ item, onNavigate }: { item: NavigationItem; onNavigate: () => void }) {
  const Icon = item.icon
  return (
    <NavLink to={item.to} onClick={onNavigate} className={({ isActive }) => `nav-item ${isActive ? 'nav-item--active' : ''}`}>
      <Icon size={19} />
      <span>{item.label}</span>
    </NavLink>
  )
}

interface ProfileMenuProps {
  name?: string
  initials: string
  roleLabel: string
  onSignOut: () => void
}

function ProfileMenu({ name, initials, roleLabel, onSignOut }: ProfileMenuProps) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
      const closeOnEscape = (event: KeyboardEvent) => {
        if (event.key === 'Escape') {
          setIsOpen(false)
          triggerRef.current?.focus()
        }
      }
    const closeOnOutsideClick = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) setIsOpen(false)
    }
    window.addEventListener('keydown', closeOnEscape)
    document.addEventListener('mousedown', closeOnOutsideClick)
    return () => {
      window.removeEventListener('keydown', closeOnEscape)
      document.removeEventListener('mousedown', closeOnOutsideClick)
    }
  }, [])

  useEffect(() => {
    if (isOpen) menuRef.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus()
  }, [isOpen])

  return (
    <div className="profile-menu" ref={menuRef}>
      <button ref={triggerRef} type="button" className="profile-chip" aria-haspopup="menu" aria-expanded={isOpen} aria-label={`${name}, ${roleLabel}. Account menu`} onClick={() => setIsOpen(open => !open)}>
        <span className="avatar avatar--small">{initials}</span>
        <span>{roleLabel}</span>
        <ChevronDown size={15} />
      </button>
      {isOpen && (
        <div className="profile-menu__panel" role="menu" onKeyDown={event => {
          if (event.key === 'Escape') {
            event.preventDefault()
            setIsOpen(false)
            triggerRef.current?.focus()
          }
        }}>
          <div className="profile-menu__identity"><strong>{name}</strong><small>{roleLabel}</small></div>
          <button type="button" role="menuitem" className="profile-menu__item" onClick={() => { setIsOpen(false); onSignOut() }}><LogOut size={15} /> Sign out</button>
        </div>
      )}
    </div>
  )
}

export function AppLayout() {
  const [isOpen, setIsOpen] = useState(false)
  const [isMobile, setIsMobile] = useState(() => window.matchMedia('(max-width: 760px)').matches)
  const { user, signOut } = useAuth()
  const initials = getInitials(user?.name)
  const roleLabel = getRoleLabel(user?.role)
  const greeting = getGreeting()
  const visibleNavigation = getNavigation(user)

  useEffect(() => {
    const query = window.matchMedia('(max-width: 760px)')
    const onChange = (event: MediaQueryListEvent) => { setIsMobile(event.matches); if (!event.matches) setIsOpen(false) }
    query.addEventListener('change', onChange)
    return () => query.removeEventListener('change', onChange)
  }, [])

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape') setIsOpen(false) }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [])

  return (
    <div className="app-shell">
      <aside className={`sidebar ${isOpen ? 'sidebar--open' : ''}`} aria-label="Primary navigation" inert={isMobile && !isOpen}>
        <div className="sidebar__brand"><span className="brand-mark">B</span><span>BranchBook</span></div>
        <div className="sidebar__section-label">Workspace</div>
        <nav className="sidebar__nav">
          {visibleNavigation.map(item => <SidebarNavItem key={item.to} item={item} onNavigate={() => setIsOpen(false)} />)}
        </nav>
        <div className="sidebar__footer">
          <a className="help-card" href="mailto:support@branchbook.example"><span className="help-card__dot" />Need a hand?<strong>Contact support</strong></a>
          <div className="user-menu" aria-label={`${user?.name ?? 'User'} account`}>
            <span className="avatar">{initials}</span>
            <span className="user-menu__text"><strong>{user?.name}</strong><small>{roleLabel}</small></span>
          </div>
        </div>
      </aside>
      {isOpen && <button className="sidebar-backdrop" onClick={() => setIsOpen(false)} aria-label="Close navigation" />}
      <div className="main-area">
        <header className="mobile-header">
          <button className="icon-button" onClick={() => setIsOpen(!isOpen)} aria-label={isOpen ? 'Close navigation' : 'Open navigation'} aria-expanded={isOpen}>{isOpen ? <X /> : <Menu />}</button>
          <span className="sidebar__brand"><span className="brand-mark">B</span>BranchBook</span>
        </header>
        <header className="topbar">
          <div><h2>{greeting}, {user?.name.split(' ')[0]}</h2></div>
          <ProfileMenu name={user?.name} initials={initials} roleLabel={roleLabel} onSignOut={signOut} />
        </header>
        <main className="page-content"><Outlet /></main>
      </div>
    </div>
  )
}
