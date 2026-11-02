import { useCallback, useEffect, useState } from 'react'
import { createOperationalSchedule, getOperationalSchedule } from '../services/operational-schedule.service'
import type { CreateOperationalScheduleRequest, OperationalSchedule } from '../types/operational-schedule.types'

export function useOperationalSchedule(branchId: number) {
  const [schedule, setSchedule] = useState<OperationalSchedule | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setIsLoading(true)
    try {
      setSchedule(await getOperationalSchedule(branchId))
      setError(null)
    } catch {
      setError('We could not load this branch configuration.')
    } finally {
      setIsLoading(false)
    }
  }, [branchId])

  useEffect(() => { void load() }, [load])

  const save = useCallback(async (request: CreateOperationalScheduleRequest) => {
    const scheduleId = await createOperationalSchedule(branchId, request)
    setSchedule({ ...request, id: scheduleId, branchId, isActive: true })
    return scheduleId
  }, [branchId])

  return { schedule, isLoading, error, save }
}
