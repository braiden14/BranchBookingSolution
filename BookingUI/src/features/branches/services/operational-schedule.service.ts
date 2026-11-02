import { apiClient } from '../../../lib/api-client'
import type { CreateOperationalScheduleRequest, OperationalSchedule } from '../types/operational-schedule.types'

export async function getOperationalSchedule(branchId: number): Promise<OperationalSchedule | null> {
  const response = await fetch(`/api/branches/${branchId}/operational-schedules`)
  if (response.status === 404) return null
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`)
  return response.json() as Promise<OperationalSchedule>
}

export async function createOperationalSchedule(branchId: number, request: CreateOperationalScheduleRequest): Promise<number> {
  const response = await apiClient<{ scheduleId: number }>(`/api/branches/${branchId}/operational-schedules`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
  return response.scheduleId
}
