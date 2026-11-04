import { useCallback, useEffect, useState } from 'react'
import { getServices, createService, updateService, deleteService } from '../services.service'
import type { OfferedService, OfferedServiceRequest } from '../services.service'

export type ServiceAvailability = 'all' | 'available' | 'unavailable'

export function useServices() {
  const [services, setServices] = useState<OfferedService[]>([])
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchTerm, setSearchTerm] = useState('')
  const [availability, setAvailability] = useState<ServiceAvailability>('all')
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    setIsLoading(true)
    try {
      const page = await getServices(pageNumber, pageSize, searchTerm, availability)
      setServices(page.items)
      setTotalCount(page.totalCount)
      setTotalPages(page.totalPages)
      setError(null)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'We could not load offered services.')
    } finally {
      setIsLoading(false)
    }
  }, [availability, pageNumber, pageSize, searchTerm])

  useEffect(() => { void refresh() }, [refresh])

  const addService = useCallback(async (request: OfferedServiceRequest) => {
    await createService(request)
    await refresh()
  }, [refresh])

  const editService = useCallback(async (serviceId: number, request: OfferedServiceRequest) => {
    await updateService(serviceId, request)
    await refresh()
  }, [refresh])

  const removeService = useCallback(async (serviceId: number) => {
    await deleteService(serviceId)
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

  const updateAvailability = useCallback((nextAvailability: ServiceAvailability) => {
    setAvailability(nextAvailability)
    setPageNumber(1)
  }, [])

  return {
    services,
    pageNumber,
    pageSize,
    searchTerm,
    availability,
    totalCount,
    totalPages,
    isLoading,
    error,
    refresh,
    addService,
    editService,
    removeService,
    goToPage,
    updatePageSize,
    updateSearchTerm,
    updateAvailability,
  }
}
