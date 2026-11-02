import { apiClient } from '../../../lib/api-client'
import type { Branch, CreateBranchRequest, PagedBranches, UpdateBranchRequest } from '../types/branch.types'

export async function getBranches(pageNumber = 1, pageSize = 20): Promise<PagedBranches> {
  return await apiClient<PagedBranches>(`/api/branches?pageNumber=${pageNumber}&pageSize=${pageSize}`);
}

export async function createBranch(request: CreateBranchRequest): Promise<number> {
  const response = await apiClient<number>('/api/branches', { method: 'POST', body: JSON.stringify(request) })
  console.log('createBranch response:', response)

  return response;
}

export async function activateBranch(branchId: number): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/activate`, { method: 'POST' })
}

export async function updateBranch(branchId: number, request: UpdateBranchRequest): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}`, { method: 'PUT', body: JSON.stringify(request) })
}

export async function deactivateBranch(branchId: number): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/deactivate`, { method: 'POST' })
}
