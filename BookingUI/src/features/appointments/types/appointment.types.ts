export type AppointmentStatus = 'Confirmed' | 'Cancelled'

export interface CustomerBranch { branchId: number; code: string; name: string; city: string }
export interface CustomerService { serviceId: number; code: string; name: string; durationMinutes: number }
export interface AvailableSlot { startAt: string; endAt: string }
export interface Appointment { appointmentId: number; branchId: number; branchName: string; serviceId: number; serviceName: string; startAt: string; endAt: string; status: AppointmentStatus }
