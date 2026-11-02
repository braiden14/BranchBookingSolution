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

export type UpdateBranchRequest = CreateBranchRequest

export interface PagedBranches {
  items: Branch[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}
