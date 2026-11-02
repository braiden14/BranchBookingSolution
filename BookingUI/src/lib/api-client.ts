const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export async function apiClient<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options?.headers },
  })
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`)
  return response.json() as Promise<T>
}
