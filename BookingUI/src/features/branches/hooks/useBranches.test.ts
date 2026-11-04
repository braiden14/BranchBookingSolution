import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createBranch, deactivateBranch, getBranches, updateBranch } from '../services/branches.service'
import { useBranches } from './useBranches'

vi.mock('../services/branches.service', () => ({
  createBranch: vi.fn(),
  deactivateBranch: vi.fn(),
  getBranches: vi.fn(),
  updateBranch: vi.fn(),
}))

const page = (pageNumber = 1, pageSize = 5, searchTerm = '') => ({
  items: [],
  pageNumber,
  pageSize,
  totalCount: 11,
  totalPages: 3,
  searchTerm,
})

describe('useBranches', () => {
  beforeEach(() => {
    vi.mocked(getBranches).mockResolvedValue(page())
    vi.mocked(createBranch).mockResolvedValue(42)
    vi.mocked(updateBranch).mockResolvedValue(undefined)
    vi.mocked(deactivateBranch).mockResolvedValue(undefined)
  })

  it('loads the first page with five rows by default', async () => {
    const { result } = renderHook(() => useBranches())

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getBranches).toHaveBeenCalledWith(1, 5, '')
    expect(result.current.totalCount).toBe(11)
    expect(result.current.totalPages).toBe(3)
  })

  it('resets to page one when the search term or page size changes', async () => {
    const { result } = renderHook(() => useBranches())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.goToPage(3))
    act(() => result.current.updateSearchTerm('Cape'))
    await waitFor(() => expect(getBranches).toHaveBeenLastCalledWith(1, 5, 'Cape'))

    act(() => result.current.updatePageSize(20))
    await waitFor(() => expect(getBranches).toHaveBeenLastCalledWith(1, 20, 'Cape'))
    expect(result.current.pageNumber).toBe(1)
  })

  it('clamps page navigation to the available range', async () => {
    const { result } = renderHook(() => useBranches())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.goToPage(99))
    await waitFor(() => expect(getBranches).toHaveBeenLastCalledWith(3, 5, ''))

    act(() => result.current.goToPage(0))
    await waitFor(() => expect(getBranches).toHaveBeenLastCalledWith(1, 5, ''))
  })

  it('refreshes after branch mutations and returns the new branch id', async () => {
    const { result } = renderHook(() => useBranches())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    await act(async () => {
      await expect(result.current.addBranch({ code: 'CT', name: 'Cape Town', addressLine1: '1 Main', city: 'Cape Town', province: 'WC', postalCode: '8000' })).resolves.toBe(42)
      await result.current.editBranch(42, { code: 'CT', name: 'Cape Town', addressLine1: '1 Main', city: 'Cape Town', province: 'WC', postalCode: '8000' })
      await result.current.disableBranch(42)
    })

    expect(createBranch).toHaveBeenCalledOnce()
    expect(updateBranch).toHaveBeenCalledWith(42, expect.any(Object))
    expect(deactivateBranch).toHaveBeenCalledWith(42)
    expect(vi.mocked(getBranches).mock.calls.length).toBe(4)
  })
})
