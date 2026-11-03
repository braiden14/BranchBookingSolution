export type StaffRole = 'Admin' | 'Staff' | 'BranchManager'
export type StaffStatus = 0 | 1 | 2 | 3 | 4

export interface StaffMember {
  userId: number
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  status: StaffStatus
  roles: string[]
  branchId: number | null
  branchName: string | null
  createdAtUtc: string
  lastLoginAtUtc: string | null
  invitationExpiresAtUtc: string | null
}

export interface PagedStaff {
  items: StaffMember[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface InviteStaffRequest {
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  roleCode: StaffRole
  branchId: number | null
}
