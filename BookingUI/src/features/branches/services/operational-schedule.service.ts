import { apiClient } from '../../../lib/api-client'
import type { CreateOperationalScheduleRequest, OperationalSchedule } from '../types/operational-schedule.types'

export async function getOperationalSchedule(branchId: number): Promise<OperationalSchedule | null> {
  try {
    return await apiClient<OperationalSchedule>(`/api/branches/${branchId}/operational-schedules`)
  } catch (error) {
    if (error instanceof Error && error.message === 'Request failed with status 404') return null
    throw error
  }
}

export async function createOperationalSchedule(branchId: number, request: CreateOperationalScheduleRequest): Promise<number> {
  const response = await apiClient<{ scheduleId: number }>(`/api/branches/${branchId}/operational-schedules`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
  return response.scheduleId
}
