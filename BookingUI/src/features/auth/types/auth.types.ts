export type UserRole = 'admin' | 'branch-manager' | 'staff'

export interface AuthUser {
  id: string
  name: string
  email: string
  role: UserRole
  roles: UserRole[]
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
}

export interface AuthenticationResponse {
  user: AuthUserResponse
  accessToken: string
  accessTokenExpiresAtUtc: string
}
