export type OperationalAppointmentStatus = 'Confirmed' | 'Cancelled'

export interface OperationalAppointment {
  appointmentId: number
  customerId: number
  customerName: string
  customerEmail: string
  branchId: number
  branchName: string
  serviceId: number
  serviceName: string
  startAt: string
  endAt: string
  status: OperationalAppointmentStatus
}

export interface PagedOperationalAppointments {
  items: OperationalAppointment[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}
