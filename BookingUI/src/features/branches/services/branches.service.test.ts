import { describe, expect, it, vi } from 'vitest'
import { apiClient } from '../../../lib/api-client'
import { getBranches } from './branches.service'

vi.mock('../../../lib/api-client', () => ({
  apiClient: vi.fn(),
}))

describe('getBranches', () => {
  it('uses the default paging query without an empty search term', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ items: [], pageNumber: 1, pageSize: 5, totalCount: 0, totalPages: 0 })

    await getBranches()

    expect(apiClient).toHaveBeenCalledWith('/api/branches?pageNumber=1&pageSize=5')
  })

  it('trims and encodes the search term', async () => {
    vi.mocked(apiClient).mockResolvedValueOnce({ items: [], pageNumber: 2, pageSize: 10, totalCount: 0, totalPages: 0 })

    await getBranches(2, 10, '  Cape Town & North  ')

    expect(apiClient).toHaveBeenCalledWith('/api/branches?pageNumber=2&pageSize=10&searchTerm=Cape+Town+%26+North')
  })
})
