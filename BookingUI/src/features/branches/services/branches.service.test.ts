import { describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../../lib/api-client'
import { getBranch, getBranches } from './branches.service'

vi.mock('../../../lib/api-client', () => ({
  apiClient: vi.fn(),
}))

describe('getBranches', () => {
  it('uses the default paging query without an empty search term', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ items: [], pageNumber: 1, pageSize: 5, totalCount: 0, totalPages: 0 })

    await getBranches()

    expect(apiClient).toHaveBeenCalledWith('/api/branches?pageNumber=1&pageSize=5')
  })

describe('getBranch', () => {
  it('loads a branch by id for direct configuration navigation', async () => {
    const branch = { branchId: 7, code: 'CBD', name: 'Central', addressLine1: '1 Main', city: 'Cape Town', province: 'Western Cape', postalCode: '8000', status: 'Active', acceptsBookings: true, createdAtUtc: '2025-01-01T00:00:00Z' }
    vi.mocked(apiClient).mockResolvedValueOnce(branch)

    await expect(getBranch(7)).resolves.toEqual(branch)

    expect(apiClient).toHaveBeenCalledWith('/api/branches/7')
  })
})

  it('trims and encodes the search term', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ items: [], pageNumber: 2, pageSize: 10, totalCount: 0, totalPages: 0 })

    await getBranches(2, 10, '  Cape Town & North  ')

    expect(apiClient).toHaveBeenCalledWith('/api/branches?pageNumber=2&pageSize=10&searchTerm=Cape+Town+%26+North')
  })
})
