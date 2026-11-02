const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export async function apiClient<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  })
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
