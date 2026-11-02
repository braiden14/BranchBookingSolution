import { apiClient } from '../../../lib/api-client'
import type {
  CapacityOverride,
  CapacityOverrideRequest,
  BranchService,
  DateOverride,
  DateOverrideRequest,
  OfferedService,
  UpsertBranchServiceRequest,
} from '../types/branch-configuration.types'

export async function getOfferedServices(): Promise<OfferedService[]> {
  return apiClient<OfferedService[]>('/api/offered-services')
}

export async function getBranchServices(branchId: number): Promise<BranchService[]> {
  return apiClient<BranchService[]>(`/api/branches/${branchId}/services`)
}

export async function upsertBranchService(
  branchId: number,
  request: UpsertBranchServiceRequest,
): Promise<number> {
  const response = await apiClient<{ branchServiceId: number }>(`/api/branches/${branchId}/services`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return response.branchServiceId
}

export async function getDateOverrides(branchId: number): Promise<DateOverride[]> {
  return apiClient<DateOverride[]>(`/api/branches/${branchId}/date-overrides`)
}

export async function createDateOverride(
  branchId: number,
  request: DateOverrideRequest,
): Promise<number> {
  const response = await apiClient<{ overrideId: number }>(`/api/branches/${branchId}/date-overrides`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return response.overrideId
}

export async function updateDateOverride(
  branchId: number,
  overrideId: number,
  request: Omit<DateOverrideRequest, 'scheduleId'>,
): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/date-overrides/${overrideId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export async function deleteDateOverride(branchId: number, overrideId: number): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/date-overrides/${overrideId}`, {
    method: 'DELETE',
  })
}

export async function getCapacityOverrides(branchId: number): Promise<CapacityOverride[]> {
  return apiClient<CapacityOverride[]>(`/api/branches/${branchId}/capacity-overrides`)
}

export async function upsertCapacityOverride(
  branchId: number,
  request: CapacityOverrideRequest,
): Promise<number> {
  const response = await apiClient<{ overrideId: number }>(`/api/branches/${branchId}/capacity-overrides`, {
    method: 'POST',
    body: JSON.stringify(request),
  })

  return response.overrideId
}

export async function deleteCapacityOverride(branchId: number, overrideId: number): Promise<void> {
  await apiClient<void>(`/api/branches/${branchId}/capacity-overrides/${overrideId}`, {
    method: 'DELETE',
  })
}
