const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''
let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export async function apiClient<T>(path: string, options?: RequestInit): Promise<T> {
  const headers = new Headers(options?.headers)
  headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    credentials: 'include',
    headers,
  })
  if (response.status === 401 && path !== '/api/auth/login' && path !== '/api/auth/refresh') {
    const refreshed = await fetch(`${API_BASE_URL}/api/auth/refresh`, { method: 'POST', credentials: 'include' })
    if (refreshed.ok) {
      const authentication = await refreshed.json() as { accessToken: string }
      setAccessToken(authentication.accessToken)
      return apiClient<T>(path, options)
    }
  }
  if (!response.ok) {
    let message = `Request failed with status ${response.status}`
    try {
      const problem = await response.json() as { detail?: string }
      if (problem.detail) message = problem.detail
    } catch {
      // Keep the status-based message when the response has no JSON body.
    }
    throw new Error(message)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}
