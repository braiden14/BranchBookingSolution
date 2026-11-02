import { useCallback, useEffect, useState } from 'react'
import { createBranch, getBranches } from '../services/branches.service'
import type { Branch, CreateBranchRequest } from '../types/branch.types'

export function useBranches() {
  const [branches, setBranches] = useState<Branch[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      setBranches(await getBranches())
      setError(null)
    } catch {
      setError('We could not load your branches. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => { void refresh() }, [refresh])

  const addBranch = useCallback(async (request: CreateBranchRequest) => {
    const branchId = await createBranch(request)
    setBranches((current) => [...current, {
      ...request,
      branchId,
      status: 'Inactive',
      acceptsBookings: false,
      createdAtUtc: new Date().toISOString(),
    }])
    return branchId
  }, [])

  return { branches, isLoading, error, refresh, addBranch }
}
