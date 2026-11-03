import { useCallback, useEffect, useState } from 'react'
import { createBranch, deactivateBranch, getBranches, updateBranch } from '../services/branches.service'
import type { Branch, CreateBranchRequest, UpdateBranchRequest } from '../types/branch.types'

export function useBranches() {
  const [branches, setBranches] = useState<Branch[]>([])
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(5)
  const [searchTerm, setSearchTerm] = useState('')
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      const page = await getBranches(pageNumber, pageSize, searchTerm)
      setBranches(page.items)
      setTotalCount(page.totalCount)
      setTotalPages(page.totalPages)
      setError(null)
    } catch {
      setError('We could not load your branches. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }, [pageNumber, pageSize, searchTerm])

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

  const updatePageSize = useCallback((nextPageSize: number) => {
    setPageSize(nextPageSize)
    setPageNumber(1)
  }, [])

  const updateSearchTerm = useCallback((nextSearchTerm: string) => {
    setSearchTerm(nextSearchTerm)
    setPageNumber(1)
  }, [])

  return { branches, pageNumber, pageSize, searchTerm, totalCount, totalPages, isLoading, error, refresh, addBranch, editBranch, disableBranch, goToPage, updatePageSize, updateSearchTerm }
}
