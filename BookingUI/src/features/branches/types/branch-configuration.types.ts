export interface OfferedService {
  id: number
  code: string
  name: string
  serviceDurationMinutes: number
  createdAtUtc: string
}

export interface BranchService {
  branchServiceId: number
  branchId: number
  serviceId: number
  serviceCode: string
  serviceName: string
  serviceDurationMinutes: number
  isActive: boolean
  createdAtUtc: string
}

export interface DateOverride {
  overrideId: number
  scheduleId: number
  branchId: number
  overrideDate: string
  reason: string | null
  isClosed: boolean
  dateHoursId: number | null
  opensAt: string | null
  closesAt: string | null
  capacity: number | null
}

export interface CapacityOverride {
  capacityOverrideId: number
  branchId: number
  overrideDate: string
  capacity: number
}

export interface UpsertBranchServiceRequest {
  serviceId: number
  isActive: boolean
}

export interface DateOverrideRequest {
  scheduleId: number
  overrideDate: string
  reason: string | null
  isClosed: boolean
  opensAt: string | null
  closesAt: string | null
  capacity: number | null
}

export interface CapacityOverrideRequest {
  overrideDate: string
  capacity: number
}
