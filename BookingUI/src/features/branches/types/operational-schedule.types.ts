export interface WeeklyOperatingHours {
  weeklyHoursId?: number
  scheduleId?: number
  dayOfWeek: number
  opensAt: string | null
  closesAt: string | null
  isClosed: boolean
  capacity: number | null
}

export interface CreateOperationalScheduleRequest {
  effectiveFrom: string
  effectiveTo: string | null
  weeklyHours: WeeklyOperatingHours[]
}

export interface OperationalSchedule extends CreateOperationalScheduleRequest {
  id: number
  branchId: number
  isActive: boolean
}
