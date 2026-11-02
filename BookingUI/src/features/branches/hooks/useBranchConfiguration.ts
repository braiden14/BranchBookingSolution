import { useCallback, useEffect, useState } from 'react'
import {
  createDateOverride,
  deleteCapacityOverride,
  deleteDateOverride,
  getBranchServices,
  getCapacityOverrides,
  getDateOverrides,
  getOfferedServices,
  updateDateOverride,
  upsertBranchService,
  upsertCapacityOverride,
} from '../services/branch-configuration.service'
import type {
  CapacityOverride,
  CapacityOverrideRequest,
  BranchService,
  DateOverride,
  DateOverrideRequest,
  OfferedService,
  UpsertBranchServiceRequest,
} from '../types/branch-configuration.types'

export function useBranchConfiguration(branchId: number) {
  const [offeredServices, setOfferedServices] = useState<OfferedService[]>([])
  const [branchServices, setBranchServices] = useState<BranchService[]>([])
  const [dateOverrides, setDateOverrides] = useState<DateOverride[]>([])
  const [capacityOverrides, setCapacityOverrides] = useState<CapacityOverride[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setIsLoading(true)
    try {
      const [available, configured, dates, capacities] = await Promise.all([
        getOfferedServices(),
        getBranchServices(branchId),
        getDateOverrides(branchId),
        getCapacityOverrides(branchId),
      ])
      setOfferedServices(available)
      setBranchServices(configured)
      setDateOverrides(dates)
      setCapacityOverrides(capacities)
      setError(null)
    } catch {
      setError('We could not load this branch configuration.')
    } finally {
      setIsLoading(false)
    }
  }, [branchId])

  useEffect(() => { void load() }, [load])

  const saveBranchService = useCallback(async (request: UpsertBranchServiceRequest) => {
    await upsertBranchService(branchId, request)
    setBranchServices((current) => {
      const existing = current.find((service) => service.serviceId === request.serviceId)
      if (!existing) {
        const offered = offeredServices.find((service) => service.id === request.serviceId)
        if (!offered) return current
        return [...current, {
          branchServiceId: 0,
          branchId,
          serviceId: offered.id,
          serviceCode: offered.code,
          serviceName: offered.name,
          serviceDurationMinutes: offered.serviceDurationMinutes,
          isActive: request.isActive,
          createdAtUtc: new Date().toISOString(),
        }]
      }
      return current.map((service) => service.serviceId === request.serviceId
        ? { ...service, isActive: request.isActive }
        : service)
    })
  }, [branchId, offeredServices])

  const addDateOverride = useCallback(async (request: DateOverrideRequest) => {
    const overrideId = await createDateOverride(branchId, request)
    await load()
    return overrideId
  }, [branchId, load])

  const editDateOverride = useCallback(async (
    overrideId: number,
    request: Omit<DateOverrideRequest, 'scheduleId'>,
  ) => {
    await updateDateOverride(branchId, overrideId, request)
    await load()
  }, [branchId, load])

  const removeDateOverride = useCallback(async (overrideId: number) => {
    await deleteDateOverride(branchId, overrideId)
    setDateOverrides((current) => current.filter((override) => override.overrideId !== overrideId))
  }, [branchId])

  const saveCapacityOverride = useCallback(async (request: CapacityOverrideRequest) => {
    await upsertCapacityOverride(branchId, request)
    await load()
  }, [branchId, load])

  const removeCapacityOverride = useCallback(async (overrideId: number) => {
    await deleteCapacityOverride(branchId, overrideId)
    setCapacityOverrides((current) => current.filter((override) => override.capacityOverrideId !== overrideId))
  }, [branchId])

  return {
    offeredServices,
    branchServices,
    dateOverrides,
    capacityOverrides,
    isLoading,
    error,
    reload: load,
    saveBranchService,
    addDateOverride,
    editDateOverride,
    removeDateOverride,
    saveCapacityOverride,
    removeCapacityOverride,
  }
}
