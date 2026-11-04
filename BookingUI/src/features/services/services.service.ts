import { apiClient } from '../../lib/api-client'

export interface OfferedService {
  id: number
  code: string
  name: string
  serviceDurationMinutes: number
  isActive: boolean
  createdAtUtc: string
}

export interface OfferedServiceRequest {
  code: string
  name: string
  serviceDurationMinutes: number
  isActive?: boolean
}

export interface PagedOfferedServices {
  items: OfferedService[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type ServiceAvailability = 'all' | 'available' | 'unavailable'

export async function getServices(pageNumber = 1, pageSize = 10, searchTerm = '', availability: ServiceAvailability = 'all'): Promise<PagedOfferedServices> {
  const query = new URLSearchParams({ pageNumber: String(pageNumber), pageSize: String(pageSize) })
  if (searchTerm.trim()) query.set('searchTerm', searchTerm.trim())
  if (availability !== 'all') query.set('isActive', String(availability === 'available'))
  return await apiClient<PagedOfferedServices>(`/api/offered-services?${query.toString()}`)
}

export function createService(request: OfferedServiceRequest) {
  return apiClient<{ id: number }>('/api/offered-services', { method: 'POST', body: JSON.stringify(request) })
}

export function updateService(id: number, request: OfferedServiceRequest) {
  return apiClient<void>(`/api/offered-services/${id}`, { method: 'PATCH', body: JSON.stringify(request) })
}

export function deleteService(id: number) {
  return apiClient<void>(`/api/offered-services/${id}`, { method: 'DELETE' })
}

export const servicesService = {
  getAll: () => getServices(1, 200).then(page => page.items),
  create: createService,
  update: updateService,
  remove: deleteService,
}
