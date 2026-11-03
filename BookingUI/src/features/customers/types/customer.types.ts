export interface Customer {
  customerId: number
  firstName: string
  lastName: string
  email: string
  phoneNumber: string | null
  status: number
  createdAtUtc: string
}

export interface PagedCustomers {
  items: Customer[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
}
