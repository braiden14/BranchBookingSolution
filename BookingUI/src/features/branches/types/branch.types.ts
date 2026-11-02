export type BranchStatus = 'Active' | 'Inactive'

export interface Branch {
  branchId: number
  code: string
  name: string
  addressLine1: string
  city: string
  province: string
  postalCode: string
  status: BranchStatus
  acceptsBookings: boolean
  createdAtUtc: string
}

export interface CreateBranchRequest {
  code: string
  name: string
  addressLine1: string
  city: string
  province: string
  postalCode: string
}
