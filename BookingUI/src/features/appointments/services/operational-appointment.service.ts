import { apiClient } from '../../../lib/api-client'
import type { PagedOperationalAppointments } from '../types/operational-appointment.types'

export function getOperationalAppointments(history: boolean, pageNumber = 1, pageSize = 20) {
  return apiClient<PagedOperationalAppointments>(`/api/appointments?history=${history}&pageNumber=${pageNumber}&pageSize=${pageSize}`)
}
