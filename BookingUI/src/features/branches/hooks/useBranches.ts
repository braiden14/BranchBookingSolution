import { useCallback, useEffect, useState } from 'react'
import { createBranch, deactivateBranch, getBranches, updateBranch } from '../services/branches.service'
import type { Branch, CreateBranchRequest, UpdateBranchRequest } from '../types/branch.types'

export function useBranches() {
  const [branches, setBranches] = useState<Branch[]>([])
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      const page = await getBranches(pageNumber, pageSize)
      setBranches(page.items)
      setTotalCount(page.totalCount)
      setTotalPages(page.totalPages)
      setError(null)
    } catch {
      setError('We could not load your branches. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }, [pageNumber, pageSize])

  useEffect(() => { void refresh() }, [refresh])

  const addBranch = useCallback(async (request: CreateBranchRequest) => {
    const branchId = await createBranch(request)
    await refresh()
    return branchId
  }, [refresh])

  const editBranch = useCallback(async (branchId: number, request: UpdateBranchRequest) => {
    await updateBranch(branchId, request)
    await refresh()
  }, [refresh])

  const disableBranch = useCallback(async (branchId: number) => {
    await deactivateBranch(branchId)
    await refresh()
  }, [refresh])

  const goToPage = useCallback((nextPage: number) => {
    setPageNumber(Math.min(Math.max(nextPage, 1), Math.max(totalPages, 1)))
  }, [totalPages])

  return { branches, pageNumber, pageSize, totalCount, totalPages, isLoading, error, refresh, addBranch, editBranch, disableBranch, goToPage }
}
