import { apiClient } from '../../../lib/api-client'
import type { Appointment, AvailableDay, AvailableSlot, CustomerBranch, CustomerService } from '../types/appointment.types'

export const appointmentService = {
  getBranches: () => apiClient<CustomerBranch[]>('/api/customer/branches'),
  getServices: (branchId: number) => apiClient<CustomerService[]>(`/api/customer/branches/${branchId}/services`),
  getSlots: (branchId: number, serviceId: number, date: string, excludingAppointmentId?: number) => apiClient<AvailableSlot[]>(`/api/customer/branches/${branchId}/availability?serviceId=${serviceId}&date=${date}${excludingAppointmentId ? `&excludingAppointmentId=${excludingAppointmentId}` : ''}`),
  getWeekSlots: (branchId: number, serviceId: number, fromDate: string, toDate: string, excludingAppointmentId?: number) => apiClient<AvailableDay[]>(`/api/customer/branches/${branchId}/availability?serviceId=${serviceId}&fromDate=${fromDate}&toDate=${toDate}${excludingAppointmentId ? `&excludingAppointmentId=${excludingAppointmentId}` : ''}`),
  getAppointments: (history: boolean) => apiClient<Appointment[]>(`/api/customer/appointments?history=${history}`),
  create: (branchId: number, serviceId: number, startAt: string) => apiClient<{ appointmentId: number }>('/api/customer/appointments', { method: 'POST', body: JSON.stringify({ branchId, serviceId, startAt }) }),
  cancel: (appointmentId: number) => apiClient<void>(`/api/customer/appointments/${appointmentId}/cancel`, { method: 'POST' }),
  reschedule: (appointmentId: number, startAt: string) => apiClient<void>(`/api/customer/appointments/${appointmentId}/reschedule`, { method: 'POST', body: JSON.stringify({ startAt }) }),
}
