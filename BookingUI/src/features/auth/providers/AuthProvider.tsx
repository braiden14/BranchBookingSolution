import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { AuthUser, UserRole } from '../types/auth.types'

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  hasRole: (roles: UserRole[]) => boolean
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>({
    id: 'demo-admin',
    name: 'Thabo Mokoena',
    email: 'thabo@branchbook.co.za',
    role: 'admin',
  })

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    hasRole: (roles) => user !== null && roles.includes(user.role),
    signOut: () => setUser(null),
  }), [user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
