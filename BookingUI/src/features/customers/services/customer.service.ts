import { apiClient } from '../../../lib/api-client'
import type { PagedCustomers } from '../types/customer.types'

export function getCustomers(pageNumber = 1, pageSize = 20) {
  return apiClient<PagedCustomers>(`/api/customers?pageNumber=${pageNumber}&pageSize=${pageSize}`)
}
