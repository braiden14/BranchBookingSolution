export type UserRole = 'admin' | 'branch-manager' | 'staff'

export interface AuthUser {
  id: string
  name: string
  email: string
  role: UserRole
}
