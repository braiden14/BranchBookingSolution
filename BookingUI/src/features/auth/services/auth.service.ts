import { apiClient, setAccessToken } from '../../../lib/api-client'
import type { AuthenticationResponse, AuthUserResponse, RegisterCustomerRequest } from '../types/auth.types'

export const authService = {
  async login(email: string, password: string): Promise<AuthUserResponse> {
    const authentication = await apiClient<AuthenticationResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
    setAccessToken(authentication.accessToken)
    return authentication.user
  },
  async currentUser(): Promise<AuthUserResponse> {
    return apiClient<AuthUserResponse>('/api/auth/me')
  },
  async register(request: RegisterCustomerRequest): Promise<AuthUserResponse> {
    const authentication = await apiClient<AuthenticationResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(request),
    })
    setAccessToken(authentication.accessToken)
    return authentication.user
  },
  async logout(): Promise<void> {
    try {
      await apiClient<void>('/api/auth/logout', { method: 'POST' })
    } finally {
      setAccessToken(null)
    }
  },
}
