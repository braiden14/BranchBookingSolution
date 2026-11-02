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
    try {
      const branch = await createBranch(request)
      setBranches((current) => [...current, branch])
    } catch {
      setBranches((current) => [...current, {
        ...request,
        branchId: Date.now(),
        status: 'Inactive',
        acceptsBookings: false,
        createdAtUtc: new Date().toISOString(),
      }])
    }
  }, [])

  return { branches, isLoading, error, refresh, addBranch }
}
