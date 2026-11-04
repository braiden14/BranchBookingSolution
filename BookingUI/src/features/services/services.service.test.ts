import { describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../lib/api-client'
import { getServices, servicesService } from './services.service'

vi.mock('../../lib/api-client', () => ({ apiClient: vi.fn() }))

describe('servicesService', () => {
  it('requests database pagination, search, and availability filtering', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ items: [], pageNumber: 2, pageSize: 10, totalCount: 0, totalPages: 0 })

    await getServices(2, 10, 'passport', 'available')

    expect(apiClient).toHaveBeenCalledWith('/api/offered-services?pageNumber=2&pageSize=10&searchTerm=passport&isActive=true')
  })

  it('creates a service through the admin catalogue endpoint', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ id: 8 })
    const request = { code: 'PASSPORT', name: 'Passport renewal', serviceDurationMinutes: 30, isActive: true }

    await servicesService.create(request)

    expect(apiClient).toHaveBeenCalledWith('/api/offered-services', { method: 'POST', body: JSON.stringify(request) })
  })

  it('updates and deletes a service by id', async () => {
    vi.mocked(apiClient).mockResolvedValue(undefined)

    await servicesService.update(8, { code: 'PASSPORT', name: 'Passport renewal', serviceDurationMinutes: 45, isActive: false })
    await servicesService.remove(8)

    expect(apiClient).toHaveBeenNthCalledWith(1, '/api/offered-services/8', expect.objectContaining({ method: 'PATCH' }))
    expect(apiClient).toHaveBeenNthCalledWith(2, '/api/offered-services/8', { method: 'DELETE' })
  })
})
