export type UserRole = 'admin' | 'branch-manager' | 'staff' | 'customer'

export interface AuthUser {
  id: string
  name: string
  email: string
  role: UserRole
  roles: UserRole[]
  assignedBranchId: number | null
}

export interface RegisterCustomerRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber: string | null
}

export interface AuthUserResponse {
  id: number
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  roles: string[]
  assignedBranchId: number | null
}

export interface AuthenticationResponse {
  user: AuthUserResponse
  accessToken: string
  accessTokenExpiresAtUtc: string
}
