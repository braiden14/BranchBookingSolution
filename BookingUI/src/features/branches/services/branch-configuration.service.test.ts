import { describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../../lib/api-client'
import { getOfferedServices } from './branch-configuration.service'

vi.mock('../../../lib/api-client', () => ({
  apiClient: vi.fn(),
}))

describe('getOfferedServices', () => {
  it('unwraps the paged offered-services response for branch configuration', async () => {
    const items = [{ id: 3, code: 'CONSULT', name: 'Consultation', serviceDurationMinutes: 30, isActive: true, createdAtUtc: '2025-01-01T00:00:00Z' }]
    vi.mocked(apiClient).mockResolvedValueOnce({ items, pageNumber: 1, pageSize: 200, totalCount: 1, totalPages: 1 })

    await expect(getOfferedServices()).resolves.toEqual(items)

    expect(apiClient).toHaveBeenCalledWith('/api/offered-services?pageNumber=1&pageSize=200')
  })
})