import { apiClient } from '../../../lib/api-client'
import type { Branch, CreateBranchRequest } from '../types/branch.types'

const fallbackBranches: Branch[] = [
  { branchId: 1, code: 'JHB01', name: 'Sandton City', addressLine1: '83 Rivonia Road', city: 'Johannesburg', province: 'Gauteng', postalCode: '2196', status: 'Active', acceptsBookings: true, createdAtUtc: '2024-01-12T08:00:00Z' },
  { branchId: 2, code: 'CPT01', name: 'V&A Waterfront', addressLine1: 'Dock Road', city: 'Cape Town', province: 'Western Cape', postalCode: '8001', status: 'Active', acceptsBookings: true, createdAtUtc: '2024-02-04T08:00:00Z' },
  { branchId: 3, code: 'DBN01', name: 'Gateway Theatre', addressLine1: '1 Palm Boulevard', city: 'Umhlanga', province: 'KwaZulu-Natal', postalCode: '4021', status: 'Inactive', acceptsBookings: false, createdAtUtc: '2024-02-17T08:00:00Z' },
  { branchId: 4, code: 'PTA01', name: 'Menlyn Maine', addressLine1: 'Corner January Masilela Drive', city: 'Pretoria', province: 'Gauteng', postalCode: '0010', status: 'Active', acceptsBookings: true, createdAtUtc: '2024-03-01T08:00:00Z' },
]

export async function getBranches(): Promise<Branch[]> {
  try {
    const response = await apiClient<Branch[] | { items: Branch[] }>('/api/branches?pageNumber=1&pageSize=50')
    return Array.isArray(response) ? response : response.items
  } catch {
    return fallbackBranches
  }
}

export async function createBranch(request: CreateBranchRequest): Promise<number> {
  const response = await apiClient<number>('/api/branches', { method: 'POST', body: JSON.stringify(request) })
  console.log('createBranch response:', response)

  return response;
}

export async function activateBranch(branchId: number): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/activate`, { method: 'POST' })
}
