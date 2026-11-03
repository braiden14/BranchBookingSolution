import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authService } from '../services/auth.service'
import type { AuthUser, UserRole } from '../types/auth.types'

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  hasRole: (roles: UserRole[]) => boolean
  signIn: (email: string, password: string) => Promise<AuthUser>
  signOut: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    void authService.currentUser().then((response) => {
      setUser(toAuthUser(response))
    }).catch(() => {
      setUser(null)
    }).finally(() => setIsLoading(false))
  }, [])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    isLoading,
    hasRole: (roles) => user !== null && roles.some((role) => user.roles.includes(role)),
    signIn: async (email, password) => {
      const signedInUser = toAuthUser(await authService.login(email, password))
      setUser(signedInUser)
      return signedInUser
    },
    signOut: () => {
      setUser(null)
      void authService.logout()
    },
  }), [isLoading, user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function toAuthUser(response: { id: number; email: string; firstName: string; lastName: string; roles: string[] }): AuthUser {
  const roles = response.roles.map((role) => role.toLowerCase().replace('administrator', 'admin').replace('branchmanager', 'branch-manager').replace('branch manager', 'branch-manager')) as UserRole[]
  return { id: response.id.toString(), name: `${response.firstName} ${response.lastName}`, email: response.email, role: roles[0] ?? 'staff', roles }
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider')
  return context
}
